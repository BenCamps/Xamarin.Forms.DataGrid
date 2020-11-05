using System;
using Xamarin.Forms;
using Xamarin.Forms.DataGrid.Platform.MacOS;
using Xamarin.Forms.Platform.MacOS;

[assembly: ExportEffect(typeof(Xamarin.Forms.DataGrid.Platform.MacOS.ClipEffect), nameof(ClipEffect))]
namespace Xamarin.Forms.DataGrid.Platform.MacOS
{
    public class ClipEffect : PlatformEffect
    {
        protected override void OnAttached()
        {
            UpdateClipToBounds();
        }

        protected override void OnDetached()
        {
        }


        void UpdateClipToBounds()
        {
            if (Element is Layout layout)
            {
                var view = Control ?? Container;

                if (view.Layer != null)
                {
                    var c = view.WantsDefaultClipping;
                    view.Layer.MasksToBounds = layout.IsClippedToBounds;
                }
            }
        }
    }
    
}