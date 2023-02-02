using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Game
{
    public static class GameTags
    {
        public const string TowerPlaceholderTag = "TowerPlaceholder";
        public const string TowerDecors = "TowerDecors";
        public const string FairyRestPoint = "FairyRestPoint";
        public const string BlocksLight = "BlocksLight";
    }

    public static class GameLayers
    {
        public const string BlocksName = "Blocks";

        public static int BlocksMask => LayerMask.GetMask(BlocksName);
    }
}