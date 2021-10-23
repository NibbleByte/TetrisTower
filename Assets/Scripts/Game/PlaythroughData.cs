using Newtonsoft.Json;
using System;
using TetrisTower.TowerLevels;
using UnityEngine;

namespace TetrisTower.Game
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PlaythroughData
	{
		[SerializeReference] public TowerLevelData TowerLevel;

		public int ScoreOnLevelStart = 0;
		public int TotalScore => ScoreOnLevelStart + TowerLevel?.Score.Score ?? 0;
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(PlaythroughData))]
	public class PlaythroughDataDrawer : Tools.SerializeReferenceCreatorDrawer<PlaythroughData>
	{
	}
#endif
}