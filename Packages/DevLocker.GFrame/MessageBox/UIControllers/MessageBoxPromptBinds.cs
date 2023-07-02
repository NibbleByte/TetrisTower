using System;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.MessageBox.UIControllers
{
	[Serializable]
	public class MessageBoxIconBind
	{
		public MessageBoxIcon Icon;
		public GameObject Visual;
	}

	[Serializable]
	public class MessageBoxButtonBind
	{
		public MessageBoxButtons ButtonType;
		public Button Button;
	}

	[Serializable]
	public class MessageBoxUIControllerBind
	{
		public MessageMode MessageMode;

		// This is needed in order to set reference to the controllers, because IMessageBoxUIController doesn't inherit from MonoBehaviour.
		[Tooltip("GameObject that has IMessageBoxUIController component.")]
		public GameObject ControllerGameObject;

		private IMessageBoxUIController m_Controller;
		public IMessageBoxUIController Controller => m_Controller ?? (m_Controller = ControllerGameObject.GetComponent<IMessageBoxUIController>());
	}

	[Serializable]
	public struct MessageBoxUIText
	{
		public TMPro.TextMeshProUGUI TextMeshProText;

		public string Text {
			get {
				if (TextMeshProText) {
					return TextMeshProText.text;
				}

				return string.Empty;
			}

			set {
				if (TextMeshProText) {
					TextMeshProText.text = value;
				}
			}
		}
	}

}
