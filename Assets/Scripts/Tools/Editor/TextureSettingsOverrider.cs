using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.AssetProcessingTools
{
	public class TextureSettingsOverrider : AssetPostprocessor
	{
		public static string[] ArtAssets = new string[] {
			"Assets/Art",
			"Assets/ArtExternal"
		};

		public static readonly Dictionary<BuildTargetGroup, string> PlatformNames = new Dictionary<BuildTargetGroup, string>() {

			{BuildTargetGroup.Unknown, "DefaultTexturePlatform"},
			{BuildTargetGroup.Standalone, BuildTargetGroup.Standalone.ToString()},
			{BuildTargetGroup.WSA, "Windows Store Apps"},	// Cause why not...
			{BuildTargetGroup.Stadia, BuildTargetGroup.Stadia.ToString()},
			{BuildTargetGroup.XboxOne, BuildTargetGroup.XboxOne.ToString()},
			{BuildTargetGroup.PS4, BuildTargetGroup.PS4.ToString()},
			{BuildTargetGroup.Android, BuildTargetGroup.Android.ToString()},
			{BuildTargetGroup.iOS, "iPhone"}, // Cause why not...
		};

		private static HashSet<string> m_PendingPaths = new HashSet<string>();

		public static bool IsGameAsset(string assetPath)
		{
			if (assetPath.Contains("/Editor/"))
				return false;

			return ArtAssets.Any(p => assetPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
		}

		[MenuItem("Platforms/Texture Overrider/Flush All Textures")]
		public static void ProcessAllTexturesPrompt()
		{
			if (!EditorUtility.DisplayDialog("Flush ALL Textures?", "Are you sure you want to flush ALL texture settings?!\nThis will cause reimport of textures that have changes and will take time.", "Yes, DO IT!", "No"))
				return;

			ProcessAllTextures();
		}

		public static double ProcessAllTextures()
		{
			var texturePaths = AssetDatabase.FindAssets("t:Texture", ArtAssets)
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) == -1)
				.ToArray();

			return ProcessTextures(texturePaths);
		}

		[MenuItem("Platforms/Texture Overrider/Flush Selected Textures")]
		public static void ProcessSelectedTextures()
		{
			var texturePaths = Selection.objects
					.OfType<Texture>()
					.Select(AssetDatabase.GetAssetPath)
					.Where(path => path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) == -1)
					.ToArray()
				;

			ProcessTextures(texturePaths);
		}

		public static double ProcessTextures(IList<string> texturePaths)
		{
			double crunchingTime = 0;
			/*
			for (int i = 0; i < texturePaths.Count; ++i) {
				var assetPath = texturePaths[i];

				if (!IsGameAsset(assetPath))	// Just in case...
					continue;

				var cancel = EditorUtility.DisplayCancelableProgressBar("Flushing Textures...", $"{assetPath}", (float)i / texturePaths.Count);
				if (cancel)
					break;

				var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
				if (textureImporter == null)
					continue;

				DoOverride(textureImporter);

				// This will write down the changes (if any) to the texture meta file.
				// AssetDatabase.Refresh() will detect that change (if any) and reimport the asset.
				AssetDatabase.WriteImportSettingsIfDirty(assetPath);


				// Crunching all textures in one go allocates a lot of memory. Try to make crunch it in chunks.
				if (i % 1000 == 0 && i != 0) {
					var partialStartTime = EditorApplication.timeSinceStartup;
					AssetDatabase.Refresh();
					crunchingTime += EditorApplication.timeSinceStartup - partialStartTime;

					EditorUtility.UnloadUnusedAssetsImmediate(true);
					UnityEngine.Scripting.GarbageCollector.CollectIncremental((ulong)30 * 1000 * 1000 * 1000);
				}
			}

			EditorUtility.ClearProgressBar();

			var startTime = EditorApplication.timeSinceStartup;
			AssetDatabase.Refresh();
			crunchingTime += EditorApplication.timeSinceStartup - startTime;
			*/
			return crunchingTime;
		}



		public void OnPreprocessTexture()
		{
			// TODO: When the time comes, release the kraken.
			if (Time.frameCount == -1)
				return;

			// This is probably editor start up.
			if (Time.frameCount == 0) {
				// Flush texture on next update as not all policies may have loaded, leading to wrong policies being applied.
				// NOTE: While flushing delayed textures, this method will get called again, so don't re-subscribe if already delayed.
				if (m_PendingPaths.Add(assetPath)) {
					EditorApplication.update -= ProcessPendingOnUpdate;
					EditorApplication.update += ProcessPendingOnUpdate;
				}

				return;
			}

			if (!IsGameAsset(assetPath))
				return;

			var textureImporter = assetImporter as TextureImporter;
			if (textureImporter == null)
				return;

			DoOverride(textureImporter);
		}

		private static void ProcessPendingOnUpdate()
		{
			EditorApplication.update -= ProcessPendingOnUpdate;
			Debug.Log($"Delayed processing of textures on startup: {m_PendingPaths.Count}");
			ProcessTextures(m_PendingPaths.ToList());
			m_PendingPaths.Clear();
		}



		public static void DoOverride(TextureImporter textureImporter)
		{
			if (!IsGameAsset(textureImporter.assetPath))
				return;

			// Per platform settings.
			//ApplyPlatformSettings(textureImporter, BuildTargetGroup.Standalone);
			//ApplyPlatformSettings(textureImporter, BuildTargetGroup.Stadia);
			//ApplyPlatformSettings(textureImporter, BuildTargetGroup.XboxOne);
			//ApplyPlatformSettings(textureImporter, BuildTargetGroup.PS4);
			ApplyPlatformSettings(textureImporter, BuildTargetGroup.Android);
			ApplyPlatformSettings(textureImporter, BuildTargetGroup.iOS);

			// NOTE: WSA logs some errors like this:
			// Selected texture format 'RGB(A) Compressed BC7' for platform 'Windows Store Apps' is not valid with the current texture type 'Default'.
			// This is a bug in Unity but they said it should work anyway.
			// Skip in batch mode so we don't spam the build logs with hundreds of false-positive errors.
			//if (!Application.isBatchMode) {
			//	ApplyPlatformSettings(textureImporter, BuildTargetGroup.WSA, ssTextureType, policySettings);
			//}
		}

		private static void ApplyPlatformSettings(TextureImporter textureImporter, BuildTargetGroup platform)
		{
			// TODO: Don't do sprites for now as UI will get ugly, although VFX will use them as well. Check which case this is.
			if (textureImporter.textureType == TextureImporterType.Sprite)
				return;

			string platformString = PlatformNames[platform];
			TextureImporterPlatformSettings platformSettings = textureImporter.GetPlatformTextureSettings(platformString);

			TextureImporterFormat chosenOne = textureImporter.GetAutomaticFormat(platformString);

			if (!platformSettings.overridden)
			{
				// If Unity doesn't have the plugin installed for this platform, automatic is returned.
				// If no overrides found and no plugin installed, don't add anything since it won't be much valid.
				if (chosenOne == TextureImporterFormat.Automatic) {
					//Debug.Log($"No {platformString} platform plugin installed. Ignore platform settings for \"{textureImporter.assetPath}\"");
					return;
				}
			}

			//TextureImporterPlatformSettings defaultSettings = textureImporter.GetPlatformTextureSettings(PlatformNames[BuildTargetGroup.Unknown]);

			bool isMobile =
				platform == BuildTargetGroup.Android ||
				platform == BuildTargetGroup.iOS;

			if (isMobile) {
				platformSettings.overridden = true;
				platformSettings.maxTextureSize = 1024;
			}

			textureImporter.SetPlatformTextureSettings(platformSettings);
		}
	}
}
