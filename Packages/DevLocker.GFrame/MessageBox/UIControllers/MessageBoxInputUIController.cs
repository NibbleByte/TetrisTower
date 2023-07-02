using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.MessageBox.UIControllers
{
	/// <summary>
	/// Displays an in-game prompt with message and input field for the user to answer.
	/// </summary>
	public class MessageBoxInputUIController : MessageBoxSimpleUIController
	{
		public TMPro.TMP_InputField TMPInputField;

		public override void Init()
		{
			base.Init();

			if (TMPInputField) {
				TMPInputField.onValidateInput += OnValidateInput;
			}
		}

		public override void Show(MessageData data)
		{
			base.Show(data);

			if (TMPInputField) {
				TMPInputField.text = data.SuggestedText;
				TMPInputField.Select();
				TMPInputField.ActivateInputField();
			}
		}

		private char OnValidateInput(string text, int charIndex, char addedChar)
		{
			return m_ShownData?.InputValidator?.Invoke(text, charIndex, addedChar) ?? addedChar;
		}

		protected override bool ValidateResponse(MessageBoxResponse result)
		{
			switch (result) {
				case MessageBoxResponse.Yes:
				case MessageBoxResponse.OK:
				case MessageBoxResponse.Retry:
					if (TMPInputField)
						return !string.IsNullOrWhiteSpace(TMPInputField.text);

					return false;
			}

			return true;
		}

		protected override MessageBoxResponseData CreateResponseData(MessageBoxResponse result)
		{
			MessageBoxResponseData resultData = base.CreateResponseData(result);

			if (TMPInputField) {
				resultData.InputTextResponse = TMPInputField.text.Trim();
			}

			return resultData;
		}
	}
}
