using System.Collections.Generic;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.WorldMap
{
	public class WorldMapController : MonoBehaviour
	{
		public Transform Discworld;

		public Transform Camera;

		public Transform Sun;

		public Transform LocationsRoot;

		public Vector2 MoveRange = new Vector2(-200f, 0f);
		public Vector2 ZoomRange = new Vector2(50f, 150f);

		public float RotateSpeed = 20f;
		public float MoveSpeed = 16f;
		public float ZoomSpeed = 20f;

		public float SunSpeed = 20f;

		// Normalized means 0 is farthest out, 1 is on the discworld surface (which should be impossible).
		public float ZoomNormalized => 1 - Camera.localPosition.y / ZoomRange.y;

		public AnimationCurve ZoomToDrag = AnimationCurve.Linear(0f, 0.4f, 1f, 0f);
		public float DragDamp = 0.2f;

		public WorldMapLocation WorldMapLocation;

		private WorldPlaythroughData m_PlaythroughData;

		private List<WorldMapLocation> m_Locations = new List<WorldMapLocation>();

		public void Init(WorldPlaythroughData playthroughData)
		{
			m_PlaythroughData = playthroughData;

			foreach(WorldMapLevelParamAsset level in playthroughData.LevelsSet.Levels) {
				WorldMapLocation location = GameObject.Instantiate(WorldMapLocation, LocationsRoot);
				location.name = "L-" + level.LevelID;
				location.transform.localPosition = new Vector3(level.WorldMapPosition.x, 0f, level.WorldMapPosition.y);
				location.LevelID = level.LevelID;

				m_Locations.Add(location);
			}
		}

		public void TryWorldSelect(Vector2 screenPos)
		{
			Ray ray = Camera.GetComponentInChildren<Camera>().ScreenPointToRay(screenPos);

			if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
				var location = hitInfo.collider.GetComponentInParent<WorldMapLocation>();
				if (location) {
					m_PlaythroughData.SetCurrentLevel(location.LevelID);

					var supervisor = m_PlaythroughData.PrepareSupervisor();
					Game.GameManager.Instance.SwitchLevelAsync(supervisor);
				}
			}
		}

		public WorldMapLocation GetLevelParamForLocation(string levelID)
		{
			foreach(WorldMapLocation location in m_Locations) {
				if (location.LevelID == levelID) {
					return location;
				}
			}

			return null;
		}

		public void RotateDiscworld(float rotate)
		{
			if (rotate == 0f)
				return;

			Discworld.Rotate(Vector3.up, rotate * Time.deltaTime * RotateSpeed);
			transform.Rotate(Vector3.up, -rotate * Time.deltaTime * RotateSpeed * 0.25f, Space.Self);
		}

		public void MoveCamera(float move)
		{
			if (move == 0f)
				return;

			Vector3 pos = Camera.localPosition;
			pos.z = Mathf.Clamp(pos.z + move * Time.deltaTime * MoveSpeed, MoveRange.x, MoveRange.y);
			Camera.localPosition = pos;
		}

		public void ZoomCamera(float zoom)
		{
			if (zoom == 0f)
				return;

			Vector3 pos = Camera.localPosition;
			pos.y = Mathf.Clamp(pos.y - zoom * Time.deltaTime * ZoomSpeed, ZoomRange.x, ZoomRange.y);
			Camera.localPosition = pos;
		}

		void Update()
		{
			Sun.Rotate(Vector3.up, SunSpeed * Time.deltaTime);
		}
	}
}