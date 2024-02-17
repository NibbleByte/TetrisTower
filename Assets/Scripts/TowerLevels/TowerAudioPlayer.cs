using DevLocker.Audio;
using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	/// <summary>
	/// Split-screen support (forces spatial blend to be 3D with endless range).
	/// </summary>
	public class TowerAudioPlayer : AudioSourcePlayer, ILevelLoadedListener
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