using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.DataGrid
{
	public partial class NGDataGrid
	{

		public static readonly BindableProperty HeaderHeightProperty =
			BindableProperty.Create(nameof(HeaderHeight), typeof(int), typeof(NGDataGrid), 40,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					self.HeaderView.HeightRequest = (int)n;
					self.InvalidateLayout();

				});

		public static readonly BindableProperty HeaderBackgroundProperty =
			BindableProperty.Create(nameof(HeaderBackground), typeof(Color), typeof(NGDataGrid), Color.White);

		public static readonly BindableProperty HeaderLabelStyleProperty =
			BindableProperty.Create(nameof(HeaderLabelStyle), typeof(Style), typeof(NGDataGrid));

		public static readonly BindableProperty HeaderFontSizeProperty =
			BindableProperty.Create(nameof(HeaderFontSize), typeof(double), typeof(NGDataGrid), 13.0);

		public static readonly BindableProperty HeaderFontFamilyProperty =
			BindableProperty.Create(nameof(HeaderFontFamily), typeof(string), typeof(NGDataGrid), Font.Default.FontFamily);

		public static readonly BindableProperty HeaderFontAttributesProperty =
			BindableProperty.Create(nameof(HeaderFontAttributes), typeof(FontAttributes), typeof(NGDataGrid), Font.Default.FontAttributes);

		
		
		public int HeaderHeight
		{
			get => (int)GetValue(HeaderHeightProperty);
			set => SetValue(HeaderHeightProperty, value);
		}

		public Color HeaderBackground
		{
			get => (Color)GetValue(HeaderBackgroundProperty);
			set => SetValue(HeaderBackgroundProperty, value);
		}

		[Obsolete("Please use HeaderLabelStyle", true)]
		public Color HeaderTextColor
		{
			get; set;
		}
		
		public Style HeaderLabelStyle
		{
			get => (Style)GetValue(HeaderLabelStyleProperty);
			set => SetValue(HeaderLabelStyleProperty, value);
		}

		public double HeaderFontSize
		{
			get => (double)GetValue(HeaderFontSizeProperty);
			set => SetValue(HeaderFontSizeProperty, value);
		}

		public string HeaderFontFamily
		{
			get => (string)GetValue(HeaderFontFamilyProperty);
			set => SetValue(HeaderFontFamilyProperty, value);
		}
		
		public FontAttributes HeaderFontAttributes
		{
			get => (FontAttributes)GetValue(HeaderFontAttributesProperty);
			set => SetValue(HeaderFontAttributesProperty, value);
		}
		
		

		
		
		
		
		
		internal double ComputedColumnsWidth = -1;

		internal void InvalidateColumnsWidth()
		{
			ComputedColumnsWidth = -1;
		}

		internal double GetComputedColumnWidth(int index)
		{
			if (index < Columns.Count)
				return Columns[index].ComputedWidth;

			return -1;
		}

		internal double GetComputedColumnStart(int index)
		{
			if (index < Columns.Count)
				return Columns[index].ComputedX;

			return -1;
		}



		private void ComputeColumnsWidth()
		{
			if (ComputedColumnsWidth > -1 || Columns.Count == 0 || Width <= 0)
				return;

			var totalWidth = 0d;
			var starColumns = 0;
			var starUnits = 0;
			var starMinWidth = 0d;

			foreach (var column in Columns)
			{
				var gl = column.Width;

				if (gl.IsAbsolute)
				{
					column.ComputedWidth = gl.Value;
					totalWidth += column.ComputedWidth;
				}
				//todo: for auto add text measure feature (platform and font specific, very slow)
				else if (gl.IsAuto || gl.IsStar)
				{
					starColumns++;
					starUnits += (int)gl.Value;
					starMinWidth += column.MinimumWidth;
				}
			}

			var remainingWidth = Width - totalWidth;

			//if we are out of remaining width, default columns to 100
			// if (remainingWidth < starMinWidth)
			//  remainingWidth = starColumns * 100;

			DataGridColumn prevColumn = null;
			var x = 0d;
			//distribute and compute x
			var i = 0;
			foreach (var column in Columns)
			{
				var gl = column.Width;

				if (!gl.IsAbsolute)
				{
					var w = Math.Max(column.MinimumWidth, Math.Floor((remainingWidth / starUnits) * gl.Value));
					column.ComputedWidth = w;
					totalWidth += column.ComputedWidth;
				}

				column.ColumnIndex = i++;
				column.ComputedX = x;
				x += column.ComputedWidth;
				column.HeaderLabel.WidthRequest = column.ComputedWidth;
			}

			ComputedColumnsWidth = totalWidth;
		}


		private void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateColumnsWidth();
			//CreateHeaderView();
		}


		private void CreateHeaderView()
		{
			if (ComputedColumnsWidth > -1)
				return;

			SetColumnsBindingContext();

			ComputeColumnsWidth();
			
			var hv = HeaderView as StackLayout;

			hv.DisableLayout = true;
			
			hv.Children.Clear();
			_sortingOrders.Clear();
			
			foreach (var col in Columns)
			{
				var cell = CreateHeaderViewForColumn(col);

				hv.Children.Add(cell);

				_sortingOrders.Add(Columns.IndexOf(col), SortingOrder.None);
			}

			hv.DisableLayout = false;
			
			hv.ForceLayout();
		}

		//todo:is this needed?
		private void SetColumnsBindingContext()
		{
			Columns?.ForEach(c => c.BindingContext = BindingContext);
		}


		private View CreateHeaderViewForColumn(DataGridColumn column)
		{
			// column.HeaderLabel.Style = column.HeaderLabelStyle ?? this.HeaderLabelStyle ?? (Style)HeaderView.Resources["HeaderDefaultStyle"];
			//
			// Grid grid = new Grid
			// {
			//  ColumnSpacing = 0,
			// };
			//
			// grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			// grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
			//
			// if (IsSortable)
			// {
			//  column.SortingIcon.Style = (Style)HeaderView.Resources["ImageStyleBase"];
			//
			//  grid.Children.Add(column.SortingIcon);
			//  Grid.SetColumn(column.SortingIcon, 1);
			//
			//  TapGestureRecognizer tgr = new TapGestureRecognizer();
			//  tgr.Tapped += (s, e) =>
			//  {
			//   int index = Columns.IndexOf(column);
			//   SortingOrder order = _sortingOrders[index] == SortingOrder.Ascendant ? SortingOrder.Descendant : SortingOrder.Ascendant;
			//
			//   if (Columns.ElementAt(index).SortingEnabled)
			//    SortedColumnIndex = new SortData(index, order);
			//  };
			//  grid.GestureRecognizers.Add(tgr);
			// }
			//
			// grid.Children.Add(column.HeaderLabel);
			//
			// return grid;

			if (column.HeaderCell != null)
			{
				column.HeaderCell.Column = column;
				return column.HeaderCell;
			}
			
			var cell = new NGDataGridViewCell();

			var label = column.HeaderLabel;
			
			label.SetBinding(BackgroundColorProperty, new Binding(nameof(HeaderBackground), BindingMode.OneWay, source: this));
			label.SetBinding(Label.FontFamilyProperty, new Binding(nameof(HeaderFontFamily), BindingMode.OneWay, source: this));
			label.SetBinding(Label.FontSizeProperty, new Binding(nameof(HeaderFontSize), BindingMode.OneWay, source: this));
			label.SetBinding(Label.FontAttributesProperty, new Binding(nameof(HeaderFontAttributes), BindingMode.OneWay, source: this));
			
			cell.Column = column;
			cell.Content = column.HeaderLabel;
			cell.WidthRequest = column.ComputedWidth;
			
			//add grid line
			BoxView vline = new BoxView();

			vline.SetBinding(WidthRequestProperty, new Binding(nameof(GridLineWidth), BindingMode.OneWay, source: this));
			vline.SetBinding(BackgroundColorProperty, new Binding(nameof(GridLineColor), BindingMode.OneWay, source: this));
			vline.HorizontalOptions = LayoutOptions.Start;

			cell.Children.Add(vline);

			column.HeaderCell = cell;

			
			// if (IsSortable)
			// {
				//column.SortingIcon.Style = (Style)_headerView.Resources["ImageStyleBase"];

				column.SortingIcon.HorizontalOptions = LayoutOptions.End;
				column.SortingIcon.WidthRequest = Math.Max(11, Math.Min(16, HeaderHeight));
				
				cell.Children.Add(column.SortingIcon);
				
				//Grid.SetColumn(column.SortingIcon, 1);

				var tgr = new TapGestureRecognizer();
				tgr.Tapped += (s, e) =>
				{
					if (!IsSortable)
						return;
					
					int index = column.ColumnIndex; //Columns.IndexOf(column);
					SortingOrder order = _sortingOrders[index] == SortingOrder.None ? SortingOrder.Ascendant 
						: _sortingOrders[index] == SortingOrder.Ascendant ? SortingOrder.Descendant 
						: SortingOrder.None;

					if (column.SortingEnabled)
						SortedColumnIndex = new SortData(index, order);
				};
				cell.Content.GestureRecognizers.Add(tgr);
			// }
			
			return cell;
		}


		private void UpdateHeaderGridLines()
		{
			foreach (var column in Columns)
			{
				var cell = column.HeaderCell;

				if (cell == null)
					continue;

				cell.WidthRequest = column.ComputedWidth;
				
				var gridLine = (View) cell.Children.FirstOrDefault((x) => x is BoxView);

				if (cell.Column.ColumnIndex > 0 && (GridLinesVisibility == GridLineVisibility.Both || GridLinesVisibility == GridLineVisibility.Vertical))
				{
					gridLine.IsVisible = true;
					cell.Content.Margin = new Thickness(GridLineWidth, 0,0,0);
				}
				else
				{
					gridLine.IsVisible = false;
					cell.Content.Margin = 0;
				}
			}
		}

	}
}