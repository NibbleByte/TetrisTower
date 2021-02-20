using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TetrisTower.Logic
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public struct GridCoords : IEquatable<GridCoords>
	{
		public int Row;
		public int Column;

		public GridCoords(int row, int column) { Row = row; Column = column; }

		public static float Distance(GridCoords a, GridCoords b) =>
			UnityEngine.Mathf.Sqrt((a.Row - b.Row) * (a.Row - b.Row) + (a.Column - b.Column) * (a.Column - b.Column));

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

		IEnumerator PlaceShape(GridCoords placedCoords, BlocksShape placedShape);

		IEnumerator ClearMatchedCells(IReadOnlyCollection<GridCoords> coords);

		IEnumerator MoveCells(IReadOnlyCollection<KeyValuePair<GridCoords, GridCoords>> movedCells);
	}

	public interface GridAction
	{
		IEnumerator Apply(GameGrid grid);
	}

	// Implement this if you need to pre-process the actions in some way (for example: group them by type)
	public interface GridActionsTransformer
	{
		IEnumerable<GridAction> Transform(IEnumerable<GridAction> actions);
	}


	public class PlaceAction : GridAction
	{
		public GridCoords PlaceCoords;
		public BlocksShape PlacedShape;

		public IEnumerator Apply(GameGrid grid) { yield return grid.PlaceShape(PlaceCoords, PlacedShape); }
	}

	public class ClearMatchedAction : GridAction
	{
		public IReadOnlyCollection<GridCoords> Coords;

		public IEnumerator Apply(GameGrid grid) { yield return grid.ClearMatchedCells(Coords); }
	}

	public class MoveCellsAction : GridAction
	{
		public IReadOnlyCollection<KeyValuePair<GridCoords, GridCoords>> MovedCells;

		public IEnumerator Apply(GameGrid grid) { yield return grid.MoveCells(MovedCells);}
	}
}