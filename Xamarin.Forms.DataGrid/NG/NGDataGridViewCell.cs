using System.Collections.ObjectModel;

namespace Xamarin.Forms.DataGrid
{
	internal class NGDataGridViewCell : ContentView
	{

		public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(ContentView), null, propertyChanged: OnContentChanged);
		
		public static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var self = (NGDataGridViewCell) bindable;
			var newElement = (Element) newValue;
			var oldElement = (Element) oldValue;

			if (oldValue != null)
				self.Children.Remove(oldElement);
			
			if (newValue != null)
				self.Children.Insert(0, newElement);
		}
		
		
		public View Content
		{
			get { return (View)GetValue(ContentProperty); }
			set { SetValue(ContentProperty, value); }
		}
		
		
		public DataGridColumn Column { get; set; }
		
		public bool IsFromTemplate { get; set; }
		
		
		public NGDataGridViewCell()
		{
			CompressedLayout.SetIsHeadless(this, true);
		}
		
		
		public new ObservableCollection<Element> Children => (ObservableCollection<Element>) base.Children;



		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			for (var i = 0; i < Children.Count; i++)
			{
				var element = Children[i];

				if (element is View child)
					LayoutChildIntoBoundingRegion(child, new Rectangle(x, y, width, height));
			}
		}

		//we don't care about measures
		protected override SizeRequest OnSizeRequest(double widthConstraint, double heightConstraint)
		{
			double widthRequest = WidthRequest;
			double heightRequest = HeightRequest;
			var childRequest = new SizeRequest();

			if ((widthRequest == -1 || heightRequest == -1) && Children.Count > 0)
			{
				childRequest = ((View) Children[0]).Measure(widthConstraint, heightConstraint, MeasureFlags.IncludeMargins);
			}

			return new SizeRequest
			{
				Request = new Size {Width = widthRequest != -1 ? widthRequest : childRequest.Request.Width, Height = heightRequest != -1 ? heightRequest : childRequest.Request.Height},
				Minimum = childRequest.Minimum
			};
		}

	}
}