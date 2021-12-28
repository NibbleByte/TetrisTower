using DevLocker.Tools;
using UnityEditor;
using UnityEngine;

namespace TetrisTower.Logic
{
	[CustomPropertyDrawer(typeof(GridCoords))]
	public class GridCoordsPropertyDrawer : OneLineBasePropertyDrawer
	{
		protected override PropertyDescriptor[] Properties => new[]
		{
			new PropertyDescriptor(nameof(GridCoords.Row), new GUIContent("Row"), 30f),
			new PropertyDescriptor(nameof(GridCoords.Column), new GUIContent("Column"), 50f),

		};
	}

}