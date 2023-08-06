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
		[MenuItem("Tetris Tower/Focus Camera On Mouse &3")]
		public static void FocusCamera()
		{
			if (Application.isPlaying)
				return;

			SceneView.duringSceneGui += FocusCameraOnSceneGUI;

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

			var plane = new Plane(Vector3.up, towerGO.transform.position);

			Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

			float rayDist;
			plane.Raycast(ray, out rayDist);

			Vector3 focusPos = ray.GetPoint(rayDist);

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
