using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace DevLocker.GFrame.Pools
{
	/// <summary>
	/// Simple pool for a TComponent prefab instances.
	/// Use to avoid hiccups when instantiating often prefabs of the same type.
	/// Don't forget to release your instance when you're done with it.
	///
	/// Inherit to specify the type.
	/// </summary>
	public abstract class GenericComponentPrefabsPool<TComponent> : MonoBehaviour where TComponent : Component
	{
		public enum ParentPolicy
		{
			CreateEmptyRootAsParent,
			UseThisAsParent,
			SpecifyParent,
		}

		public TComponent Prefab;

		[Tooltip("Maximum pooled instances. Excess instances will be destroyed, not pooled.")]
		public int MaxPoolSize = 10;

		[Tooltip("Create instances in advance on create.")]
		public int PreloadCount = 0;

		public bool CollectionChecks = true;

		public ParentPolicy PoolParentPolicy = ParentPolicy.CreateEmptyRootAsParent;

		[Tooltip("Used with ParentPolicy.SpecifyParent")]
		public Transform SpecifiedPoolParent;

		private IObjectPool<TComponent> m_Pool;
		private int m_NextNameId = 1;

		/// <summary>
		/// Get pooled instance of the <see cref="Prefab"/>.
		/// </summary>
		/// <param name="activateObject">Should instance be activated or the user should do it (for optimizations).</param>
		/// <returns></returns>
		public TComponent Get(bool activateObject = true)
		{
			TComponent instance = m_Pool.Get();

			if (activateObject) {
				instance.gameObject.SetActive(true);
			}

			return instance;
		}

		/// <summary>
		/// Release the instance back to the pool.
		/// </summary>
		/// <param name="instance">Pool instance</param>
		public void Release(TComponent instance)
		{
			m_Pool.Release(instance);
		}



		/// <summary>
		/// Get pooled instance of the <see cref="Prefab"/>.
		/// </summary>
		/// <param name="parent">Set the instance parent.</param>
		/// <param name="worldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before.</param>
		/// <param name="activateObject">Should instance be activated or the user should do it (for optimizations).</param>
		/// <returns></returns>
		public TComponent Get(Transform parent, bool worldPositionStays = true, bool activateObject = true)
		{
			TComponent instance = Get(false);
			instance.transform.SetParent(parent, worldPositionStays);

			if (activateObject) {
				instance.gameObject.SetActive(true);
			}

			return instance;
		}

		/// <summary>
		/// Get pooled instance of the <see cref="Prefab"/>.
		/// </summary>
		/// <param name="parent">Set the instance parent.</param>
		/// <param name="activateObject">Should instance be activated or the user should do it (for optimizations).</param>
		/// <returns></returns>
		public TComponent Get(Vector3 worldPosition, Quaternion worldRotation, Transform parent = null, bool activateObject = true)
		{
			TComponent instance = Get(false);
			var instanceTransform = instance.transform;
			instanceTransform.SetParent(parent);
			instanceTransform.SetPositionAndRotation(worldPosition, worldRotation);

			if (activateObject) {
				instance.gameObject.SetActive(true);
			}

			return instance;
		}



		protected virtual void Awake()
		{
			if (Prefab == null) {
				throw new NullReferenceException($"Pool {name} of type {GetType().Name} has no prefab specified.");
			}

			switch(PoolParentPolicy) {
				case ParentPolicy.CreateEmptyRootAsParent:
					SpecifiedPoolParent = new GameObject($"__{GetType().Name}_{Prefab.name}_{name}").transform;
					break;
				case ParentPolicy.UseThisAsParent:
					SpecifiedPoolParent = transform;
					break;
				case ParentPolicy.SpecifyParent:
					break;

				default:
					throw new NotSupportedException();
			}

			if (MaxPoolSize < PreloadCount) {
				MaxPoolSize = PreloadCount;
			}

			m_Pool = new ObjectPool<TComponent>(CreatePooledObject, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, CollectionChecks, PreloadCount, MaxPoolSize);

			if (PreloadCount > 0) {
				List<TComponent> preloaded = new List<TComponent>();
				for (int i = 0; i < PreloadCount; ++i) {
					preloaded.Add(Get(false));
				}

				foreach (TComponent component in preloaded) {
					Release(component);
				}
			}
		}

		protected virtual void OnDestroy()
		{
			m_Pool.Clear();
		}

		protected virtual void OnValidate()
		{
			if (Prefab == null) {
				Debug.LogError($"Pool {name} of type {GetType().Name} has no prefab specified.", this);
			}

			if (PoolParentPolicy == ParentPolicy.SpecifyParent && SpecifiedPoolParent == null) {
				Debug.LogError($"Pool {name} of type {GetType().Name} has no specified pool parent.", this);
			}
		}

		protected virtual TComponent CreatePooledObject()
		{
			TComponent instance = GameObject.Instantiate(Prefab, SpecifiedPoolParent);
			instance.name = $"{Prefab.name} - P{m_NextNameId:000}";
			m_NextNameId++;
			return instance;
		}

		protected virtual void OnTakeFromPool(TComponent obj)
		{
			// Activation happens in the caller.
			obj.transform.SetParent(null);
		}

		protected virtual void OnReturnedToPool(TComponent obj)
		{
			obj.gameObject.SetActive(false);
			obj.transform.SetParent(SpecifiedPoolParent);
		}

		protected virtual void OnDestroyPoolObject(TComponent obj)
		{
			if (obj) {
				Destroy(obj.gameObject);
			}
		}
	}
}