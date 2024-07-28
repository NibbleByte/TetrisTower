using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Game.Preferences
{
	public class RuntimeSessionPreferencesManager : MonoBehaviour, IPreferencesManager
	{
		private Dictionary<string, object> m_SessionPreferences = new Dictionary<string, object>();

		public void Init(GameContext gameContext)
		{
			// Do nothing really...
		}

		public void Store(string key, object value)
		{
			m_SessionPreferences[key] = value;
		}

		public object Restore(string key, object defaultValue)
		{
			return m_SessionPreferences.TryGetValue(key, out object value) ? value : defaultValue;
		}
	}
}