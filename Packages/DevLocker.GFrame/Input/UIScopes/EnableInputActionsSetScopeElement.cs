#if USE_INPUT_SYSTEM

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Enable whole input actions set when the scope element is active.
	/// </summary>
	public class EnableInputActionsSetScopeElement : MonoBehaviour, IScopeElement, IHotkeysWithInputActions
	{
		public InputActionsSetDef ActionsSet;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		protected virtual void Reset()
		{
			// Let scopes do the enabling or else you'll get warnings for hotkey conflicts for multiple scopes with the same hotkey on screen.
			enabled = false;
		}

		protected virtual void Awake()
		{
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

			m_PlayerContext.InputContext.Enable(this, GetUsedActions(m_PlayerContext.InputContext));
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			m_PlayerContext.InputContext.Disable(this, GetUsedActions(m_PlayerContext.InputContext));
		}

		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			if (ActionsSet == null)
				return Enumerable.Empty<InputAction>();

			return ActionsSet.GetActions(inputContext);
		}

		protected virtual void OnValidate()
		{
			// Check the Reset() message.
			if (!Application.isPlaying && enabled) {
				enabled = false;
			}
		}
	}
}

#endif