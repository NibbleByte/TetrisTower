using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class LevelParamData
	{
		public SceneReference BackgroundScene;

		public SceneReference BackgroundSceneMobile;

		[Multiline]
		public string GreetMessage = "";

		// Spawn blocks from the array in range [0, TypesCount).
		public int InitialSpawnBlockTypesCount = 3;

		[Tooltip("How much matches (according to the rules) does the player has to do to pass this level. 0 means it is an endless game.")]
		public int ObjectiveEndCount;

		public GridRules Rules = new GridRules();

		[Range(5, 20)]
		public int GridRows = 9;

		// Validation value - cone visuals require different visual models for each size.
		// We don't support any other values for now.
		public const int SupportedColumnsCount = 13;
		[Range(SupportedColumnsCount, SupportedColumnsCount)]
		public int GridColumns = SupportedColumnsCount;

		public GridShapeTemplate[] ShapeTemplates;


		public bool StartGridWithPinnedBlocks = false;

		[SerializeReference]
		public BlocksGrid StartGrid;

		public BlocksSkinSet[] SkinOverrides;

		public SceneReference GetAppropriateBackgroundScene()
		{
			if (Platforms.PlatformsUtils.IsMobile) {
				return BackgroundSceneMobile.IsEmpty ? BackgroundScene : BackgroundSceneMobile;
			} else {
				return BackgroundScene;
			}
		}

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (Rules.ObjectiveType == 0) {
				Debug.LogError($"{context} has no ObjectiveType set.", context);
			}

#if UNITY_EDITOR
			if (GridRows == 0 || GridColumns == 0) {
				GridRows = 9;
				GridColumns = SupportedColumnsCount;
				UnityEditor.EditorUtility.SetDirty(context);
			}

			if (StartGrid != null && StartGrid.Rows == 0) {
				StartGrid = new BlocksGrid(GridRows, GridColumns);
				UnityEditor.EditorUtility.SetDirty(context);
			}
#endif

			if (GridRows == 0 || GridColumns == 0) {
				Debug.LogError($"Level param {BackgroundScene} has invalid grid size set.", context);
			}

			if (StartGrid != null && (StartGrid.Rows != GridRows || StartGrid.Columns != GridColumns)) {
				Debug.LogError($"Level param {BackgroundScene} has starting grid with wrong size.", context);
			}

			if (SkinOverrides != null && SkinOverrides.Any(s => !repo.IsRegistered(s))) {
				Debug.LogError($"Level param {nameof(SkinOverrides)} has sets that are not registered as serializable.", context);
			}
		}
	}
}