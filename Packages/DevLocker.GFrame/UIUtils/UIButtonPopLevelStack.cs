using DevLocker.GFrame.Input;
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

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		void Awake()
		{
			m_Button = GetComponent<Button>();

			m_Button.onClick.AddListener(OnButtonClick);

			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		private void OnDestroy()
		{
			if (m_Button) {
				m_Button.onClick.RemoveListener(OnButtonClick);
			}
		}

		private async void OnButtonClick()
		{
			await m_PlayerContext.StatesStack.PopStateAsync();
		}
	}
}