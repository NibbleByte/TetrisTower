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
	public class EnableInputActionsSetScopeElement : MonoBehaviour, IScopeElement, IHotkeyWithInputAction
	{
		public InputActionsSetDef ActionsSet;

		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			if (ActionsSet == null)
				return Enumerable.Empty<InputAction>();

			return ActionsSet.GetActions(inputContext);
		}
	}
}

#endif