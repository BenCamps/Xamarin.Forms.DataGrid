using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms.DataGrid.Utils;

namespace Xamarin.Forms.DataGrid
{
    public partial class NGDataGrid
    {
	    public static readonly BindableProperty IsSortableProperty =
		    BindableProperty.Create(nameof(IsSortable), typeof(bool), typeof(NGDataGrid), true);
	    
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
						self.InvalidateInternalItems();
				});


		public static readonly BindableProperty AscendingIconProperty =
			BindableProperty.Create(nameof(AscendingIcon), typeof(ImageSource), typeof(NGDataGrid), new FontImageSource{Glyph = "\uea95", FontFamily = "XDGIcons", Size = 16});

		public static readonly BindableProperty DescendingIconProperty =
			BindableProperty.Create(nameof(DescendingIcon), typeof(ImageSource), typeof(NGDataGrid), new FontImageSource{Glyph = "\uea92", FontFamily = "XDGIcons", Size = 16});

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

		
		public bool IsSortable
		{
			get => (bool)GetValue(IsSortableProperty);
			set => SetValue(IsSortableProperty, value);
		}
		
	    public SortData SortedColumnIndex
	    {
		    get => (SortData)GetValue(SortedColumnIndexProperty);
		    set => SetValue(SortedColumnIndexProperty, value);
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
	    
	    
	    
	    
	    
	    private readonly Dictionary<int, SortingOrder> _sortingOrders = new Dictionary<int, SortingOrder>();
	    
	    
		#region Sorting methods
		
		//this should be called from InternalItems setter only
		private void UpdateSorting()
		{
			var sData = SortedColumnIndex;
			
			if (InternalItems == null || !IsSortable || sData == null || sData.Index >= Columns.Count || !Columns[sData.Index].SortingEnabled)
				return;

			var items = InternalItems;
			var column = Columns[sData.Index];
			SortingOrder order = sData.Order;

			if (!IsSortable)
				throw new InvalidOperationException("This DataGrid is not sortable");
			else if (column.PropertyName == null)
				throw new InvalidOperationException("Please set the PropertyName property of Column");

			//Sort
			if (order == SortingOrder.Ascendant)
				items = items.OrderBy(x => ReflectionUtils.GetValueByPath(x, column.PropertyName)).ToList();
			else if (order == SortingOrder.Descendant)
				items = items.OrderByDescending(x => ReflectionUtils.GetValueByPath(x, column.PropertyName)).ToList();

			// column.SortingIcon.Style = (order == SortingOrder.Descendant) ?
			// 	AscendingIconStyle ?? (Style)HeaderView.Resources["DescendingIconStyle"] :
			// 	DescendingIconStyle ?? (Style)HeaderView.Resources["AscendingIconStyle"];
			column.SortingIcon.Source = (order == SortingOrder.None) ? null :
				(order == SortingOrder.Ascendant) ? AscendingIcon : DescendingIcon;

			//Support DescendingIcon property (if setted)
			// if (!column.SortingIcon.Style.Setters.Any(x => x.Property == Image.SourceProperty))
			// {
			// 	if (order == SortingOrder.Descendant && DescendingIconProperty.DefaultValue != DescendingIcon)
			// 		column.SortingIcon.Source = DescendingIcon;
			// 	if (order == SortingOrder.Ascendant && AscendingIconProperty.DefaultValue != AscendingIcon)
			// 		column.SortingIcon.Source = AscendingIcon;
			// }

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
			
			_sortingOrders[sData.Index] = order;
			SortedColumnIndex = sData;

			//update the backing field and not the property
			_internalItems = items;
		}
		#endregion
        
        
    }
}