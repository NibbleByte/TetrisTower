using System.Collections;
using System.Collections.Generic;
using TetrisTower.Levels;
using UnityEngine;

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

		void Update()
		{
			int offsetColumns = 0;
			if (Input.GetKeyDown("left")) offsetColumns--;
			if (Input.GetKeyDown("right")) offsetColumns++;

			if (offsetColumns != 0) {
				LevelController.RequestFallingShapeOffset(offsetColumns);
			}

			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
				LevelController.RequestFallingSpeedUp(FallSpeedup);
			}
		}
	}
}