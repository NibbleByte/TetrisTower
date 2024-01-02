using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	/// <summary>
	/// Destroy current object before components on it initialize, if this is player 2 or more (leave only single instance).
	/// Example: the ocean renderer in the vikings level needs to be only one.
	/// </summary>
	[DefaultExecutionOrder(-50000)]
	public class DestroyOnMultiplayer : MonoBehaviour
	{
		private void Awake()
		{
			if (SceneManager.sceneCount > 1) {
				DestroyImmediate(gameObject);
			}
		}
	}
}