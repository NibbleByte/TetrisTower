using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenReplaysEntryElement : MonoBehaviour
	{
		public TMP_Text EntryLabel;

		public Button PlayButton;
		public Button DeleteButton;

		public string ReplayName { get; private set; }
		private System.Action<HomeScreenReplaysEntryElement> m_PlayCallback;
		private System.Action<HomeScreenReplaysEntryElement> m_DeleteCallback;

		void Awake()
		{
			PlayButton.onClick.AddListener(OnPlayClicked);
			DeleteButton.onClick.AddListener(OnDeleteClicked);
		}

		public void Init(string replayName, System.Action<HomeScreenReplaysEntryElement> playCallback, System.Action<HomeScreenReplaysEntryElement> deleteCallback)
		{
			ReplayName = replayName;

			EntryLabel.text = replayName;

			m_PlayCallback = playCallback;
			m_DeleteCallback = deleteCallback;
		}

		private void OnPlayClicked()
		{
			m_PlayCallback?.Invoke(this);
		}

		private void OnDeleteClicked()
		{
			m_DeleteCallback?.Invoke(this);
		}
	}

}