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
		public Text UGUIText;

#if USE_TEXT_MESH_PRO
		public TMPro.TextMeshPro TextMeshProText;
#endif

		public string Text {
			get {

				if (UGUIText) {
					return UGUIText.text;
				}

#if USE_TEXT_MESH_PRO
				if (TextMeshProText) {
					return TextMeshProText.text;
				}
#endif
				return string.Empty;
			}

			set {

				if (UGUIText) {
					UGUIText.text = value;
				}

#if USE_TEXT_MESH_PRO
				if (TextMeshProText) {
					TextMeshProText.text = value;
				}
#endif
			}
		}
	}

}
