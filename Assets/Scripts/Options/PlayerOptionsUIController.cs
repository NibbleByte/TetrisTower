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
			var gameContext = (GameContext)LevelsManager.Instance.GameContext;

			switch(gameContext.Options.TouchInputControls) {
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
			var gameContext = (GameContext)LevelsManager.Instance.GameContext;

			gameContext.Options.TouchInputControls = drag
				? PlayerOptions.TouchInputControlMethod.Drag
				: PlayerOptions.TouchInputControlMethod.Swipes
				;
		}
	}
}