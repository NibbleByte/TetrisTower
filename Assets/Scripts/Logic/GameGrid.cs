using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Core;

namespace TetrisTower.Logic
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public struct GridCoords : IEquatable<GridCoords>
	{
		public int Row;
		public int Column;

		public GridCoords(GridCoords coords) { Row = coords.Row; Column = coords.Column; }
		public GridCoords(int row, int column) { Row = row; Column = column; }

		public static float Distance(GridCoords a, GridCoords b) =>
			UnityEngine.Mathf.Sqrt((a.Row - b.Row) * (a.Row - b.Row) + (a.Column - b.Column) * (a.Column - b.Column));


		public void WrapAround(int rows, int columns)
		{
			Row = MathUtils.WrapValue(Row, rows);
			Column = MathUtils.WrapValue(Column, columns);
		}

		public void WrapAround(GameGrid grid)
		{
			Row = MathUtils.WrapValue(Row, grid.Rows);
			Column = MathUtils.WrapValue(Column, grid.Columns);
		}

		public void WrapRow(int rows) => Row = MathUtils.WrapValue(Row, rows);
		public void WrapRow(GameGrid grid) => Row = MathUtils.WrapValue(Row, grid.Rows);
		public void WrapColumn(int columns) => Column = MathUtils.WrapValue(Column, columns);
		public void WrapColumn(GameGrid grid) => Column = MathUtils.WrapValue(Column, grid.Columns);

		public bool IsInside(GameGrid grid)
		{
			return 0 <= Row && Row < grid.Rows && 0 <= Column && Column < grid.Columns;
		}

		public static GridCoords operator +(GridCoords a, GridCoords b) => new GridCoords(a.Row + b.Row, a.Column + b.Column);
		public static GridCoords operator -(GridCoords a, GridCoords b) => new GridCoords(a.Row - b.Row, a.Column - b.Column);
		public static bool operator ==(GridCoords c1, GridCoords c2) => c1.Equals(c2);
		public static bool operator !=(GridCoords c1, GridCoords c2) => !c1.Equals(c2);

		public bool Equals(GridCoords other) => Row == other.Row && Column == other.Column;
		public override int GetHashCode() => Row ^ Column;
		public override bool Equals(object obj)
		{
			if (obj is GridCoords other) {
				return Equals(other);
			}
			return false;
		}

		public static GridCoords Zero => new GridCoords();

		public override string ToString()
		{
			return $"({Row},{Column})";
		}

		// Useful for algorithms.
		public static int GetRow(GridCoords coords) => coords.Row;
		public static int GetColumn(GridCoords coords) => coords.Column;
	}

	public interface GameGrid
	{
		int Rows { get; }
		int Columns { get; }

		IEnumerator ApplyActions(IEnumerable<GridAction> actions);
	}

	public interface GridAction
	{
	}

	public static class GameGridExtensions
	{
		public static int WrappedColumn(this GameGrid grid, int column) => MathUtils.WrapValue(column, grid.Columns);

		public static GridCoords WrappedCoords(this GameGrid grid, GridCoords coords) => new GridCoords(coords.Row, MathUtils.WrapValue(coords.Column, grid.Columns));

		public static IEnumerable<GridCoords> EnumerateAllCoords(this GameGrid grid)
		{
			for (int row = 0; row < grid.Rows; ++row) {
				for (int column = 0; column < grid.Columns; ++column) {
					yield return new GridCoords(row, column);
				}
			}
		}
	}


	public class PlaceAction : GridAction
	{
		public GridCoords PlaceCoords;
		public BlocksShape PlacedShape;
	}

	public class ClearMatchedAction : GridAction
	{
		public IReadOnlyList<GridCoords> Coords;
		public MatchScoringType MatchedType;
		public bool SpecialMatch = false;	// Match by a special block, not a normal match. Important for scoring.
	}

	public class MoveCellsAction : GridAction
	{
		public IReadOnlyList<KeyValuePair<GridCoords, GridCoords>> MovedCells;
	}

	public class PushUpCellsAction : GridAction
	{
		public IReadOnlyList<KeyValuePair<int, BlockType>> PushBlocks;
	}

	public class ReplaceCellsAction : GridAction
	{
		public IReadOnlyList<KeyValuePair<GridCoords, BlockType>> ReplacePairs;
	}

	public class EvaluationSequenceFinishAction : GridAction
	{

	}
}