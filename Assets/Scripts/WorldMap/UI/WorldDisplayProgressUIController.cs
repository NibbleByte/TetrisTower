using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.TowerLevels.Playthroughs;
using TMPro;
using UnityEngine;

namespace TetrisTower.WorldMap.UI
{
	public class WorldDisplayProgressUIController : MonoBehaviour, ILevelLoadedListener
	{
		public string StarsFormat = "Stars: {STARS_COUNT}";
		public TMP_Text StarsLabel;

		private WorldPlaythroughData m_PlaythroughData;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);

			StarsLabel.text = StarsFormat.Replace("{STARS_COUNT}", m_PlaythroughData.CalculateEarnedStars().ToString());
		}

		public void OnLevelUnloading()
		{
		}
	}
}