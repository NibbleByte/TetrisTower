using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	[RequireComponent(typeof(AudioSource))]
	public class TowerPlayerSound : MonoBehaviour, ILevelLoadedListener
	{
		void Awake()
		{
			// Level already loaded - object was instantiated later on.
			// If not, will be called by the level supervisor.
			if (!GameManager.Instance.ChangingLevel) {
				OnLevelLoaded(null);
			}
		}

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			var supervisor = (TowerLevelSupervisor)GameManager.Instance.LevelSupervisor;
			if (supervisor.IsMultiplayer) {
				var audioSources = GetComponents<AudioSource>();
				foreach (AudioSource audioSource in audioSources) {
					audioSource.spatialBlend = GameManager.Instance.GameContext.GameConfig.MultiplayerSpatialBlend;
					audioSource.minDistance = 10000f;
					audioSource.maxDistance = 10005f;
				}
			}
		}

		public void OnLevelUnloading()
		{
		}
	}

}