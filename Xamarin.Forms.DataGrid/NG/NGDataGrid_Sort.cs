using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.DataGrid.Utils;

namespace Xamarin.Forms.DataGrid
{
    public partial class NGDataGrid
    {

	    private readonly Dictionary<int, SortingOrder> _sortingOrders = new Dictionary<int, SortingOrder>();
	    
	    
		#region Sorting methods
		internal void SortItems(SortData sData)
		{
			if (InternalItems == null || sData.Index >= Columns.Count || !Columns[sData.Index].SortingEnabled)
				return;

			var items = InternalItems;
			var column = Columns[sData.Index];
			SortingOrder order = sData.Order;

			if (!IsSortable)
				throw new InvalidOperationException("This DataGrid is not sortable");
			else if (column.PropertyName == null)
				throw new InvalidOperationException("Please set the PropertyName property of Column");

			//Sort
			if (order == SortingOrder.Descendant)
				items = items.OrderByDescending(x => ReflectionUtils.GetValueByPath(x, column.PropertyName)).ToList();
			else
				items = items.OrderBy(x => ReflectionUtils.GetValueByPath(x, column.PropertyName)).ToList();

			column.SortingIcon.Style = (order == SortingOrder.Descendant) ?
				AscendingIconStyle ?? (Style)HeaderView.Resources["DescendingIconStyle"] :
				DescendingIconStyle ?? (Style)HeaderView.Resources["AscendingIconStyle"];

			//Support DescendingIcon property (if setted)
			if (!column.SortingIcon.Style.Setters.Any(x => x.Property == Image.SourceProperty))
			{
				if (order == SortingOrder.Descendant && DescendingIconProperty.DefaultValue != DescendingIcon)
					column.SortingIcon.Source = DescendingIcon;
				if (order == SortingOrder.Ascendant && AscendingIconProperty.DefaultValue != AscendingIcon)
					column.SortingIcon.Source = AscendingIcon;
			}

			for (int i = 0; i < Columns.Count; i++)
			{
				if (i != sData.Index)
				{
					if (Columns[i].SortingIcon.Style != null)
						Columns[i].SortingIcon.Style = null;
					if (Columns[i].SortingIcon.Source != null)
						Columns[i].SortingIcon.Source = null;
					_sortingOrders[i] = SortingOrder.None;
				}
			}

			_internalItems = items;

			_sortingOrders[sData.Index] = order;
			SortedColumnIndex = sData;

			//_listView.ItemsSource = _internalItems;
			Container.ItemsSource = _internalItems;
		}
		#endregion
        
        
    }
}