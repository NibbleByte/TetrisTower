using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	public class EffectsOverrider : MonoBehaviour
	{
		[Header(nameof(ConeVisualsGrid))]
		public ParticleSystem FallHitEffect;
		public FallTrailEffectsManager GridMoveFallTrailEffectsManager;

		[Header(nameof(TowerConeVisualsController))]
		public FallTrailEffectsManager SelectedShapeFallTrailEffectsManager;
		public FallTrailEffectsManager WonFallTrailEffectsManager;

		[Header(nameof(BonusActionsVisuals))]
		public Transform PushUpWarningEffectsPool;

		public void Override(ConeVisualsGrid visualsGrid)
		{
			if (FallHitEffect) {
				visualsGrid.FallHitEffect = FallHitEffect;
			}

			if (GridMoveFallTrailEffectsManager) {
				visualsGrid.FallTrailEffectsManager = GridMoveFallTrailEffectsManager;
			}
		}

		public void Override(TowerConeVisualsController visualsController)
		{
			if (SelectedShapeFallTrailEffectsManager) {
				visualsController.FallTrailEffectsManager = SelectedShapeFallTrailEffectsManager;
			}

			if (WonFallTrailEffectsManager) {
				visualsController.WonFallTrailEffectsManager = WonFallTrailEffectsManager;
			}
		}

		public void Override(BonusActionsVisuals bonusActionsVisuals)
		{
			if (PushUpWarningEffectsPool) {
				bonusActionsVisuals.PushUpWarningEffectsPool?.gameObject.SetActive(false);
				bonusActionsVisuals.PushUpWarningEffectsPool = PushUpWarningEffectsPool;
			}
		}
	}

}