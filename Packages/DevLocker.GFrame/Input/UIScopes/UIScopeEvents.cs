using UnityEngine;
using UnityEngine.Events;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Exposes scope activated and focused events.
	/// </summary>
	public class UIScopeEvents : MonoBehaviour
	{
		public UIScope Scope;

		[Header("Focus Events")]
		public UnityEvent Focused;
		public UnityEvent Unfocusing;

		[Header("Activate Events")]
		public UnityEvent Activated;
		public UnityEvent Deactivating;

		void OnEnable()
		{
			if (Scope == null) {
				Scope = GetComponentInParent<UIScope>();
			}

			if (Scope == null) {
				enabled = false;
				return;
			}

			Scope.Activated += OnActivated;
			Scope.Focused += OnFocused;
			Scope.Deactivating += OnDeactivating;
			Scope.Unfocusing += OnUnfocusing;
		}

		void OnDisable()
		{
			if (Scope == null)
				return;

			Scope.Activated -= OnActivated;
			Scope.Focused -= OnFocused;
			Scope.Deactivating -= OnDeactivating;
			Scope.Unfocusing -= OnUnfocusing;
		}

		private void OnActivated()
		{
			Activated.Invoke();
		}

		private void OnFocused()
		{
			Focused.Invoke();
		}

		private void OnDeactivating()
		{
			Deactivating.Invoke();
		}

		private void OnUnfocusing()
		{
			Unfocusing.Invoke();
		}

	}
}