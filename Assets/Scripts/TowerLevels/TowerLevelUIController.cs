using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelUIController : MonoBehaviour
	{
		public GameObject PausedPanel;

		public void SetPause(bool pause)
		{
			PausedPanel.SetActive(pause);
		}
	}
}