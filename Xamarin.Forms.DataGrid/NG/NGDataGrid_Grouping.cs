using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Linq;


namespace Xamarin.Forms.DataGrid
{

    public class GroupDescriptor : BindableObject
    {
        #region PropertyName
        
        public static readonly BindableProperty PropertyNameProperty = 
            BindableProperty.Create(propertyName: nameof(PropertyName), returnType: typeof(string), declaringType: typeof(NGDataGrid), defaultValue: default, defaultBindingMode: BindingMode.OneWay);
        
        public string PropertyName
        {
            get => (string) GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value); 
        }
        
        #endregion
        
        #region Comparer
        
        public static readonly BindableProperty ComparerProperty = 
            BindableProperty.Create(propertyName: nameof(Comparer), returnType: typeof(IComparer<object>), declaringType: typeof(NGDataGrid), defaultValue: default, defaultBindingMode: BindingMode.OneWay);
        
        public IComparer<object> Comparer
        {
            get => (IComparer<object>) GetValue(ComparerProperty);
            set => SetValue(ComparerProperty, value); 
        }
        
        #endregion
        
        
        #region Converter
        
        public static readonly BindableProperty ConverterProperty = 
            BindableProperty.Create(propertyName: nameof(Converter), returnType: typeof(IValueConverter), declaringType: typeof(NGDataGrid), defaultValue: default, defaultBindingMode: BindingMode.OneWay);
        
        public IValueConverter Converter
        {
            get => (IValueConverter) GetValue(ConverterProperty);
            set => SetValue(ConverterProperty, value); 
        }
        
        #endregion
        
        #region SortGroupItems
        
        public static readonly BindableProperty SortGroupItemsProperty = 
            BindableProperty.Create(propertyName: nameof(SortGroupItems), returnType: typeof(bool), declaringType: typeof(NGDataGrid), defaultValue: false, defaultBindingMode: BindingMode.OneWay);
        
        public bool SortGroupItems
        {
            get => (bool) GetValue(SortGroupItemsProperty);
            set => SetValue(SortGroupItemsProperty, value); 
        }
        
        #endregion
        
    }

    internal class ObjectGroup : BindableObject
    {
        public object Key;
        public IList<object> Items;
        
        public static readonly BindableProperty TextProperty = 
            BindableProperty.Create(propertyName: nameof(Text), returnType: typeof(string), declaringType: typeof(ObjectGroup), defaultValue: default, defaultBindingMode: BindingMode.OneWay);

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }
    
    
    
    public partial class NGDataGrid
    {
        
        public static readonly BindableProperty GroupBackgroundColorProperty =
            BindableProperty.Create(nameof(GroupBackgroundColor), typeof(Color), typeof(NGDataGrid), Color.Transparent);
        
        public Color GroupBackgroundColor
        {
            get => (Color) GetValue(GroupBackgroundColorProperty);
            set => SetValue(GroupBackgroundColorProperty, value);
        }

        public static readonly BindableProperty GroupForegroundColorProperty =
            BindableProperty.Create(nameof(GroupForegroundColor), typeof(Color), typeof(NGDataGrid), Color.Black);
        
        public Color GroupForegroundColor
        {
            get => (Color) GetValue(GroupForegroundColorProperty);
            set => SetValue(GroupForegroundColorProperty, value);
        }

        
        public static readonly BindableProperty GroupDescriptionProperty =
            BindableProperty.Create(nameof(GroupDescription), typeof(GroupDescriptor), typeof(NGDataGrid), default, propertyChanged: OnGroupDescriptionChanged);

        private static void OnGroupDescriptionChanged(BindableObject b, object o, object n)
        {
            var dg = (NGDataGrid) b;

            dg.InvalidateInternalItems();
        }


        public GroupDescriptor GroupDescription
        {
            get => (GroupDescriptor) GetValue(GroupDescriptionProperty);
            set => SetValue(GroupDescriptionProperty, value);
        }


        //this should be called from InternalItems setter only
        void UpdateGrouping()
        {
            if (GroupDescription == null || InternalItems == null)
                return;

            var gcdProperty = GroupDescription.PropertyName;
            var gcdConverter = GroupDescription.Converter;
            var gcdComparer = GroupDescription.Comparer;
            
            var groups = new Dictionary<object, ObjectGroup>();
            var result = new List<object>();

            var items = InternalItems;
            foreach (var item in items)
            {
                var value = GetItemProperty(item, gcdProperty) ?? "";

                if (!groups.TryGetValue(value, out var group))
                {
                    group = new ObjectGroup
                    {
                        Key = value, 
                        Text = GetItemPropertyString(item, gcdProperty, gcdConverter),
                        Items = new List<object>()
                    };
                    
                    groups.Add(value, group);
                    result.Add(group);
                }
                
                group.Items.Add(item);
            }

            if (GroupDescription.SortGroupItems)
            {    
                result.Sort(gcdComparer);
            }

            //update the backing field and not the property
            _internalItems = result;
        }


        private Dictionary<string, PropertyInfo> _itemPropertyCache = new Dictionary<string, PropertyInfo>();

        internal string GetItemPropertyString(object item, string propertyName, IValueConverter converter = null)
        {
            var value = GetItemProperty(item, propertyName);

            if (converter != null)
                value = converter?.Convert(value, typeof(string), null, CultureInfo.CurrentUICulture);
            else
                value = value.ToString();

            return (string) value;
        }

        internal object GetItemProperty(object item, string propertyName)
        {

            if (!_itemPropertyCache.TryGetValue(propertyName, out var propertyInfo))
            {
                var pi = item.GetType().GetProperty(propertyName);
                
                if (pi == null)
                    throw new ArgumentException($"Property {propertyName} not found on object {item.GetType().Name}");
                
                _itemPropertyCache.Add(propertyName, pi);
                propertyInfo = pi;
            }

            return propertyInfo.GetValue(item);
        }

    }
}