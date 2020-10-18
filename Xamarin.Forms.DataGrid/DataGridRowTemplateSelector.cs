namespace Xamarin.Forms.DataGrid
{
	internal class DataGridRowTemplateSelector : DataTemplateSelector
	{
		private static DataTemplate _dataGridRowTemplate;

		public DataGridRowTemplateSelector()
		{
			_dataGridRowTemplate = new DataTemplate(typeof(DataGridViewRow));
		}

		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			var listView = container as View;
			var dataGrid = GetDataGridParentOf(listView);
			var items = dataGrid.InternalItems;

			_dataGridRowTemplate.SetValue(DataGridViewRow.DataGridProperty, dataGrid);

			if (items != null)
				_dataGridRowTemplate.SetValue(DataGridViewRow.IndexProperty, items.IndexOf(item));

			return _dataGridRowTemplate;
		}


		private DataGrid GetDataGridParentOf(VisualElement v)
		{
			Element p = v;

			while (p != null && !(p is DataGrid))
				p = p.Parent;

			return (DataGrid)p;
		}
	}
}
