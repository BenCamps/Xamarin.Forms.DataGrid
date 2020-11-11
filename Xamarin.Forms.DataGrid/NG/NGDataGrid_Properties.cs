using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace Xamarin.Forms.DataGrid
{
	public partial class NGDataGrid
	{
		#region Bindable properties

		public static readonly BindableProperty RowsBackgroundColorPaletteProperty =
			BindableProperty.Create(nameof(RowsBackgroundColorPalette), typeof(IColorProvider), typeof(NGDataGrid), new PaletteCollection { default(Color) },
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					self.InvalidateInternalItems();
				});

		public static readonly BindableProperty RowsTextColorPaletteProperty =
			BindableProperty.Create(nameof(RowsTextColorPalette), typeof(IColorProvider), typeof(NGDataGrid), new PaletteCollection { Color.Black },
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					self.InvalidateInternalItems();
				});

		public static readonly BindableProperty ColumnsProperty =
			BindableProperty.Create(nameof(Columns), typeof(ObservableCollection<DataGridColumn>), typeof(NGDataGrid),
				validateValue: (b, v)
					=> v != null,
				propertyChanged: (b, o, n) =>
				{
					var dg = (NGDataGrid)b;
					if (o != null)
						((ObservableCollection<DataGridColumn>)o).CollectionChanged -= dg.OnColumnsChanged;
					if (n != null)
						((ObservableCollection<DataGridColumn>)n).CollectionChanged += dg.OnColumnsChanged;

					dg.InvalidateColumnsWidth();
				},
				defaultValueCreator: bindable =>
				{
					var col = new ObservableCollection<DataGridColumn>();
					col.CollectionChanged += ((NGDataGrid)bindable).OnColumnsChanged;
					return col;
				}

			);

		public static readonly BindableProperty RowHeightProperty =
			BindableProperty.Create(nameof(RowHeight), typeof(int), typeof(NGDataGrid), 40,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					//self._listView.RowHeight = (int)n;
					//todo:add RowHeight binding to NGDataGridViewRow
				});
		
		public static readonly BindableProperty FontSizeProperty =
			BindableProperty.Create(nameof(FontSize), typeof(double), typeof(NGDataGrid), 13.0);

		public static readonly BindableProperty FontFamilyProperty =
			BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(NGDataGrid), Font.Default.FontFamily);
		
		public static readonly BindableProperty RefreshCommandProperty =
			BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(NGDataGrid), null,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					self.RefreshView.IsEnabled = n != null;
				});

		public static readonly BindableProperty IsRefreshingProperty =
			BindableProperty.Create(nameof(IsRefreshing), typeof(bool), typeof(NGDataGrid), false, BindingMode.TwoWay);


		public static readonly BindableProperty NoDataViewProperty =
			BindableProperty.Create(nameof(NoDataView), typeof(View), typeof(NGDataGrid),
				propertyChanged: (b, o, n) =>
				{
					if (o != n)
						(b as NGDataGrid)._noDataView.Content = n as View;
				});
		#endregion

		#region Properties


		public IColorProvider RowsBackgroundColorPalette
		{
			get => (IColorProvider)GetValue(RowsBackgroundColorPaletteProperty);
			set => SetValue(RowsBackgroundColorPaletteProperty, value);
		}

		public IColorProvider RowsTextColorPalette
		{
			get => (IColorProvider)GetValue(RowsTextColorPaletteProperty);
			set => SetValue(RowsTextColorPaletteProperty, value);
		}

		public ObservableCollection<DataGridColumn> Columns
		{
			get => (ObservableCollection<DataGridColumn>)GetValue(ColumnsProperty);
			set => SetValue(ColumnsProperty, value);
		}

		public double FontSize
		{
			get => (double)GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}


		public string FontFamily
		{
			get => (string)GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		public int RowHeight
		{
			get => (int)GetValue(RowHeightProperty);
			set => SetValue(RowHeightProperty, value);
		}

		public ICommand RefreshCommand
		{
			get => (ICommand)GetValue(RefreshCommandProperty);
			set => SetValue(RefreshCommandProperty, value);
		}

		public bool IsRefreshing
		{
			get => (bool)GetValue(IsRefreshingProperty);
			set => SetValue(IsRefreshingProperty, value);
		}



		public View NoDataView
		{
			get => (View)GetValue(NoDataViewProperty);
			set => SetValue(NoDataViewProperty, value);
		}
		#endregion

	}
}