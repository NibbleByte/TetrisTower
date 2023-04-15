using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TetrisTower.Game.Build
{
	[InitializeOnLoad]
	public static class BuildPlayerWindowHook
	{
		static BuildPlayerWindowHook()
		{
			BuildPlayerWindow.RegisterGetBuildPlayerOptionsHandler(ProcessBuildOptions);
		}

		private static BuildPlayerOptions ProcessBuildOptions(BuildPlayerOptions options)
		{
#if UNITY_ANDROID || UNITY_IOS
			bool isMobile = true;
#else
			bool isMobile = false;
#endif

			options = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(options);

			GameConfig gameConfig = GameConfig.FindDefaultConfig();

			List<string> scenes = new List<string>();

			scenes.Add(isMobile ? gameConfig.BootSceneMobile.ScenePath : gameConfig.BootScene.ScenePath);
			scenes.Add(isMobile ? gameConfig.WorldMapSceneMobile.ScenePath : gameConfig.WorldMapScene.ScenePath);

			var levels = AssetDatabase.FindAssets("t:PlaythroughTemplateBase")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => !Path.GetFileName(p).StartsWith("_") && p.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) == -1)   // Starting with "_" means dev playthrough.
				.Select(AssetDatabase.LoadAssetAtPath<PlaythroughTemplateBase>)
				.Where(pt => !pt.HasActiveLevel)   // Must not have active level.
				.SelectMany(pt => pt.GetAllLevels())
				.Select(l => isMobile ? l.BackgroundSceneMobile.ScenePath : l.BackgroundScene.ScenePath)
				.Distinct()
				.ToArray();
				;

			Array.Sort(levels);

			scenes.AddRange(levels);

			Debug.Log("========= Build Scenes Selected =========\n" + string.Join("\n", scenes) + "\n");

			options.scenes = scenes.ToArray();

			return options;
		}
	}
}
