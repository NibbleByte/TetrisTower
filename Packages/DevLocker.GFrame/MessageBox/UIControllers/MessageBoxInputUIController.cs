using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.MessageBox.UIControllers
{
	/// <summary>
	/// Displays an in-game prompt with message and input field for the user to answer.
	/// </summary>
	public class MessageBoxInputUIController : MessageBoxSimpleUIController
	{
#if USE_UGUI_TEXT
		public InputField InputField;
#endif

#if USE_TEXT_MESH_PRO
		public TMPro.TMP_InputField TMPInputField;
#endif

		public override void Init()
		{
			base.Init();

#if USE_UGUI_TEXT
			if (InputField) {
				InputField.onValidateInput += OnValidateInput;
			}
#endif

#if USE_TEXT_MESH_PRO
			if (TMPInputField) {
				TMPInputField.onValidateInput += OnValidateInput;
			}
#endif
		}

		public override void Show(MessageData data)
		{
			base.Show(data);

#if USE_UGUI_TEXT
			if (InputField) {
				InputField.text = data.SuggestedText;
				InputField.Select();
				InputField.ActivateInputField();
			}
#endif

#if USE_TEXT_MESH_PRO
			if (TMPInputField) {
				TMPInputField.text = data.SuggestedText;
				TMPInputField.Select();
				TMPInputField.ActivateInputField();
			}
#endif

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
#if USE_UGUI_TEXT
					if (InputField)
						return !string.IsNullOrWhiteSpace(InputField.text);
#endif

#if USE_TEXT_MESH_PRO
					if (TMPInputField)
						return !string.IsNullOrWhiteSpace(TMPInputField.text);
#endif

					return false;
			}

			return true;
		}

		protected override MessageBoxResponseData CreateResponseData(MessageBoxResponse result)
		{
			MessageBoxResponseData resultData = base.CreateResponseData(result);

#if USE_UGUI_TEXT
			if (InputField) {
				resultData.InputTextResponse = InputField.text.Trim();
			}
#endif

#if USE_TEXT_MESH_PRO
			if (TMPInputField) {
				resultData.InputTextResponse = TMPInputField.text.Trim();
			}
#endif

			return resultData;
		}
	}
}
