using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using UnityEngine;

namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Will deactivate <see cref="CanvasGroup.blocksRaycasts"/> if <see cref="PlayerContextUIRootObject.StatesStack"/>
	/// is destroyed (i.e. level supervisor is changing) or states are changing.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public class UIDisableCanvasGroupOnStatesChanging : MonoBehaviour
	{
		private CanvasGroup m_CanvasGroup;

		// Used for multiple event systems (e.g. split screen).
		private IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		void Awake()
		{
			m_CanvasGroup = GetComponent<CanvasGroup>();
			m_CanvasGroup.blocksRaycasts = false;

			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			m_PlayerContext.AddSetupCallback((delayedSetup) => {
				m_HasInitialized = true;

				if (delayedSetup && isActiveAndEnabled) {
					OnEnable();
				}
			});
		}

		void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			PlayerContextUIRootObject root = m_PlayerContext.GetRootObject();
			root.StatesStackCreated += OnStatesStackCreated;
			root.GetRootObject().StatesStackDestroyed += OnStatesStackDestroyed;

			if (root.StatesStack != null) {
				OnStatesStackCreated();
			} else {
				OnStatesStackDestroyed();
			}
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			PlayerContextUIRootObject root = m_PlayerContext.GetRootObject();
			root.GetRootObject().StatesStackCreated -= OnStatesStackCreated;
			root.GetRootObject().StatesStackDestroyed -= OnStatesStackDestroyed;
		}

		private void OnStatesStackCreated()
		{
			m_CanvasGroup.blocksRaycasts = true;

			PlayerContextUIRootObject root = m_PlayerContext.GetRootObject();
			root.StatesStack.StateChangesStarted += OnStateChangesStarted;
			root.StatesStack.StateChangesEnded += OnStateChangesEnded;
		}

		private void OnStatesStackDestroyed()
		{
			m_CanvasGroup.blocksRaycasts = false;

			PlayerContextUIRootObject root = m_PlayerContext.GetRootObject();
			if (root.StatesStack != null) {
				root.StatesStack.StateChangesStarted -= OnStateChangesStarted;
				root.StatesStack.StateChangesEnded -= OnStateChangesEnded;
			}
		}

		private void OnStateChangesStarted()
		{
			m_CanvasGroup.blocksRaycasts = false;
		}

		private void OnStateChangesEnded()
		{
			m_CanvasGroup.blocksRaycasts = true;
		}


		void OnDestroy()
		{
			if (m_CanvasGroup) {
				m_CanvasGroup.blocksRaycasts = true;
			}
		}
	}
}