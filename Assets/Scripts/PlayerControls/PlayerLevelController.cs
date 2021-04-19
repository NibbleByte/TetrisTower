using System.Collections;
using System.Collections.Generic;
using TetrisTower.Levels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.PlayerControls
{
	public class PlayerLevelController : MonoBehaviour
	{
		public LevelController LevelController { get; private set; }
		public LevelData LevelData => LevelController?.LevelData;

		public float FallSpeedup = 40f;

		public void Init(LevelController levelController)
		{
			LevelController = levelController;
		}

		public void OnMoveShape(InputValue value)
		{
			var direction = value.Get<float>();

			if (direction != 0f) {
				LevelController.RequestFallingShapeMove((int) direction);
			}
		}

		public void OnRotateShape(InputValue value)
		{
			var direction = value.Get<float>();

			if (direction != 0) {
				LevelController.RequestFallingShapeRotate((int) direction);
			}
		}

		public void OnFallSpeedUp()
		{
			LevelController.RequestFallingSpeedUp(FallSpeedup);
		}
	}
}