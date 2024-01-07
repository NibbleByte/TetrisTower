using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TetrisTower.Game.Preferences
{
	public class AudioPreferencesManager : MonoBehaviour, IPreferencesManager
	{
		public AudioMixer MasterMixer;

		private IUserPreferences m_UserPrefs;

		public void Init(GameContext gameContext)
		{
			m_UserPrefs = gameContext.UserPrefs;
			m_UserPrefs.Changed += OnUserPrefsChanged;

			OnUserPrefsChanged();
		}

		private void OnUserPrefsChanged()
		{
			MasterMixer.SetFloat("MasterVolume", FloatToDb(m_UserPrefs.MasterVolume));
			MasterMixer.SetFloat("MusicVolume", MuteableFloatToDb(m_UserPrefs.MusicMute, m_UserPrefs.MusicVolume));
			MasterMixer.SetFloat("AmbienceVolume", MuteableFloatToDb(m_UserPrefs.SoundsMute, m_UserPrefs.AmbienceVolume));
			MasterMixer.SetFloat("GameplayVolume", MuteableFloatToDb(m_UserPrefs.SoundsMute, m_UserPrefs.SoundsVolume));
			// UI volume is mapped to the SoundVolume at the moment.
			MasterMixer.SetFloat("UIVolume", MuteableFloatToDb(m_UserPrefs.SoundsMute, /* NOTE */ m_UserPrefs.SoundsVolume));
		}

		// Convert normalized 0-1 value to db properly.
		// https://forum.unity.com/threads/changing-audio-mixer-group-volume-with-ui-slider.297884/#post-3494983
		private static float FloatToDb(float fvalue) => Mathf.Log10(fvalue != 0f ? fvalue : 0.000001f) * 20;

		private float MuteableFloatToDb(bool mute, float fvalue) => mute ? -80f : FloatToDb(fvalue);
	}
}