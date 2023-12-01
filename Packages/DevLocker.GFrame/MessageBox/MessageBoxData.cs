using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevLocker.GFrame.MessageBox
{
	public enum MessageMode
	{
		SimpleMessageBox,
		InputMessageBox,
		ProcessingBox,

		CustomUIController = 50,		// Used with custom types.
	}

	public enum MessageBoxIcon
	{
		None,
		Error,
		Stop,
		Information,
		Warning,
		Question,
	}

	[Flags]
	public enum MessageBoxButtons
	{
		None = 0,

		OK = 1 << 0,
		Cancel = 1 << 1,
		Retry = 1 << 2,
		Abort = 1 << 3,
		Ignore = 1 << 4,
		Yes = 1 << 5,
		No = 1 << 6,

		OKCancel = OK | Cancel,
		RetryCancel = Retry | Cancel,
		AbortRetryIgnore = Abort | Retry | Ignore,
		YesNo = Yes | No,
		YesNoCancel = Yes | No | Cancel,

		All = ~0,

		LastButton = No,
	}

	public static class MessageBoxButtonsExtensions
	{
		public static MessageBoxResponse ToResponse(this MessageBoxButtons response) => response switch
		{
			MessageBoxButtons.OK => MessageBoxResponse.OK,
			MessageBoxButtons.Cancel => MessageBoxResponse.Cancel,

			MessageBoxButtons.Retry => MessageBoxResponse.Retry,
			MessageBoxButtons.Abort => MessageBoxResponse.Abort,
			MessageBoxButtons.Ignore => MessageBoxResponse.Ignore,

			MessageBoxButtons.Yes => MessageBoxResponse.Yes,
			MessageBoxButtons.No => MessageBoxResponse.No,

			_ => throw new ArgumentException($"Unrecognized MessageBoxButtons value {response}")
		};
	}

	public enum MessageBoxResponse
	{
		None,
		OK,
		Cancel,
		Abort,
		Retry,
		Ignore,
		Yes,
		No,
	}

	public static class MessageBoxResponseExtensions
	{
		public static bool IsConfirm(this MessageBoxResponse response) =>
			response == MessageBoxResponse.OK ||
			response == MessageBoxResponse.Yes ||
			response == MessageBoxResponse.Retry
			;

		public static bool IsDeny(this MessageBoxResponse response) =>
			response == MessageBoxResponse.Cancel ||
			response == MessageBoxResponse.No ||
			response == MessageBoxResponse.Abort ||
			response == MessageBoxResponse.Ignore
			;
	}

	public class MessageData
	{
		public MessageData(MessageMode mode, MessageBoxResponseCallback callback)
		{
			MessageMode = mode;
			Callback = callback;
		}

		public delegate void MessageBoxResponseCallback(MessageBoxResponseData responseData);

		public MessageMode MessageMode;
		public Type UIControllerType;   // Overrides MessageMode and searches for this specific type directly. Useful for extensions.
		public string Title;
		public string Subtitle;
		public string Content;
		public string SuggestedText;
		public UnityEngine.UI.InputField.OnValidateInput InputValidator;
		public MessageBoxIcon Icon;
		public MessageBoxButtons Buttons;
		public Dictionary<MessageBoxButtons, string> ButtonsOverrideLabels;
		public IMessageBoxProgressTracker ProgressTracker;

		public MessageBoxResponseCallback Callback;
		public object Sender;
		public object UserData;

		/// <summary>
		/// Await on the response. In that case you may not provide actual callback.
		/// </summary>
		public async Task<MessageBoxResponseData> WaitResponseAsync()
		{
			MessageBoxResponseData? response = null;
			Callback += (r) => {
				response = r;
			};

			while(!response.HasValue) {
				await Task.Yield();
			}

			return await Task.FromResult(response.Value);
		}
	}

	public struct MessageBoxResponseData
	{
		public MessageBoxResponse MessageResponse;
		public bool ConfirmResponse => MessageResponse.IsConfirm();
		public bool DenyResponse => MessageResponse.IsDeny();

		public string InputTextResponse;
		public object Sender;
		public object UserData;
	}
}