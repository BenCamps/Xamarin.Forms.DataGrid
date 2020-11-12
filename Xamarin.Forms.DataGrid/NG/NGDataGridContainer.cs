using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
					FreezeRows();
					ReleaseItems();
					itemsSource = value;
					BuildItems(itemsSource);
					UpdateContainerSelectionMode();
					//if (doneFirstLayout)
					//{
					ResetLastView();
					LayoutRows();
					//}
					UnfreezeRows();
					InvalidateMeasure();
				}
			}
		}

		#endregion


		internal NGDataGridContainer(NGDataGrid dg, NGDataGridScroller scroller)
		{
			DataGrid = dg;
			Scroller = scroller;
			DataGrid.PropertyChanged += DataGridOnPropertyChanged;
			Scroller.Scrolled += OnScrolled;


			GestureRecognizers.Add(new TapGestureRecognizer()
			{
				NumberOfTapsRequired = 2,
				TappedCallback = (v, o) =>
{
	Scroller.ScrollToAsync(0, Scroller.ScrollY + 1000, false);
	DataGrid.GridLineWidth += 1;
	DataGrid.HeaderGridLinesVisible = !DataGrid.HeaderGridLinesVisible;
	DataGrid.GridLinesVisibility = (GridLineVisibility)Enum.ToObject(typeof(GridLineVisibility), (int)(DataGrid.GridLinesVisibility + 1) % 4);

}
			});
		}

		private void DataGridOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var propertyName = e.PropertyName;

			if (propertyName == nameof(DataGrid.SelectedItem)
				|| propertyName == nameof(DataGrid.SelectedItems))
			{
				UpdateContainerSelection();
			}
			else if (propertyName == nameof(DataGrid.SelectionMode)
					 || propertyName == nameof(DataGrid.SelectionColor))
			{
				UpdateContainerSelectionMode();
			}
			else if (propertyName == nameof(DataGrid.GridLinesVisibility)
					 || propertyName == nameof(DataGrid.GridLineWidth))
			{
				UpdateRowsGridLines();
			}
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
			//if (!doneFirstLayout && height > 0 && (Items?.Count ?? 0) > 0)
			{
				if (Device.RuntimePlatform == Device.macOS)
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						await Task.Delay(100);
						LayoutRows();
						doneFirstLayout = true;
					});
				}
				else
				{
					LayoutRows();
					doneFirstLayout = true;
				}
			}
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			return new SizeRequest(new Size(DataGrid.ComputedColumnsWidth, Items?.LastOrDefault()?.End ?? 0));
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			//shortcut default Layout
			//base.OnSizeAllocated(width, height);
			LayoutChildren(0, 0, width, height);
		}

		#endregion


		#region Scrolling

		private double _lastViewStart = 0;
		private double _lastViewEnd = 0;

		void ResetLastView()
		{
			_lastViewStart = 0;
			_lastViewEnd = 0;
		}

		private void OnScrolled(object sender, ScrolledEventArgs e)
		{
			//Device.BeginInvokeOnMainThread(LayoutRows);
			LayoutRows();
		}


		private void LayoutRows()
		{

			var sw = new Stopwatch();
			sw.Start();
			// Debug.WriteLine($"Container.Scrolled {e.ScrollX},{e.ScrollY} X6");

			if (Items == null || Items.Count == 0)
				return; //nothing to show

			//don't process items for negative scroll bounce
			var scrollY = Scroller.ScrollY;
			var rowHeight = DataGrid.RowHeight;

			var viewStart = _lastViewStart;
			var viewEnd = _lastViewEnd;

			var windowHeight = Scroller.Height;
			var windowStart = scrollY;
			var windowEnd = windowStart + windowHeight;

			var itemsCount = Items.Count;

			var isForward = windowStart >= viewStart;


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

			if (Math.Abs(clearStart - clearEnd) > 0.0001)
			{
				if (Device.RuntimePlatform != Device.macOS)
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						//delay 2 frames
						await Task.Delay(16 * 2);
						ClearItems();
					});
				}
				else
					//				Device.BeginInvokeOnMainThread(() => Device.BeginInvokeOnMainThread(() => Device.BeginInvokeOnMainThread(ClearItems)));
					ClearItems();

				//LOCAL FUNCTION
				void ClearItems()
				{
					for (var i = GetItemIndexAt(clearStart); i < itemsCount; i++)
					{
						var info = Items[i];
						var newScrollY = Scroller.ScrollY;

						if (isForward)
						{
							if (info.End <= clearEnd)
							{
								//protect against quick successive scrolls up and down 
								if (info.End < newScrollY || info.Start >= newScrollY + windowHeight)
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
								if (info.End < newScrollY || info.Start >= newScrollY + windowHeight)
									DetachRow(info);
							}
						}
					}
				}
			}

			if (Math.Abs(showStart - showEnd) > 0.0001)
			{
				// var doneAttach = false;
				for (var i = GetItemIndexAt(showStart); i < itemsCount; i++)
				{
					var info = Items[i];
					if (info.Start <= showEnd)
					{
						AttacheRow(info);
						// doneAttach = true;
					}
					else
					{
						break;
					}
				}

				// if (doneAttach)
				// 	Debug.WriteLine($"Scroll Attach: Rows {Children.Count - cachedRows.Count} Cache {cachedRows.Count} Ellapsed {sw.ElapsedMilliseconds}ms Clear {clearStart}-{clearEnd} Show {showStart}-{showEnd} Distance {windowStart - viewStart}");
			}


			//set the last view
			_lastViewStart = windowStart;
			_lastViewEnd = windowEnd;

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
			if (Items != null)
			{
				foreach (var info in Items)
				{
					if (info.Item == item)
						return info;
				}
			}

			return null;
		}

		#endregion



		#region Rows

		private readonly Queue<NGDataGridViewRow> cachedRows = new Queue<NGDataGridViewRow>();
		private readonly Queue<NGDataGridViewGroup> cachedGroups = new Queue<NGDataGridViewGroup>();

		void CreateCachedRow()
		{
			var row = new NGDataGridViewRow(DataGrid);

			row.Opacity = 0;
			// row.IsVisible = false;

			cachedRows.Enqueue(row);
			
			InternalChildren.Add(row);
			//layout after adding so it can access the DataGrid?
			row.Layout(new Rectangle(0, 0, DataGrid.ComputedColumnsWidth, DataGrid.RowHeight));
		}

		void CreateCachedGroup()
		{
			var row = new NGDataGridViewGroup(DataGrid);

			row.Opacity = 0;
			// row.IsVisible = false;

			cachedGroups.Enqueue(row);

			InternalChildren.Add(row);
			row.Layout(new Rectangle(0, 0, DataGrid.ComputedColumnsWidth, DataGrid.RowHeight));
		}

		void Create2CachedRows()
		{
			CreateCachedRow();
			CreateCachedRow();
		}


		void AttacheRow(ItemInfo info)
		{
			if (info.View != null && info.View.ItemInfo == info)
				return;

			NGDataGridViewItem row = info.View;
			
			if (row == null)
			{
				if (info is ItemGroup)
				{
					if (cachedGroups.Count == 0)
						CreateCachedGroup();
					
					row = cachedGroups.Dequeue();
				}
				else
				{
					if (cachedRows.Count == 0)
						Create2CachedRows();
				
					row = cachedRows.Dequeue();
				}
				
				info.View = row;
			}

			row.BatchBegin();

			row.ItemInfo = info;
			// row.TranslationY = info.Y;
			row.SetPosition(0, info.Y);

			// row.IsVisible = true;
			row.Opacity = 1;
			row.BatchCommit();
		}


		void DetachRow(ItemInfo info, bool unbind = false)
		{
			if (info.View == null)
				return;

			var row = info.View;

			// row.BatchBegin();
			row.Opacity = 0;
			// row.IsVisible = false;

			if (unbind)
				row.ItemInfo = null;
			// row.BatchCommit();

			info.View = null;
			
			if (info is ItemGroup)
				cachedGroups.Enqueue((NGDataGridViewGroup)row);
			else
				cachedRows.Enqueue((NGDataGridViewRow)row);
		}


		void BuildItems(IEnumerable items)
		{
			var result = new List<ItemInfo>();
			var h = DataGrid.RowHeight;
			var y = 0;
			var i = 0;

			foreach (var item in items)
			{
				if (item is ObjectGroup group)
				{
					var groupInfo = new ItemGroup();

					groupInfo.Items = new List<ItemInfo>();
					groupInfo.Key = group.Key;
					groupInfo.Text = group.Text;
					groupInfo.Expanded = true;
					BuildItemInfo(group, groupInfo);

					foreach (var groupItem in group.Items)
					{
						groupInfo.Items.Add(BuildItemInfo(groupItem, new ItemInfo()));
					}

				}
				else
				{
					BuildItemInfo(item, new ItemInfo());
				}
			}


			ItemInfo BuildItemInfo(object item, ItemInfo info)
			{
				info.Item = item;
				info.Height = h;
				info.Y = y;
				info.Index = i;

				result.Add(info);

				y += h;
				i++;

				return info;
			}
			
			
			Items = result;
		}

		void ReleaseItems()
		{
			if (Items == null)
				return;

			foreach (var info in Items)
			{
				DetachRow(info, true);
			}
		}


		void FreezeRows()
		{
			foreach (View child in Children)
			{
				child.BatchBegin();
			}
		}

		void UnfreezeRows()
		{
			foreach (View child in Children)
			{
				child.BatchCommit();
			}
		}


		void WarmUpCache()
		{
			if (Width == -1 || Height == -1)
				return;

			//estimate double the items needed for 1 frame
			var n = (int)(Height / DataGrid.RowHeight * 2);

			if (Items != null && Items.Count <= n)
				return;

			//exclude items already in the cache
			n -= cachedRows.Count;

			while (n-- > 0)
				CreateCachedRow();
		}

		#endregion


		#region Selection

		internal void SelectRow(ItemInfo info)
		{
			if (info == null)
				return;

			switch (DataGrid.SelectionMode)
			{
				case SelectionMode.None:
					break;
				case SelectionMode.Single:
					{
						DataGrid.SetValueFromRenderer(NGDataGrid.SelectedItemProperty, info.Item);
					}
					break;
				case SelectionMode.Multiple:
					{
						//this is called when a row is tapped. 
						//if info.Selected == false, select the items
						//if true, deselect the item
						if (info.Selected)
							DataGrid.SelectedItems.Remove(info.Item);
						else
							DataGrid.SelectedItems.Add(info.Item);
					}
					break;
			}
		}

		void UpdateContainerSelection()
		{
			switch (DataGrid.SelectionMode)
			{
				case SelectionMode.None:
					break;
				case SelectionMode.Single:
					{
						ClearRowSelections();
						UpdateItemSelection(DataGrid.SelectedItem, true);
					}
					break;
				case SelectionMode.Multiple:
					{
						ClearRowSelections();
						foreach (var item in DataGrid.SelectedItems)
						{
							UpdateItemSelection(item, true);
						}
					}
					break;
			}
		}

		void UpdateContainerSelectionMode()
		{
			switch (DataGrid.SelectionMode)
			{
				case SelectionMode.None:
					ClearRowSelections();
					break;
				case SelectionMode.Single:
					break;
				case SelectionMode.Multiple:
					break;
			}

			UpdateContainerSelection();
		}

		void ClearRowSelections()
		{
			if (Items == null)
				return;

			foreach (var info in Items)
			{
				UpdateItemInfoSelection(info, false);
			}
		}

		void UpdateItemSelection(object item, bool selected)
		{
			UpdateItemInfoSelection(GetItemInfoFor(item), selected);
		}

		void UpdateItemInfoSelection(ItemInfo itemInfo, bool selected)
		{
			if (itemInfo == null || itemInfo.Selected == selected)
				return;

			itemInfo.Selected = selected;
			itemInfo.View?.InvalidateColors();
		}


		#endregion

		#region GridLines

		void UpdateRowsGridLines()
		{
			foreach (var element in Children)
			{
				if (element is NGDataGridViewRow row)
				{
					row.SetNeedsLayout();
					row.ForceLayout();
				}
			}
		}

		#endregion


		#region Grouping

		internal void ToggleGroup(ItemGroup group)
		{
			FreezeRows();

			var nextIndex = Items.IndexOf(group) + 1;

			if (group.Expanded)
			{
				group.Expanded = false;

				//hide the items
				foreach (var info in group.Items)
				{
					DetachRow(info);
				}

				Items.RemoveRange(nextIndex, group.Items.Count);
			}
			else
			{
				group.Expanded = true;

				Items.InsertRange(nextIndex, group.Items);
			}

			var y = group.End;
			for (var i = nextIndex; i < Items.Count; i++)
			{
				var info = Items[i];

				info.Index = i;
				info.Y = y;
				y += info.Height;

				if (info.View != null)
				{
					if (info.Y >= _lastViewEnd)
						DetachRow(info);
					else
						info.View.SetPosition(info.View.X, info.Y);
				}
			}


			ResetLastView();
			LayoutRows();

			InvalidateMeasure();

			UnfreezeRows();
		}


		#endregion


	}


	internal class ItemInfo
	{
		public double Y;
		public double Height;

		public bool Selected;

		public object Item;
		public int Index;
		public NGDataGridViewItem View;

		public double Start => Y;
		public double End => Y + Height; //todo: cache value
	}

	internal class ItemGroup : ItemInfo
	{
		public object Key;
		public string Text;
		public List<ItemInfo> Items;

		public bool Expanded;

	}

}