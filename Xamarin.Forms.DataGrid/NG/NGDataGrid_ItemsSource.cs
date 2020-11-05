using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Xamarin.Forms.DataGrid
{
	public partial class NGDataGrid
	{

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
				//self.UpdateInternalItems((IEnumerable)n);
			}

			//todo: handle resetting selection
			//if (self.SelectedItem != null && !self.InternalItems.Contains(self.SelectedItem))
			//	self.SelectedItem = null;

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
			UpdateInternalItems((IEnumerable)e);

			//todo: handle updating selectedItem
			//if (SelectedItem != null && !InternalItems.Contains(SelectedItem))
			//	SelectedItem = null;
		}


		private bool internalItemsSet = false;
		private void InvalidateInternalItems()
		{
			internalItemsSet = false;
		}

		private void UpdateInternalItems(IEnumerable e = null)
		{
			//do not set internal items unless columns have been set
			if (ComputedColumnsWidth < 0 || internalItemsSet)
				return;

			if (e == null)
				e = ItemsSource;

			if (e == null)
				return;

			internalItemsSet = true;

			// Device.BeginInvokeOnMainThread(() =>
			// {
			InternalItems = new List<object>(e.Cast<object>());
			// });
		}

	}
}