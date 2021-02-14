using System.Linq;
using UnityEngine;

namespace TetrisTower.Core
{
	[CreateAssetMenu(fileName = "Unknown_AssetsRepository", menuName = "Tetris Tower/Assets Repository")]
	public class AssetsRepository : ScriptableObject
	{
		public SerializableAsset[] Assets;

		public T GetAssetById<T>(string blockTypeId) where T : SerializableAsset
		{
			return Assets.OfType<T>().FirstOrDefault(sa => sa.SEID == blockTypeId);
		}

#if UNITY_EDITOR
		[ContextMenu("Populate Repository")]
		private void PopulateRepository()
		{
			Assets = GatherAssets();
		}

		private SerializableAsset[] GatherAssets()
			=> UnityEditor.AssetDatabase.FindAssets($"t:{typeof(SerializableAsset).Name}")
				.Distinct()
				.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
				.Select(UnityEditor.AssetDatabase.LoadAssetAtPath<SerializableAsset>)
				.OrderBy(sa => sa.GetType().Name)
				.ToArray()
			;
#endif
	}
}