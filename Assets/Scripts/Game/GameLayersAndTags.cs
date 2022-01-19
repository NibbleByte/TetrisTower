using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Game
{
    public static class GameTags
    {
        public const string TowerPlaceholderTag = "TowerPlaceholder";
    }

    public static class GameLayers
    {
        public const string BlocksName = "Blocks";

        public static int BlocksMask => LayerMask.GetMask(BlocksName);
    }
}