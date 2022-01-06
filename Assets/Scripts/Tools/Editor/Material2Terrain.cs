using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.AssetProcessingTools
{
	public static class Material2Terrain
	{
		[MenuItem("Assets/Convert Selected/Material to Terrain Layer")]
		public static void ProcessSelectedTextures()
		{
			const string TargetFolderName = "_TerrainLayersConverted";
			if (!Directory.Exists($"Assets/{TargetFolderName}")) {
				AssetDatabase.CreateFolder("Assets", TargetFolderName);
			}

			ConvertMaterial2Terrain(Selection.objects.OfType<Material>(), $"Assets/{TargetFolderName}");
		}

		public static void ConvertMaterial2Terrain(IEnumerable<Material> materials, string destinationFolder, string prefix = "", string suffix = "")
		{
			destinationFolder = destinationFolder.TrimEnd('/', '\\');

			foreach(Material material in materials) {

				string targetPath = $"{destinationFolder}/{prefix}{material.name}{suffix}.terrainlayer";
				if (File.Exists(targetPath)) {
					Debug.LogError($"Trying to create \"{targetPath}\" layer, but that one already exists.\nMaterial \"{AssetDatabase.GetAssetPath(material)}\"", AssetDatabase.LoadMainAssetAtPath(targetPath));
					continue;
				}

				var layer = new TerrainLayer();

				layer.diffuseTexture = (Texture2D) (material.GetTexture("_MainTex") ?? material.GetTexture("_BaseMap"));
				layer.normalMapTexture = (Texture2D) material.GetTexture("_BumpMap");
				layer.tileOffset = material.mainTextureOffset;
				layer.tileSize = material.mainTextureScale;

				AssetDatabase.CreateAsset(layer, targetPath);
			}

			AssetDatabase.SaveAssets();
		}
	}
}
