using UnityEngine;

namespace DevLocker.GFrame.Pools
{
	/// <summary>
	/// Simple pool for a particle prefab instances.
	/// Use to avoid hiccups when instantiating often prefabs of the same type.
	/// Don't forget to release your instance when you're done with it.
	///
	/// NOTE: If your particles are not looping (children including) and has StopAction set to Callback it will get auto-released back in the pool when the particles finish.
	/// </summary>
	public class ParticlePrefabsPool : GenericComponentPrefabsPool<ParticleSystem>
	{
		[Space(16)]
		[Tooltip("Enable this to release the instance back to the pool when the particle system finishes.\n\n" +
			"Top particle system must have \"StopAction\" set to \"Callback\" and it and it's children must not be looping.")]
		public bool ReleaseWhenParticlesStop = false;

		protected override ParticleSystem CreatePooledObject()
		{
			ParticleSystem instance = base.CreatePooledObject();

			if (ReleaseWhenParticlesStop) {
				if (ValidateParticleSystem()) {
					ParticlesStoppedListener listener = instance.gameObject.AddComponent<ParticlesStoppedListener>();
					listener.ParticleSystem = instance;
					listener.ParticleSystemStopped += OnParticlesInstanceStopped;
				}
			}

			return instance;
		}

		private void OnParticlesInstanceStopped(ParticleSystem particleSystem)
		{
			Release(particleSystem);
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			if (ReleaseWhenParticlesStop && Prefab) {
				ValidateParticleSystem();
			}
		}

		private bool ValidateParticleSystem()
		{
			if (Prefab.main.loop || Prefab.main.stopAction != ParticleSystemStopAction.Callback) {
				Debug.LogWarning($"Pool {name} for {Prefab.name} is trying to listen for particles {Prefab.name} stop action, but it is not setup correctly. It needs to be NOT looped and have StopAction set to Callback. Auto-release won't work!", Prefab);
				return false;
			}

#if UNITY_EDITOR
			// NOTE: Children particles must not be looping as well!!!
			foreach (ParticleSystem child in Prefab.GetComponentsInChildren<ParticleSystem>()) {
				if (child.main.loop) {
					Debug.LogError($"Pool {name} for {Prefab.name} has child {child.name} particle instance that is looped. Auto-release won't work!", Prefab);
					return false;
				}
			}
#endif

			return true;
		}
	}
}