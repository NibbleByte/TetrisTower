using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerUI
{
	// HACK: This should be in the TowerUI folder, but because of dependency hell, it's not. :D
	public class PVPAttackChargeUIController : MonoBehaviour, ILevelLoadedListener
	{
		public Slider ProgressBar;

		private PlaythroughPlayer m_PlaythroughPlayer;
		private PVPPlaythroughData m_PlaythroughData;

		private bool m_Rising;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughPlayer);
			context.TrySetByType(out m_PlaythroughData);    // Will be missing in normal playthroughs.

			enabled = m_PlaythroughData != null;
			ProgressBar.gameObject.SetActive(m_PlaythroughData != null);
			m_Rising = true;
		}

		public void OnLevelUnloading()
		{
			enabled = false;
			m_PlaythroughPlayer = null;
			m_PlaythroughData = null;
		}

		void Awake()
		{
			enabled = false;
			ProgressBar.value = 0f;
		}

		void Update()
		{
			if (m_PlaythroughData.HaveFinishedLevels)
				return;

			float attackCharge = m_PlaythroughData.GetAttackChargeNormalized(m_PlaythroughPlayer);
			if (ProgressBar.value == attackCharge)
				return;

			if (Mathf.Abs(attackCharge - ProgressBar.value) < 0.005f) {
				ProgressBar.value = attackCharge;
				m_Rising = true;

			} else {

				float lerpTime = 8f * Time.deltaTime;

				// Resetting the charge should visually fill up the bar, then empty it.
				if (m_Rising) {
					if (ProgressBar.value < attackCharge) {
						ProgressBar.value = Mathf.Lerp(ProgressBar.value, attackCharge, lerpTime);

					} else {

						ProgressBar.value = Mathf.Lerp(ProgressBar.value, 1f, lerpTime);

						if (ProgressBar.value > 0.99f) {
							m_Rising = false;
						}
					}

				} else {

					ProgressBar.value = Mathf.Lerp(ProgressBar.value, attackCharge, lerpTime * 2);
				}
			}
		}
	}

}