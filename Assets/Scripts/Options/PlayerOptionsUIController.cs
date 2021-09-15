using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.Options
{
	public class PlayerOptionsUIController : MonoBehaviour
	{
		public Toggle TouchInputType;

		void Awake()
		{
			TouchInputType.onValueChanged.AddListener(SetTouchInputType);
		}

		void OnEnable()
		{
			switch(GameManager.Instance.GameContext.Options.TouchInputControls) {
				case PlayerOptions.TouchInputControlMethod.Drag:
					TouchInputType.SetIsOnWithoutNotify(true);
					break;
				case PlayerOptions.TouchInputControlMethod.Swipes:
					TouchInputType.SetIsOnWithoutNotify(false);
					break;
			}
		}

		private void SetTouchInputType(bool drag)
		{
			GameManager.Instance.GameContext.Options.TouchInputControls = drag
				? PlayerOptions.TouchInputControlMethod.Drag
				: PlayerOptions.TouchInputControlMethod.Swipes
				;
		}
	}
}