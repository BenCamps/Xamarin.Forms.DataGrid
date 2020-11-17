using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Shapes;
using System.Reflection;

namespace Xamarin.Forms.DataGrid
{
	internal abstract class NGDataGridViewItem : DeafLayout
	{
		#region SetPosition
		
		private static readonly BindablePropertyKey RowXProperty;
		private static readonly BindablePropertyKey RowYProperty;

		static NGDataGridViewItem()
		{
			RowXProperty = (BindablePropertyKey)typeof(VisualElement).GetField("XPropertyKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			RowYProperty = (BindablePropertyKey)typeof(VisualElement).GetField("YPropertyKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		}

		internal void SetPosition(double x, double y)
		{
			BatchBegin();
			SetValue(RowXProperty, x);
			SetValue(RowYProperty, y);
			BatchCommit();
		}

		private readonly BoxView HorizontalGridLineView;

		#endregion

		protected static bool alt;

		protected NGDataGridViewItem(NGDataGrid dg)
		{
			//empty
			BackgroundColor = alt ? Color.PaleGreen : Color.Orchid;
			alt = !alt;
			
			DataGrid = dg;

			//handle row selection
			GestureRecognizers.Add(new TapGestureRecognizer
			{
				NumberOfTapsRequired = 1,
				TappedCallback = (v, o) => { OnTapped(); }
			});
			
			//Setup the grid line view
			HorizontalGridLineView = new BoxView();
			HorizontalGridLineView.SetBinding(BackgroundColorProperty, new Binding(nameof(DataGrid.GridLineColor), BindingMode.OneWay, source: DataGrid));
			HorizontalGridLineView.SetBinding(HeightRequestProperty, new Binding(nameof(DataGrid.GridLineWidth), BindingMode.OneWay, source: DataGrid));
			HorizontalGridLineView.VerticalOptions = LayoutOptions.Start;

			InternalChildren.Add(HorizontalGridLineView);
		}
		
		
		#region properties
		protected NGDataGrid DataGrid
		{
			get => (NGDataGrid)GetValue(DataGridProperty);
			set => SetValue(DataGridProperty, value);
		}
		
		protected int ItemIndex => ItemInfo?.Index ?? -1;
		
		internal ItemInfo ItemInfo
		{
			get => (ItemInfo)GetValue(ItemInfoProperty);
			set => SetValue(ItemInfoProperty, value);
		}

		// protected Color ItemBackgroundColor
		// {
		// 	get => (Color)GetValue(ItemBackgroundColorProperty);
		// 	set => SetValue(ItemBackgroundColorProperty, value);
		// }
		//
		// protected Color ItemForegroundColor
		// {
		// 	get => (Color)GetValue(ItemForegroundColorProperty);
		// 	set => SetValue(ItemForegroundColorProperty, value);
		// }


		#endregion

		#region Bindable Properties
		public static readonly BindableProperty DataGridProperty =
			BindableProperty.Create(nameof(DataGrid), typeof(NGDataGrid), typeof(NGDataGridViewItem), null,
				propertyChanged: (b, o, n) => ((NGDataGridViewItem)b).CreateView());

		// public static readonly BindableProperty ItemBackgroundColorProperty =
		// 	BindableProperty.Create(nameof(ItemBackgroundColor), typeof(Color), typeof(NGDataGridViewItem), Color.Transparent);
		//
		// public static readonly BindableProperty ItemForegroundColorProperty =
		// 	BindableProperty.Create(nameof(ItemForegroundColor), typeof(Color), typeof(NGDataGridViewItem), Color.Transparent);

		public static readonly BindableProperty ItemInfoProperty =
			BindableProperty.Create(nameof(ItemInfo), typeof(ItemInfo), typeof(NGDataGridViewItem),
				propertyChanged: (b, o, n) => ((NGDataGridViewItem)b).BindingContext = (n as ItemInfo)?.Item);

		#endregion


		#region Layout

		private bool needsLayout;

		protected bool NeedsLayout => needsLayout;

		internal void SetNeedsLayout(bool value = true)
		{
			needsLayout = value;
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

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			Debug.WriteLine($"Item Layout {x},{y} {width},{height}");
			
			var gridLineVisibility = DataGrid.GridLinesVisibility;
			var showHorizontalLines = gridLineVisibility == GridLineVisibility.Horizontal || gridLineVisibility == GridLineVisibility.Both;

			//show the grid line and adjust child layout area
			HorizontalGridLineView.IsVisible = showHorizontalLines;
			LayoutChildIntoBoundingRegion(HorizontalGridLineView, new Rectangle(x, y, width, height));
		}

		#endregion


		#region Methods
		protected virtual void CreateView()
		{
		}
		

		//used to prevent multiple updates when setting RowContext and Index properties
		private bool updateNeeded;

		internal void InvalidateColors()
		{
			if (DataGrid == null || ItemInfo == null || updateNeeded)
				return;

			updateNeeded = true;

			OnUpdateColors();

			updateNeeded = false;
		}

		protected virtual void OnUpdateColors()
		{
		}
		
		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

			InvalidateColors();
		}

		protected override void OnParentSet()
		{
			base.OnParentSet();
			
			SetNeedsLayout();
		}
		
		#endregion

		#region Selection

		protected virtual void OnTapped()
		{
		}
		
		#endregion
		
	}
	
}
