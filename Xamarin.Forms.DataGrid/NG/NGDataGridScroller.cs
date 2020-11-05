using System;
using System.Reflection;

namespace Xamarin.Forms.DataGrid
{
    internal class NGDataGridScroller : ScrollView
    {

	    internal NGDataGridScroller()
	    {
		    Orientation = ScrollOrientation.Both;
	    }
	    
		#region Layout Children - Fix farctional measured sizes
		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			var over = ((IScrollViewController)this).LayoutAreaOverride;
			if (!over.IsEmpty)
			{
				x = over.X + Padding.Left;
				y = over.Y + Padding.Top;
				width = over.Width - Padding.HorizontalThickness;
				height = over.Height - Padding.VerticalThickness;
			}

			if (Content != null)
			{
				SizeRequest size;
				switch (Orientation)
				{
					case ScrollOrientation.Horizontal:
						size = Content.Measure(double.PositiveInfinity, height, MeasureFlags.IncludeMargins);
						size.Request = new Size(Math.Round(size.Request.Width), Math.Round(size.Request.Height));
						LayoutChildIntoBoundingRegion(Content, new Rectangle(x, y, GetMaxWidth(width, size), height));
						SetContentSize(new Size(GetMaxWidth(width), height));
						break;
					case ScrollOrientation.Vertical:
						size = Content.Measure(width, double.PositiveInfinity, MeasureFlags.IncludeMargins);
						size.Request = new Size(Math.Round(size.Request.Width), Math.Round(size.Request.Height));
						LayoutChildIntoBoundingRegion(Content, new Rectangle(x, y, width, GetMaxHeight(height, size)));
						SetContentSize(new Size(width, GetMaxHeight(height)));
						break;
					case ScrollOrientation.Both:
						size = Content.Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins);
						size.Request = new Size(Math.Round(size.Request.Width), Math.Round(size.Request.Height));
						LayoutChildIntoBoundingRegion(Content, new Rectangle(x, y, GetMaxWidth(width, size), GetMaxHeight(height, size)));
						SetContentSize(new Size(GetMaxWidth(width), GetMaxHeight(height)));
						break;
					case ScrollOrientation.Neither:
						LayoutChildIntoBoundingRegion(Content, new Rectangle(x, y, width, height));
						SetContentSize(new Size(width, height));
						break;
				}
			}
		}

		FieldInfo ContentSizeFieldInfo = typeof(ScrollView).GetField("ContentSizePropertyKey", BindingFlags.Static | BindingFlags.NonPublic);
		void SetContentSize(Size s)
		{
			BindablePropertyKey csPK =(BindablePropertyKey) ContentSizeFieldInfo.GetValue(null);

			SetValue(csPK, s);
		}

		double GetMaxHeight(double height)
		{
			return Math.Max(height, Content.Bounds.Top + Padding.Top + Content.Bounds.Bottom + Padding.Bottom);
		}

		static double GetMaxHeight(double height, SizeRequest size)
		{
			return Math.Max(size.Request.Height, height);
		}

		double GetMaxWidth(double width)
		{
			return Math.Max(width, Content.Bounds.Left + Padding.Left + Content.Bounds.Right + Padding.Right);
		}

		static double GetMaxWidth(double width, SizeRequest size)
		{
			return Math.Max(size.Request.Width, width);
		}

		#endregion        
        
    }
}