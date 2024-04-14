using System.Collections.Generic;
using System.IO;
using System.Linq;
using TetrisTower.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTools.AssetProcessingTools
{
	public static class FocusCameraOnMouseTool
	{
		[MenuItem("Tetris Tower/Focus Camera On Mouse &F3")]
		public static void FocusCamera()
		{
			if (Application.isPlaying)
				return;

			if (SceneView.lastActiveSceneView.autoRepaintOnSceneChange) {
				SceneView.duringSceneGui += FocusCameraOnSceneGUI;
			} else {
				FocusCameraOnGameView();
			}

			UnsubscribeForRevert();
			SubscribeForRevert();
		}

		private static void SubscribeForRevert()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorSceneManager.sceneSaving += OnSceneSaving;
			PrefabStage.prefabStageOpened += OnPrefabStageChanged;
			PrefabStage.prefabStageClosing += OnPrefabStageChanged;
		}

		private static void UnsubscribeForRevert()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorSceneManager.sceneSaving -= OnSceneSaving;
			PrefabStage.prefabStageOpened -= OnPrefabStageChanged;
			PrefabStage.prefabStageClosing -= OnPrefabStageChanged;
		}

		private static void FocusCameraOnGameView()
		{
			GameObject rotatorGO = GameObject.Find("TowerRotator");
			if (rotatorGO == null) {
				Debug.LogError("No camera rotator found!");
				return;
			}

			GameObject towerGO = GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag);
			if (towerGO == null) {
				Debug.LogError("No tower placeholder found.");
				return;
			}

			var camera = rotatorGO.GetComponentInChildren<Camera>();
			Vector3 mousePos = Event.current.mousePosition;	// Turns out this works in the MenuItem call.
			mousePos.y = Screen.height - mousePos.y;
			Ray ray = camera.ScreenPointToRay(mousePos);

			FocusRotatorTo(rotatorGO, towerGO, ray);
		}

		private static void FocusCameraOnSceneGUI(SceneView sceneView)
		{
			SceneView.duringSceneGui -= FocusCameraOnSceneGUI;

			GameObject rotatorGO = GameObject.Find("TowerRotator");
			if (rotatorGO == null) {
				Debug.LogError("No camera rotator found!");
				return;
			}

			GameObject towerGO = GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag);
			if (towerGO == null) {
				Debug.LogError("No tower placeholder found.");
				return;
			}

			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			FocusRotatorTo(rotatorGO, towerGO, ray);
		}

		private static void FocusRotatorTo(GameObject rotatorGO, GameObject towerGO, Ray ray)
		{
			Vector3 focusPos;
			if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
				focusPos = hitInfo.point;

			} else {

				var plane = new Plane(Vector3.up, towerGO.transform.position);

				if (!plane.Raycast(ray, out float rayDist))
					return;

				focusPos = ray.GetPoint(rayDist);
			}


			focusPos.y = rotatorGO.transform.position.y;
			rotatorGO.transform.LookAt(focusPos, Vector3.up);
			rotatorGO.transform.Rotate(0, 180f, 0f);
		}

		private static void RevertCameraOrientation()
		{
			UnsubscribeForRevert();

			GameObject rotatorGO = GameObject.Find("TowerRotator");
			if (rotatorGO == null) {
				Debug.LogError("No camera rotator found!");
				return;
			}

			PrefabUtility.RevertObjectOverride(rotatorGO.transform, InteractionMode.AutomatedAction);
		}

		#region Event handlers for revert

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state) {
				case PlayModeStateChange.ExitingEditMode:
					RevertCameraOrientation();
					break;
			}
		}

		private static void OnSceneSaving(Scene scene, string path)
		{
			RevertCameraOrientation();
		}

		private static void OnPrefabStageChanged(PrefabStage obj)
		{
			RevertCameraOrientation();
		}

		#endregion
	}
}
