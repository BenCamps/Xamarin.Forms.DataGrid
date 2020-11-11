using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Xamarin.Forms.DataGrid
{
	public partial class NGDataGrid
	{
		public static readonly BindableProperty ItemsSourceProperty =
			BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(NGDataGrid), null,
				propertyChanged: HandleItemsSourcePropertyChanged);


		public IEnumerable ItemsSource
		{
			get => (IEnumerable)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		
		private static void HandleItemsSourcePropertyChanged(object b, object o, object n)
		{
			var self = (NGDataGrid)b;

			//ObservableCollection Tracking 
			if (o is INotifyCollectionChanged oldCollection)
				oldCollection.CollectionChanged -= self.HandleItemsSourceCollectionChanged;

			if (n != null)
			{
				if (n is INotifyCollectionChanged newCollection)
					newCollection.CollectionChanged += self.HandleItemsSourceCollectionChanged;

				self.InvalidateInternalItems();
			}

			//todo:handle showing NoDataView
			//if (self.NoDataView != null)
			//{
			//	if (self.ItemsSource == null || self.InternalItems.Any())
			//		self._noDataView.IsVisible = true;
			//	else if (self._noDataView.IsVisible)
			//		self._noDataView.IsVisible = false;
			//}
		}


		private void HandleItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateInternalItems();
		}


		private bool internalItemsSet = false;
		private bool updateInternalItemsScheduled = false;
		private void InvalidateInternalItems()
		{
			if (!updateInternalItemsScheduled)
			{
				internalItemsSet = false;
				updateInternalItemsScheduled = true;
				
				Device.BeginInvokeOnMainThread(async () =>
				{
					await Task.Delay(200); //no more than 5 times per second
					UpdateInternalItems();
				});
			}
		}

		private void UpdateInternalItems()
		{
			//do not set internal items unless columns have been set
			if (ComputedColumnsWidth < 0 || internalItemsSet)
				return;

			if (ItemsSource == null)
				return;

			InternalItems = ItemsSource.Cast<object>().ToList();
			internalItemsSet = true;
			updateInternalItemsScheduled = false;
		}

	}
}