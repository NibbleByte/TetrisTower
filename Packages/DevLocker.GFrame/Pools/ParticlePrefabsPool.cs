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

		// NOTE: If your particles are not looping (children including) and has StopAction set to Callback it will get auto-released back in the pool when the particles finish.
		protected override ParticleSystem CreatePooledObject()
		{
			ParticleSystem instance = base.CreatePooledObject();

			if (instance.main.loop == false && instance.main.stopAction == ParticleSystemStopAction.Callback) {

#if UNITY_EDITOR
				// NOTE: Children particles must not be looping as well!!!
				foreach(ParticleSystem child in instance.GetComponentsInChildren<ParticleSystem>()) {
					if (child.main.loop) {
						Debug.LogError($"Pooled {instance.name} particle instance has setup for auto release, but one of it's children {child.name} is looped. Auto-release won't work!", Prefab);
						break;
					}
				}
#endif

				ParticlesStoppedListener listener = instance.gameObject.AddComponent<ParticlesStoppedListener>();
				listener.ParticleSystem = instance;
				listener.ParticleSystemStopped += OnParticlesInstanceStopped;
			}

			return instance;
		}

		private void OnParticlesInstanceStopped(ParticleSystem particleSystem)
		{
			Release(particleSystem);
		}
	}
}