using UnityEngine;

namespace DevLocker.GFrame.Pools
{
	/// <summary>
	/// Helper class to notify when the particle has finished.
	/// </summary>
	internal class ParticlesStoppedListener : MonoBehaviour
	{
		public ParticleSystem ParticleSystem;
		public event System.Action<ParticleSystem> ParticleSystemStopped;

		void OnParticleSystemStopped()
		{
			ParticleSystemStopped?.Invoke(ParticleSystem);
		}
	}
}