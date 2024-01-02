using UnityEngine;

namespace TetrisTower.Game
{
	public static class GameTags
	{
		public const string TowerPlaceholderTag = "TowerPlaceholder";
		public const string TowerDecors = "TowerDecors";
		public const string FairyRestPoint = "FairyRestPoint";
		public const string BlocksLight = "BlocksLight";

		public const string LevelBounds = "LevelBounds";
	}

	public static class GameLayers
	{
		private const string BlocksName = "Blocks-";
		private const int BlocksLayerCount = 8;

		public static int BlocksLayer(int playerIndex) => LayerMask.NameToLayer(BlocksName + playerIndex % 8);
		public static int BlocksMask(int playerIndex) => LayerMask.GetMask(BlocksName + playerIndex);
		public static int BlocksMaskAll()
		{
			int mask = 0;
			for (int playerIndex = 0; playerIndex < BlocksLayerCount; ++playerIndex) {
				mask |= BlocksMask(playerIndex);
			}

			return mask;
		}

		public static void SetLayerRecursively(GameObject obj, int layer)
		{
			obj.layer = layer;

			foreach (Transform child in obj.transform) {
				SetLayerRecursively(child.gameObject, layer);
			}
		}

	}
}