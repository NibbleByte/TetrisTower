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
			m_ContextReferences = contextReferences.ToList();
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
		/// Fill in all your references implicitly via reflection from the current context.
		/// The process will scan all public and private fields and set them based on their type if appropriate reference exists.
		/// The level supervisor should have added all the needed references for you.
		/// </summary>
		public void SetAllReferencesTo(ILevelState state)
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
	}
}