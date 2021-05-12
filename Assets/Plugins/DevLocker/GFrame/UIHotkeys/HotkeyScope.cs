using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevLocker.GFrame.UI
{
	/// <summary>
	/// Marks hotkey elements to be controlled by the HotkeyScope.
	/// </summary>
	public interface IHotkeyElement
	{
		bool enabled { get; set; }
	}

	/// <summary>
	/// When working with more hotkeys on screen, some conflicts may arise.
	/// For example:
	/// > "Back" hotkey element is attached on the displayed menu and another "Back" hotkey attached to the "Yes/No" pop up, displayed on top.
	/// > The usual solution is to invoke only the last enabled hotkey instead of all of them.
	/// HotkeyScope groups all child hotkey elements into a single group. The last enabled HotkeyScope is active, while the rest will be disabled.
	/// </summary>
	[SelectionBase]
	public class HotkeyScope : MonoBehaviour
	{
		public static HotkeyScope ActiveScope { get; private set; }

		private static List<HotkeyScope> s_Scopes = new List<HotkeyScope>();

		private List<IHotkeyElement> m_HotkeyElements = new List<IHotkeyElement>();

		private void Awake()
		{
			ScanForChildHotkeyElements();
		}

		void OnEnable()
		{
			ActiveScope?.SetScopeState(false);

			s_Scopes.Add(this);

			ActiveScope = this;
			ActiveScope.SetScopeState(true);
		}

		void OnDisable()
		{
			bool wasActive = false;
			if (ActiveScope == this) {
				ActiveScope.SetScopeState(false);
				wasActive = true;
			}

			s_Scopes.Remove(this);

			if (wasActive) {
				ActiveScope = s_Scopes.Count > 0 ? s_Scopes[s_Scopes.Count - 1] : null;
				ActiveScope?.SetScopeState(true);
			}
		}

		/// <summary>
		/// Force selected scope to be active, instead of the last enabled.
		/// </summary>
		/// <param name="scope"></param>
		public void ForceActiveScope()
		{
			ActiveScope?.SetScopeState(false);

			ActiveScope = this;
			ActiveScope.SetScopeState(true);
		}

		/// <summary>
		/// Call this if you changed your UI hierarchy and expect added or removed hotkey elements.
		/// </summary>
		public void ScanForChildHotkeyElements()
		{
			m_HotkeyElements.Clear();
			ScanForChildHotkeyElements(this, transform, m_HotkeyElements);

			if (ActiveScope == this) {
				ActiveScope.SetScopeState(true);

			} else if (ActiveScope != null) {
				SetScopeState(false);
			}
		}

		private static void ScanForChildHotkeyElements(HotkeyScope parentScope, Transform transform, List<IHotkeyElement> hotkeyElements)
		{
			var scope = transform.GetComponent<HotkeyScope>();
			// Another scope begins, it will handle its own child hotkey elements.
			if (scope && parentScope != scope)
				return;

			hotkeyElements.AddRange(transform.GetComponents<IHotkeyElement>());

			foreach(Transform child in transform) {
				ScanForChildHotkeyElements(parentScope, child, hotkeyElements);
			}
		}

		private void SetScopeState(bool active)
		{
			foreach(IHotkeyElement hotkeyElement in m_HotkeyElements) {
				hotkeyElement.enabled = active;
			}
		}
	}
}