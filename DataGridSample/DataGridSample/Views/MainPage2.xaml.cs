using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.DataGrid;

namespace DataGridSample
{
	public partial class MainPage2
	{
		public MainPage2()
		{
			InitializeComponent();
		}

		private void NGDataGrid_OnQueryCellStyle(object sender, DataGridQueryCellStyleEventArgs e)
		{

			if (e.RowIndex == 3 && e.ColumnIndex == 2)
			{
				e.Handled = true;
				e.Style.BackgroundColor = Color.Chocolate;
			}
		}
	}
}
