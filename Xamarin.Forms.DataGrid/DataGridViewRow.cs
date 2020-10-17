﻿namespace Xamarin.Forms.DataGrid
{
	internal sealed class DataGridViewRow : Grid
	{
		#region Fields
		Grid _mainLayout;
		Color _bgColor;
		Color _textColor;
		bool _hasSelected;
		#endregion


		public DataGridViewRow()
		{
			_mainLayout = this;
		}


		#region properties
		public DataGrid DataGrid
		{
			get { return (DataGrid)GetValue(DataGridProperty); }
			set { SetValue(DataGridProperty, value); }
		}

		public int Index
		{
			get { return (int)GetValue(IndexProperty); }
			set { SetValue(IndexProperty, value); }
		}

		public object RowContext
		{
			get { return GetValue(RowContextProperty); }
			set { SetValue(RowContextProperty, value); }
		}
		#endregion

		#region Bindable Properties
		public static readonly BindableProperty DataGridProperty =
			BindableProperty.Create(nameof(DataGrid), typeof(DataGrid), typeof(DataGridViewRow), null,
				propertyChanged: (b, o, n) => (b as DataGridViewRow).CreateView());

		public static readonly BindableProperty IndexProperty =
			BindableProperty.Create(nameof(Index), typeof(int), typeof(DataGridViewRow), 0,
				propertyChanged: (b, o, n) => (b as DataGridViewRow).UpdateBackgroundColor());

		public static readonly BindableProperty RowContextProperty =
			BindableProperty.Create(nameof(RowContext), typeof(object), typeof(DataGridViewRow),
				propertyChanged: (b, o, n) => (b as DataGridViewRow).UpdateBackgroundColor());
		#endregion

		#region Methods
		private void CreateView()
		{
			//			_mainLayout = new Grid()
			//			{
			BackgroundColor = DataGrid.BorderColor;
			RowSpacing = 0;
			ColumnSpacing = DataGrid.BorderThickness.HorizontalThickness / 2;
			Padding = new Thickness(DataGrid.BorderThickness.HorizontalThickness / 2,
									DataGrid.BorderThickness.VerticalThickness / 2);
			//			};

			HeightRequest = DataGrid.RowHeight;

			foreach (var col in DataGrid.Columns)
			{
				_mainLayout.ColumnDefinitions.Add(new ColumnDefinition() { Width = col.Width });
				View cell;

				if (col.CellTemplate != null)
				{
					cell = new ContentView() { Content = col.CellTemplate.CreateContent() as View };
					if (col.PropertyName != null)
					{
						cell.SetBinding(BindingContextProperty,
							new Binding(col.PropertyName, source: RowContext, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
					}
				}
				else
				{
					var text = new Label
					{
						TextColor = _textColor,
						HorizontalOptions = col.HorizontalContentAlignment,
						VerticalOptions = col.VerticalContentAlignment,
						LineBreakMode = LineBreakMode.WordWrap,
					};
					text.SetBinding(Label.TextProperty, new Binding(col.PropertyName, BindingMode.Default, stringFormat: col.StringFormat, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
					text.SetBinding(Label.FontSizeProperty, new Binding(DataGrid.FontSizeProperty.PropertyName, BindingMode.Default, source: DataGrid));
					text.SetBinding(Label.FontFamilyProperty, new Binding(DataGrid.FontFamilyProperty.PropertyName, BindingMode.Default, source: DataGrid));

					cell = new ContentView
					{
						Padding = 0,
						BackgroundColor = _bgColor,
						Content = text,
					};
				}

				Grid.SetColumn(cell, DataGrid.Columns.IndexOf(col));
				_mainLayout.Children.Add(cell);
			}

			UpdateBackgroundColor();

			//View = _mainLayout;
		}

		private void UpdateBackgroundColor()
		{
			_hasSelected = DataGrid.SelectedItem == RowContext;
			int actualIndex = DataGrid?.InternalItems?.IndexOf(RowContext) ?? -1;
			if (actualIndex > -1)
			{
				_bgColor = (DataGrid.SelectionEnabled && DataGrid.SelectedItem != null && DataGrid.SelectedItem == RowContext) ?
					DataGrid.ActiveRowColor : DataGrid.RowsBackgroundColorPalette.GetColor(actualIndex, RowContext);
				_textColor = DataGrid.RowsTextColorPalette.GetColor(actualIndex, RowContext);

				ChangeColor(_bgColor);
			}
		}

		private void ChangeColor(Color color)
		{
			foreach (var v in _mainLayout.Children)
			{
				v.BackgroundColor = color;
				var contentView = v as ContentView;
				if (contentView?.Content is Label)
					((Label)contentView.Content).TextColor = _textColor;
			}
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			RowContext = BindingContext;
		}

		protected override void OnParentSet()
		{
			base.OnParentSet();
			if (Parent != null)
				DataGrid.ItemSelected += DataGrid_ItemSelected;
			else
				DataGrid.ItemSelected -= DataGrid_ItemSelected;
		}

		private void DataGrid_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			if (DataGrid.SelectionEnabled && (e.SelectedItem == RowContext || _hasSelected))
			{
				UpdateBackgroundColor();
			}
		}
		#endregion


		protected override void OnChildAdded(Element child)
		{
			base.OnChildAdded(child);

			var c = (VisualElement)child;

			//don't listen to cells measure invalidation to reduce layout calls
			c.MeasureInvalidated -= this.OnChildMeasureInvalidated;
		}


	}
}