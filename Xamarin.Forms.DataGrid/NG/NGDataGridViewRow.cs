using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms.Shapes;

namespace Xamarin.Forms.DataGrid
{
	internal sealed class NGDataGridViewRow : DeafLayout
	{
		#region Fields
		
		Color _bgColor;
		Color _textColor;
		
		BoxView HorizontalGridLineView;

		#endregion


		public NGDataGridViewRow(NGDataGrid dg)
		{
			//empty
			BackgroundColor = Color.PaleGreen;
			DataGrid = dg;

			//handle row selection
			GestureRecognizers.Add(new TapGestureRecognizer
			{
				NumberOfTapsRequired = 1,
				TappedCallback = (v, o) => { RowTapped(); }
			});
			
			//Setup the grid line view
			HorizontalGridLineView = new BoxView();
			HorizontalGridLineView.SetBinding(BackgroundColorProperty, new Binding(nameof(DataGrid.GridLineColor), BindingMode.OneWay, source: DataGrid));
			HorizontalGridLineView.SetBinding(HeightRequestProperty, new Binding(nameof(DataGrid.GridLineWidth), BindingMode.OneWay, source: DataGrid));
			HorizontalGridLineView.VerticalOptions = LayoutOptions.Start;
			
			InternalChildren.Add(HorizontalGridLineView);
		}


		#region properties
		public NGDataGrid DataGrid
		{
			get => (NGDataGrid)GetValue(DataGridProperty);
			set => SetValue(DataGridProperty, value);
		}

		//private int _index = -1;

		// public int RowIndex
		// {
		// 	get
		// 	{
		// 		if (_index == -1 && ItemInfo != null)
		// 			_index = DataGrid.InternalItems?.IndexOf(RowContext) ?? -1;
		//
		// 		return _index;
		// 	}
		// 	
		// 	private set =>_index = value;
		// } 

		private int RowIndex => ItemInfo?.Index ?? -1;

		private bool IsItemSelected => ItemInfo?.Selected ?? false;

		internal ItemInfo ItemInfo
		{
			get => (ItemInfo)GetValue(ItemInfoProperty);
			set => SetValue(ItemInfoProperty, value);
		}

		// public object RowContext
		// {
		// 	get => GetValue(RowContextProperty);
		// 	set => SetValue(RowContextProperty, value);
		// }

		public Color RowBackgroundColor
		{
			get => (Color)GetValue(RowBackgroundColorProperty);
			set => SetValue(RowBackgroundColorProperty, value);
		}

		public Color RowBorderColor
		{
			get => (Color)GetValue(RowBorderColorProperty);
			set => SetValue(RowBorderColorProperty, value);
		}

		public Color RowForegroundColor
		{
			get => (Color)GetValue(RowForegroundColorProperty);
			set => SetValue(RowForegroundColorProperty, value);
		}


		#endregion

		#region Bindable Properties
		public static readonly BindableProperty DataGridProperty =
			BindableProperty.Create(nameof(DataGrid), typeof(NGDataGrid), typeof(NGDataGridViewRow), null,
				propertyChanged: (b, o, n) => ((NGDataGridViewRow)b).CreateView());

		// public static readonly BindableProperty RowContextProperty =
		// 	BindableProperty.Create(nameof(RowContext), typeof(object), typeof(NGDataGridViewRow)/*,
		// 		propertyChanged: (b, o, n) => ((NGDataGridViewRow)b).RowIndex = -1*/);

		public static readonly BindableProperty RowBackgroundColorProperty =
			BindableProperty.Create(nameof(RowBackgroundColor), typeof(Color), typeof(NGDataGridViewRow), Color.Transparent);

		public static readonly BindableProperty RowBorderColorProperty =
			BindableProperty.Create(nameof(RowBorderColor), typeof(Color), typeof(NGDataGridViewRow), Color.Transparent);

		public static readonly BindableProperty RowForegroundColorProperty =
			BindableProperty.Create(nameof(RowForegroundColor), typeof(Color), typeof(NGDataGridViewRow), Color.Transparent);


		public static readonly BindableProperty ItemInfoProperty =
			BindableProperty.Create(nameof(ItemInfo), typeof(ItemInfo), typeof(NGDataGridViewRow),
				propertyChanged: (b, o, n) => ((NGDataGridViewRow)b).BindingContext = (n as ItemInfo)?.Item);

		#endregion


		#region Layout

		private bool needsLayout;

		internal void SetNeedsLayout()
		{
			needsLayout = true;
		}


		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			Debug.WriteLine($"Row Layout {x},{y} {width},{height}");

			if (!needsLayout)
				return;

			needsLayout = false;

			var g = DataGrid;

			var cy = y;
			var ch = (double) g.RowHeight;
			
			var boxRect = new Rectangle(x, y, width, height);

