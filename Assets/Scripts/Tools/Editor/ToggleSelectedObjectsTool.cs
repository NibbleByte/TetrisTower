using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.AssetProcessingTools
{
	public static class ToggleSelectedObjectsTool
	{
		[MenuItem("Tools/Toggle Selected Objects &t")]
		public static void ToggleSelectedObjects()
		{
			foreach(var go in Selection.gameObjects) {
				go.SetActive(!go.activeSelf);
			}
		}
	}
}
