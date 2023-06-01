#if USE_INPUT_SYSTEM

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Will display progress of Hotkey interaction (e.g. hold / long press etc.)
	/// </summary>
	public class HotkeyProgressUIScopeElement : MonoBehaviour, IScopeElement, IHotkeysWithInputActions
	{
		[Tooltip("Skip the hotkey on the selected condition.")]
		[Utils.EnumMask]
		public SkipHotkeyOption SkipHotkey;

		[Tooltip("(Optional) Input action to be used. Can be missing - indicator root will be deactivated.")]
		[SerializeField]
		public InputActionReference InputAction;

		[Tooltip("The root object of the indicator. It will be deactivated if the action doesn't have continues integration (e.g. \"hold\" interaction) or no action is specified.")]
		public GameObject IndicatorRoot;

		[Tooltip("Image to be used as a progress bar of the action. It will fill it from 0 to 1.\nLeave empty to use the image of the current game object.")]
		public Image FillImage;

#if USE_UGUI_TEXT
		[Tooltip("Optional - Text to set the progress.")]
		public Text Text;
#endif

#if USE_TEXT_MESH_PRO
		[Tooltip("Optional - Text to set the progress.")]
		public TMPro.TextMeshProUGUI TextMeshProText;
#endif

		[Tooltip("Optional - enter how the progress text should be displayed. Use \"{value}\" to be replaced with the matched text.")]
		public string FormatText = "{value}";

		[Space]
		[Space]
		public UnityEvent Started;
		public UnityEvent Performed;
		public UnityEvent Cancelled;

		protected InputAction m_InputAction { get; private set; }

		protected bool m_ActionStarted { get; private set; } = false;
		protected bool m_ActionPerformed { get; private set; } = false;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		public virtual bool HasContinuesInteractions()
		{
			if (m_InputAction == null)
				return false;

			StringComparison comparison = StringComparison.OrdinalIgnoreCase;
			
			if (m_InputAction.interactions.Contains("hold", comparison) || m_InputAction.interactions.Contains("slowTap", comparison))
				return true;

			foreach (InputBinding binding in m_InputAction.bindings) {
				if ((binding.interactions?.Contains("hold", comparison) ?? false) || (binding.interactions?.Contains("slowTap", comparison) ?? false))
					return true;
			}

			return false;
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

		protected virtual void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			m_InputAction = GetUsedActions(m_PlayerContext.InputContext).FirstOrDefault();

			if (IndicatorRoot) {
				IndicatorRoot.SetActive(HasContinuesInteractions());
			}

			if (m_InputAction == null)
				return;

			if (FillImage == null) {
				FillImage = GetComponent<Image>();
				if (FillImage == null) {
					Debug.LogWarning($"{nameof(HotkeyProgressUIScopeElement)} \"{name}\" has no image specified to use.", this);
					enabled = false;
					return;
				}
			}

			FillImage.fillAmount = 0f;

			m_InputAction.started += OnInputStarted;
			m_InputAction.performed += OnInputPerformed;
			m_InputAction.canceled += OnInputCancel;
		}

		protected virtual void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			m_ActionStarted = false;
			m_ActionPerformed = false;

			if (m_InputAction == null)
				return;

			m_InputAction.started -= OnInputStarted;
			m_InputAction.performed -= OnInputPerformed;
			m_InputAction.canceled -= OnInputCancel;
		}

		private void OnInputStarted(InputAction.CallbackContext obj)
		{
			// Copy-pasted from HotkeyBaseScopeElement

			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = true;

			Started.Invoke();
		}

		private void OnInputPerformed(InputAction.CallbackContext obj)
		{
			// Copy-pasted from HotkeyBaseScopeElement

			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = false;
			m_ActionPerformed = true;

			Performed.Invoke();
		}

		private void OnInputCancel(InputAction.CallbackContext obj)
		{
			// Copy-pasted from HotkeyBaseScopeElement

			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = false;
			m_ActionPerformed = false;

			Cancelled.Invoke();
		}


		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			if (InputAction == null)
				yield break;

			InputAction action = inputContext.FindActionFor(InputAction.name);
			if (action != null) {
				yield return action;
			}
		}

		protected virtual void Update()
		{
			if (m_InputAction == null || (IndicatorRoot && !IndicatorRoot.activeSelf))
				return;

			if (m_ActionStarted) {
				float progress = m_InputAction.GetTimeoutCompletionPercentage();
				FillImage.fillAmount = progress;

#if USE_UGUI_TEXT
				if (Text) {
					Text.text = FormatText.Replace("{value}", Mathf.RoundToInt(progress * 100).ToString());
				}
#endif

#if USE_TEXT_MESH_PRO
				if (TextMeshProText) {
					TextMeshProText.text = FormatText.Replace("{value}", Mathf.RoundToInt(progress * 100).ToString());
				}
#endif

			} else if (FillImage.fillAmount != 0f) {
				FillImage.fillAmount = 0f;

#if USE_UGUI_TEXT
				if (Text) {
					Text.text = "";
				}
#endif

#if USE_TEXT_MESH_PRO
				if (TextMeshProText) {
					TextMeshProText.text = "";
				}
#endif
			}
		}

		protected virtual void OnValidate()
		{
			Utils.Validation.ValidateMissingObject(this, InputAction, nameof(InputAction));
		}
	}
}

#endif