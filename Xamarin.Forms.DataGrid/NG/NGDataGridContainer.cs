using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.DataGrid
{
	internal class NGDataGridContainer : DeafLayout
	{
		#region Fields

		internal NGDataGridScroller Scroller;
		internal NGDataGrid DataGrid;
		private List<ItemInfo> Items;

		#endregion


		#region Properties

		private IEnumerable itemsSource;
		internal IEnumerable ItemsSource
		{
			get => itemsSource;
			set
			{
				if (itemsSource != value)
				{
					ReleaseItems();
					itemsSource = value;
					BuildItems(itemsSource);
					WarmUpCache();
					InvalidateMeasure();
				}
			}
		}

		#endregion


		internal NGDataGridContainer(NGDataGrid dg, NGDataGridScroller scroller)
		{
			DataGrid = dg;
			Scroller = scroller;
			Scroller.Scrolled += OnScrolled;


			GestureRecognizers.Add(new TapGestureRecognizer()
			{
				NumberOfTapsRequired = 2,
				TappedCallback = (v, o) =>
{
	Scroller.ScrollToAsync(0, Scroller.ScrollY + 1000, false);
}
			});
		}

		// protected override void OnPropertyChanged(string propertyName = null)
		// {
		//     base.OnPropertyChanged(propertyName);
		//
		//     if (propertyName == "Renderer")
		//     {
		//          ForceLayout();    
		//     }
		// }


		#region Layout

		private bool doneFirstLayout;
		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			if (!doneFirstLayout && height > 0 && (Items?.Count ?? 0) > 0)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					OnScrolled(Scroller, new ScrolledEventArgs(Scroller.ScrollX, Scroller.ScrollY));
					doneFirstLayout = true;
					//                InvalidateMeasure();
				});
			}
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			return new SizeRequest(new Size(DataGrid.ComputedColumnsWidth, Items?.LastOrDefault()?.End ?? 0));
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			//shortcut Layout
			//base.OnSizeAllocated(width, height);
			LayoutChildren(0, 0, width, height);
		}

		#endregion


		#region Scrolling

		private double lastViewStart = 0;
		private double lastViewEnd = 0;
		private int windowIndexStart = 0;
		private int windowIndexEnd = 0;

		private double extendedWindowStart = 0d;
		private double extendedWindowEnd = 0d;

		private int viewFirstItemIndex = 0;


		private void OnScrolled(object sender, ScrolledEventArgs e)
		{
			var sw = new Stopwatch();
			sw.Start();

			// Debug.WriteLine($"Container.Scrolled {e.ScrollX},{e.ScrollY} X6");

			if (Items == null || Items.Count == 0)
				return; //nothing to show

			//don't process items for negative scroll bounce
			var scrollY = e.ScrollY;
			var rowHeight = DataGrid.RowHeight;
			//			if (e.ScrollY < 0)
			//				scrollY = 0;

			var viewStart = lastViewStart;
			var viewEnd = lastViewEnd;//viewStart + viewHeight;
									  //var viewHeight = Scroller.Height;
			var windowHeight = Scroller.Height;
			var windowStart = scrollY;
			var windowEnd = windowStart + windowHeight;
			var itemsCount = Items.Count;
			//var lastItemIndex = itemsCount - 1;
			var isForward = windowStart >= viewStart; //viewStart >= lastViewStart;


			//Algo 4 -
			var clearStart = 0d;
			var clearEnd = 0d;
			var showStart = 0d;
			var showEnd = 0d;
			var padSize = windowHeight / 4;

			//extend show area by a portion of the view height in the the direction we're moving in
			if (isForward)
			{
				clearStart = viewStart;
				clearEnd = Math.Min(windowStart, viewEnd + padSize);
				showStart = Math.Max(viewEnd, windowStart);
				showEnd = windowEnd;

				clearStart -= rowHeight;
				showEnd += padSize;
			}
			else
			{
				clearStart = Math.Max(windowEnd, viewStart - padSize);
				clearEnd = viewEnd;
				showStart = windowStart;
				showEnd = Math.Min(viewStart, windowEnd);

				clearEnd += rowHeight;
				showStart -= padSize;
			}

			if (clearStart != clearEnd)
			{
				if (Device.RuntimePlatform != Device.macOS)
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						await Task.Delay(16 * 2);
						clearItems();
					});
				}
				else
					clearItems();

				void clearItems()
				{
					for (var i = GetItemIndexAt(clearStart); i < itemsCount; i++)
					{
						var info = Items[i];
						if (isForward)
						{
							if (info.End <= clearEnd)
							{
								//protect against quick successive scrolls up and down 
								if (info.End < e.ScrollY || info.Start >= e.ScrollY + windowHeight)
									DetachRow(info);
							}
							else
								break;
						}
						else
						{
							if (info.Start >= clearEnd)
								break;

							if (info.Start >= clearStart)
							{
								//protect against quick successive scrolls up and down 
								if (info.End < e.ScrollY || info.Start >= e.ScrollY + windowHeight)
									DetachRow(info);
							}
						}
					}
				}
			}

			if (showStart != showEnd)
			{
				var doneAttach = false;
				for (var i = GetItemIndexAt(showStart); i < itemsCount; i++)
				{
					var info = Items[i];
					if (info.Start <= showEnd)
					{
						AttacheRow(info);
						doneAttach = true;
					}
					else
					{
						break;
					}
				}

				if (doneAttach)
					Debug.WriteLine($"Scroll Attach: Rows {Children.Count - cachedRows.Count} Cache {cachedRows.Count} Ellapsed {sw.ElapsedMilliseconds}ms Clear {clearStart}-{clearEnd} Show {showStart}-{showEnd} Distance {windowStart - viewStart}");
			}


			//Algo 3
			/*
			var expandSize = DataGrid.RowHeight * 1;
			var expandedViewStart = viewStart - expandSize;
			var expandedViewEnd = viewEnd + expandSize;
			var expandedWindowStart = windowStart - expandSize;
			var expandedWindowEnd = windowEnd + expandSize;

			var localViewFirstIndex = viewFirstItemIndex;

			//hide items
			//Task.Run(async () =>
			Device.BeginInvokeOnMainThread(async () =>
			{
				await Task.Delay(16); //about five frames at 60fps
				for (var i = Math.Max(0, localViewFirstIndex - 2); i < itemsCount; i++)
				{
					var info = Items[i];

					//process all rows in the previous frame
					if (info.Start > expandedViewEnd)
						break;

					if (info.End < expandedWindowStart || info.Start > expandedWindowEnd)
						//defer detach, this increases cache pressure but prevents blanking of rows still visible
						//Device.BeginInvokeOnMainThread(() => DetachRow(info));
						DetachRow(info);
				}
			});


			//get first row index
			var windowFirstItemIndex = GetItemIndexAt(expandedWindowStart, viewFirstItemIndex, isForward);

			//show items
			for (var i = windowFirstItemIndex; i < itemsCount; i++)
			{
				var info = Items[i];
				if (info.End <= expandedWindowEnd)
					AttacheRow(info);
				else
					break;
			}

			viewFirstItemIndex = windowFirstItemIndex;
			*/


			/*
			 //Algo 2
			var shiftWindow = false;

			if (windowStart <= extendedWindowStart || windowEnd >= extendedWindowEnd)
			{
				var shiftSize = Math.Abs(windowStart - viewStart) * 1.5; //viewHeight/3;
				extendedWindowStart = Math.Max(windowStart - shiftSize, 0);
				extendedWindowEnd = Math.Min(windowEnd + shiftSize, this.Height);
				shiftWindow = true;
			}



			//horizontal only?
			if (viewStart == windowStart && doneFirstLayout)
				return;

			if (isForward)
			{
				//show items moved into view
				for (var i = windowIndexEnd; i < itemsCount; i++)
				{
					var info = Items[i];
					//skip rows that have moved past the top on a big scroll
					if (info.End < windowStart)
					{
						// DetachRow(info);
						continue;
					}
					//attach rows that became visible
					if (info.Start < windowEnd)
					{
						AttacheRow(info);
					}
					//stop because we reached the end of the window
					else
					{
						windowIndexEnd = Math.Max(0, i - 1);
						break;
					}
				}

				if (shiftWindow)
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						//prepare items in extended area
						for (var i = windowIndexEnd; i < itemsCount; i++)
						{
							var info = Items[i];
							//attach rows that became visible
							if (info.Start < extendedWindowEnd)
							{
								AttacheRow(info);
							}
							//stop because we reached the end of the window
							else
							{
								windowIndexEnd = Math.Max(0, i - 1);
								break;
							}
						}
						Debug.WriteLine($"Scroll UP Extended Rows {Children.Count - cachedRows.Count} Cached {cachedRows.Count} **");
						// });
						//
						//
						// Device.BeginInvokeOnMainThread(() =>
						// {
						//detach items moved out of view
						for (var i = windowIndexStart; i <= windowIndexEnd; i++)
						{
							var info = Items[i];
							//detach rows that became hidden
							if (info.End < extendedWindowStart) //viewStart)
							{
								DetachRow(info);
							}
							else
							{
								windowIndexStart = Math.Max(0, i - 1);
								break;
							}
						}

						Debug.WriteLine($"Scroll UP {windowIndexStart},{windowIndexEnd}");
					});
				}
			}
			else
			{
				//show items moved into view
				for (var i = windowIndexStart; i >= 0; i--)
				{
					var info = Items[i];
					//attach rows that became visible
					if (info.Start >= windowEnd)
					{
						// DetachRow(info);
						continue;
					}
					if (info.End >= windowStart)
					{
						AttacheRow(info);
					}
					else
					{
						windowIndexStart = Math.Min(lastItemIndex, i + 1);
						break;
					}
				}

				if (shiftWindow)
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						//prepare items in extended area
						for (var i = windowIndexStart; i >= 0; i--)
						{
							var info = Items[i];
							if (info.End >= extendedWindowStart)
							{
								AttacheRow(info);
							}
							else
							{
								windowIndexStart = Math.Min(lastItemIndex, i + 1);
								break;
							}
						}
						Debug.WriteLine($"Scroll DOWN Extended Rows {Children.Count - cachedRows.Count} Cached {cachedRows.Count} *");
						// });
						//
						//
						// Device.BeginInvokeOnMainThread(() =>
						// {
						//detach items moved out of view
						for (var i = windowIndexEnd; i >= windowIndexStart; i--)
						{
							var info = Items[i];
							//detach rows that became hidden
							if (info.Start >= extendedWindowEnd) //viewEnd)
							{
								DetachRow(info);
							}
							else
							{
								windowIndexEnd = Math.Min(lastItemIndex, i + 1);
								break;
							}
						}

						Debug.WriteLine($"Scroll DOWN {windowIndexStart},{windowIndexEnd}");
					});
				}
			}
			*/



			/*
			 //Algo 1
            foreach (var info in Items)
            {
                //hide rows outside view window
                if (info.End < windowStart || info.Start > windowEnd)
                {
                    DetachRow(info);
                    continue;
                }

                AttacheRow(info);
            }
            */

			//set the last y
			lastViewStart = windowStart;
			lastViewEnd = windowEnd;

			// Debug.WriteLine($"Scroll END Visible Rows {Children.Count - cachedRows.Count} Cache {cachedRows.Count} Ellapsed {sw.ElapsedMilliseconds}ms");
		}


		internal int GetItemIndexAt(double position, int startIndex = 0, bool directionForward = true)
		{
			var itemsCount = Items.Count;

			if (directionForward)
			{
				for (int i = startIndex; i < itemsCount; i++)
				{
					var info = Items[i];
					if (info.Start <= position && info.End > position)
					{
						return info.Index;
					}
				}
			}
			else
				for (int i = startIndex; i >= 0; i--)
				{
					var info = Items[i];
					if (info.Start <= position && info.End > position)
					{
						return info.Index;
					}
				}


			if (itemsCount == 0)
				return -1; // this will produce an error since this method should not be called with empty Items collection 

			//cap the result to either 0 or last item depending on direction
			if (position <= 0)
				return 0;

			var lastIndex = itemsCount - 1;
			if (position >= Items[lastIndex].End)
				return lastIndex;

			//satisfy compiler
			return -1;
		}

		internal ItemInfo GetItemInfoFor(object item)
		{
			foreach (var info in Items)
			{
				if (info.Item == item)
					return info;
			}

			return null;
		}

		#endregion



		#region Rows

		private readonly Queue<NGDataGridViewRow> cachedRows = new Queue<NGDataGridViewRow>();
		//private readonly ConcurrentQueue<NGDataGridViewRow> cachedRows = new ConcurrentQueue<NGDataGridViewRow>();

		void CreateCachedRow()
		{
			var row = new NGDataGridViewRow(DataGrid);

			//            row.TranslationX = -10000;
			row.Opacity = 0;
			// row.IsVisible = false;
			row.ItemInfo = null;

			cachedRows.Enqueue(row);

			// Device.BeginInvokeOnMainThread(() =>
			// {
			InternalChildren.Add(row);

			row.Layout(new Rectangle(0, 0, DataGrid.ComputedColumnsWidth, DataGrid.RowHeight));
			// });
		}

		void Create2CachedRows()
		{
			CreateCachedRow();
			CreateCachedRow();
		}

		void AttacheRow(ItemInfo info)
		{
			if (info.View != null)
				return;

			if (cachedRows.Count == 0)
				Create2CachedRows();

			var row = cachedRows.Dequeue();
			//cachedRows.TryDequeue(out var row);

			// if (row.Parent == null)
			// {
			//     InternalChildren.Add(row);
			//     row.Layout(new Rectangle(0, 0, DataGrid.ComputedColumnsWidth, DataGrid.RowHeight));
			// }


			info.View = row;

			//row.BatchBegin();
			row.TranslationY = info.Y;
			row.ItemInfo = info;
			// row.IsVisible = true;
			// row.TranslationX = 0;
			row.Opacity = 1;
			//row.BatchCommit();
		}


		void DetachRow(ItemInfo info)
		{
			if (info.View == null)
				return;

			var row = info.View;
			info.View = null;

			// row.BatchBegin();
			// row.TranslationX = -10000;
			row.Opacity = 0;
			// row.IsVisible = false;
			// row.ItemInfo = null;
			// row.BatchCommit();

			cachedRows.Enqueue(row);
		}


		void BuildItems(IEnumerable items)
		{
			var result = new List<ItemInfo>();
			var h = DataGrid.RowHeight;
			var y = 0;
			var i = 0;

			foreach (var item in items)
			{
				var info = new ItemInfo();

				info.Item = item;
				info.Height = h;
				info.Y = y;
				info.Index = i;

				result.Add(info);

				y += h;
				i++;
			}

			Items = result;
		}

		void ReleaseItems()
		{
			if (Items == null)
				return;

			foreach (var info in Items)
			{
				if (info.View != null)
					info.View.ItemInfo = null;

				DetachRow(info);
			}
		}


		void WarmUpCache()
		{
			if (cachedRows.Count > 0)
				return;

			//attache the first item so we can get an initial layout call
			var n = (int)(Device.Info.ScaledScreenSize.Height / DataGrid.RowHeight * 1.5) - cachedRows.Count;

			// Task.Run(() =>
			// {
			while (n-- > 0)
				CreateCachedRow();
			// });

			//
			// if (Height > 0)
			// {
			//     var count = ((Scroller.Height / DataGrid.RowHeight * 2) - cachedRows.Count) / 4;
			//
			//     for (var l = 0; l < 4; l++)
			//     {
			//         Device.BeginInvokeOnMainThread(async () =>
			//         {
			//             for (var j = 0; j < count; j++)
			//             {
			//                 CreateCachedRow();
			//                 await Task.Delay(10);
			//             }
			//
			//         });
			//     }
			// }
		}

		#endregion



	}


	internal class ItemInfo
	{
		internal double Y;
		internal double Height;

		internal bool Selected;

		internal object Item;
		internal int Index;
		internal NGDataGridViewRow View;

		internal double Start => Y;
		internal double End => Y + Height; //todo: cache value
	}


}