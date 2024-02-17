using DevLocker.Audio;
using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	public class TowerMusicController : MonoBehaviour, ILevelLoadedListener
	{
		public AudioSourcePlayer MusicSource;

		private GridLevelController m_TowerLevel;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_TowerLevel);

			m_TowerLevel.FinishedLevel += OnLevelFinished;

			MusicSource.Play();
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel.FinishedLevel -= OnLevelFinished;
		}

		private void OnLevelFinished()
		{
			MusicSource.Stop();
		}
	}
}