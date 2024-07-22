using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Stepper UI control (i.e. arrows to the left and right modifying a number)
	/// </summary>
	public class UIStepperNumbered : Selectable, IPointerClickHandler, ISubmitHandler
	{
		public enum Axis
		{
			Horizontal = 0,
			Vertical = 1,
		}

		public int SelectedValue {
			get => m_SelectedValue;
			set => SetSelectedValueClamped(value, sendCallback: true);
		}

		[Header("Stepper")]
		[SerializeField]
		private TMPro.TMP_Text m_DisplayText;

		[SerializeField]
		private Button m_NextStep;

		[SerializeField]
		private Button m_PrevStep;

		[Tooltip("Which input axis should be used to change the selected value (keyboard and controllers)? Up/down or left/right.")]
		public Axis InputAxis;

		[SerializeField]
		private int m_SelectedValue;

		[Tooltip("Min value allowed")]
		public int MinValue = 0;
		[Tooltip("Max value allowed. If less than MinValue, no maximum is set.")]
		public int MaxValue = 0;

		[Tooltip("Step to increase/decrease.")]
		public int Step = 1;

		[Tooltip("Should selecting next/previous option wrap around?")]
		public bool WrapValues = false;

		public int WorkingRange => MaxValue - MinValue + 1;

		[Serializable]
		public class StepperEvent : UnityEvent<int>
		{ }

		public StepperEvent SelectedValueChanged = new StepperEvent();

		[Tooltip("If this selectable is clicked instead of the next/prev buttons.")]
		public Button.ButtonClickedEvent MainSelectableClick = new Button.ButtonClickedEvent();

		public void SelectNextValue()
		{
			int value = m_SelectedValue + Step;
			if (WrapValues) {
				while (value > MaxValue && MaxValue > MinValue) {
					value -= WorkingRange;
				}
			}
			SetSelectedValueClamped(value, sendCallback: true);
		}

		public void SelectPrevValue()
		{
			int value = m_SelectedValue - Step;
			if (WrapValues) {
				while (value < MinValue && MaxValue > MinValue) {
					value += WorkingRange;
				}
			}

			SetSelectedValueClamped(value, sendCallback: true);
		}

		public void SetSelectedValueWithoutNotify(int value)
		{
			SetSelectedValueClamped(value, false);
		}

		protected override void Start()
		{
			base.Start();

			// Clamp and refresh.
			m_SelectedValue = Mathf.Clamp(m_SelectedValue, MinValue, MaxValue > MinValue ? MaxValue : int.MaxValue);
			RefreshDisplay();

			if (m_NextStep) {
				m_NextStep.onClick.RemoveListener(SelectNextValue);	// If assembly isn't reloaded.
				m_NextStep.onClick.AddListener(SelectNextValue);
			}

			if (m_PrevStep) {
				m_PrevStep.onClick.RemoveListener(SelectPrevValue); // If assembly isn't reloaded.
				m_PrevStep.onClick.AddListener(SelectPrevValue);
			}
		}

		private void SetSelectedValueClamped(int value, bool sendCallback)
		{
			value = Mathf.Clamp(value, MinValue, MaxValue > MinValue ? MaxValue : int.MaxValue);
			if (value != SelectedValue) {
				m_SelectedValue = value;

				RefreshDisplay();

				if (sendCallback) {
					SelectedValueChanged.Invoke(value);
				}
			}
		}

		private void RefreshDisplay()
		{
			m_DisplayText.text = m_SelectedValue.ToString();

			m_NextStep.interactable = WrapValues || MaxValue <= MinValue || m_SelectedValue < MaxValue;
			m_PrevStep.interactable = WrapValues || m_SelectedValue > MinValue;
		}

		public override void OnMove(AxisEventData eventData)
		{
			if (InputAxis == Axis.Horizontal && (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)) {
				if (eventData.moveDir == MoveDirection.Right) {
					SelectNextValue();
				} else {
					SelectPrevValue();
				}

				return;
			}

			if (InputAxis == Axis.Vertical && (eventData.moveDir == MoveDirection.Up || eventData.moveDir == MoveDirection.Down)) {
				if (eventData.moveDir == MoveDirection.Down) {
					SelectNextValue();
				} else {
					SelectPrevValue();
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

			if (Step < 1) {
				Step = 1;
				UnityEditor.EditorUtility.SetDirty(this);
			}

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