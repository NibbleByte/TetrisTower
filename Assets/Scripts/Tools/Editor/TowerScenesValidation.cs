using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using DevLocker.Utils;

namespace UnityTools.AssetProcessingTools
{
	public static class TowerScenesValidation
	{
		[MenuItem("Tetris Tower/Scene Validate")]
		static void ValidateScene()
		{
			for(int i = 0; i < SceneManager.sceneCount; ++i) {
				Scene scene = SceneManager.GetSceneAt(i);

				foreach(Transform transform in scene.EnumerateComponentsInChildren<Transform>(true)) {

					if (GameObjectUtility.AreStaticEditorFlagsSet(transform.gameObject, StaticEditorFlags.BatchingStatic)) {
						Debug.LogError($"\"{GameObjectUtils.GetFindPath(transform)}\" has \"Static Batching\" flag set. This will not work with Split Screen mode.", transform);
					}
				}
			}
		}
	}
}
