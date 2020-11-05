using Xamarin.Forms.Platform.MacOS;

using System;
using CoreGraphics;
using Xamarin.Forms;
using Xamarin.Forms.DataGrid.Platform.MacOS;

[assembly: ExportRenderer(typeof(Layout), typeof(LayoutRenderer))]
namespace Xamarin.Forms.DataGrid.Platform.MacOS
{
	public class LayoutRenderer : VisualElementRenderer<VisualElement>
	{
		CGRect _bounds;

		public override void Layout()
		{
			            //SetElementSize(new Size(Bounds.Width, Bounds.Height));
		}

		/*
				public override void Layout()
				{
					base.Layout();

					if (_bounds == Bounds)
						return;

					_bounds = Bounds;

					//when the layout changes we might need to update  the children position based in our new size,
					//this is only needed in MacOS because of the inversion of the Y coordinate. 
					//Forms layout system doesn't know we need to relayout the other items ,(first ones for example)
					//so we do it here 
					for (int i = 0; i < Subviews.Length; i++)
					{
						var item = Subviews[i] as IVisualElementRenderer;
						if (item == null)
							continue;
						var oldFrame = item.NativeView.Frame;

						var newY = Math.Max(0, (float)(Element.Height - item.Element.Height - item.Element.Y));
						if (oldFrame.Y == newY)
							continue;
						var newPosition = new CGPoint(oldFrame.X, newY);
						item.NativeView.Frame = new CGRect(newPosition, oldFrame.Size);
						Console.WriteLine($"XX New Frame - {item.NativeView.Frame}");
					}
				}
		*/
	}

}