using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Pools;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals
{
	/// <summary>
	/// Responsible for visualizing any additional aspects of the bonus actions.
	/// </summary>
	public class BonusActionsVisuals : MonoBehaviour, ILevelLoadedListener
	{
		public Transform PushUpWarningEffectsPool;

		private GridLevelData m_LevelData;
		private ConeVisualsGrid m_VisualsGrid;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_LevelData);
			context.SetByType(out m_VisualsGrid);

			if (PushUpWarningEffectsPool) {
				PushUpWarningEffectsPool.gameObject.SetActive(false);

				if (PushUpWarningEffectsPool.childCount > 0) {
					InPlacePools.TransformPool(PushUpWarningEffectsPool, m_LevelData.Grid.Columns);
				} else {
					Debug.LogError($"{PushUpWarningEffectsPool.name} doesn't have any child effects for {nameof(PushUpLine_BonusAction)}.", PushUpWarningEffectsPool);
				}

				for(int column = 0; column < PushUpWarningEffectsPool.childCount; ++column) {
					Transform child = PushUpWarningEffectsPool.GetChild(column);

					child.position = m_VisualsGrid.GridToWorldBottomCenter(new GridCoords(0, column));
					Vector3 direction = child.position - m_VisualsGrid.transform.position;
					direction.y = 0;
					child.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
				}
			}
		}

		public void OnLevelUnloading()
		{
			m_LevelData = null;
			m_VisualsGrid = null;
		}

		void Update()
		{
			if (m_LevelData == null)
				return;

			if (PushUpWarningEffectsPool) {
				bool pushUpPending = m_LevelData.PendingBonusActions.OfType<PushUpLine_BonusAction>().Any();
				PushUpWarningEffectsPool.gameObject.SetActive(pushUpPending);
			}
		}
	}

}
