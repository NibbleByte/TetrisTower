using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Logs the message when the button gets pressed.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class SampleUITesterButtonLogger : MonoBehaviour
	{
		public string AdditionalMessage;

		void Awake()
		{
			GetComponent<Button>().onClick.AddListener(OnButtonClicked);
		}

		private void OnDestroy()
		{
			GetComponent<Button>().onClick.RemoveListener(OnButtonClicked);
		}

		private void OnButtonClicked()
		{
			string buttonText = GetComponentInChildren<Text>().text;
			Debug.Log($"{buttonText} was pressed! {AdditionalMessage}", this);
		}
	}
}