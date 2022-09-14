#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Will display progress of Hotkey interaction (e.g. hold / long press etc.)
	/// </summary>
	public class HotkeyProgressUIScopeElement : HotkeyBaseScopeElement
	{
		[Tooltip("Image to be used as a progress bar of the action. It will fill it from 0 to 1.\nLeave empty to use the image of the current game object.")]
		public Image FillImage;

		[Tooltip("Optional - Text to set the progress.")]
		public Text Text;

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

		protected override void OnEnable()
		{
			base.OnEnable();

			if (FillImage == null) {
				FillImage = GetComponent<Image>();
				if (FillImage == null) {
					Debug.LogWarning($"{nameof(HotkeyProgressUIScopeElement)} \"{name}\" has no image specified to use.", this);
					enabled = false;
					return;
				}
			}

			FillImage.fillAmount = 0f;
		}

		protected override void OnStarted()
		{
			Started.Invoke();
		}

		protected override void OnInvoke()
		{
			Performed.Invoke();
		}

		void Update()
		{
			if (m_ActionStarted) {
				float progressSum = 0f;
				foreach(InputAction action in m_SubscribedActions) {
					progressSum += action.GetTimeoutCompletionPercentage();
				}

				float averageProgress = progressSum / m_SubscribedActions.Count;
				FillImage.fillAmount = averageProgress;

				if (Text) {
					Text.text = FormatText.Replace("{value}", Mathf.RoundToInt(averageProgress * 100).ToString());
				}

#if USE_TEXT_MESH_PRO
				if (TextMeshProText) {
					TextMeshProText.text = FormatText.Replace("{value}", Mathf.RoundToInt(averageProgress * 100).ToString());
				}
#endif

			} else if (FillImage.fillAmount != 0f) {
				FillImage.fillAmount = 0f;

				if (Text) {
					Text.text = "";
				}

#if USE_TEXT_MESH_PRO
				if (TextMeshProText) {
					TextMeshProText.text = "";
				}
#endif
			}
		}
	}
}

#endif