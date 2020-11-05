using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Xamarin.Forms.DataGrid
{
    public partial class NGDataGrid
    {

	    public static readonly BindableProperty SelectionColorProperty =
		    BindableProperty.Create(nameof(SelectionColor), typeof(Color), typeof(NGDataGrid), Color.CornflowerBlue);

	    public static readonly BindableProperty SelectionModeProperty =
			BindableProperty.Create(nameof(SelectionMode), typeof(SelectionMode), typeof(NGDataGrid),
				SelectionMode.None, propertyChanged: SelectionModePropertyChanged);

		public static readonly BindableProperty SelectedItemProperty =
			BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(NGDataGrid), default(object),
				defaultBindingMode: BindingMode.TwoWay,
				propertyChanged: SelectedItemPropertyChanged);

		public static readonly BindableProperty SelectedItemsProperty =
			BindableProperty.Create(nameof(SelectedItems), typeof(IList<object>), typeof(NGDataGrid), null,
				defaultBindingMode: BindingMode.OneWay,
				propertyChanged: SelectedItemsPropertyChanged,
				coerceValue: CoerceSelectedItems,
				defaultValueCreator: DefaultValueCreator);

		public static readonly BindableProperty SelectionChangedCommandProperty =
			BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(NGDataGrid));

		public static readonly BindableProperty SelectionChangedCommandParameterProperty =
			BindableProperty.Create(nameof(SelectionChangedCommandParameter), typeof(object),
				typeof(SelectableItemsView));

		static readonly IList<object> s_empty = new List<object>(0);

		bool _suppressSelectionChangeNotification;


		public Color SelectionColor
		{
			get => (Color)GetValue(ActiveRowColorProperty);
			set => SetValue(ActiveRowColorProperty, value);
		}

		public object SelectedItem
		{
			get => GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public IList<object> SelectedItems
		{
			get => (IList<object>)GetValue(SelectedItemsProperty);
			set => SetValue(SelectedItemsProperty, new SelectionList(this, value));
		}

		public ICommand SelectionChangedCommand
		{
			get => (ICommand)GetValue(SelectionChangedCommandProperty);
			set => SetValue(SelectionChangedCommandProperty, value);
		}

		public object SelectionChangedCommandParameter
		{
			get => GetValue(SelectionChangedCommandParameterProperty);
			set => SetValue(SelectionChangedCommandParameterProperty, value);
		}

		public SelectionMode SelectionMode
		{
			get => (SelectionMode)GetValue(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

		//used from UAP
		public void UpdateSelectedItems(IList<object> newSelection)
		{
			var oldSelection = new List<object>(SelectedItems);

			_suppressSelectionChangeNotification = true;

			SelectedItems.Clear();

			if (newSelection?.Count > 0)
			{
				for (int n = 0; n < newSelection.Count; n++)
				{
					SelectedItems.Add(newSelection[n]);
				}
			}

			_suppressSelectionChangeNotification = false;

			SelectedItemsPropertyChanged(oldSelection, newSelection);
		}

		protected virtual void OnSelectionChanged(SelectionChangedEventArgs args)
		{
		}

		static object CoerceSelectedItems(BindableObject bindable, object value)
		{
			if (value == null)
			{
				return new SelectionList((NGDataGrid)bindable);
			}

			if (value is SelectionList)
			{
				return value;
			}

			return new SelectionList((NGDataGrid)bindable, value as IList<object>);
		}

		static object DefaultValueCreator(BindableObject bindable)
		{
			return new SelectionList((NGDataGrid)bindable);
		}

		static void SelectedItemsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var selectableItemsView = (NGDataGrid)bindable;
			var oldSelection = (IList<object>)oldValue ?? s_empty;
			var newSelection = (IList<object>)newValue ?? s_empty;

			selectableItemsView.SelectedItemsPropertyChanged(oldSelection, newSelection);
		}

		internal void SelectedItemsPropertyChanged(IList<object> oldSelection, IList<object> newSelection)
		{
			if (_suppressSelectionChangeNotification)
			{
				return;
			}

			SelectionPropertyChanged(this, new SelectionChangedEventArgs(oldSelection, newSelection));

			OnPropertyChanged(SelectedItemsProperty.PropertyName);
		}

		static void SelectionPropertyChanged(NGDataGrid selectableItemsView, SelectionChangedEventArgs args)
		{
			var command = selectableItemsView.SelectionChangedCommand;

			if (command != null)
			{
				var commandParameter = selectableItemsView.SelectionChangedCommandParameter;

				if (command.CanExecute(commandParameter))
				{
					command.Execute(commandParameter);
				}
			}

			selectableItemsView.SelectionChanged?.Invoke(selectableItemsView, args);
			selectableItemsView.OnSelectionChanged(args);
		}

		static void SelectedItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var selectableItemsView = (NGDataGrid)bindable;

			var args = new SelectionChangedEventArgs(oldValue, newValue);

			SelectionPropertyChanged(selectableItemsView, args);
		}

		static void SelectionModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var selectableItemsView = (NGDataGrid)bindable;

			var oldMode = (SelectionMode)oldValue;
			var newMode = (SelectionMode)newValue;

			IList<object> previousSelection = new List<object>();
			IList<object> newSelection = new List<object>();

			switch (oldMode)
			{
				case SelectionMode.None:
					break;
				case SelectionMode.Single:
					if (selectableItemsView.SelectedItem != null)
					{
						previousSelection.Add(selectableItemsView.SelectedItem);
					}
					break;
				case SelectionMode.Multiple:
					previousSelection = selectableItemsView.SelectedItems;
					break;
			}

			switch (newMode)
			{
				case SelectionMode.None:
					break;
				case SelectionMode.Single:
					if (selectableItemsView.SelectedItem != null)
					{
						newSelection.Add(selectableItemsView.SelectedItem);
					}
					break;
				case SelectionMode.Multiple:
					newSelection = selectableItemsView.SelectedItems;
					break;
			}

			if (previousSelection.Count == newSelection.Count)
			{
				if (previousSelection.Count == 0 || (previousSelection[0] == newSelection[0]))
				{
					// Both selections are empty or have the same single item; no reason to signal a change
					return;
				}
			}

			var args = new SelectionChangedEventArgs(previousSelection, newSelection);
			SelectionPropertyChanged(selectableItemsView, args);
		}
	}        
 
    
    
	internal class SelectionList : IList<object>
	{
		static readonly IList<object> s_empty = new List<object>(0);
		readonly NGDataGrid _selectableItemsView;
		readonly IList<object> _internal;
		IList<object> _shadow;
		bool _externalChange;

		public SelectionList(NGDataGrid selectableItemsView, IList<object> items = null)
		{
			_selectableItemsView = selectableItemsView ?? throw new ArgumentNullException(nameof(selectableItemsView));
			_internal = items ?? new List<object>();
			_shadow = Copy();

			if (items is INotifyCollectionChanged incc)
			{
				incc.CollectionChanged += OnCollectionChanged;
			}
		}

		public object this[int index] { get => _internal[index]; set => _internal[index] = value; }

		public int Count => _internal.Count;

		public bool IsReadOnly => false;

		public void Add(object item)
		{
			_externalChange = true;
			_internal.Add(item);
			_externalChange = false;

			_selectableItemsView.SelectedItemsPropertyChanged(_shadow, _internal);
			_shadow.Add(item);
		}

		public void Clear()
		{
			_externalChange = true;
			_internal.Clear();
			_externalChange = false;

			_selectableItemsView.SelectedItemsPropertyChanged(_shadow, s_empty);
			_shadow.Clear();
		}

		public bool Contains(object item)
		{
			return _internal.Contains(item);
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			_internal.CopyTo(array, arrayIndex);
		}

		public IEnumerator<object> GetEnumerator()
		{
			return _internal.GetEnumerator();
		}

		public int IndexOf(object item)
		{
			return _internal.IndexOf(item);
		}

		public void Insert(int index, object item)
		{
			_externalChange = true;
			_internal.Insert(index, item);
			_externalChange = false;

			_selectableItemsView.SelectedItemsPropertyChanged(_shadow, _internal);
			_shadow.Insert(index, item);
		}

		public bool Remove(object item)
		{
			_externalChange = true;
			var removed = _internal.Remove(item);
			_externalChange = false;

			if (removed)
			{
				_selectableItemsView.SelectedItemsPropertyChanged(_shadow, _internal);
				_shadow.Remove(item);
			}

			return removed;
		}

		public void RemoveAt(int index)
		{
			_externalChange = true;
			_internal.RemoveAt(index);
			_externalChange = false;

			_selectableItemsView.SelectedItemsPropertyChanged(_shadow, _internal);
			_shadow.RemoveAt(index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _internal.GetEnumerator();
		}

		List<object> Copy()
		{
			var items = new List<object>();
			for (int n = 0; n < _internal.Count; n++)
			{
				items.Add(_internal[n]);
			}

			return items;
		}

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (_externalChange)
			{
				// If this change was initiated by a renderer or direct manipulation of ColllectionView.SelectedItems,
				// we don't need to send a selection change notification
				return;
			}

			// This change is coming from a bound viewmodel property
			// Emit a selection change notification, then bring the shadow copy up-to-date
			_selectableItemsView.SelectedItemsPropertyChanged(_shadow, _internal);
			_shadow = Copy();
		}
	}    

	
	public class SelectionChangedEventArgs : EventArgs
	{
		public IReadOnlyList<object> PreviousSelection { get; }
		public IReadOnlyList<object> CurrentSelection { get; }

		static readonly IReadOnlyList<object> s_empty = new List<object>(0);

		internal SelectionChangedEventArgs(object previousSelection, object currentSelection)
		{
			PreviousSelection = previousSelection != null ? new List<object>(1) { previousSelection } : s_empty;
			CurrentSelection = currentSelection != null ? new List<object>(1) { currentSelection } : s_empty;
		}

		internal SelectionChangedEventArgs(IList<object> previousSelection, IList<object> currentSelection)
		{
			PreviousSelection = new List<object>(previousSelection ?? throw new ArgumentNullException(nameof(previousSelection)));
			CurrentSelection = new List<object>(currentSelection ?? throw new ArgumentNullException(nameof(currentSelection)));
		}
	}	
}