using System;
using System.Collections;
using System.Collections.Generic;
using DevLocker.GFrame.MessageBox.UIControllers;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.MessageBox
{
	/// <summary>
	/// Displays an in-game modal popup prompting the user for some response. There are different types of prompts:
	/// - Simple text.
	/// - Text input field for user to enter any text.
	/// - Progress bar for the user to wait (or cancel).
	///
	/// NOTE: This just shows the message UI, handles button clicks and invokes user callback with the response.
	///		  This DOES NOT handle Input in any way! Input systems can be widely different and
	///		  it is up to you to handle it properly (confirm and deny hotkeys, gamepad, UI clicks).
	///		  Depending on the input setup your project uses, you have a few options:
	///		  - Use the GFrame.UIHotkeys behaviours with Unity Input System:
	///		    attach a HotkeyScope and set InputBinding mask valid only for the MessageBox.
	///		  - Implement or extend the UIController classes. When UIController is shown, activate the proper input actions.
	///		  - Subscribe for the MessageShown and MessageClosed events. When invoked, adapt the input according to your needs.
	///		    You may invoke ForceConfirmShownMessage() and ForceDenyShownMessage() for confirm and deny hotkeys.
	///		  - Combination of the above.
	/// </summary>
	public class MessageBox : MonoBehaviour
	{
		public static MessageBox Instance { get; private set; }

#pragma warning disable 0649  // never assigned to

		[SerializeField]
		[Tooltip("Will be displayed when a message is shown.\nTo be used for blocking clicks (and maybe simple background).")]
		private GameObject InputBlocker;

		[SerializeField]
		private List<MessageBoxUIControllerBind> m_UIControllers;

#pragma warning disable 0649

		public bool IsShowingMessage => ShownData != null;
		public MessageData ShownData { get; private set; }
		private List<MessageData> m_PendingData = new List<MessageData>();

		public event Action MessageShown;
		public event Action MessageClosed;

		/// <summary>
		/// Show message box in the specified configuration.
		/// </summary>
		/// <param name="data">Detailed parameters of what the message box should display.</param>
		public void Show(MessageData data)
		{
			if (data.MessageMode == MessageMode.CustomUIController && data.UIControllerType == null)
				throw new ArgumentException("Trying to start a custom message with no specified type.");

			bool wasShowingMessage = ShownData != null;

			if (ShownData != null) {
				m_PendingData.Add(ShownData);
				GetUIController(ShownData).Close();
			}

			ShownData = data;

			InputBlocker?.SetActive(true);
			GetUIController(ShownData).Show(ShownData);

			if (!wasShowingMessage) {
				MessageShown?.Invoke();
			}
		}

		private void OnMessageBoxResponse(MessageBoxResponseData responseData)
		{
			responseData.Sender = ShownData.Sender;
			responseData.UserData = ShownData.UserData;

			var userCallback = ShownData.Callback;
			ShownData = null;

			userCallback?.Invoke(responseData);


			// Show next pending message, but check if the user callback didn't show another one in the mean time.
			if (m_PendingData.Count > 0 && ShownData == null) {
				MessageData poppedData = m_PendingData[m_PendingData.Count - 1];
				m_PendingData.RemoveAt(m_PendingData.Count - 1);

				Show(poppedData);
			}

			if (ShownData == null) {
				InputBlocker?.SetActive(false);
				MessageClosed?.Invoke();
			}
		}

		/// <summary>
		/// Closes the currently shown message and calls the response callback as if the user confirmed it.
		/// Next message will pop immediately if there are pending.
		/// Useful for custom input on confirm action pressed.
		/// </summary>
		/// <returns>Returns true if successful.</returns>
		public bool ForceConfirmShownMessage()
		{
			if (ShownData == null)
				return false;

			var confirmButton = ShownData.Buttons & (MessageBoxButtons.Yes | MessageBoxButtons.OK | MessageBoxButtons.Retry);
			if (confirmButton == MessageBoxButtons.None)
				return false;

			return GetUIController(ShownData).TryInvokeResponse(confirmButton.ToResponse());
		}

		/// <summary>
		/// Closes the currently shown message and calls the response callback as if the user denied it.
		/// Next message will pop immediately if there are pending.
		/// Useful for custom input on confirm action pressed.
		/// </summary>
		/// <returns>Returns true if successful.</returns>
		public bool ForceDenyShownMessage()
		{
			if (ShownData == null)
				return false;

			var denyButton = ShownData.Buttons & (MessageBoxButtons.Cancel | MessageBoxButtons.Ignore |  MessageBoxButtons.No);
			if (denyButton == MessageBoxButtons.None)
				return false;

			return GetUIController(ShownData).TryInvokeResponse(denyButton.ToResponse());
		}

		/// <summary>
		/// Closes the provided message (if shown) and calls the response callback with the provided response, bypassing the UI.
		/// If message was pending it will be removed from the pending list.
		/// Next message will pop immediately if there are pending.
		/// </summary>
		/// <returns>Returns true if successful.</returns>
		public bool ForceResponseMessage(MessageData messageData, MessageBoxResponseData responseData)
		{
			if (messageData == ShownData) {
				OnMessageBoxResponse(responseData);

				return true;

			} else {
				bool removed = m_PendingData.Remove(messageData);

				if (removed) {
					responseData.Sender = messageData.Sender;
					responseData.UserData = messageData.UserData;

					var userCallback = messageData.Callback;

					userCallback?.Invoke(responseData);
				}

				return removed;
			}
		}

		/// <summary>
		/// Closes all messages. No response callbacks will be called.
		/// Useful on exiting the game or level.
		/// </summary>
		public void ForceCloseAllMessages()
		{
			bool wasShowingMessage = ShownData != null;

			if (ShownData != null) {
				GetUIController(ShownData).Close();
				ShownData = null;
			}


			m_PendingData.Clear();

			if (wasShowingMessage) {
				InputBlocker?.SetActive(false);
				MessageClosed?.Invoke();
			}
		}

		/// <summary>
		/// Closes provided messages. No response callbacks will be called.
		/// </summary>
		public bool ForceCloseMessage(MessageData messageData)
		{
			if (messageData == ShownData) {

				GetUIController(ShownData).Close();
				ShownData = null;

				if (m_PendingData.Count > 0) {
					MessageData poppedData = m_PendingData[m_PendingData.Count - 1];
					m_PendingData.RemoveAt(m_PendingData.Count - 1);

					Show(poppedData);
				}

				if (ShownData == null) {
					InputBlocker?.SetActive(false);
					MessageClosed?.Invoke();
				}

				return true;

			} else {
				return m_PendingData.Remove(messageData);
			}

		}

		private IMessageBoxUIController GetUIController(MessageData data)
		{
			if (data.UIControllerType == null) {
				foreach (MessageBoxUIControllerBind bind in m_UIControllers) {
					if (bind.MessageMode == data.MessageMode)
						return bind.Controller;
				}

				throw new ArgumentException($"Not supported ui controller {data.MessageMode}");

			} else {

				foreach (MessageBoxUIControllerBind bind in m_UIControllers) {
					if (data.UIControllerType.IsAssignableFrom(bind.Controller.GetType()))
						return bind.Controller;
				}

				throw new ArgumentException($"Not supported ui controller {data.UIControllerType.Name}");
			}
		}

		void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(this);
				return;
			}

			Instance = this;

			if (transform.parent == null) {
				DontDestroyOnLoad(gameObject);
			}

			InputBlocker?.SetActive(false);

			foreach (MessageBoxUIControllerBind bind in m_UIControllers) {
				if (bind.Controller == null) {
					Debug.LogError($"MessageBox has refers to {bind.ControllerGameObject.name} UIController game object that doesn't have an IMessageBoxUIController component!", bind.ControllerGameObject);
				}

				bind.Controller.UserMadeChoice += OnMessageBoxResponse;
				bind.Controller.Init();
			}
		}

		void OnDestroy()
		{
			if (Instance == this) {
				Instance = null;
			}
		}
	}

	public static class MessageBoxExtensions
	{
		/// <summary>
		/// Show a modal dialog to the user and wait for confirmation.
		/// String parameters are optional.
		/// </summary>
		/// <param name="content">message to be shown on the screen or question</param>
		/// <param name="icon">icon in the top left corner of the dialog, giving visual feedback for the text content</param>
		/// <param name="buttons">configuration of buttons</param>
		/// <param name="callback">callback handler when the user presses one of the buttons in the dialog</param>
		/// <param name="sender">sender or context who initiated the call</param>
		/// <param name="userData">user data to be passed down with the result</param>
		public static MessageData ShowSimple(this MessageBox instance,
			string content,
			MessageBoxIcon icon,
			MessageBoxButtons buttons,
			MessageData.MessageBoxResponseCallback callback,
			object sender = null,
			object userData = null
			)
		{
			var messageData = new MessageData(MessageMode.SimpleMessageBox, callback) {
				Content = content,
				Icon = icon,
				Buttons = buttons,
				Sender = sender,
				UserData = userData,
			};

			instance.Show(messageData);

			return messageData;
		}

		/// <summary>
		/// Show a modal dialog to the user and wait for confirmation.
		/// String parameters are optional.
		/// </summary>
		/// <param name="title">title of the message</param>
		/// <param name="content">message to be shown on the screen or question</param>
		/// <param name="icon">icon in the top left corner of the dialog, giving visual feedback for the text content</param>
		/// <param name="buttons">configuration of buttons</param>
		/// <param name="callback">callback handler when the user presses one of the buttons in the dialog</param>
		/// <param name="sender">sender or context who initiated the call</param>
		/// <param name="userData">user data to be passed down with the result</param>
		public static MessageData ShowSimple(this MessageBox instance,
			string title,
			string content,
			MessageBoxIcon icon,
			MessageBoxButtons buttons,
			MessageData.MessageBoxResponseCallback callback,
			object sender = null,
			object userData = null
			)
		{
			var messageData = new MessageData(MessageMode.SimpleMessageBox, callback) {
				Title = title,
				Content = content,
				Icon = icon,
				Buttons = buttons,
				Sender = sender,
				UserData = userData,
			};

			instance.Show(messageData);

			return messageData;
		}
		/// <summary>
		/// Show a modal dialog to the user and wait for confirmation.
		/// String parameters are optional.
		/// </summary>
		/// <param name="title">title of the message</param>
		/// <param name="content">message to be shown on the screen or question</param>
		/// <param name="icon">icon in the top left corner of the dialog, giving visual feedback for the text content</param>
		/// <param name="buttons">configuration of buttons</param>
		/// <param name="confirmCallback">callback handler to be called if the user confirms the message</param>
		/// <param name="sender">sender or context who initiated the call</param>
		/// <param name="userData">user data to be passed down with the result</param>
		public static MessageData ShowSimple(this MessageBox instance,
			string title,
			string content,
			MessageBoxIcon icon,
			MessageBoxButtons buttons,
			Action confirmCallback,
			object sender = null,
			object userData = null
			)
		{
			MessageData.MessageBoxResponseCallback callback = (response) => {
				if (response.ConfirmResponse) {
					confirmCallback?.Invoke();
				}
			};

			var messageData = new MessageData(MessageMode.SimpleMessageBox, callback) {
				Title = title,
				Content = content,
				Icon = icon,
				Buttons = buttons,
				Sender = sender,
				UserData = userData,
			};

			instance.Show(messageData);

			return messageData;
		}

		/// <summary>
		/// Show a modal dialog to the user and wait for confirmation.
		/// String parameters are optional.
		/// </summary>
		/// <param name="title">title of the message</param>
		/// <param name="content">message to be shown on the screen or question</param>
		/// <param name="icon">icon in the top left corner of the dialog, giving visual feedback for the text content</param>
		/// <param name="buttons">configuration of buttons</param>
		/// <param name="buttonsOverrideLabels">override the texts of some of the buttons</param>
		/// <param name="callback">callback handler when the user presses one of the buttons in the dialog</param>
		/// <param name="sender">sender or context who initiated the call</param>
		/// <param name="userData">user data to be passed down with the result</param>
		public static MessageData ShowSimple(this MessageBox instance,
			string title,
			string content,
			MessageBoxIcon icon,
			MessageBoxButtons buttons,
			Dictionary<MessageBoxButtons, string> buttonsOverrideLabels,
			MessageData.MessageBoxResponseCallback callback,
			object sender = null,
			object userData = null
			)
		{
			var messageData = new MessageData(MessageMode.SimpleMessageBox, callback) {
				Title = title,
				Content = content,
				Icon = icon,
				Buttons = buttons,
				ButtonsOverrideLabels = buttonsOverrideLabels,
				Sender = sender,
				UserData = userData,
			};

			instance.Show(messageData);

			return messageData;
		}

		// TODO async/await version

		// TODO
		//public IEnumerator ShowSimpleCrt(string content, MessageBoxIcon icon, MessageBoxButtons buttons,
		//	ByRef<MessageBoxCallbackResult> result, object sender = null)
		//{
		//	bool finished = false;
		//	MessageBoxCallback callback = res => {
		//		if (result != null) {
		//			result.Value = res;
		//		}
		//
		//		finished = true;
		//	};
		//	ShowSimple(content, icon, buttons, callback, sender);
		//	while (!finished) {
		//		yield return NextUpdate.NextFrame;
		//	}
		//}

		/// <summary>
		/// Show a modal input dialog with suggested text to the user and wait for confirmation
		/// </summary>
		/// <param name="title">title of the message</param>
		/// <param name="content">message to be shown on the screen or question</param>
		/// <param name="suggestedText">predefined text that the input field will contain</param>
		/// <param name="icon">icon in the top left corner of the dialog, giving visual feedback for the text content</param>
		/// <param name="buttons">configuration of buttons</param>
		/// <param name="callback">callback handler when the user presses one of the buttons in the dialog</param>
		public static MessageData ShowInput(this MessageBox instance,
			string title,
			string content,
			string suggestedText,
			InputField.OnValidateInput validateHandler,
			MessageBoxIcon icon,
			MessageBoxButtons buttons,
			MessageData.MessageBoxResponseCallback callback,
			object sender = null,
			object userData = null
			)
		{
			var messageData = new MessageData(MessageMode.InputMessageBox, callback) {
				Title = title,
				Content = content,
				SuggestedText = suggestedText,
				InputValidator = validateHandler,
				Icon = icon,
				Buttons = buttons,
				Sender = sender,
				UserData = userData,
			};

			instance.Show(messageData);

			return messageData;
		}

		/// <summary>
		/// Show processing modal dialog to the user that shows progress bar and user waits for thing to be processed.
		/// </summary>
		/// <param name="title">title of the message</param>
		/// <param name="subtitle">subtitle of the message</param>
		/// <param name="content">message to be shown on the screen or question</param>
		/// <param name="icon">icon in the top left corner of the dialog, giving visual feedback for the text content</param>
		/// <param name="buttons">configuration of buttons</param>
		/// <param name="buttonsOverrideLabels">button texts (if needed). null means don't override anything.</param>
		/// <param name="progressTracker">polled for the progress of the task. Progress will be displayed via a progress bar</param>
		/// <param name="callback">callback handler when the user presses one of the buttons in the dialog</param>
		/// <param name="sender">sender or context who initiated the call</param>
		/// <param name="userData">user data to be passed down with the result</param>
		public static MessageData ShowProcessing(this MessageBox instance,
			string title,
			string subtitle,
			string content,
			MessageBoxIcon icon,
			MessageBoxButtons buttons,
			Dictionary<MessageBoxButtons, string> buttonsOverrideLabels,
			IMessageBoxProgressTracker progressTracker,
			MessageData.MessageBoxResponseCallback callback,
			object sender = null,
			object userData = null
			)
		{
			var messageData = new MessageData(MessageMode.ProcessingBox, callback) {
				Title = title,
				Subtitle = subtitle,
				Content = content,
				Icon = icon,
				Buttons = buttons,
				ButtonsOverrideLabels = buttonsOverrideLabels,
				ProgressTracker = progressTracker,
				Sender = sender,
				UserData = userData,
			};

			instance.Show(messageData);

			return messageData;
		}
	}
}