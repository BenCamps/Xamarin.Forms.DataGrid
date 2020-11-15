using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.DataGrid
{
//	/*
	#region Extensions
	
	internal static class SKExtensions
	{

		public static SKTypeface ToSKTypeface(this Font font)
		{
			if (font.IsDefault)
				return null;

			SKFontStyleWeight weight = font.FontAttributes.HasFlag(FontAttributes.Bold) ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
			SKFontStyleSlant slant = font.FontAttributes.HasFlag((Enum) FontAttributes.Italic) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

			return SKTypeface.FromFamilyName(font.FontFamily, weight, SKFontStyleWidth.Normal, slant);
		}
		
	}

	#endregion


	public class SKLabel : SKCanvasView, IFontElement, IDisposable
	{

		#region Font

		public static readonly BindableProperty FontProperty =
			BindableProperty.Create(nameof(Font), typeof(Font), typeof(SKLabel), default(Font),
									propertyChanged: OnFontPropertyChanged);

		public static readonly BindableProperty FontFamilyProperty =
			BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(SKLabel), default(string),
									propertyChanged: OnFontFamilyChanged,
									defaultValueCreator: FontFamilyDefaultValueCreator);
		
		public static readonly BindableProperty FontSizeProperty =
			BindableProperty.Create(nameof(FontSize), typeof(double), typeof(SKLabel), -1.0,
									propertyChanged: OnFontSizeChanged,
									defaultValueCreator: FontSizeDefaultValueCreator);

		public static readonly BindableProperty FontAttributesProperty =
			BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(SKLabel), FontAttributes.None,
									propertyChanged: OnFontAttributesChanged);

		static readonly BindableProperty CancelEventsProperty =
			BindableProperty.Create("CancelEvents", typeof(bool), typeof(SKLabel), false);

		
		static bool GetCancelEvents(BindableObject bindable) => (bool)bindable.GetValue(CancelEventsProperty);
		static void SetCancelEvents(BindableObject bindable, bool value)
		{
			bindable.SetValue(CancelEventsProperty, value);
			
			if (!value)
				(bindable as SKLabel).InvalidateSurface();
		}
		
		static void OnFontPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (GetCancelEvents(bindable))
				return;

			SetCancelEvents(bindable, true);

			var font = (Font)newValue;
			if (font == Forms.Font.Default) {
				bindable.ClearValue(FontFamilyProperty);
				bindable.ClearValue(FontSizeProperty);
				bindable.ClearValue(FontAttributesProperty);
			} else {
				bindable.SetValue(FontFamilyProperty, font.FontFamily);
				if (font.UseNamedSize)
					bindable.SetValue(FontSizeProperty, Device.GetNamedSize(font.NamedSize, bindable.GetType(), true));
				else
					bindable.SetValue(FontSizeProperty, font.FontSize);
				bindable.SetValue(FontAttributesProperty, font.FontAttributes);
			}
			SetCancelEvents(bindable, false);
		}

		static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (GetCancelEvents(bindable))
				return;

			SetCancelEvents(bindable, true);

			var fontSize = (double)bindable.GetValue(FontSizeProperty);
			var fontAttributes = (FontAttributes)bindable.GetValue(FontAttributesProperty);
			var fontFamily = (string)newValue;

			if (fontFamily == null)
				fontFamily = (string) FontFamilyDefaultValueCreator(bindable);

			bindable.SetValue(FontProperty, Font.OfSize(fontFamily, fontSize).WithAttributes(fontAttributes));

			SetCancelEvents(bindable, false);
			((IFontElement)bindable).OnFontFamilyChanged((string)oldValue, (string)newValue);
		}

		static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (GetCancelEvents(bindable))
				return;

			SetCancelEvents(bindable, true);

			var fontSize = (double)newValue;
			var fontAttributes = (FontAttributes)bindable.GetValue(FontAttributesProperty);
			var fontFamily = (string)bindable.GetValue(FontFamilyProperty);

			if (fontFamily == null)
				fontFamily = (string) FontFamilyDefaultValueCreator(bindable);

			bindable.SetValue(FontProperty, Font.OfSize(fontFamily, fontSize).WithAttributes(fontAttributes));

			SetCancelEvents(bindable, false);
			((IFontElement)bindable).OnFontSizeChanged((double)oldValue, (double)newValue);
		}

		static object FontSizeDefaultValueCreator(BindableObject bindable)
		{
			return ((IFontElement)bindable).FontSizeDefaultValueCreator();
		}

		private static object FontFamilyDefaultValueCreator(BindableObject bindable)
		{
			switch (Device.RuntimePlatform)
			{
				case Device.iOS:
				case Device.macOS:
					return ".AppleSystemUIFont";
					// return "San Francisco Display";
				case Device.Android:
					return "Roboto";
				case Device.UWP:
				case Device.WPF:
					return "Segoe UI";
				default:
					return "Helvetica";
			}
		}
		
		static void OnFontAttributesChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (GetCancelEvents(bindable))
				return;

			SetCancelEvents(bindable, true);

			var fontSize = (double)bindable.GetValue(FontSizeProperty);
			var fontAttributes = (FontAttributes)newValue;
			var fontFamily = (string)bindable.GetValue(FontFamilyProperty);

			if (fontFamily == null)
				fontFamily = (string) FontFamilyDefaultValueCreator(bindable);

			bindable.SetValue(FontProperty, Font.OfSize(fontFamily, fontSize).WithAttributes(fontAttributes));

			SetCancelEvents(bindable, false);
			((IFontElement)bindable).OnFontAttributesChanged((FontAttributes)oldValue, (FontAttributes)newValue);
		}
		
		
		public Font Font
		{
			get =>(Font) GetValue(FontProperty);
			set => SetValue(FontProperty, value);
		}
		
		public string FontFamily
		{
			get =>(string) GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}
		
		public double FontSize
		{
			get => (double) GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}
		
		public FontAttributes FontAttributes
		{
			get => (FontAttributes) GetValue(FontAttributesProperty);
			set => SetValue(FontAttributesProperty, value);
		}
		
		
		void IFontElement.OnFontFamilyChanged(string oldValue, string newValue)
		{
			InvalidateMeasure();
		}

		void IFontElement.OnFontSizeChanged(double oldValue, double newValue)
		{
			InvalidateMeasure();
		}

		double IFontElement.FontSizeDefaultValueCreator()
		{
			return Device.GetNamedSize(NamedSize.Default, typeof(Label));
		}

		void IFontElement.OnFontAttributesChanged(FontAttributes oldValue, FontAttributes newValue)
		{
			InvalidateMeasure();
		}

		void IFontElement.OnFontChanged(Font oldValue, Font newValue)
		{
			InvalidateMeasure();
		}
		
		#endregion


		#region Properties

		public static readonly BindableProperty PaddingProperty = 
			BindableProperty.Create(nameof(Padding), typeof(Thickness), typeof(SKLabel), default(Thickness), 
				propertyChanged: InvalidatePropertyChanged);

		public static readonly BindableProperty TextColorProperty = 
			BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(SKLabel), Color.Black, 
				propertyChanged: InvalidatePropertyChanged);
		
		public static readonly BindableProperty TextProperty = 
			BindableProperty.Create(nameof(Text), typeof(string), typeof(SKLabel), string.Empty, 
				propertyChanged: InvalidatePropertyChanged);
		
		public static readonly BindableProperty HorizontalTextAlignmentProperty =
			BindableProperty.Create(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(SKLabel), TextAlignment.Start,
				propertyChanged: InvalidatePropertyChanged);

		public static readonly BindableProperty VerticalTextAlignmentProperty =
			BindableProperty.Create(nameof(VerticalTextAlignment), typeof(TextAlignment), typeof(SKLabel), TextAlignment.Center,
				propertyChanged: InvalidatePropertyChanged);

		public static readonly BindableProperty LineBreakModeProperty = 
			BindableProperty.Create(nameof(LineBreakMode), typeof(LineBreakMode), typeof(SKLabel), LineBreakMode.WordWrap,
				propertyChanged: InvalidatePropertyChanged);
		
		static void InvalidatePropertyChanged(BindableObject bo,object oldValue,object newValue)
		{
			((SKLabel)bo).InvalidateMeasure();
			((SKLabel)bo).InvalidateSurface();
		}

		
		public Thickness Padding
		{
			get => (Thickness) GetValue(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}
		
		public Color TextColor
		{
			get => (Color) GetValue(TextColorProperty);
			set => SetValue(TextColorProperty, value);
		}

		public string Text
		{
			get => (string) GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public TextAlignment HorizontalTextAlignment
		{
			get => (TextAlignment) GetValue(HorizontalTextAlignmentProperty);
			set => SetValue(HorizontalTextAlignmentProperty, value);
		}

		public TextAlignment VerticalTextAlignment
		{
			get => (TextAlignment) GetValue(VerticalTextAlignmentProperty);
			set => SetValue(VerticalTextAlignmentProperty, value);
		}
		
		public LineBreakMode LineBreakMode
		{
			get { return (LineBreakMode)GetValue(LineBreakModeProperty); }
			set { SetValue(LineBreakModeProperty, value); }
		}

		#endregion

		public SKLabel()
		{
		}

		private static readonly float DisplayScale = (float) Device.Info.ScalingFactor;

		private static SKTypeface _defaultFont;
		public static SKTypeface DefaultFont => _defaultFont = _defaultFont ?? SKTypeface.Default;

		private SKPaint _cachedPaint;

		private double _cachedWidthConstraint;
		private Line[] _cachedLines;
		private Size _cachedSize;

		protected override void InvalidateMeasure()
		{
			_cachedWidthConstraint = -1;
			base.InvalidateMeasure();
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{

			if (_cachedWidthConstraint != widthConstraint)
			{
				_cachedWidthConstraint = widthConstraint;

				if (_cachedPaint != null)
				{
					_cachedPaint.Dispose();
					_cachedPaint = null;
				}

				_cachedPaint = new SKPaint
				{
					IsAntialias = true,
					IsAutohinted = true,
					SubpixelText = true,
					LcdRenderText = true,
					Color = TextColor.ToSKColor(),
					Typeface = Font.ToSKTypeface() ?? DefaultFont,
					TextSize = (float) (FontSize * DisplayScale)
				};
				
				_cachedLines = SplitLines(Text, _cachedPaint, widthConstraint * DisplayScale);

				_cachedSize = new Size(_cachedLines.Max(l => l.WidthPixels) / DisplayScale, _cachedLines.Length * (float)(FontSize * DisplayScale));
			}

			
			return new SizeRequest(_cachedSize);
		}

		
		protected override void OnTouch(SKTouchEventArgs e)
		{
			base.OnTouch(e);
		}

		
		protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
		{
			//clear surface
			e.Surface.Canvas.Clear();

			Draw(e.Surface.Canvas, e.Info);
			
			base.OnPaintSurface(e);
		}


		public void Draw(SKCanvas canvas, SKImageInfo info)
		{
			if (Text == "")
				return;
			
			//measure lines if needed
			if (Width != _cachedWidthConstraint)
				Measure(Width, Height);
			
			//clip for padding
			var padding = Padding;
			var clipRect = new SKRect(
				(float) padding.Left * DisplayScale,
				(float) padding.Top * DisplayScale,
				(float) (Width - padding.HorizontalThickness) * DisplayScale,
				(float) (Height - padding.Bottom) * DisplayScale
				);
			canvas.ClipRect(clipRect);
			
			//calculate font pixel size
			var fontSize = (float)(FontSize * DisplayScale);

			var startY = 0f;
			
			switch (VerticalTextAlignment)
			{
				case TextAlignment.Start:
					startY = clipRect.Top;
					break;
				case TextAlignment.Center:
					startY = clipRect.MidY - (_cachedLines.Length * fontSize) / 2;
					break;
				case TextAlignment.End:
					startY = clipRect.Bottom - (_cachedLines.Length * fontSize);
					break;
			}

			var horizontalTextAlignment = HorizontalTextAlignment;
			
			// {
				var lines = _cachedLines;
				var offsetY = startY - _cachedPaint.FontMetrics.Descent;
				
				foreach (var line in lines)
				{
					offsetY += fontSize;
					var offsetX = clipRect.Left;

					switch (horizontalTextAlignment)
					{
						case TextAlignment.Start:
							break;
						case TextAlignment.Center:
							offsetX = clipRect.MidX - line.WidthPixels/2;
							break;
						case TextAlignment.End:
							offsetX = clipRect.Right - line.WidthPixels;
							break;
					}

					// canvas.DrawText(line.Value, (float) Padding.Left, (float) offsetY, paint);
					canvas.DrawText(line.Text, offsetX, (float) offsetY, _cachedPaint);

				}
				
				// canvas.DrawLine( clipRect.MidX - 5, clipRect.MidY, clipRect.MidX + 5, clipRect.MidY, _cachedPaint);
				// canvas.DrawLine( clipRect.MidX, clipRect.MidY - 5, clipRect.MidX, clipRect.MidY + 5, _cachedPaint);
				
				//Add text alignment
				
			// }
		}


		private static Line[] SplitLines(string text, SKPaint paint, double maxWidth)
		{
			var spaceWidth = paint.MeasureText(" ");
			var lines = text.Split('\n');

			return lines.SelectMany((line) =>
			{
				var result = new List<Line>();

				var words = line.Split(new[] { " " }, StringSplitOptions.None);

				var lineResult = new StringBuilder();
				float width = 0;
				foreach (var word in words)
				{
					var wordWidth = paint.MeasureText(word);
					var wordWithSpaceWidth = wordWidth + spaceWidth;
					var spaceWithWord = " " + word;

					if ((width == 0 ? wordWidth : width + wordWithSpaceWidth) > maxWidth)
					{
						result.Add(new Line() { Text = lineResult.ToString(), WidthPixels = width });
						lineResult.Clear();
						width = 0;
					}

					if (lineResult.Length == 0)
					{
						lineResult.Append(word);
						width = wordWidth;
					}
					else
					{
						lineResult.Append(spaceWithWord);
						width += wordWithSpaceWidth;
					}
				}

				if (lineResult.Length > 0)
					result.Add(new Line() { Text = lineResult.ToString(), WidthPixels = width });

				return result;
			}).ToArray();
		}
		
		
		private class Line
		{
			public string Text { get; set; }

			public float WidthPixels { get; set; }
		}

		public void Dispose()
		{
			_cachedPaint?.Dispose();
			_cachedPaint = null;
		}
	}
//	*/

}