			var gridLineVisibility = DataGrid.GridLinesVisibility;
			var showVerticalLines = gridLineVisibility == GridLineVisibility.Vertical || gridLineVisibility == GridLineVisibility.Both;
			var showHorizontalLines = gridLineVisibility == GridLineVisibility.Horizontal || gridLineVisibility == GridLineVisibility.Both;

			//show the grid line and adjust child layout area
			if (showHorizontalLines)
			{
				HorizontalGridLineView.IsVisible = true;
				cy += DataGrid.GridLineWidth;
				ch -= DataGrid.GridLineWidth;
			}
			else
			{
				HorizontalGridLineView.IsVisible = false;
			}
			
			foreach (View c in Children)
			{
				if (!c.IsVisible)
					continue;
				
				if (c is BoxView)
					LayoutChildIntoBoundingRegion(c, boxRect);
				else
				{
					var cellView = c as NGDataGridViewCell;
					var colIndex = cellView.Column.ColumnIndex;
					
					var cw = Math.Ceiling(g.GetComputedColumnWidth(colIndex));
					var cx = x + Math.Ceiling(g.GetComputedColumnStart(colIndex));
					var r = new Rectangle(cx, cy, cw, ch);

					var gridLine = cellView.Children[1] as View;
					//adjust for gridLine
					if (colIndex == 0 || !showVerticalLines)
					{
						gridLine.IsVisible = false;
						cellView.Content.Margin = 0;
					}
					else
					{
						gridLine.IsVisible = true;
						cellView.Content.Margin = new Thickness(gridLine.WidthRequest, 0,0,0);
					}
					
					if (c.Width != cw || c.Height != ch)
						c.Layout(r);
				}
			}
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			// shortcut the LayoutChildren call and ignore everything
			// done in Layout.OnSizeAllocated because it is just overhead for us.
			//base.OnSizeAllocated(width, height);

			LayoutChildren(0, 0, width, height);
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			//return base.OnMeasure(widthConstraint, heightConstraint);

			var sr = new SizeRequest(new Size(DataGrid.ComputedColumnsWidth, DataGrid.RowHeight));
			return sr;
		}

		#endregion


