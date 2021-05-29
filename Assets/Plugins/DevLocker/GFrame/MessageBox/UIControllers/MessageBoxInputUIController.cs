using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.MessageBox.UIControllers
{
	/// <summary>
	/// Displays an in-game prompt with message and input field for the user to answer.
	/// </summary>
	public class MessageBoxInputUIController : MessageBoxSimpleUIController
	{
		public InputField InputField;

#if USE_TEXT_MESH_PRO
		public TMPro.TMP_InputField TMPInputField;
#endif

		public override void Init()
		{
			base.Init();

			if (InputField) {
				InputField.onValidateInput += OnValidateInput;
			}

			if (TMPInputField) {
				TMPInputField.onValidateInput += OnValidateInput;
			}
		}

		public override void Show(MessageData data)
		{
			base.Show(data);

			if (InputField) {
				InputField.text = data.SuggestedText;
				InputField.Select();
				InputField.ActivateInputField();
			}

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
					if (InputField)
						return !string.IsNullOrWhiteSpace(InputField.text);
					if (TMPInputField)
						return !string.IsNullOrWhiteSpace(TMPInputField.text);

					return false;
			}

			return true;
		}

		protected override MessageBoxResponseData CreateResponseData(MessageBoxResponse result)
		{
			MessageBoxResponseData resultData = base.CreateResponseData(result);

			if (InputField) {
				resultData.InputTextResponse = InputField.text.Trim();
			}
			if (TMPInputField) {
				resultData.InputTextResponse = TMPInputField.text.Trim();
			}

			return resultData;
		}
	}
}
