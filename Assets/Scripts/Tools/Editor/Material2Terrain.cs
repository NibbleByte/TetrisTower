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

				layer.diffuseTexture = TryGetTexture(material, "_MainTex", "_BaseMap", "_Albedo", "Albedo");
				layer.normalMapTexture = TryGetTexture(material, "_BumpMap", "_Normal", "Normal");
				layer.tileOffset = material.mainTextureOffset;
				layer.tileSize = material.mainTextureScale;

				AssetDatabase.CreateAsset(layer, targetPath);

				if (layer.diffuseTexture == null) {
					Debug.LogError($"\"{material.name}\" has no diffuse/albedo texture.", material);
				}
			}

			AssetDatabase.SaveAssets();
		}

		private static Texture2D TryGetTexture(Material material, params string[] propertyNames)
		{
			foreach(string propertyName in propertyNames) {
				if (!material.HasTexture(propertyName))
					continue;

				Texture2D texture = (Texture2D) material.GetTexture(propertyName);
				if (texture)
					return texture;
			}

			return null;
		}
	}
}
