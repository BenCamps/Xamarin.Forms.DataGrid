using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Xamarin.Forms.DataGrid
{

    internal abstract class DeafLayout : Layout
    {

        internal DeafLayout()
        {
            InternalChildren.CollectionChanged += InternalChildrenOnCollectionChanged;
        }

        
        #region InternalChildren (use to add child views)

        internal ObservableCollection<Element> InternalChildren => (ObservableCollection<Element>) Children;

        //reduce layout calls
        protected override bool ShouldInvalidateOnChildAdded(View child) => false;
        protected override bool ShouldInvalidateOnChildRemoved(View child) => false;
        private void InternalChildrenOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null || e.Action == NotifyCollectionChangedAction.Move)
                return;

            //don't listen to child measure invalidations. We control the layout.
            foreach (View item in e.NewItems)
            {
                item.MeasureInvalidated -= OnChildMeasureInvalidated;
            }
        }

        #endregion
        
    }
    
    
}