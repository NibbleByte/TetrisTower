using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using UnityEngine;

namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Will deactivate <see cref="CanvasGroup.blocksRaycasts"/> if <see cref="PlayerContextUIRootObject.IsLevelLoading"/> is set to true.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public class UIDisableCanvasGroupOnLevelLoading : MonoBehaviour
	{
		private CanvasGroup m_CanvasGroup;

		private PlayerContextUIRootObject m_PlayerContext;

		private bool m_LastIsLevelLoading;

		void Awake()
		{
			m_CanvasGroup = GetComponent<CanvasGroup>();
			m_CanvasGroup.blocksRaycasts = false;
			m_LastIsLevelLoading = true;

			PlayerContextUtils.GetPlayerContextFor(gameObject).AddSetupCallback((delayedSetup) => {
				m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject).GetRootObject();
			});
		}

		void Update()
		{
			if (m_PlayerContext == null || m_CanvasGroup == null)
				return;

			// Set the canvas group only if level loading flag changed. Others may also set the canvas during states change etc.
			if (m_LastIsLevelLoading == m_PlayerContext.IsLevelLoading)
				return;

			m_LastIsLevelLoading = m_PlayerContext.IsLevelLoading;

			m_CanvasGroup.blocksRaycasts = !m_PlayerContext.IsLevelLoading;
		}

		void OnDestroy()
		{
			if (m_CanvasGroup) {
				m_CanvasGroup.blocksRaycasts = true;
			}
		}
	}
}