using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TetrisTower.SystemUI
{
	public class ToastNotificationElement : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField]
		private TMP_Text m_Message;

		public event Action<ToastNotificationElement> CloseRequested;

		public void SetMessage(string message)
		{
			m_Message.text = message;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			CloseRequested?.Invoke(this);
		}
	}
}