using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.AssetProcessingTools
{
	public class SimplePrefabReplacer : EditorWindow
	{
		[SerializeField]
		private GameObject PrefabReplacement;

		[MenuItem("Tools/Simple Prefab Replacer")]
		public static void ShowWindow()
		{
			GetWindow<SimplePrefabReplacer>("Simple Prefab Replacer");
		}

		private void OnGUI()
		{
			PrefabReplacement = (GameObject) EditorGUILayout.ObjectField(PrefabReplacement, typeof(GameObject), false);

			if (GUILayout.Button("Replace!")) {
				GameObject[] objects = Selection.gameObjects;
				Array.Sort(objects, (l, r) => l.transform.GetSiblingIndex().CompareTo(r.transform.GetSiblingIndex()));

				foreach(GameObject go in objects) {
					GameObject instance = (GameObject) PrefabUtility.InstantiatePrefab(PrefabReplacement);

					instance.transform.SetParent(go.transform.parent);
					instance.transform.position = go.transform.position;
					instance.transform.rotation = go.transform.rotation;
					instance.transform.localScale = go.transform.localScale;

					instance.name = go.name.Replace("(Clone)", "", StringComparison.OrdinalIgnoreCase);

					EditorUtility.SetDirty(instance.transform.parent);

					GameObject.DestroyImmediate(go);
				}
			}
		}
	}
}
