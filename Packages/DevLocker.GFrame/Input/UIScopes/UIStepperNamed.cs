using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Stepper UI control (i.e. arrows to the left and right with specific options to choose from)
	/// </summary>
	public class UIStepperNamed : Selectable, IPointerClickHandler, ISubmitHandler
	{
		public enum Axis
		{
			Horizontal = 0,
			Vertical = 1,
		}

		public int SelectedIndex {
			get => m_SelectedIndex;
			set => SetSelectedIndexClamped(value, sendCallback: true);
		}

		public IReadOnlyCollection<string> Options {
			get => m_Options;
			set {
				m_Options = value.ToArray();
				SetSelectedIndexClamped(m_SelectedIndex, sendCallback: true);

				OptionsChanged?.Invoke();
			}
		}

		public string SelectedOption => m_Options.Length > 0 ? m_Options[m_SelectedIndex] : string.Empty;

		[Header("Stepper")]
		[SerializeField]
		private TMPro.TMP_Text m_DisplayText;

		[SerializeField]
		private Button m_NextStep;

		[SerializeField]
		private Button m_PrevStep;

		[Tooltip("Which input axis should be used to change the selected option (keyboard and controllers)? Up/down or left/right.")]
		public Axis InputAxis;

		[SerializeField]
		private int m_SelectedIndex;

		[SerializeField]
		private string[] m_Options = new string[0];

		[Tooltip("Should selecting next/previous option wrap around?")]
		public bool WrapOptions = false;

		[Serializable]
		public class StepperEvent : UnityEvent<int>
		{ }

		public StepperEvent SelectedIndexChanged = new StepperEvent();

		[Tooltip("If this selectable is clicked instead of the next/prev buttons.")]
		public Button.ButtonClickedEvent MainSelectableClick = new Button.ButtonClickedEvent();

		public event Action OptionsChanged;

		public void SelectNextOption()
		{
			int index = WrapOptions ? (m_SelectedIndex + 1) % m_Options.Length : m_SelectedIndex + 1;
			SetSelectedIndexClamped(index, sendCallback: true);
		}

		public void SelectPrevOption()
		{
			int index = WrapOptions ? m_SelectedIndex - 1 : Mathf.Clamp(m_SelectedIndex - 1, 0, m_Options.Length - 1);
			if (index < 0) {
				index += m_Options.Length;
			}

			SetSelectedIndexClamped(index, sendCallback: true);
		}

		public void SetSelectedIndexWithoutNotify(int index)
		{
			SetSelectedIndexClamped(index, false);
		}

		protected override void Start()
		{
			base.Start();

			if (!Application.isPlaying)
				return;

			// Clamp and refresh.
			m_SelectedIndex = Mathf.Clamp(m_SelectedIndex, 0, m_Options.Length - 1);
			RefreshDisplay();

			if (m_NextStep) {
				m_NextStep.onClick.RemoveListener(SelectNextOption); // If assembly isn't reloaded.
				m_NextStep.onClick.AddListener(SelectNextOption);
			}

			if (m_PrevStep) {
				m_PrevStep.onClick.RemoveListener(SelectPrevOption); // If assembly isn't reloaded.
				m_PrevStep.onClick.AddListener(SelectPrevOption);
			}
		}

		private void SetSelectedIndexClamped(int index, bool sendCallback)
		{
			index = Mathf.Clamp(index, 0, m_Options.Length - 1);
			if (index != m_SelectedIndex) {
				m_SelectedIndex = index;

				RefreshDisplay();

				if (sendCallback) {
					SelectedIndexChanged.Invoke(index);
				}
			}
		}

		private void RefreshDisplay()
		{
			if (m_DisplayText) {
				m_DisplayText.text = SelectedOption;
			}

			if (m_NextStep) {
				m_NextStep.interactable = WrapOptions || m_SelectedIndex < m_Options.Length - 1;
			}
			if (m_PrevStep) {
				m_PrevStep.interactable = WrapOptions || m_SelectedIndex > 0;
			}
		}

		public override void OnMove(AxisEventData eventData)
		{
			if (InputAxis == Axis.Horizontal && (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)) {
				if (eventData.moveDir == MoveDirection.Right) {
					SelectNextOption();
				} else {
					SelectPrevOption();
				}

				return;
			}

			if (InputAxis == Axis.Vertical && (eventData.moveDir == MoveDirection.Up || eventData.moveDir == MoveDirection.Down)) {
				if (eventData.moveDir == MoveDirection.Down) {
					SelectNextOption();
				} else {
					SelectPrevOption();
				}

				return;
			}

			base.OnMove(eventData);
		}

		#region Copied from UnityEngine.UI.Button

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			Press();
		}

		public virtual void OnSubmit(BaseEventData eventData)
		{
			Press();

			// if we get set disabled during the press
			// don't run the coroutine.
			if (!IsActive() || !IsInteractable())
				return;

			DoStateTransition(SelectionState.Pressed, false);
			StartCoroutine(OnFinishSubmit());
		}

		private IEnumerator OnFinishSubmit()
		{
			var fadeTime = colors.fadeDuration;
			var elapsedTime = 0f;

			while (elapsedTime < fadeTime) {
				elapsedTime += Time.unscaledDeltaTime;
				yield return null;
			}

			DoStateTransition(currentSelectionState, false);
		}

		private void Press()
		{
			if (!IsActive() || !IsInteractable())
				return;

			MainSelectableClick.Invoke();
		}

		#endregion

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			// Step buttons can't be selectable.
			if (m_NextStep && m_NextStep.navigation.mode != Navigation.Mode.None) {
				var navigation = m_NextStep.navigation;
				navigation.mode = Navigation.Mode.None;
				m_NextStep.navigation = navigation;
				UnityEditor.EditorUtility.SetDirty(m_NextStep);
			}

			if (m_PrevStep && m_PrevStep.navigation.mode != Navigation.Mode.None) {
				var navigation = m_PrevStep.navigation;
				navigation.mode = Navigation.Mode.None;
				m_PrevStep.navigation = navigation;
				UnityEditor.EditorUtility.SetDirty(m_PrevStep);
			}
		}
#endif
	}
}