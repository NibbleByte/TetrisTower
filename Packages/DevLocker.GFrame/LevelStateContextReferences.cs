using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Contains static shared level references that can be accessed by the level state.
	/// Use them explicitly or implicitly (by reflection).
	/// References are provided by the level supervisor on initialization.
	/// </summary>
	public class LevelStateContextReferences
	{
		private List<object> m_ContextReferences;

		public LevelStateContextReferences(IEnumerable<object> contextReferences)
		{
			m_ContextReferences = contextReferences.Where(obj => obj != null).ToList();
		}

		/// <summary>
		/// Fill in your reference explicitly from the current context.
		/// The level supervisor should have added all the needed references for you.
		/// </summary>
		public void SetByType<T>(out T reference)
		{
			reference = m_ContextReferences.OfType<T>().First();
		}

		/// <summary>
		/// Fill in your reference explicitly from the current context.
		/// The level supervisor should have added all the needed references for you.
		/// Returns true if reference was found by type.
		/// </summary>
		public bool TrySetByType<T>(out T reference)
		{
			reference = m_ContextReferences.OfType<T>().FirstOrDefault();

			return reference != null;
		}

		/// <summary>
		/// Find reference from the current context.
		/// The level supervisor should have added all the needed references for you.
		/// </summary>
		public T FindByType<T>() => m_ContextReferences.OfType<T>().First();

		/// <summary>
		/// Try to find reference from the current context.
		/// The level supervisor should have added all the needed references for you.
		/// </summary>
		public T TryFindByType<T>() => m_ContextReferences.OfType<T>().FirstOrDefault();

		/// <summary>
		/// Fill in all your references implicitly via reflection from the current context.
		/// The process will scan all public and private fields and set them based on their type if appropriate reference exists.
		/// The level supervisor should have added all the needed references for you.
		/// </summary>
		public void SetAllReferencesTo(object state)
		{
			var fields = state.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			var properties = state.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var field in fields) {
				if (field.IsInitOnly)
					continue;

				foreach (var reference in m_ContextReferences) {
					if (field.FieldType.IsAssignableFrom(reference.GetType())) {
						field.SetValue(state, reference);
						break;
					}
				}
			}

			foreach (var property in properties) {
				if (!property.CanWrite)
					continue;

				foreach (var reference in m_ContextReferences) {
					if (property.PropertyType.IsAssignableFrom(reference.GetType())) {
						property.SetValue(state, reference);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Add reference to the already existing list of reference. Should not have another reference of the same type.
		/// </summary>
		public void AddReference<T>(T reference)
		{
			if (m_ContextReferences.OfType<T>().Any())
				throw new ArgumentException($"Trying to add reference of type \"{nameof(T)}\" that already exists. {reference}");

			m_ContextReferences.Add(reference);
		}

		/// <summary>
		/// Remove reference by type.
		/// </summary>
		/// <returns>true if successfully removed, otherwise false.</returns>
		public bool RemoveByType<T>()
		{
			for(int i = 0; i < m_ContextReferences.Count; ++i) {
				if (m_ContextReferences[i] is T) {
					m_ContextReferences.RemoveAt(i);
					return true;
				}
			}

			return false;
		}
	}
}