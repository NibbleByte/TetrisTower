using System.Collections.Generic;
using TetrisTower.Core;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "UnknownPlaythroughTemplate", menuName = "Tetris Tower/Playthrough Template")]
	public class PlaythroughTemplate : ScriptableObject
	{
		[SerializeField] private PlaythroughData m_PlayerData;

		public PlaythroughData GeneratePlaythroughData(GameConfig config)
		{
			// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
			var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(m_PlayerData, config.Converters);

			return Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(serialized, config.Converters);
		}

	}
}