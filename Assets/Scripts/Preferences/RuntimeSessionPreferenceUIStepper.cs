using DevLocker.GFrame.Input.UIScope;
using DevLocker.Utils;
using TetrisTower.Game;
using TetrisTower.Game.Preferences;
using UnityEngine;

namespace TetrisTower.UserPreferences
{
	[RequireComponent(typeof(UIStepperNamed))]
	public class RuntimeSessionPreferenceUIStepper : MonoBehaviour
	{
		private string GenerateKey() => GameObjectUtils.GetFindPath(transform);

		void OnEnable()
		{
			var stepper = GetComponent<UIStepperNamed>();

			stepper.SelectedIndex = (int) GameManager.Instance.GetManager<RuntimeSessionPreferencesManager>().Restore(GenerateKey(), stepper.SelectedIndex);
		}

		void OnDisable()
		{
			var stepper = GetComponent<UIStepperNamed>();

			if (GameManager.Instance) {
				GameManager.Instance.GetManager<RuntimeSessionPreferencesManager>().Store(GenerateKey(), stepper.SelectedIndex);
			}
		}
	}
}