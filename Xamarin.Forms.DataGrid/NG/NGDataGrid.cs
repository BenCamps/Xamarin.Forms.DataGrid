using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms.DataGrid.Platform;
using Xamarin.Forms.Shapes;

namespace Xamarin.Forms.DataGrid
{
	//done: implement selection
	//done: implement grouping
	//done: implement group display expand/collapse
	//done: implement query cell colors
	//done: implement header cells sort and sort arrow
	//todo: implement skiasharp label (with border support?)
	//done: implement grid lines



	public partial class NGDataGrid : Layout
	{
		private View HeaderView;
		private RefreshView RefreshView;
		private NGDataGridScroller Scroller;
		internal NGDataGridContainer Container;
		// private CollectionView Container;

		private ContentView _noDataView;

		RectangleGeometry clipRect = new RectangleGeometry();

		#region Init

		public NGDataGrid()
		{
			IsClippedToBounds = true;
			BackgroundColor = Color.Blue;


			//fix clipping bug on MacOS
			//if (Device.RuntimePlatform == Device.macOS)
			//Clip = clipRect;

			//Effects.Add(new ClipEffect());

			//            HeaderScrollView = new NGDataGridScroller();
			var headerView = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				Spacing = 0,
			};

			//if we go headless, TranslateX,Y don't have any effect
			//CompressedLayout.SetIsHeadless(headerView, true);

			HeaderView = headerView;
			RefreshView = new RefreshView();
			Scroller = new NGDataGridScroller();
			Container = new NGDataGridContainer(this, Scroller);
			// Container = new CollectionView();


			Scroller.Content = Container;
			RefreshView.Content = Scroller;

			InternalChildren.Add(HeaderView);
			InternalChildren.Add(RefreshView);


			Container.BackgroundColor = Color.Transparent;
												  // Container.ItemTemplate = new DataTemplate(typeof(NGDataGridViewRow)); //DataGridRowTemplateSelector(),
												  // Container.SelectionMode = SelectionMode.Single;
												  // Container.ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem;
												  //
												  // Container.SelectionChanged += (s, e) =>
												  // {
												  //     if (SelectionEnabled)
												  //         SelectedItem = Container.SelectedItem;
												  //     else
												  //         Container.SelectedItem = null;
												  //
												  //     var ee = new SelectedItemChangedEventArgs(Container.SelectedItem);
												  //     //todo: change ItemSelected event type to match e
												  //     ItemSelected?.Invoke(this, ee);
												  // };


			RefreshView.Refreshing += (s, e) =>
			{
				Refreshing?.Invoke(this, e);
			};

			RefreshView.IsEnabled = false; //changes when RefreshCommand property changes
			RefreshView.SetBinding(RefreshView.CommandProperty, new Binding(nameof(RefreshCommand), BindingMode.OneWay, source: this));
			RefreshView.SetBinding(RefreshView.IsRefreshingProperty, new Binding(nameof(IsRefreshing), BindingMode.TwoWay, source: this));


			//_listView.SetBinding(ListView.RowHeightProperty, new Binding("RowHeight", source: this));
			//todo: Bind RowHeight propety to DataGridViewRow so it can inform the ItemSizingStrategy

			//listen to scrolled event to handle frozen column
			Scroller.Scrolled += OnScrolled;
		}


		#endregion


		#region Events

		public event EventHandler Refreshing;
		public event EventHandler<SelectedItemChangedEventArgs> ItemSelected;


		#endregion

		#region Public methods

		private void Reload()
		{
			InvalidateInternalItems();
		}


		#endregion

		#region Scrolling Header

		private void OnScrolled(object sender, ScrolledEventArgs e)
		{
			var sx = e.ScrollX;

			//header position follows content position
			HeaderView.TranslationX = -sx;

			// if (sx < 0)
			//     sx = 0;
			//
			// HeaderView.Children[0].TranslationX = sx;
			//
			// foreach (var row in _attachedRows)
			// {
			//     row.Children[0].TranslationX = sx;
			// }
		}


		#endregion


		#region Layout

		/*
         * Grid view contains:
         *
         * Header
         * RefreshView
         *  |-> NGDataGridScroller
         *    |-> NGDataGridContainer
         *
         * LayoutChildren should control positioning and visibility of header and refresh views.
         */
		private bool inLayout = false;

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			if (inLayout)// || ComputedColumnsWidth > -1 || internalItemsSet)
				return;

			inLayout = true;
			DisableLayout = true;

			if (Device.RuntimePlatform == Device.macOS)
				clipRect.Rect = new Rect(x, y, width, height);

			CreateHeaderView();
			UpdateHeaderGridLines();
			UpdateInternalItems();

			//			if (HeaderView.Width != ComputedColumnsWidth || HeaderView.Height != HeaderHeight)
			HeaderView.Layout(new Rectangle(x, y, ComputedColumnsWidth, HeaderHeight));

			y += HeaderHeight;
			height -= HeaderHeight;

			RefreshView.Layout(new Rectangle(x, y, width, height));

			DisableLayout = false;
			inLayout = false;
		}

		//only called if the grid layout is governed by children instead of parent. (Options other than FILL)
		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			if (heightConstraint > -1)
			{
				heightConstraint -= HeaderHeight;
			}
			
			var sr = RefreshView.Measure(widthConstraint, heightConstraint);
			sr.Request = new Size(sr.Request.Width, sr.Request.Height + HeaderHeight);
			
			return sr;
		}

		protected override void InvalidateMeasure()
		{
			//todo: may need to comment out base call.
			base.InvalidateMeasure();
		}

		protected override void InvalidateLayout()
		{
			//todo: may need to comment out base call.
			base.InvalidateLayout();
		}

		#endregion


		#region InternalChildren (use to add child views)

		private ObservableCollection<Element> InternalChildren => (ObservableCollection<Element>)Children;

		//reduce layout calls
		protected override bool ShouldInvalidateOnChildAdded(View child) => false;
		protected override bool ShouldInvalidateOnChildRemoved(View child) => false;

		#endregion


		#region InternalItems

		IList<object> _internalItems;

		internal IList<object> InternalItems
		{
			get => _internalItems;

			set
			{
				_internalItems = value;

				UpdateSorting();

				UpdateGrouping();

				UpdateSelection();

				Container.ItemsSource = _internalItems;
			}
		}

		#endregion


		#region Overrides

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();

		}

		#endregion

		#region Handlers

		protected override void OnPropertyChanged(string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);


			if (propertyName == nameof(HeaderGridLinesVisible)
				|| propertyName == nameof(GridLinesVisibility)
				|| propertyName == nameof(GridLineWidth))
			{
				UpdateHeaderGridLines();
			}

		}

		#endregion

	}
}
