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

			_dataGridRowTemplate.SetValue(DataGridViewRow.DataGridProperty, dataGrid);

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
