using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Shapes;
using System.Reflection;

namespace Xamarin.Forms.DataGrid
{
	internal sealed class NGDataGridViewRow : NGDataGridViewItem
	{
		#region Fields

		#endregion


		public NGDataGridViewRow(NGDataGrid dg) : base(dg)
		{ }


		#region properties

		private bool IsItemSelected => ItemInfo?.Selected ?? false;

		#endregion


		#region Bindable Properties

		public static readonly BindableProperty RowBorderColorProperty =
			BindableProperty.Create(nameof(RowBorderColor), typeof(Color), typeof(NGDataGridViewRow), Color.Transparent);

		public Color RowBorderColor
		{
			get => (Color)GetValue(RowBorderColorProperty);
			set => SetValue(RowBorderColorProperty, value);
		}

		#endregion


		#region Layout

		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			if (!NeedsLayout)
				return;

			SetNeedsLayout(false);

			base.LayoutChildren(x, y, width, height);


			Debug.WriteLine($"Row Layout {x},{y} {width},{height}");

			var g = DataGrid;

			var cy = y;
			var ch = (double)g.RowHeight;

			var boxRect = new Rectangle(x, y, width, height);

			var gridLineVisibility = DataGrid.GridLinesVisibility;
			var showVerticalLines = gridLineVisibility == GridLineVisibility.Vertical || gridLineVisibility == GridLineVisibility.Both;
			var showHorizontalLines = gridLineVisibility == GridLineVisibility.Horizontal || gridLineVisibility == GridLineVisibility.Both;

			//show the grid line and adjust child layout area
			if (showHorizontalLines)
			{
				//				cy += DataGrid.GridLineWidth;
				//				ch -= DataGrid.GridLineWidth;
			}

			foreach (View c in Children)
			{
				if (!c.IsVisible)
					continue;

				if (c is NGDataGridViewCell cellView)
				{
					var colIndex = cellView.Column.ColumnIndex;

					var cw = Math.Ceiling(g.GetComputedColumnWidth(colIndex));
					var cx = x + Math.Ceiling(g.GetComputedColumnStart(colIndex));
					var r = new Rectangle(cx, cy, cw, ch);

					var gridLine = (View)cellView.Children[1];

					//adjust for gridLine
					if (colIndex == 0 || !showVerticalLines)
					{
						gridLine.IsVisible = false;
						cellView.Content.Margin = 0;
					}
					else
					{
						gridLine.IsVisible = true;
						cellView.Content.Margin = new Thickness(gridLine.WidthRequest, 0, 0, 0);
					}

					if (c.Width != cw || c.Height != ch)
						c.Layout(r);
				}
			}
		}

		#endregion


