using System;
using TetrisTower.Game;
using TetrisTower.Game.Preferences;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.UserPreferences
{
	public class AudioPreferencesUIController : MonoBehaviour
	{
		public Slider MasterVolumeSlider;

		public Toggle MusicMuteToggle;
		public Toggle SoundsMuteToggle;

		public Slider MusicVolumeSlider;
		public Slider AmbienceVolumeSlider;
		public Slider SoundsVolumeSlider;

		private IUserPreferences m_UserPrefs => GameManager.Instance.GameContext.UserPrefs;

		void Awake()
		{
			MusicMuteToggle?.onValueChanged.AddListener(SetMusicMute);
			SoundsMuteToggle?.onValueChanged.AddListener(SetSoundsMute);

			MasterVolumeSlider?.onValueChanged.AddListener(SetMasterVolume);
			MusicVolumeSlider?.onValueChanged.AddListener(SetMusicVolume);
			SoundsVolumeSlider?.onValueChanged.AddListener(SetSoundsVolume);
			AmbienceVolumeSlider?.onValueChanged.AddListener(SetAmbienceVolume);
		}

		void OnEnable()
		{
			MusicMuteToggle?.SetIsOnWithoutNotify(m_UserPrefs.MusicMute);
			SoundsMuteToggle?.SetIsOnWithoutNotify(m_UserPrefs.SoundsMute);

			MasterVolumeSlider?.SetValueWithoutNotify(m_UserPrefs.MasterVolume);
			MusicVolumeSlider?.SetValueWithoutNotify(m_UserPrefs.MusicVolume);
			SoundsVolumeSlider?.SetValueWithoutNotify(m_UserPrefs.SoundsVolume);
			AmbienceVolumeSlider?.SetValueWithoutNotify(m_UserPrefs.AmbienceVolume);
		}

		private void SetMusicMute(bool isOn)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.MusicMute = isOn;
			}
		}

		private void SetSoundsMute(bool isOn)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.SoundsMute = isOn;
			}
		}

		private void SetMasterVolume(float value)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.MasterVolume = value;
			}
		}

		private void SetMusicVolume(float value)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.MusicVolume = value;
			}
		}

		private void SetSoundsVolume(float value)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.SoundsVolume = value;
			}
		}

		private void SetAmbienceVolume(float value)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.AmbienceVolume = value;
			}
		}
	}
}