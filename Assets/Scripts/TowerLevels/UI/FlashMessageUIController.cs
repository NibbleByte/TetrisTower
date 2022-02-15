using System.Collections;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerLevels.UI
{
	public class FlashMessageUIController : MonoBehaviour
	{
		[SerializeField]
		private float m_FlashDuration;

		private TextMeshProUGUI m_Text;

		private float m_FlashStarted;

		private void Awake()
		{
			m_Text = GetComponent<TextMeshProUGUI>();
			m_Text.text = string.Empty;
			gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			m_Text.text = string.Empty;
		}

		public void ShowMessage(string message, bool logMessage = true)
		{
			m_Text.text = message;
			m_FlashStarted = Time.time;

			gameObject.SetActive(true);

			if (logMessage) {
				Debug.Log($"Flash message: \"{message}\"", this);
			}
		}

		public void AppendMessage(string message, bool resetTimer = true, bool logMessage = true)
		{
			if (string.IsNullOrEmpty(m_Text.text)) {
				m_Text.text = message;
			} else {
				m_Text.text += $"\n{message}";
			}
			if (resetTimer) {
				m_FlashStarted = Time.time;
			}

			gameObject.SetActive(true);

			if (logMessage) {
				Debug.Log($"Flash message: \"{message}\"", this);
			}
		}

		public void ClearMessage()
		{
			gameObject.SetActive(false);
		}

		void Update()
		{
			if (m_FlashStarted + m_FlashDuration <= Time.time) {
				gameObject.SetActive(false);
			}
		}
	}
}