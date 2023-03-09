using System;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Tools
{
	/// <summary>
	/// Displays logs on screen.
	/// Useful for mobile devices where development build doesn't display error logs on screen.
	/// (Otherwise you need logcat on Android to know that something happened).
	/// </summary>
	public class DebugLogDisplay : MonoBehaviour
	{
		private struct MessageEntry
		{
			public LogType Type;
			public string Message;
		}


		public LogType SeverityFilter = LogType.Assert;
		public int MessagesLimit = 20;
		public bool AscendingOrder = true;

		public Color LogColor = Color.white;
		public Color WarningColor = new Color(1f, 0.6471f, 0f);
		public Color AssertColor = Color.red;
		public Color ErrorColor = Color.red;
		public Color ExceptionColor = Color.red;

		private List<MessageEntry> m_Messages = new List<MessageEntry>();

		private GUIStyle m_MessageStyle;

		void OnEnable()
		{
			Application.logMessageReceived += OnLogMessageReceived;
		}

		void OnDisable()
		{
			Application.logMessageReceived -= OnLogMessageReceived;
		}

		private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
		{
			if ((SeverityFilter >= type && SeverityFilter != LogType.Exception) || type == LogType.Exception) {

				// Avoid repeating logs spam.
				if (m_Messages.Count == 0 || m_Messages[m_Messages.Count - 1].Message != condition) {
					m_Messages.Add(new MessageEntry() { Message = condition, Type = type });
				}
			}
		}

		private void InitStyles()
		{
			m_MessageStyle = new GUIStyle(GUI.skin.label);
			m_MessageStyle.padding = new RectOffset();
			m_MessageStyle.margin = new RectOffset(0, 0, 4, 4);
		}

		void OnGUI()
		{
			if (m_MessageStyle == null) {
				InitStyles();
			}

			if (m_Messages.Count == 0)
				return;

			// 96 DPI is default for PC monitor. Mobile have much higher DPI.
			float guiScale = Screen.dpi / 96f;

			Rect rect = new Rect(0, 0, Screen.width / guiScale, Screen.height / guiScale);

			GUIUtility.ScaleAroundPivot(Vector2.one * guiScale, Vector2.zero);	// Or else looks tiny on mobile.

			GUI.Box(rect, "");

			using (new GUILayout.AreaScope(rect)) {

				if (m_Messages.Count > MessagesLimit) {
					m_Messages.RemoveRange(0, m_Messages.Count - MessagesLimit);
				}

				GUILayout.BeginVertical();

				if (GUILayout.Button("Clear")) {
					m_Messages.Clear();
				}

				Color prevColor = GUI.color;

				for (int i = 0; i < m_Messages.Count; ++i) {
					MessageEntry entry = m_Messages[AscendingOrder ? i : m_Messages.Count - i - 1];

					switch (entry.Type) {
						case LogType.Log: GUI.color = LogColor; break;
						case LogType.Warning: GUI.color = WarningColor; break;
						case LogType.Error: GUI.color = ErrorColor; break;
						case LogType.Assert: GUI.color = AssertColor; break;
						case LogType.Exception: GUI.color = ExceptionColor; break;
					}

					GUILayout.Label(entry.Message, m_MessageStyle);
				}

				GUI.color = prevColor;

				GUILayout.EndVertical();
			}
		}
	}
}