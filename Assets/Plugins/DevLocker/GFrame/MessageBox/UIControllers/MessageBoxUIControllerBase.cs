using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.MessageBox.UIControllers
{
	/// <summary>
	/// MessageBox UI controller.
	/// Implement different classes for the respective message box modes.
	/// </summary>
	public interface IMessageBoxUIController
	{
		public event Action<MessageBoxResponseData> UserMadeChoice;

		public void Init();

		public void Show(MessageData data);

		public void Close();

		public bool TryInvokeResponse(MessageBoxResponse response);
	}

	/// <summary>
	/// Base class for all MessageBox UI controllers of all message box modes.
	/// If different implementation is needed, feel free to inherit this class.
	/// </summary>
	public abstract class MessageBoxUIControllerBase : MonoBehaviour, IMessageBoxUIController
	{
		public event Action<MessageBoxResponseData> UserMadeChoice;

		public List<MessageBoxIconBind> Icons;

		public List<MessageBoxButtonBind> Buttons;

		protected MessageData m_ShownData;

		protected Dictionary<MessageBoxButtons, string> m_ButtonsOriginalLabels = new Dictionary<MessageBoxButtons, string>();

		public virtual void Init()
		{
			gameObject.SetActive(false);

			foreach (MessageBoxIconBind bind in Icons) {
				bind.Visual.gameObject.SetActive(false);
			}

			foreach (MessageBoxButtonBind bind in Buttons) {
				bind.Button.gameObject.SetActive(false);

				var button = bind.Button; // Careful! Closure!
				UnityAction handler = () => { OnButtonClick(button); };

				bind.Button.onClick.AddListener(handler);
			}
		}

		public virtual void Show(MessageData data)
		{
			// Should not happen but just in case.
			if (m_ShownData != null) {
				Close();
			}

			m_ShownData = data;

			foreach (MessageBoxIconBind bind in Icons) {
				if (bind.Icon == m_ShownData.Icon) {
					bind.Visual.gameObject.SetActive(true);
				}
			}

			foreach (Button button in GetButtons(m_ShownData.Buttons)) {
				button.gameObject.SetActive(true);
			}

			// This should be done last for optimization reasons.
			gameObject.SetActive(true);

			// Make sure overrides happen after buttons are enabled, so localization don't rewrite them.
			if (m_ShownData.ButtonsOverrideLabels != null) {
				m_ButtonsOriginalLabels.Clear();

				OverrideButtonTexts();
			}


			var confirmButton = GetActiveConfirmButton();
			if (confirmButton) {
				EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
			}
		}


		public virtual void Close()
		{
			if (m_ShownData == null)
				return;

			foreach (MessageBoxIconBind bind in Icons) {
				bind.Visual.gameObject.SetActive(false);
			}

			foreach (MessageBoxButtonBind bind in Buttons) {
				bind.Button.gameObject.SetActive(false);
			}

			// Restore button labels to normal.
			if (m_ShownData.ButtonsOverrideLabels != null) {
				RestoreButtonTexts();
			}

			gameObject.SetActive(false);

			m_ShownData = null;
		}

		protected virtual void OverrideButtonTexts()
		{
			foreach (var pair in m_ShownData.ButtonsOverrideLabels) {
				var button = GetButtons(pair.Key).First();

				var label = button.GetComponentInChildren<Text>();
				if (label) {
					m_ButtonsOriginalLabels.Add(pair.Key, label.text);
					// Cache those every time in case the texts changed due to switched localization.
					label.text = pair.Value;
					continue;
				}

#if USE_TEXT_MESH_PRO
				var textMeshProlabel = button.GetComponentInChildren<TMPro.TextMeshPro>();
				if (textMeshProlabel) {
					m_ButtonsOriginalLabels.Add(pair.Key, textMeshProlabel.text);
					// Cache those every time in case the texts changed due to switched localization.
					textMeshProlabel.text = pair.Value;
					continue;
				}
#endif
			}
		}
		protected virtual void RestoreButtonTexts()
		{
			foreach (var pair in m_ShownData.ButtonsOverrideLabels) {
				var button = GetButtons(pair.Key).First();

				var label = button.GetComponentInChildren<Text>();
				if (label) {
					label.text = m_ButtonsOriginalLabels[pair.Key];
					continue;
				}

#if USE_TEXT_MESH_PRO
				var textMeshProlabel = button.GetComponentInChildren<TMPro.TextMeshPro>();
				if (textMeshProlabel) {
					textMeshProlabel.text = m_ButtonsOriginalLabels[pair.Key];
					continue;
				}
#endif
			}
		}

		protected IEnumerable<Button> GetButtons(MessageBoxButtons buttonMask)
		{
			bool found = false;
			foreach (MessageBoxButtonBind bind in Buttons) {
				if ((buttonMask & bind.ButtonType) != 0) {
					found = true;
					yield return bind.Button;
				}
			}

			if (!found) {
				throw new ArgumentException($"Unrecognized MessageBoxButtons value {buttonMask}");
			}
		}

		protected Button GetActiveConfirmButton()
		{
			return GetButtons(MessageBoxButtons.Yes | MessageBoxButtons.OK | MessageBoxButtons.Retry)
				.FirstOrDefault(b => b.gameObject.activeSelf);
		}

		protected Button GetActiveDenyButton()
		{
			return GetButtons(MessageBoxButtons.Cancel | MessageBoxButtons.Ignore | MessageBoxButtons.Abort | MessageBoxButtons.No)
				.FirstOrDefault(b => b.gameObject.activeSelf);
		}

		private void OnButtonClick(Button button)
		{
			foreach (MessageBoxButtonBind bind in Buttons) {
				if (bind.Button == button) {
					var response = bind.ButtonType.ToResponse();

					TryInvokeResponse(response);
					return;
				}
			}

			throw new ArgumentException($"Unrecognized MessageBoxButtons value {button?.name}");
		}

		public bool TryInvokeResponse(MessageBoxResponse response)
		{
			if (ValidateResponse(response)) {
				MessageBoxResponseData resultData = CreateResponseData(response);
				Close();
				UserMadeChoice?.Invoke(resultData);
				return true;
			}

			return false;
		}

		protected virtual bool ValidateResponse(MessageBoxResponse result)
		{
			return true;
		}

		protected virtual MessageBoxResponseData CreateResponseData(MessageBoxResponse result)
		{
			var resultData = new MessageBoxResponseData
			{
				MessageResponse = result,
			};

			return resultData;
		}

		// TODO
		//protected virtual bool OnInputEventInternal(InputEvent ev)
		//{
		//	if (ev.Type == InputEventType.Pressed) {
		//		if (ev.Name == "Cancel") {
		//			Button denyButton = GetActiveDenyButton();
		//			if (denyButton) {
		//				denyButton.onClick?.Invoke();
		//				return true;
		//			}
		//		}
		//		else if (ev.Name == "Submit" || ev.Name == "ConfirmAction") {
		//
		//			if (_input.InputType == InputType.Joystick) {
		//				FreeCursorController cursor = GameUtl.GetFreeCursorController();
		//				if (cursor != null) {
		//					if (cursor.IsSelectableUnderTheCursor()) {
		//						//if cursor is over a button - do not handle hotkeys
		//						return false;
		//					}
		//				}
		//			}
		//
		//			Button confirmButton = GetActiveConfirmButton();
		//			if (confirmButton) {
		//				confirmButton.onClick?.Invoke();
		//				return true;
		//			}
		//		}
		//	}
		//
		//	return false;
		//}
	}
}
