using System;
using System.Threading.Tasks;

namespace Xamarin.Forms.DataGrid
{
	internal sealed class DataGridViewRow : Layout<View>
	{
		#region Fields
		Layout<View> _mainLayout;
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
				propertyChanged: (b, o, n) => (b as DataGridViewRow).InvalidateBackground());

		public static readonly BindableProperty RowContextProperty =
			BindableProperty.Create(nameof(RowContext), typeof(object), typeof(DataGridViewRow),
				propertyChanged: (b, o, n) => (b as DataGridViewRow).InvalidateBackground());
		#endregion


		#region Layout impl
		protected override bool ShouldInvalidateOnChildAdded(View child)
		{
			return false;
		}

		protected override bool ShouldInvalidateOnChildRemoved(View child)
		{
			return false;
		}

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			var g = DataGrid;
			var t = g.BorderThickness;

			var cy = t.Top;
			var ch = g.RowHeight - t.VerticalThickness;

			var i = 0;

			foreach (var c in Children)
			{
				if (!c.IsVisible)
					continue;

				var cw = g.GetComputedColumnWidth(i);
				var cx = g.GetComputedColumnStart(i) - t.Left;

				var r = new Rectangle(cx, cy, cw, ch);

				if (c.Width != cw || c.Height != ch)
					c.Layout(r);

				i++;
			}
		}
		#endregion


		#region Methods
		private void CreateView()
		{
			BackgroundColor = DataGrid.BorderColor;

			HeightRequest = DataGrid.RowHeight;

			foreach (var col in DataGrid.Columns)
			{
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
						Padding = 2,
						BackgroundColor = _bgColor,
						Content = text,
					};
				}

				_mainLayout.Children.Add(cell);
			}
		}


		//used to prevent multiple updates when setting RowContext and Index properties
		private bool updateNeeded;

		private void InvalidateBackground()
		{
			if (!updateNeeded)
			{
				updateNeeded = true;
				
				//defer execution for 10ms until other properties and context have been updated.
				Action a = async () =>
				{
					await Task.Delay(10); 
					UpdateBackgroundColor();
				};
				a.Invoke();
			}
		}
		
		private void UpdateBackgroundColor()
		{
			if (!updateNeeded)
				return;
			
			_hasSelected = DataGrid.SelectedItem == RowContext;
			int actualIndex = DataGrid?.InternalItems?.IndexOf(RowContext) ?? -1;
			if (actualIndex > -1)
			{
				_bgColor = (DataGrid.SelectionEnabled && DataGrid.SelectedItem != null && DataGrid.SelectedItem == RowContext) ?
					DataGrid.ActiveRowColor : DataGrid.RowsBackgroundColorPalette.GetColor(actualIndex, RowContext);
				_textColor = DataGrid.RowsTextColorPalette.GetColor(actualIndex, RowContext);

				ChangeColor(_bgColor);
				updateNeeded = false;
			}
		}

		private void ChangeColor(Color color)
		{
			foreach (var v in _mainLayout.Children)
			{
				v.BackgroundColor = color;

				if (v is Label label)
					label.TextColor = _textColor;
				else if (v is ContentView contentView && contentView.Content is Label label2)
					label2.TextColor = _textColor;
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
			
			if (Parent != null)
				DataGrid.AddAttachedRow(this);
			else
				DataGrid.RemoveAttachedRow(this);
		}

		private void DataGrid_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			if (DataGrid.SelectionEnabled && (e.SelectedItem == RowContext || _hasSelected))
			{
				InvalidateBackground();
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
