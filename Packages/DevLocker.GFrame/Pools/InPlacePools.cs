using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLocker.GFrame.Pools
{
	/// <summary>
	/// Set of methods for creating pools from template game objects inside the scene sharing the same parent transform.
	/// Specify the template object and the method will instantiate it under the same parent if needed or deactivate the excess amount.
	/// </summary>
	public static class InPlacePools
	{
		/// <summary>
		/// Instantiate additional GameObjects from the template if not enough, or disable redundant GameObjects.
		/// </summary>
		public static void ListPool<T>(IList<T> pool, T template, Transform parent, int count, Action<T> onCreate = null, Action<T> onHide = null) where T : Component
		{
			for (int i = 0; i < Math.Min(pool.Count, count); i++) {
				pool[i].gameObject.SetActive(true);
			}

			while (pool.Count < count) {
				T instance = GameObject.Instantiate<T>(template, parent, false);
				instance.name = template.name;
				onCreate?.Invoke(instance);
				instance.gameObject.SetActive(true);
				pool.Add(instance);
			}

			for(int i = count; i < pool.Count; i++) {
				T instance = pool[i];
				onHide?.Invoke(instance);
				instance.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Instantiate additional GameObjects from the template if not enough, or disable redundant GameObjects.
		/// </summary>
		public static void ListPool(IList<GameObject> pool, GameObject template, Transform parent, int count, Action<GameObject> onCreate = null, Action<GameObject> onHide = null)
		{
			for (int i = 0; i < Math.Min(pool.Count, count); i++) {
				pool[i].gameObject.SetActive(true);
			}

			while (pool.Count < count) {
				GameObject instance = GameObject.Instantiate(template, parent, false);
				instance.name = template.name;
				onCreate?.Invoke(instance);
				instance.gameObject.SetActive(true);
				pool.Add(instance);
			}

			for(int i = count; i < pool.Count; i++) {
				GameObject instance = pool[i];
				onHide?.Invoke(instance);
				instance.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Instantiate additional GameObjects from the template if not enough, or disable redundant GameObjects.
		/// Template is considered the first child of the transform. All transform children are part of the pool and should be the same.
		/// </summary>
		public static void TransformPool(Transform parent, int count, Action<Transform> onCreate = null, Action<Transform> onHide = null)
		{
			int childCount = parent.childCount;
			Transform template = parent.GetChild(0);

			for (int i = 0; i < Math.Min(childCount, count); i++) {
				parent.GetChild(i).gameObject.SetActive(true);
			}

			while (childCount < count) {
				Transform instance = GameObject.Instantiate(template, parent, false);
				instance.name = template.name;
				onCreate?.Invoke(instance);
				instance.gameObject.SetActive(true);
				childCount++;
			}

			for (int i = count; i < childCount; i++) {
				Transform instance = parent.GetChild(i);
				onHide?.Invoke(instance);
				instance.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Instantiate additional GameObjects from the template if not enough, or disable redundant GameObjects.
		/// Template is considered the first child of the transform. All transform children are part of the pool and should be the same.
		/// </summary>
		public static IEnumerable<T> TransformPool<T>(Transform parent, int count, Action<Transform> onCreate = null, Action<Transform> onHide = null) where T : Component
		{
			TransformPool(parent, count, onCreate, onHide);

			for(int i = 0; i < count; i++) {
				yield return parent.GetChild(i).GetComponent<T>();
			}
		}
	}
}