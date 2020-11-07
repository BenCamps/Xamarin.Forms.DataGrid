namespace Xamarin.Forms.DataGrid
{
    public enum GridLineVisibility
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    
    public partial class NGDataGrid
    {
        
        
        public static readonly BindableProperty GridLineColorProperty =
            BindableProperty.Create(nameof(GridLineColor), typeof(Color), typeof(NGDataGrid), Color.Gray);

        public static readonly BindableProperty GridLineWidthProperty =
            BindableProperty.Create(nameof(GridLineWidth), typeof(double), typeof(NGDataGrid), 1d);

        public static readonly BindableProperty HeaderGridLinesVisibleProperty =
            BindableProperty.Create(nameof(HeaderGridLinesVisible), typeof(bool), typeof(NGDataGrid), true);

        public static readonly BindableProperty GridLinesVisibilityProperty =
            BindableProperty.Create(nameof(GridLinesVisibility), typeof(GridLineVisibility), typeof(NGDataGrid), GridLineVisibility.Both);


        public Color GridLineColor
        {
            get => (Color)GetValue(GridLineColorProperty);
            set => SetValue(GridLineColorProperty, value);
        }
        
        public double GridLineWidth
        {
            get => (double)GetValue(GridLineWidthProperty);
            set => SetValue(GridLineWidthProperty, value);
        }

        public bool HeaderGridLinesVisible
        {
            get => (bool)GetValue(HeaderGridLinesVisibleProperty);
            set => SetValue(HeaderGridLinesVisibleProperty, value);
        }
        
        public GridLineVisibility GridLinesVisibility
        {
            get => (GridLineVisibility)GetValue(GridLinesVisibilityProperty);
            set => SetValue(GridLinesVisibilityProperty, value);
        }
        
        
        
    }
}