using UnityEditor;
using UnityEngine;

namespace UnityTools.AssetProcessingTools
{
	public static class TextMeshProMigrate
	{
		/// <summary>
		/// Migrate legacy UI Text to TextMeshPro.
		/// </summary>
		[MenuItem("Tools/TextMeshPro Migrate")]
		static void MigrateToTMP()
		{
			foreach (GameObject go in Selection.gameObjects) {
				var t = go.GetComponent<UnityEngine.UI.Text>();

				string text = t.text;
				float size = t.fontSize;
				Color color = t.color;
				bool bold = t.fontStyle == FontStyle.Bold;

				TextAnchor alignment = t.alignment;

				GameObject.DestroyImmediate(t);
				var tmpro = go.AddComponent<TMPro.TextMeshProUGUI>();
				tmpro.text = text;
				tmpro.fontSize = size;
				tmpro.color = color;
				tmpro.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;

				tmpro.alignment = alignment switch {
					TextAnchor.UpperLeft => TMPro.TextAlignmentOptions.TopLeft,
					TextAnchor.UpperCenter => TMPro.TextAlignmentOptions.Top,
					TextAnchor.UpperRight => TMPro.TextAlignmentOptions.TopRight,
					TextAnchor.MiddleLeft => TMPro.TextAlignmentOptions.Left,
					TextAnchor.MiddleCenter => TMPro.TextAlignmentOptions.Center,
					TextAnchor.MiddleRight => TMPro.TextAlignmentOptions.Right,
					TextAnchor.LowerLeft => TMPro.TextAlignmentOptions.BottomLeft,
					TextAnchor.LowerCenter => TMPro.TextAlignmentOptions.Bottom,
					TextAnchor.LowerRight => TMPro.TextAlignmentOptions.BottomRight,
					_ => throw new System.NotSupportedException(alignment.ToString()),
				};

				EditorUtility.SetDirty(tmpro);
			}
		}
	}
}
