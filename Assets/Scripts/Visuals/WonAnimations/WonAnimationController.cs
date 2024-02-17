using DevLocker.Audio;
using System.Linq;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.Audio;

namespace TetrisTower.Visuals.WonAnimations
{
	public class WonAnimationController : MonoBehaviour
	{
		// For now the animation is controlled by the state and this controller only deals with the sounds (refers to snapshots).

		[SerializeField]
		private float m_TransitionTime = 0.5f;

		[SerializeField]
		private AudioMixerSnapshot m_NormalMixerSnapshot;

		[SerializeField]
		private AudioMixerSnapshot m_WonMixerSnapshot;

		[SerializeField]
		private AudioMixerGroup m_MusicMixerGroup;

		[SerializeField]
		private AudioSourcePlayer m_WinAudio;

		public void StartAnimation()
		{
			m_WonMixerSnapshot.TransitionTo(m_TransitionTime);

			m_WinAudio?.Play();
		}

		public void EndAnimation()
		{
			m_NormalMixerSnapshot.TransitionTo(m_TransitionTime);
		}
	}
}