#if USE_TEXT_MESH_PRO
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Display log entries on screen, temporarily.
	/// </summary>
	public class SampleUITesterButtonLogDisplay : MonoBehaviour
	{
		public TextMeshProUGUI SampleText;
		public float Duration = 2f;

		private class LogEntry
		{
			public TextMeshProUGUI Text;
			public float StartTime;
		}

		private List<LogEntry> m_Entries = new List<LogEntry>();


		void Awake()
		{
			SampleText.gameObject.SetActive(false);
			m_Entries.Clear();
		}

		public void LogText(string text)
		{
			TextMeshProUGUI instance = Instantiate(SampleText, SampleText.transform.parent);
			instance.text = text;
			instance.gameObject.SetActive(true);
			GameObject.Destroy(instance.gameObject, Duration);

			m_Entries.Add(new LogEntry() { Text = instance, StartTime = Time.time });
		}
	}
}
#endif