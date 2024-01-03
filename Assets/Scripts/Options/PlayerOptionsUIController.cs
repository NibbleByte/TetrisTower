using DevLocker.GFrame;
using System;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.Options
{
	public class PlayerOptionsUIController : MonoBehaviour
	{
		public Toggle DownIsDrop;
		public Toggle TouchInputType;

		private IUserPreferences m_UserPrefs => GameManager.Instance.GameContext.UserPrefs;

		void Awake()
		{
			DownIsDrop.onValueChanged.AddListener(SetDownIsDrop);
			TouchInputType.onValueChanged.AddListener(SetTouchInputType);
		}

		void OnEnable()
		{
			DownIsDrop.SetIsOnWithoutNotify(m_UserPrefs.DownIsDrop);

			switch (m_UserPrefs.TouchInputControls) {
				case IUserPreferences.TouchInputControlMethod.Drag:
					TouchInputType.SetIsOnWithoutNotify(true);
					break;
				case IUserPreferences.TouchInputControlMethod.Swipes:
					TouchInputType.SetIsOnWithoutNotify(false);
					break;
			}
		}

		private void SetTouchInputType(bool drag)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.TouchInputControls = drag
					? IUserPreferences.TouchInputControlMethod.Drag
					: IUserPreferences.TouchInputControlMethod.Swipes
					;
			}
		}

		private void SetDownIsDrop(bool isOn)
		{
			using (var userPrefs = GameManager.Instance.GameContext.EditPreferences()) {
				userPrefs.DownIsDrop = isOn;
			}
		}

	}
}