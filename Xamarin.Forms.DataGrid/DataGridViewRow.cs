using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Xamarin.Forms.DataGrid
{
	internal sealed class DataGridViewRow : Layout<View>
	{
		#region Fields
		Color _bgColor;
		Color _textColor;
		bool _hasSelected;
		#endregion


		public DataGridViewRow()
		{
			//empty
			var collection = (INotifyCollectionChanged) Children;
			collection.CollectionChanged += OnChildrenCollectionChanged;
			BackgroundColor = Color.Blue;
			Padding = 0;
		}


		#region properties
		public DataGrid DataGrid
		{
			get => (DataGrid)GetValue(DataGridProperty);
			set => SetValue(DataGridProperty, value);
		}

		private int _index = -1;

		public int RowIndex
		{
			get
			{
				if (_index == -1 && RowContext != null)
					_index = DataGrid.InternalItems?.IndexOf(RowContext) ?? -1;

				return _index;
			}
			
			private set =>_index = value;
		} 

		public object RowContext
		{
			get => GetValue(RowContextProperty);
			set => SetValue(RowContextProperty, value);
		}

		public Color RowBackgroundColor
		{
			get => (Color) GetValue(RowBackgroundColorProperty);
			set => SetValue(RowBackgroundColorProperty, value);
		}

		public Color RowBorderColor
		{
			get => (Color) GetValue(RowBorderColorProperty);
			set => SetValue(RowBorderColorProperty, value);
		}

		public Color RowForegroundColor
		{
			get => (Color) GetValue(RowForegroundColorProperty);
			set => SetValue(RowForegroundColorProperty, value);
		}

		
		#endregion

		#region Bindable Properties
		public static readonly BindableProperty DataGridProperty =
			BindableProperty.Create(nameof(DataGrid), typeof(DataGrid), typeof(DataGridViewRow), null,
				propertyChanged: (b, o, n) => ((DataGridViewRow)b).CreateView());

		public static readonly BindableProperty RowContextProperty =
			BindableProperty.Create(nameof(RowContext), typeof(object), typeof(DataGridViewRow),
				propertyChanged: (b, o, n) => ((DataGridViewRow)b).RowIndex = -1);

		public static readonly BindableProperty RowBackgroundColorProperty =
			BindableProperty.Create(nameof(RowBackgroundColor), typeof(Color), typeof(DataGridViewRow), Color.Transparent);
		
		public static readonly BindableProperty RowBorderColorProperty =
			BindableProperty.Create(nameof(RowBorderColor), typeof(Color), typeof(DataGridViewRow), Color.Transparent);

		public static readonly BindableProperty RowForegroundColorProperty =
			BindableProperty.Create(nameof(RowForegroundColor), typeof(Color), typeof(DataGridViewRow), Color.Transparent);

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

		private bool needsLayout;

		private void SetNeedsLayout()
		{
			needsLayout = true;
		}
		
		
		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			if (!needsLayout)
				return;

			needsLayout = false;
			
			var g = DataGrid;
			//var t = g.BorderThickness;

			var cy = 0;//t.Top;
			var ch = g.RowHeight; //- t.VerticalThickness;

			var i = 0;

			foreach (var c in Children)
			{
				if (!c.IsVisible)
					continue;

				var cw = Math.Ceiling(g.GetComputedColumnWidth(i));
				var cx = Math.Ceiling(g.GetComputedColumnStart(i));// - t.Left;

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
			HeightRequest = DataGrid.RowHeight;

			foreach (var col in DataGrid.Columns)
			{
				View content;
				var cell = new ContentView()
				{
					Padding = DataGrid.BorderThickness,
				};

				if (col.CellTemplate != null)
				{
					content = col.CellTemplate.CreateContent() as View ?? new Label {Text = "Failed to create cell template."};

					content.VerticalOptions = LayoutOptions.Fill;
					content.HorizontalOptions = LayoutOptions.Fill;

					if (col.PropertyName != null)
					{
						content.SetBinding(BindingContextProperty,
							new Binding(col.PropertyName, source: RowContext, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
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
					text.SetBinding(Label.TextProperty, new Binding(col.PropertyName, BindingMode.Default, source: BindingContext, stringFormat: col.StringFormat, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
					text.SetBinding(Label.FontSizeProperty, new Binding(DataGrid.FontSizeProperty.PropertyName, BindingMode.Default, source: DataGrid));
					text.SetBinding(Label.FontFamilyProperty, new Binding(DataGrid.FontFamilyProperty.PropertyName, BindingMode.Default, source: DataGrid));

					//bind text color
					text.SetBinding(Label.TextColorProperty, new Binding(nameof(RowForegroundColor), BindingMode.OneWay, source: this));

					content = text;
				}

				//bind content background as row background color
				content.SetBinding(BackgroundColorProperty, new Binding(nameof(RowBackgroundColor), BindingMode.OneWay, source: this));
				//bind cell background as border color
				cell.SetBinding(BackgroundColorProperty, new Binding(nameof(DataGrid.BorderColor), BindingMode.OneWay, source: DataGrid));

				cell.Content = content;
				Children.Add(cell);
			}

			SetNeedsLayout();
			InvalidateBackground();
		}


		//used to prevent multiple updates when setting RowContext and Index properties
		private bool updateNeeded;

		private void InvalidateBackground()
		{
			if (DataGrid == null || RowContext == null || updateNeeded)
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
			
			_hasSelected = DataGrid.SelectedItem == RowContext;
			var rowIndex = RowIndex;
			
			if (rowIndex > -1)
			{
				RowBackgroundColor = (DataGrid.SelectedItem == RowContext && DataGrid.SelectionEnabled)
					? DataGrid.ActiveRowColor
					: DataGrid.RowsBackgroundColorPalette.GetColor(rowIndex, RowContext);
				
				RowForegroundColor = DataGrid.RowsTextColorPalette.GetColor(rowIndex, RowContext);

//				ChangeChildrenColors();
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

			RowContext = BindingContext;
			InvalidateBackground();
		}

		protected override void OnParentSet()
		{
			base.OnParentSet();

			var dg = GetDataGridParent();
			if (dg != null)
				DataGrid = dg;
			
			if (Parent != null)
			{
				DataGrid.AddAttachedRow(this);
				DataGrid.ItemSelected += DataGrid_ItemSelected;
			}
			else
			{
				DataGrid.RemoveAttachedRow(this);
				DataGrid.ItemSelected -= DataGrid_ItemSelected;
			}

			SetNeedsLayout();
		}

		private void DataGrid_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			if (DataGrid.SelectionEnabled && (e.SelectedItem == RowContext || _hasSelected))
			{
				InvalidateBackground();
			}
		}
		#endregion

		private DataGrid GetDataGridParent()
		{
			Element p = this;

			while (p != null && !(p is DataGrid))
				p = p.Parent;

			return (DataGrid)p;
		}
		
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

	
		void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Move)
			{
				return;
			}

			//don't listen to cells measure invalidation to reduce layout calls
			//defer the call because the event is hooked AFTER this method returns
			if (e.NewItems != null)
			{
				for (var i = 0; i < e.NewItems.Count; i++)
				{
					if (e.NewItems[i] is View v)
						v.MeasureInvalidated -= this.OnChildMeasureInvalidated;
				}
			}
		}
		
	}



	static class LayoutOptionsExtensions
	{
		public static TextAlignment ToTextAlignment(this LayoutOptions layoutOption)
		{
			switch (layoutOption.Alignment)
			{
				case LayoutAlignment.Fill:
				case LayoutAlignment.Center:
					return TextAlignment.Center;
				
				case LayoutAlignment.Start:
					return TextAlignment.Start;

				case LayoutAlignment.End:
					return TextAlignment.End;
				
				default:
					return TextAlignment.Center;
			}
		}
	}
	
}
