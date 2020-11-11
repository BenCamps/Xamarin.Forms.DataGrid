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
	internal sealed class NGDataGridViewGroup : NGDataGridViewItem
	{
		#region Fields
		
		#endregion


		public NGDataGridViewGroup(NGDataGrid dg) : base(dg)
		{
		}

		
		#region properties

		private ItemGroup GroupInfo => (ItemGroup) ItemInfo;
		
		private bool IsExpanded => GroupInfo?.Expanded ?? false;
		
		#endregion

		
		#region Bindable Properties
		
		#endregion


		#region Layout
		
		protected override void LayoutChildren(double x, double y, double width, double height)
		{
			if (!NeedsLayout)
				return;

			SetNeedsLayout(false);

			Debug.WriteLine($"Group Layout {x},{y} {width},{height}");

			var g = DataGrid;
			
			var boxRect = new Rectangle(x, y, width, height);
			
			foreach (View c in Children)
			{
				if (!c.IsVisible)
					continue;

				LayoutChildIntoBoundingRegion(c, boxRect);
			}
		}
		
		#endregion


		#region Methods
		protected override void CreateView()
		{
			HeightRequest = DataGrid.RowHeight;

			var text = new Label
			{
				HorizontalTextAlignment = TextAlignment.Start,
				VerticalTextAlignment = TextAlignment.Center,
				LineBreakMode = LineBreakMode.NoWrap,
				Padding = 2
			};
			text.SetBinding(Label.TextProperty, new Binding(nameof(ObjectGroup.Text), BindingMode.OneWay));
			text.SetBinding(Label.FontSizeProperty, new Binding(NGDataGrid.FontSizeProperty.PropertyName, BindingMode.Default, source: DataGrid));
			text.SetBinding(Label.FontFamilyProperty, new Binding(NGDataGrid.FontFamilyProperty.PropertyName, BindingMode.Default, source: DataGrid));

			//bind text color
			text.SetBinding(Label.TextColorProperty, new Binding(nameof(ItemForegroundColor), BindingMode.OneWay, source: this));

			View content = text;

			//bind content background as row background color
			content.BackgroundColor = ItemBackgroundColor;

			var cell = CreateCellView();
			// cell.Column = col;
			cell.Content = content;
			cell.IsFromTemplate = false; // col.CellTemplate != null;

			InternalChildren.Add(cell);
		}

		
		private NGDataGridViewCell CreateCellView()
		{
			var cell = new NGDataGridViewCell();
			
			return cell;
		}


		
		protected override void OnUpdateColors()
		{
			// if (!updateNeeded)
			// 	return;

			if (ItemIndex > -1)
			{
				// ItemBackgroundColor = IsItemSelected
				// 	? DataGrid.SelectionColor
				// 	: DataGrid.RowsBackgroundColorPalette.GetColor(ItemIndex, BindingContext);
				
				ItemBackgroundColor = DataGrid.GroupBackgroundColor;
				ItemForegroundColor = DataGrid.GroupForegroundColor;


				//CellStyle

				foreach (var child in Children)
				{
					if (child is NGDataGridViewCell cell)
					{
						var bg = ItemBackgroundColor;
						var fg = ItemForegroundColor;
						
						cell.Content.BackgroundColor = bg;

						if (!cell.IsFromTemplate)
						{
							var label = (Label)cell.Content;
							label.TextColor = fg;
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
			DataGrid.Container.ToggleGroup(GroupInfo);
		}

		#endregion
		
	}
}
