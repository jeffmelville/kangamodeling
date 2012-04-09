using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KangaModeling.Graphics;
using KangaModeling.Compiler.SequenceDiagrams;
using KangaModeling.Graphics.Primitives;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace KangaModeling.Visuals.SequenceDiagrams
{
	abstract class CellAspect
	{
	}

	sealed class LifelineAspect : CellAspect
	{
		public int EnterActivationLevel { get; set; }
		public int LeaveActivationLevel { get; set; }
	}

	sealed class LifelineNameAspect : CellAspect
	{
		public string Name { get; set; }
	}

	enum FragmentType
	{
		TopLeft,
		Top,
		TopRight,
		Left,
		Right,
		BottomLeft,
		Bottom,
		BottomRight,
	}

	sealed class FragmentAspect : CellAspect
	{
		public string Title { get; set; }

		public FragmentType Type { get; set; }
	}

	enum SignalType
	{
		Undef,
		Inside,
		Start,
		End,
	}

	enum SignalDirection
	{
		RightToLeft,
		LeftToRight,
	}

	sealed class SignalAspect : CellAspect
	{
		public string Name { get; set; }

		public SignalType Type { get; set; }

		public SignalDirection Direction { get; set; }
	}

	sealed class Cell
	{
		private readonly Grid m_Grid;
		private readonly Dictionary<Type, CellAspect> m_Aspects = new Dictionary<Type, CellAspect>();

		public Cell(Grid grid)
		{
			m_Grid = grid;
		}

		public void AddAspect(CellAspect cellAspect)
		{
			m_Aspects.Add(cellAspect.GetType(), cellAspect);
		}

		public bool HasAspect<TCellAspect>()
		{
			return m_Aspects.ContainsKey(typeof(TCellAspect));
		}

		public TCellAspect GetAspect<TCellAspect>() where TCellAspect : CellAspect
		{
			return m_Aspects[typeof(TCellAspect)] as TCellAspect;
		}
	}

	sealed class Grid
	{
		private readonly List<List<Cell>> m_Rows = new List<List<Cell>>();

		public int RowCount
		{
			get { return m_Rows.Count; }
		}

		public int ColumnCount
		{
			get { return (m_Rows.Count == 0) ? 0 : m_Rows[0].Count; }
		}

		public IEnumerable<Cell> GetRow(int row)
		{
			if (RowCount == 0) { return new Cell[0]; }

			return m_Rows[row];
		}

		public IEnumerable<Cell> GetColumn(int column)
		{
			if (ColumnCount == 0) { return new Cell[0]; }

			return m_Rows.Select(row => row.Skip(column).First());
		}

		public Cell GetCell(int row, int column)
		{
			return m_Rows[row][column];
		}

		public void AddRow()
		{
			InsertRow(RowCount);
		}

		public void InsertRow(int index)
		{
			var row = new List<Cell>();
			for (int i = 0; i < ColumnCount; i++)
			{
				row.Add(new Cell(this));
			}
			m_Rows.Insert(index, row);
		}

		public void AddColumn()
		{
			InsertColumn(ColumnCount);
		}

		public void InsertColumn(int index)
		{
			foreach (var row in m_Rows)
			{
				row.Insert(index, new Cell(this));
			}
		}
	}

	public class SequenceDiagramVisual : Visual
	{
		readonly Grid m_Grid = new Grid();

		public SequenceDiagramVisual(ISequenceDiagram sequenceDiagram)
		{
			bool rowsAdded = false;

			foreach (var lifeline in sequenceDiagram.Lifelines)
			{
				if (!rowsAdded)
				{
					int rowsToAdd = lifeline.Pins.Count();
					while (--rowsToAdd >= 0)
					{
						m_Grid.AddRow();
					}

					rowsAdded = true;
				}

				m_Grid.AddColumn();
			}

			foreach (var lifeline in sequenceDiagram.Lifelines)
			{
				int lastActivationLevel = 0;
				foreach (var pin in lifeline.Pins)
				{
					var cell = m_Grid.GetCell(pin.RowIndex, lifeline.Index);

					cell.AddAspect(new LifelineAspect()
					{
						EnterActivationLevel = lastActivationLevel,
						LeaveActivationLevel = pin.Level,
					});

					lastActivationLevel = pin.Level;

					if (pin.Signal != null)
					{
						SignalDirection signalDirection = (pin.Signal.Start.LifelineIndex > pin.Signal.End.LifelineIndex)
							? SignalDirection.RightToLeft
							: SignalDirection.LeftToRight;

						SignalType signalType = SignalType.Undef;
						if (pin.Signal.Start == pin)
						{
							signalType = SignalType.Start;

							int startColumn, endColumn;
							if (signalDirection == SignalDirection.LeftToRight)
							{
								startColumn = pin.Signal.Start.LifelineIndex + 1;
								endColumn = pin.Signal.End.LifelineIndex;
							}
							else
							{
								startColumn = pin.Signal.End.LifelineIndex + 1;
								endColumn = pin.Signal.Start.LifelineIndex;
							}

							for (int column = startColumn; column < endColumn; column++)
							{
								Cell cellInside = m_Grid.GetCell(pin.RowIndex, column);
								cellInside.AddAspect(new SignalAspect()
								{
									Direction = signalDirection,
									Type = SignalType.Inside,
									Name = pin.Signal.Name,
								});
							}
						}
						else if (pin.Signal.End == pin)
						{
							signalType = SignalType.End;
						}

						cell.AddAspect(new SignalAspect()
						{
							Name = pin.Signal.Name,
							Direction = signalDirection,
							Type = signalType,
						});
					}
				}
			}

			bool nameRowsAdded = false;

			foreach (var lifeline in sequenceDiagram.Lifelines)
			{
				if (!nameRowsAdded)
				{
					m_Grid.InsertRow(0);
					m_Grid.AddRow();
					nameRowsAdded = true;
				}

				{
					var cell = m_Grid.GetCell(0, lifeline.Index);
					cell.AddAspect(new LifelineNameAspect()
					{
						Name = lifeline.Name,
					});
				}

				{
					var cell = m_Grid.GetCell(m_Grid.RowCount - 1, lifeline.Index);
					cell.AddAspect(new LifelineNameAspect()
					{
						Name = lifeline.Name,
					});
				}
			}

			InsertFragmentCells(sequenceDiagram.Root, 0, 1);
		}

		private void InsertFragmentCells(IFragment fragment, int leftOffset, int topOffset)
		{
			if (m_Grid.RowCount == 0)
			{
				return;
			}

			int topRowIndex = fragment.Top + topOffset;
			int bottomRowIndex = fragment.Bottom + 1 + topOffset;

			m_Grid.InsertRow(topRowIndex);
			m_Grid.InsertRow(bottomRowIndex);

			int leftColumnIndex = (fragment.Left != null) ? fragment.Left.Index + leftOffset : 0;
			int rightColumnIndex = (fragment.Right != null) ? fragment.Right.Index + 2 + leftOffset : 0;

			m_Grid.InsertColumn(leftColumnIndex);
			m_Grid.InsertColumn(rightColumnIndex);

			{
				Cell cell = m_Grid.GetCell(topRowIndex, leftColumnIndex);
				cell.AddAspect(new FragmentAspect()
				{
					Title = fragment.Title,
					Type = FragmentType.TopLeft,
				});
			}

			for (int column = leftColumnIndex + 1; column < rightColumnIndex; column++)
			{
				Cell cell = m_Grid.GetCell(topRowIndex, column);
				cell.AddAspect(new FragmentAspect()
				{
					Type = FragmentType.Top,
				});
				cell.AddAspect(new LifelineAspect()
				{
				});

				Cell cell2 = m_Grid.GetCell(bottomRowIndex, column);
				cell2.AddAspect(new FragmentAspect()
				{
					Type = FragmentType.Bottom,
				});
				cell2.AddAspect(new LifelineAspect()
				{
				});
			}

			foreach (var childFragment in fragment.Children)
			{
				InsertFragmentCells(childFragment, leftOffset + 1, topOffset + 1);
			}
		}

		IDictionary<int, float> RowHeights { get; set; }

		IDictionary<int, float> ColumnWidths { get; set; }

		protected override void ArrangeCore(IGraphicContext graphicContext)
		{
			RowHeights = new Dictionary<int, float>();

			for (int row = 0; row < m_Grid.RowCount; row++)
			{
				var cellsInRow = m_Grid.GetRow(row).ToArray();

				float rowHeight = cellsInRow
					.Select(c => GetRequiredHeight(c, graphicContext))
					.Max();

				RowHeights.Add(row, rowHeight);
			}

			ColumnWidths = new Dictionary<int, float>();

			for (int column = m_Grid.ColumnCount - 1; column >= 0; column--)
			{
				var cellsInColumn = m_Grid.GetColumn(column).ToArray();

				float columnWidth = cellsInColumn
					.Select(c => GetRequiredWidth(c, graphicContext))
					.Max();

				ColumnWidths.Add(column, columnWidth);
			}

			Size = new Size(
				ColumnWidths.Select(kvp => kvp.Value).Sum(),
				RowHeights.Select(kvp => kvp.Value).Sum());
		}

		private float GetRequiredWidth(Cell cell, IGraphicContext graphicContext)
		{
			float requiredWidth = 5;

			if (cell.HasAspect<LifelineNameAspect>())
			{
				var lifelineNameAspect = cell.GetAspect<LifelineNameAspect>();
				Size sizeOfName = graphicContext.MeasureText(lifelineNameAspect.Name);
				requiredWidth = sizeOfName.Width + 10;
			}

			if (cell.HasAspect<LifelineAspect>())
			{
				var lifelineAspect = cell.GetAspect<LifelineAspect>();
				requiredWidth =
					2 +
					Math.Max(lifelineAspect.EnterActivationLevel, lifelineAspect.LeaveActivationLevel) * 10;
			}

			if (cell.HasAspect<SignalAspect>())
			{
				var signalAspect = cell.GetAspect<SignalAspect>();

				if (signalAspect.Type == SignalType.End)
				{
					requiredWidth = Math.Max(requiredWidth, 10);
				}
			}

			return requiredWidth;
		}

		private float GetRequiredHeight(Cell cell, IGraphicContext graphicContext)
		{
			float requiredHeight = 5;

			if (cell.HasAspect<FragmentAspect>())
			{
				var fragmentAspect = cell.GetAspect<FragmentAspect>();
				Size titleSize = graphicContext.MeasureText(fragmentAspect.Title);
				requiredHeight = titleSize.Height + 10;
			}

			if (cell.HasAspect<LifelineNameAspect>())
			{
				var lifelineNameAspect = cell.GetAspect<LifelineNameAspect>();
				var sizeOfName = graphicContext.MeasureText(lifelineNameAspect.Name);
				requiredHeight = sizeOfName.Height;
			}

			if (cell.HasAspect<SignalAspect>())
			{
				const float arrowHeight = 20;
				var signalAspect = cell.GetAspect<SignalAspect>();
				var sizeOfName = graphicContext.MeasureText(signalAspect.Name);
				requiredHeight = Math.Max(requiredHeight, arrowHeight + sizeOfName.Height);
			}

			return requiredHeight;
		}

		private void DrawCell(Cell cell, Point location, Size size, IGraphicContext graphicContext)
		{
			if (cell.HasAspect<LifelineNameAspect>())
			{
				var lifelineNameAspect = cell.GetAspect<LifelineNameAspect>();
				var adjustedLocation = location;
				var adjustedSize = size;

				graphicContext.DrawRectangle(adjustedLocation, adjustedSize);
				graphicContext.DrawText(
					lifelineNameAspect.Name,
					HorizontalAlignment.Center,
					VerticalAlignment.Middle,
					adjustedLocation,
					adjustedSize);
			}

			if (cell.HasAspect<LifelineAspect>())
			{
				var lifelineAspect = cell.GetAspect<LifelineAspect>();
				{
					var from = new Point(location.X + size.Width / 2, location.Y);
					var to = new Point(location.X + size.Width / 2, location.Y + size.Height / 2);

					float width = 2;
					width += lifelineAspect.EnterActivationLevel * 2;

					graphicContext.DrawLine(from, to, width);
				}
				{
					var from = new Point(location.X + size.Width / 2, location.Y + size.Height / 2);
					var to = new Point(location.X + size.Width / 2, location.Y + size.Height);

					float width = 2;
					width += lifelineAspect.LeaveActivationLevel * 2;

					graphicContext.DrawLine(from, to, width);
				}
			}

			if (cell.HasAspect<SignalAspect>())
			{
				var signalAspect = cell.GetAspect<SignalAspect>();

				const float signalBottomDistance = 10;
				
				switch (signalAspect.Type)
				{
					case SignalType.Inside:
						{
							Point from = new Point(location.X, location.Y + size.Height - signalBottomDistance);
							Point to = new Point(location.X + size.Width, location.Y + size.Height - signalBottomDistance);

							graphicContext.DrawLine(from, to, 2);
						}
						break;
					case SignalType.Start:
						if (signalAspect.Direction == SignalDirection.RightToLeft)
						{
							Point from = new Point(location.X + size.Width / 2, location.Y + size.Height - signalBottomDistance);
							Point to = new Point(location.X, location.Y + size.Height - signalBottomDistance);

							graphicContext.DrawLine(from, to, 2);
						}
						else if (signalAspect.Direction == SignalDirection.LeftToRight)
						{
							Point from = new Point(location.X + size.Width, location.Y + size.Height - signalBottomDistance);
							Point to = new Point(location.X + size.Width / 2, location.Y + size.Height - signalBottomDistance);

							graphicContext.DrawLine(from, to, 2);
						}
						break;
					case SignalType.End:
						if (signalAspect.Direction == SignalDirection.RightToLeft)
						{
							Point from = new Point(location.X + size.Width, location.Y + size.Height - signalBottomDistance);
							Point to = new Point(location.X + size.Width / 2, location.Y + size.Height - signalBottomDistance);

							graphicContext.DrawArrow(from, to, 2, 7, 4);

							//graphicContext.DrawText(
							//    signalAspect.Name,
							//    HorizontalAlignment.Left, VerticalAlignment.Middle,
							//    to.Offset(+7, -size.Height / 2), graphicContext.MeasureText(signalAspect.Name));

						}
						else if (signalAspect.Direction == SignalDirection.LeftToRight)
						{
							Point from = new Point(location.X, location.Y + size.Height - signalBottomDistance);
							Point to = new Point(location.X + size.Width / 2, location.Y + size.Height - signalBottomDistance);

							graphicContext.DrawArrow(from, to, 2, 7, 4);

							//Size nameSize = graphicContext.MeasureText(signalAspect.Name);

							//graphicContext.DrawText(
							//    signalAspect.Name,
							//    HorizontalAlignment.Right, VerticalAlignment.Middle,
							//    to.Offset(-7 - nameSize.Width, -size.Height / 2), nameSize);
						}
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			if (cell.HasAspect<FragmentAspect>())
			{
				var fragmentAspect = cell.GetAspect<FragmentAspect>();
				switch (fragmentAspect.Type)
				{
					case FragmentType.Top:
					case FragmentType.Bottom:

						Point from = new Point(location.X, location.Y + size.Height / 2);
						Point to = new Point(location.X + size.Width, location.Y + size.Height / 2);
						graphicContext.DrawLine(from, to, 1);

						break;

					case FragmentType.TopLeft:
						Size titleSize = graphicContext.MeasureText(fragmentAspect.Title);

						graphicContext.DrawText(
							fragmentAspect.Title,
							HorizontalAlignment.Left,
							VerticalAlignment.Middle,
							location, titleSize);

						break;
					case FragmentType.TopRight:
						break;
					case FragmentType.Left:
						break;
					case FragmentType.Right:
						break;
					case FragmentType.BottomLeft:
						break;
					case FragmentType.BottomRight:
						break;
					default:
						break;
				}
			}

		}

		protected override void DrawCore(IGraphicContext graphicContext)
		{
			float y = 0;

			for (int row = 0; row < m_Grid.RowCount; row++)
			{
				float rowHeight = RowHeights[row];

				float x = 0;
				for (int column = 0; column < m_Grid.ColumnCount; column++)
				{
					float columnWidth = ColumnWidths[column];

					Cell cell = m_Grid.GetCell(row, column);

					graphicContext.DrawRectangle(new Point(x, y), new Size(columnWidth, rowHeight));

					DrawCell(cell, new Point(x, y), new Size(columnWidth, rowHeight), graphicContext);

					x += columnWidth;
				}

				y += rowHeight;
			}
		}
	}

}