		#region Methods
		protected override void CreateView()
		{
			HeightRequest = DataGrid.RowHeight;

			var i = 0;
			foreach (var col in DataGrid.Columns)
			{
				var colIndex = i++;

				View content;

				if (col.CellTemplate != null)
				{
					content = col.CellTemplate.CreateContent() as View ?? new Label { Text = "Failed to create cell template." };

					content.VerticalOptions = LayoutOptions.Fill;
					content.HorizontalOptions = LayoutOptions.Fill;

					if (col.PropertyName != null)
					{
						content.SetBinding(BindingContextProperty,
							new Binding(col.PropertyName, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
					}
				}
				else
				{
					// var text = new Label
					if (true)
					{
						var text = new SKLabel
						{
							HorizontalTextAlignment = col.HorizontalContentAlignment.ToTextAlignment(),
							VerticalTextAlignment = col.VerticalContentAlignment.ToTextAlignment(),
							LineBreakMode = LineBreakMode.WordWrap,
							Padding = 2
						};
						text.SetBinding(SKLabel.TextProperty, new Binding(col.PropertyName, BindingMode.Default, stringFormat: col.StringFormat, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
						text.SetBinding(SKLabel.FontSizeProperty, new Binding(NGDataGrid.FontSizeProperty.PropertyName, BindingMode.Default, source: DataGrid));
						text.SetBinding(SKLabel.FontFamilyProperty, new Binding(NGDataGrid.FontFamilyProperty.PropertyName, BindingMode.Default, source: DataGrid));

						//bind text color
						//text.SetBinding(SKLabel.TextColorProperty, new Binding(nameof(ItemForegroundColor), BindingMode.OneWay, source: this));

						content = text;
					}
					else
					{
						var text = new Label
						{
							HorizontalTextAlignment = col.HorizontalContentAlignment.ToTextAlignment(),
							VerticalTextAlignment = col.VerticalContentAlignment.ToTextAlignment(),
							LineBreakMode = LineBreakMode.WordWrap,
							Padding = 2
						};
						text.SetBinding(Label.TextProperty, new Binding(col.PropertyName, BindingMode.Default, stringFormat: col.StringFormat, converter: col.PropertyConverter, converterParameter: col.PropertyConverterParameter));
						text.SetBinding(Label.FontSizeProperty, new Binding(NGDataGrid.FontSizeProperty.PropertyName, BindingMode.Default, source: DataGrid));
						text.SetBinding(Label.FontFamilyProperty, new Binding(NGDataGrid.FontFamilyProperty.PropertyName, BindingMode.Default, source: DataGrid));

						//bind text color
						//text.SetBinding(Label.TextColorProperty, new Binding(nameof(ItemForegroundColor), BindingMode.OneWay, source: this));

						content = text;
					}
				}

				//bind content background as row background color
				// content.SetBinding(BackgroundColorProperty, new Binding(nameof(RowBackgroundColor), BindingMode.OneWay, source: this));
				//content.BackgroundColor = ItemBackgroundColor;

				var cell = CreateCellView();
				cell.Column = col;
				cell.Content = content;
				cell.IsFromTemplate = col.CellTemplate != null;

				InternalChildren.Add(cell);
			}

			//SetNeedsLayout();
			//InvalidateBackground();
		}


		private NGDataGridViewCell CreateCellView()
		{
			var cell = new NGDataGridViewCell();

			//add vertical grid line
			BoxView vline = new BoxView();

			vline.SetBinding(WidthRequestProperty, new Binding(nameof(DataGrid.GridLineWidth), BindingMode.OneWay, source: DataGrid));
			vline.SetBinding(BackgroundColorProperty, new Binding(nameof(DataGrid.GridLineColor), BindingMode.OneWay, source: DataGrid));
			vline.HorizontalOptions = LayoutOptions.Start;

			cell.Children.Add(vline);

			return cell;
		}



		protected override void OnUpdateColors()
		{
			// if (!updateNeeded)
			// 	return;

			if (ItemIndex > -1)
			{
				var ItemBackgroundColor = IsItemSelected
					? DataGrid.SelectionColor
					: DataGrid.RowsBackgroundColorPalette.GetColor(ItemIndex, BindingContext);

				var ItemForegroundColor = DataGrid.RowsTextColorPalette.GetColor(ItemIndex, BindingContext);

				//				ChangeChildrenColors();

				//CellStyle

				var shouldQuery = DataGrid.ShouldQueryCellStyle();

				foreach (var child in Children)
				{
					if (child is NGDataGridViewCell cell)
					{
						var bg = ItemBackgroundColor;
						var fg = ItemForegroundColor;

						if (shouldQuery)
						{
							//todo: add lookup of cell value for style, or remove it if user can access it from RowData
							var style = DataGrid.NotifyQueryCellStyle(cell.Column, ItemInfo, null);
							if (style != null)
							{
								if (!style.BackgroundColor.IsDefault)
									bg = style.BackgroundColor;

								if (!style.ForegroundColor.IsDefault)
									fg = style.ForegroundColor;
							}
						}

						cell.Content.BackgroundColor = bg;

						if (!cell.IsFromTemplate)
						{
							if (cell.Content is SKLabel label)
								label.TextColor = fg;
							else if (cell.Content is Label label2)
								label2.TextColor = fg;
						}
					}
				}


				// updateNeeded = false;
			}
		}

		#endregion

		#region Selection

		protected override void OnTapped()
		{
			DataGrid.Container.SelectRow(ItemInfo);
		}

		#endregion

	}



	// static class LayoutOptionsExtensions
	// {
	// 	public static TextAlignment ToTextAlignment(this LayoutOptions layoutOption)
	// 	{
	// 		switch (layoutOption.Alignment)
	// 		{
	// 			case LayoutAlignment.Fill:
	// 			case LayoutAlignment.Center:
	// 				return TextAlignment.Center;
	// 			
	// 			case LayoutAlignment.Start:
	// 				return TextAlignment.Start;
	//
	// 			case LayoutAlignment.End:
	// 				return TextAlignment.End;
	// 			
	// 			default:
	// 				return TextAlignment.Center;
	// 		}
	// 	}
	// }

}
