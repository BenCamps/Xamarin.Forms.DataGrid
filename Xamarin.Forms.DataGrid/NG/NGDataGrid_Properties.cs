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
		public static readonly BindableProperty ActiveRowColorProperty =
			BindableProperty.Create(nameof(ActiveRowColor), typeof(Color), typeof(NGDataGrid), Color.FromRgb(128, 144, 160));

		public static readonly BindableProperty HeaderBackgroundProperty =
			BindableProperty.Create(nameof(HeaderBackground), typeof(Color), typeof(NGDataGrid), Color.White,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					if (self.HeaderView != null && !self.HeaderBordersVisible)
						self.HeaderView.BackgroundColor = (Color)n;
				});

		public static readonly BindableProperty BorderColorProperty =
			BindableProperty.Create(nameof(BorderColor), typeof(Color), typeof(NGDataGrid), Color.Black,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					if (self.HeaderBordersVisible)
						self.HeaderView.BackgroundColor = (Color)n;

					if (self.Columns != null && self.ItemsSource != null)
						self.Reload();
				});

		public static readonly BindableProperty RowsBackgroundColorPaletteProperty =
			BindableProperty.Create(nameof(RowsBackgroundColorPalette), typeof(IColorProvider), typeof(NGDataGrid), new PaletteCollection { default(Color) },
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					if (self.Columns != null && self.ItemsSource != null)
						self.Reload();
				});

		public static readonly BindableProperty RowsTextColorPaletteProperty =
			BindableProperty.Create(nameof(RowsTextColorPalette), typeof(IColorProvider), typeof(NGDataGrid), new PaletteCollection { Color.Black },
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					if (self.Columns != null && self.ItemsSource != null)
						self.Reload();
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

		public static readonly BindableProperty ItemsSourceProperty =
			BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(NGDataGrid), null,
				propertyChanged: HandleItemsSourcePropertyChanged);


		public static readonly BindableProperty RowHeightProperty =
			BindableProperty.Create(nameof(RowHeight), typeof(int), typeof(NGDataGrid), 40,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					//self._listView.RowHeight = (int)n;
					//todo:add RowHeight binding to NGDataGridViewRow
				});


		public static readonly BindableProperty HeaderHeightProperty =
			BindableProperty.Create(nameof(HeaderHeight), typeof(int), typeof(NGDataGrid), 40,
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					self.HeaderView.HeightRequest = (int)n;

				});

		public static readonly BindableProperty IsSortableProperty =
			BindableProperty.Create(nameof(IsSortable), typeof(bool), typeof(NGDataGrid), true);

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


		public static readonly BindableProperty BorderThicknessProperty =
			BindableProperty.Create(nameof(BorderThickness), typeof(Thickness), typeof(NGDataGrid), new Thickness(1),
				propertyChanged: (b, o, n) =>
				{
					((NGDataGrid)b).OnBorderThicknessChanged(b, EventArgs.Empty);
				});

		public static readonly BindableProperty HeaderBordersVisibleProperty =
			BindableProperty.Create(nameof(HeaderBordersVisible), typeof(bool), typeof(NGDataGrid), true,
				propertyChanged: (b, o, n) => (b as NGDataGrid).HeaderView.BackgroundColor = (bool)n ? (b as NGDataGrid).BorderColor : (b as NGDataGrid).HeaderBackground);

		public static readonly BindableProperty SortedColumnIndexProperty =
			BindableProperty.Create(nameof(SortedColumnIndex), typeof(SortData), typeof(NGDataGrid), null, BindingMode.TwoWay,
				validateValue: (b, v) =>
				{
					var self = b as NGDataGrid;
					var sData = (SortData)v;

					return
						sData == null || //setted to null
						self.Columns == null || // Columns binded but not setted
						self.Columns.Count == 0 || //columns not setted yet
						(sData.Index < self.Columns.Count && self.Columns.ElementAt(sData.Index).SortingEnabled);
				},
				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					if (o != n)
						self.SortItems((SortData)n);
				});


		public static readonly BindableProperty HeaderLabelStyleProperty =
			BindableProperty.Create(nameof(HeaderLabelStyle), typeof(Style), typeof(NGDataGrid));

		public static readonly BindableProperty AscendingIconProperty =
			BindableProperty.Create(nameof(AscendingIcon), typeof(ImageSource), typeof(NGDataGrid), ImageSource.FromResource("Xamarin.Forms.DataGrid.up.png", typeof(NGDataGrid).Assembly));

		public static readonly BindableProperty DescendingIconProperty =
			BindableProperty.Create(nameof(DescendingIcon), typeof(ImageSource), typeof(NGDataGrid), ImageSource.FromResource("Xamarin.Forms.DataGrid.down.png", typeof(NGDataGrid).Assembly));

		public static readonly BindableProperty DescendingIconStyleProperty =
			BindableProperty.Create(nameof(DescendingIconStyle), typeof(Style), typeof(NGDataGrid), null,

				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					var style = (n as Style).Setters.FirstOrDefault(x => x.Property == Image.SourceProperty);
					if (style != null)
					{
						if (style.Value is string vs)
							self.DescendingIcon = ImageSource.FromFile(vs);
						else
							self.DescendingIcon = (ImageSource)style.Value;
					}
				});

		public static readonly BindableProperty AscendingIconStyleProperty =
			BindableProperty.Create(nameof(AscendingIconStyle), typeof(Style), typeof(NGDataGrid), null,
				coerceValue: (b, v) =>
				{
					var self = b as NGDataGrid;

					return v;
				},

				propertyChanged: (b, o, n) =>
				{
					var self = b as NGDataGrid;
					if ((n as Style).Setters.Any(x => x.Property == Image.SourceProperty))
					{
						var style = (n as Style).Setters.FirstOrDefault(x => x.Property == Image.SourceProperty);
						if (style != null)
						{
							if (style.Value is string vs)
								self.AscendingIcon = ImageSource.FromFile(vs);
							else
								self.AscendingIcon = (ImageSource)style.Value;
						}
					}
				});

		public static readonly BindableProperty NoDataViewProperty =
			BindableProperty.Create(nameof(NoDataView), typeof(View), typeof(NGDataGrid),
				propertyChanged: (b, o, n) =>
				{
					if (o != n)
						(b as NGDataGrid)._noDataView.Content = n as View;
				});
		#endregion

		#region Properties
		public Color ActiveRowColor
		{
			get => (Color)GetValue(ActiveRowColorProperty);
			set => SetValue(ActiveRowColorProperty, value);
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

		public Color BorderColor
		{
			get => (Color)GetValue(BorderColorProperty);
			set => SetValue(BorderColorProperty, value);
		}

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

		public IEnumerable ItemsSource
		{
			get => (IEnumerable)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
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

		public int HeaderHeight
		{
			get => (int)GetValue(HeaderHeightProperty);
			set => SetValue(HeaderHeightProperty, value);
		}

		public bool IsSortable
		{
			get => (bool)GetValue(IsSortableProperty);
			set => SetValue(IsSortableProperty, value);
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

		public Thickness BorderThickness
		{
			get => (Thickness)GetValue(BorderThicknessProperty);
			set => SetValue(BorderThicknessProperty, value);
		}

		public bool HeaderBordersVisible
		{
			get => (bool)GetValue(HeaderBordersVisibleProperty);
			set => SetValue(HeaderBordersVisibleProperty, value);
		}

		public SortData SortedColumnIndex
		{
			get => (SortData)GetValue(SortedColumnIndexProperty);
			set => SetValue(SortedColumnIndexProperty, value);
		}

		public Style HeaderLabelStyle
		{
			get => (Style)GetValue(HeaderLabelStyleProperty);
			set => SetValue(HeaderLabelStyleProperty, value);
		}

		public ImageSource AscendingIcon
		{
			get => (ImageSource)GetValue(AscendingIconProperty);
			set => SetValue(AscendingIconProperty, value);
		}

		public ImageSource DescendingIcon
		{
			get => (ImageSource)GetValue(DescendingIconProperty);
			set => SetValue(DescendingIconProperty, value);
		}

		public Style AscendingIconStyle
		{
			get => (Style)GetValue(AscendingIconStyleProperty);
			set => SetValue(AscendingIconStyleProperty, value);
		}

		public Style DescendingIconStyle
		{
			get => (Style)GetValue(DescendingIconStyleProperty);
			set => SetValue(DescendingIconStyleProperty, value);
		}

		public View NoDataView
		{
			get => (View)GetValue(NoDataViewProperty);
			set => SetValue(NoDataViewProperty, value);
		}
		#endregion

	}
}