using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	public class EffectsOverrider : MonoBehaviour
	{
		[Header(nameof(ConeVisualsGrid))]
		public ParticleSystem FallHitEffect;
		public ParticleSystem MatchBlockEffect;
		public FallTrailEffectsManager GridMoveFallTrailEffectsManager;

		public ParticleSystem ReplaceBlockEffect;

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

			if (MatchBlockEffect) {
				visualsGrid.MatchBlockEffect = MatchBlockEffect;
			}

			if (GridMoveFallTrailEffectsManager) {
				visualsGrid.FallTrailEffectsManager = GridMoveFallTrailEffectsManager;
			}

			if (ReplaceBlockEffect) {
				visualsGrid.ReplaceBlockEffect = ReplaceBlockEffect;
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