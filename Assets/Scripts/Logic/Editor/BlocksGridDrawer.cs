using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TetrisTower.Logic
{
	[CustomPropertyDrawer(typeof(BlocksGrid))]
	public class BlocksGridDrawer : PropertyDrawer
	{
		private bool m_Unfolded = true;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int lines = 1;  // Property Label
			if (m_Unfolded) {
				lines += property.FindPropertyRelative("m_Rows").intValue;
			}

			return lines * EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			var prefixedPos = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			var foldPos = position;
			foldPos.width = prefixedPos.x - position.x;
			foldPos.height = EditorGUIUtility.singleLineHeight;
			m_Unfolded = EditorGUI.Foldout(foldPos, m_Unfolded, "", true);

			var rowsProp = property.FindPropertyRelative("m_Rows");
			var columnsProp = property.FindPropertyRelative("m_Columns");
			var gridProp = property.FindPropertyRelative("m_Blocks");

			int rows = rowsProp.intValue;
			int columns = columnsProp.intValue;

			int prevIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Size row.
			{
				var sizePos = prefixedPos;
				sizePos.height = EditorGUIUtility.singleLineHeight;
				GUI.Label(sizePos, "Rows x Columns:");

				sizePos.x += 104f;
				sizePos.width = 40f;
				rowsProp.intValue = EditorGUI.IntField(sizePos, rowsProp.intValue);
				sizePos.x += 40;
				GUI.Label(sizePos, " x ");
				sizePos.x += 18;
				columnsProp.intValue = EditorGUI.IntField(sizePos, columnsProp.intValue);

				position.y += EditorGUIUtility.singleLineHeight;
				position.height -= EditorGUIUtility.singleLineHeight;
			}

			// Grid
			if (m_Unfolded) {

				// Rows or columns changed - resize the grid, while preserving the data inside.
				if (rows != rowsProp.intValue || columns != columnsProp.intValue) {
					rows = rowsProp.intValue;
					columns = columnsProp.intValue;
					gridProp.arraySize = rows * columns;
				}

				float rowSize = position.height / rows;
				float columnSize = position.width / columns;

				for(int row = 0; row < rows; ++row) {
					for(int column = 0; column < columns; ++column) {
						var blockProp = gridProp.GetArrayElementAtIndex(row * columns + column);
						var cellPos = new Rect(position.x + column * columnSize, position.y + (rows - row - 1) * rowSize, columnSize, rowSize);
						blockProp.enumValueFlag = (int)(BlockType) EditorGUI.EnumPopup(cellPos, (BlockType)blockProp.enumValueFlag);
					}
				}
			}

			EditorGUI.indentLevel = prevIndent;

			EditorGUI.EndProperty();
		}
	}

}