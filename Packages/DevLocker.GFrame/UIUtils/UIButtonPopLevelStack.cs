using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Simple MonoBehaviour to quickly instruct an UI button to pop up the current state.
	/// </summary>
	public class UIButtonPopLevelStack : MonoBehaviour
	{
		private Button m_Button;

		void Awake()
		{
			m_Button = GetComponent<Button>();

			m_Button.onClick.AddListener(OnButtonClick);
		}

		private void OnDestroy()
		{
			if (m_Button) {
				m_Button.onClick.RemoveListener(OnButtonClick);
			}
		}

		private void OnButtonClick()
		{
			// Assuming there is only one instance.
			// Singleton (if present) is implemented by the user code which is not accessible here.
			var levelsManager = GameObject.FindObjectOfType<LevelsManager>();
			levelsManager.PopLevelState();
		}
	}
}