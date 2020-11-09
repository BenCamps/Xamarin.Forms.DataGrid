using System;

namespace Xamarin.Forms.DataGrid
{

    public class DataGridHandledEventArgs : EventArgs
    {
        public bool Handled { get; set; }
    }

    public class DataGridRowEventArgs : DataGridHandledEventArgs
    {
        public object RowData { get; internal set; }
        public int RowIndex { get; internal set; }
    }

    public class DataGridQueryCellStyleEventArgs : DataGridRowEventArgs
    {
        public object CellValue { get; internal set; }
        public DataGridColumn Column { get; internal set; }
        public int ColumnIndex { get; internal set; }
        public CellStyle Style { get; internal set; }
    }


    public class RowStyle
    {
        public bool IsSelection { get; internal set; }
        public Color BackgroundColor { get; set; } = Color.Default;
        public Color ForegroundColor { get; set; } = Color.Default;
    }

    public class CellStyle : RowStyle
    {
        // public string FontFamily { get; set; }
        // public FontAttributes FontAttributes { get; set; }
    }
    
    
    
    public partial class NGDataGrid
    {

        public event EventHandler<DataGridQueryCellStyleEventArgs> QueryCellStyle;

        
        internal bool ShouldQueryCellStyle()
        {
            return QueryCellStyle != null;
        }

        
        internal CellStyle NotifyQueryCellStyle(DataGridColumn column, ItemInfo info, object cellValue)
        {
            if (QueryCellStyle == null)
                return null;

            var e = new DataGridQueryCellStyleEventArgs
            {
                RowData = info.Item,
                RowIndex = info.Index,
                CellValue = cellValue,
                Column = column,
                ColumnIndex = column.ColumnIndex,
                Style = new CellStyle
                {
                    IsSelection = info.Selected
                }
            };

            QueryCellStyle.Invoke(this, e);

            if (!e.Handled)
                return null;

            return e.Style;
        }
        
    }
}