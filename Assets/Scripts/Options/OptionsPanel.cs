using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.Options
{
	public class OptionsPanel : MonoBehaviour
	{
		[Header("Video")]
		public Toggle FullScreenToggle;
		public Toggle VSyncToggle;

		[Header("Audio")]
		public Toggle MusicToggle;
		public Toggle SoundToggle;


		public void ApplyOptions()
		{
			Debug.Log("Applying options.", this);
		}

		public void DiscardOptions()
		{
			Debug.Log("Discarding options.", this);
		}
	}
}