		#region Methods
		private void CreateView()
		{
			HeightRequest = DataGrid.RowHeight;

			var i = 0;
			foreach (var col in DataGrid.Columns)
			{
				var colIndex = i++;
				
				View content;

				if (col.CellTemplate != null)
				{
					content = col.CellTemplate.CreateContent() as View ?? new Label { Text = "Failed to create cell template." };

					content.VerticalOptions = LayoutOptions.Fill;
					content.HorizontalOptions = LayoutOptions.Fill;

					if (col.PropertyName != null)
					{
						content.SetBinding(BindingContextProperty,
							new Binding(col.PropertyName, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
					}
				}
				else
				{
					var text = new Label
					{
						HorizontalTextAlignment = col.HorizontalContentAlignment.ToTextAlignment(),
						VerticalTextAlignment = col.VerticalContentAlignment.ToTextAlignment(),
						LineBreakMode = LineBreakMode.WordWrap,
						Padding = 2
					};
					text.SetBinding(Label.TextProperty, new Binding(col.PropertyName, BindingMode.Default, stringFormat: col.StringFormat, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
					text.SetBinding(Label.FontSizeProperty, new Binding(NGDataGrid.FontSizeProperty.PropertyName, BindingMode.Default, source: DataGrid));
					text.SetBinding(Label.FontFamilyProperty, new Binding(NGDataGrid.FontFamilyProperty.PropertyName, BindingMode.Default, source: DataGrid));

					//bind text color
					text.SetBinding(Label.TextColorProperty, new Binding(nameof(RowForegroundColor), BindingMode.OneWay, source: this));

					content = text;
				}

				//bind content background as row background color
				// content.SetBinding(BackgroundColorProperty, new Binding(nameof(RowBackgroundColor), BindingMode.OneWay, source: this));
				content.BackgroundColor = RowBackgroundColor;

				var cell = CreateCellView();
				cell.Column = col;
				cell.Content = content;
				cell.IsFromTemplate = col.CellTemplate != null;
				
				InternalChildren.Add(cell);
			}

			//SetNeedsLayout();
			//InvalidateBackground();
		}

		private NGDataGridViewCell CreateCellView()
		{
			var cell = new NGDataGridViewCell();

			//add vertical grid line
			BoxView vline = new BoxView();

			vline.SetBinding(WidthRequestProperty, new Binding(nameof(DataGrid.GridLineWidth), BindingMode.OneWay, source: DataGrid));
			vline.SetBinding(BackgroundColorProperty, new Binding(nameof(DataGrid.GridLineColor), BindingMode.OneWay, source: DataGrid));
			vline.HorizontalOptions = LayoutOptions.Start;

			cell.Children.Add(vline);
			
			return cell;
		}
		
		

		//used to prevent multiple updates when setting RowContext and Index properties
		private bool updateNeeded;

		private void InvalidateBackground()
		{
			if (DataGrid == null || ItemInfo == null || updateNeeded)
				return;

			updateNeeded = true;

			//defer execution for 10ms until other properties and context have been updated.
			// Action a = async () =>
			// {
			// await Task.Delay(4); 
			UpdateBackgroundColor();
			// };
			// a.Invoke();
		}

		private void UpdateBackgroundColor()
		{
			if (!updateNeeded)
				return;

			if (RowIndex > -1)
			{
				RowBackgroundColor = IsItemSelected
					? DataGrid.SelectionColor
					: DataGrid.RowsBackgroundColorPalette.GetColor(RowIndex, BindingContext);

				RowForegroundColor = DataGrid.RowsTextColorPalette.GetColor(RowIndex, BindingContext);

				//				ChangeChildrenColors();

				//CellStyle

				var shouldQuery = DataGrid.ShouldQueryCellStyle();
				
				foreach (var child in Children)
				{
					if (child is NGDataGridViewCell cell)
					{
						var bg = RowBackgroundColor;
						var fg = RowForegroundColor;

						if (shouldQuery)
						{
							//todo: add lookup of cell value for style, or remove it if user can access it from RowData
							var style = DataGrid.NotifyQueryCellStyle(cell.Column, ItemInfo, null);
							if (style != null)
							{
								if(!style.BackgroundColor.IsDefault)
									bg = style.BackgroundColor;

								if(!style.ForegroundColor.IsDefault)
									fg = style.ForegroundColor;
							}
						}

						cell.Content.BackgroundColor = bg;

						if (!cell.IsFromTemplate)
						{
							var label = (Label) cell.Content;
							label.TextColor = fg;
						}
					}
				}


				updateNeeded = false;
			}
		}
		
		// private void ChangeChildrenColors()
		// {
		// 	foreach (var v in Children)
		// 	{
		// 		v.BackgroundColor = _bgColor;
		//
		// 		if (v is Label label)
		// 			label.TextColor = _textColor;
		// 		else if (v is ContentView contentView && contentView.Content is Label label2)
		// 			label2.TextColor = _textColor;
		// 	}
		// }

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			InvalidateBackground();
		}

		protected override void OnParentSet()
		{
			base.OnParentSet();

			// DataGrid = GetDataGridParent();
			// Container = GetContainerParent();

			if (Parent != null)
			{
				//				DataGrid.AddAttachedRow(this);
//				DataGrid.ItemSelected += DataGrid_ItemSelected;
			}
			else
			{
				//				DataGrid.RemoveAttachedRow(this);
//				DataGrid.ItemSelected -= DataGrid_ItemSelected;
			}

			SetNeedsLayout();
		}
		

		// private void DataGrid_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		// {
		// 	if (DataGrid.SelectionEnabled && (e.SelectedItem == BindingContext || IsItemSelected))
		// 	{
		// 		InvalidateBackground();
		// 	}
		// }
		#endregion

		#region Selection

		private void RowTapped()
		{
			DataGrid.Container.SelectRow(ItemInfo);
		}
		
		internal void UpdateSelection()
		{
			InvalidateBackground();
			UpdateBackgroundColor();
		}

		#endregion
		

		// private NGDataGrid GetDataGridParent()
		// {
		// 	Element p = this;
		//
		// 	while (p != null && !(p is NGDataGrid))
		// 		p = p.Parent;
		//
		// 	return (NGDataGrid)p;
		// }
		//
		// private NGDataGridContainer GetContainerParent()
		// {
		// 	Element p = this;
		//
		// 	while (p != null && !(p is NGDataGridContainer))
		// 		p = p.Parent;
		//
		// 	return (NGDataGridContainer)p;
		// }


		protected override void OnChildMeasureInvalidated()
		{
			base.OnChildMeasureInvalidated();
		}

		protected override void InvalidateLayout()
		{
			base.InvalidateLayout();
		}

		protected override void InvalidateMeasure()
		{
			base.InvalidateMeasure();
		}

	}



	// static class LayoutOptionsExtensions
	// {
	// 	public static TextAlignment ToTextAlignment(this LayoutOptions layoutOption)
	// 	{
	// 		switch (layoutOption.Alignment)
	// 		{
	// 			case LayoutAlignment.Fill:
	// 			case LayoutAlignment.Center:
	// 				return TextAlignment.Center;
	// 			
	// 			case LayoutAlignment.Start:
	// 				return TextAlignment.Start;
	//
	// 			case LayoutAlignment.End:
	// 				return TextAlignment.End;
	// 			
	// 			default:
	// 				return TextAlignment.Center;
	// 		}
	// 	}
	// }

}
