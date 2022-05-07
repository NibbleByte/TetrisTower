using UnityEngine;

namespace DevLocker.GFrame.Pools
{
	/// <summary>
	/// Simple pool for a transform prefab instances.
	/// Use to avoid hiccups when instantiating often prefabs of the same type.
	/// Don't forget to release your instance when you're done with it.
	/// </summary>
	public class TransformPrefabsPool : GenericComponentPrefabsPool<Transform>
	{
		[Space(16)]
		[Tooltip("If prefab has particles on some of it's children, enable this to release the instance back to the pool when the particle system finishes.\n\n" +
			"Top particle system must have \"StopAction\" set to \"Callback\" and it and it's children must not be looping.")]
		public bool ReleaseWhenParticlesStop = false;

		protected override Transform CreatePooledObject()
		{
			Transform instance = base.CreatePooledObject();

			if (ReleaseWhenParticlesStop) {
				var particleSystem = FindStoppableParticleSystem(instance);
				if (particleSystem) {
					ParticlesStoppedListener listener = particleSystem.gameObject.AddComponent<ParticlesStoppedListener>();
					listener.ParticleSystem = particleSystem;
					listener.ParticleSystemStopped += (particles) => Release(instance);
				}
			}

			return instance;
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			if (ReleaseWhenParticlesStop && Prefab) {
				FindStoppableParticleSystem(Prefab);
			}
		}

		private ParticleSystem FindStoppableParticleSystem(Transform target)
		{
			var particleSystem = target.GetComponentInChildren<ParticleSystem>();

			if (particleSystem == null) {
				Debug.LogWarning($"Pool {name} is set to use particles, but prefab {Prefab.name} doesn't have any particles.", Prefab);
				return null;
			}

			if (particleSystem.main.loop || particleSystem.main.stopAction != ParticleSystemStopAction.Callback) {
				Debug.LogWarning($"Pool {name} for {Prefab.name} is trying to listen for particles {particleSystem.name} stop action, but it is not setup correctly. It needs to be NOT looped and have StopAction set to Callback. Auto-release won't work!", Prefab);
				return null;
			}

#if UNITY_EDITOR
			// NOTE: Children particles must not be looping as well!!!
			foreach (ParticleSystem child in particleSystem.GetComponentsInChildren<ParticleSystem>()) {
				if (child.main.loop) {
					Debug.LogError($"Pool {name} for {Prefab.name} has child {child.name} particle instance that is looped. Auto-release won't work!", Prefab);
					return null;
				}
			}
#endif

			return particleSystem;
		}
	}
}