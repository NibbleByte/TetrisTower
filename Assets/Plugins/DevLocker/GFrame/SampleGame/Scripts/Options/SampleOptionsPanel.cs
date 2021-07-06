using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.Options
{
	public class SampleOptionsPanel : MonoBehaviour
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