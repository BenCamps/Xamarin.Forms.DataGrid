using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.DataGrid
{
	public partial class NGDataGrid
	{

		internal double ComputedColumnsWidth = -1;

		internal void InvalidateColumnsWidth()
		{
			ComputedColumnsWidth = -1;
		}

		internal double GetComputedColumnWidth(int index)
		{
			if (index < Columns.Count)
				return Columns[index].ComputedWidth;

			return -1;
		}

		internal double GetComputedColumnStart(int index)
		{
			if (index < Columns.Count)
				return Columns[index].ComputedX;

			return -1;
		}



		private void ComputeColumnsWidth()
		{
			if (ComputedColumnsWidth > -1 || Width <= 0)
				return;

			var totalWidth = 0d;
			var starColumns = 0;
			var starUnits = 0;
			var starMinWidth = 0d;

			foreach (var column in Columns)
			{
				var gl = column.Width;

				if (gl.IsAbsolute)
				{
					column.ComputedWidth = gl.Value;
					totalWidth += column.ComputedWidth;
				}
				//todo: for auto add text measure feature (platform and font specific, very slow)
				else if (gl.IsAuto || gl.IsStar)
				{
					starColumns++;
					starUnits += (int)gl.Value;
					starMinWidth += column.MinimumWidth;
				}
			}

			var remainingWidth = Width - totalWidth;

			//if we are out of remaining width, default columns to 100
			// if (remainingWidth < starMinWidth)
			//  remainingWidth = starColumns * 100;

			DataGridColumn prevColumn = null;
			var x = 0d;
			//distribute and compute x
			foreach (var column in Columns)
			{
				var gl = column.Width;

				if (!gl.IsAbsolute)
				{
					var w = Math.Max(column.MinimumWidth, Math.Floor((remainingWidth / starUnits) * gl.Value));
					column.ComputedWidth = w;
					totalWidth += column.ComputedWidth;
				}

				column.ComputedX = x;
				x += column.ComputedWidth;
				column.HeaderLabel.WidthRequest = column.ComputedWidth;
			}

			ComputedColumnsWidth = totalWidth;
		}


		private void OnColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateColumnsWidth();
			//CreateHeaderView();
		}


		private void CreateHeaderView()
		{
			if (ComputedColumnsWidth > -1)
				return;

			SetColumnsBindingContext();

			ComputeColumnsWidth();


			var hv = HeaderView as StackLayout;
			hv.DisableLayout = true;
			hv.Children.Clear();
			//HeaderView.ColumnDefinitions.Clear();
			_sortingOrders.Clear();

			//HeaderView.Padding = 0;//new Thickness(BorderThickness.Left, BorderThickness.Top, BorderThickness.Right, 0);
			//HeaderView.ColumnSpacing = 0; //BorderThickness.HorizontalThickness / 2;
			hv.Orientation = StackOrientation.Horizontal;
			hv.Spacing = 0;
			hv.Padding = 0;

			foreach (var col in Columns)
			{
				//HeaderView.ColumnDefinitions.Add(new ColumnDefinition { Width = col.Width });

				var cell = CreateHeaderViewForColumn(col);

				hv.Children.Add(cell);
				//Grid.SetColumn(cell, Columns.IndexOf(col));

				_sortingOrders.Add(Columns.IndexOf(col), SortingOrder.None);
			}

			hv.DisableLayout = false;
			hv.ForceLayout();
		}

		//this is not needed since the BindingContext is automatically inherited by children
		private void SetColumnsBindingContext()
		{
			Columns?.ForEach(c => c.BindingContext = BindingContext);
		}


		private View CreateHeaderViewForColumn(DataGridColumn column)
		{
			// column.HeaderLabel.Style = column.HeaderLabelStyle ?? this.HeaderLabelStyle ?? (Style)HeaderView.Resources["HeaderDefaultStyle"];
			//
			// Grid grid = new Grid
			// {
			//  ColumnSpacing = 0,
			// };
			//
			// grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			// grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
			//
			// if (IsSortable)
			// {
			//  column.SortingIcon.Style = (Style)HeaderView.Resources["ImageStyleBase"];
			//
			//  grid.Children.Add(column.SortingIcon);
			//  Grid.SetColumn(column.SortingIcon, 1);
			//
			//  TapGestureRecognizer tgr = new TapGestureRecognizer();
			//  tgr.Tapped += (s, e) =>
			//  {
			//   int index = Columns.IndexOf(column);
			//   SortingOrder order = _sortingOrders[index] == SortingOrder.Ascendant ? SortingOrder.Descendant : SortingOrder.Ascendant;
			//
			//   if (Columns.ElementAt(index).SortingEnabled)
			//    SortedColumnIndex = new SortData(index, order);
			//  };
			//  grid.GestureRecognizers.Add(tgr);
			// }
			//
			// grid.Children.Add(column.HeaderLabel);
			//
			// return grid;

			return column.HeaderLabel;
		}

	}
}