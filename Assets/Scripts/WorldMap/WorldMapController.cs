using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.WorldMap
{
	public class WorldMapController : MonoBehaviour, ILevelLoadedListener
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

		public UI.UIObjectsTrackerController TrackerController;
		public WorldMapLocation WorldMapLocationPrefab;
		public UI.WorldMapLocationUITracker UILocationTrackerPrefab;

		private WorldPlaythroughData m_PlaythroughData;

		private List<LocationBind> m_Locations = new List<LocationBind>();

		private struct LocationBind
		{
			public string LevelID;
			public WorldMapLocation WorldLocation;
			public UI.WorldMapLocationUITracker UITracker;

			public void SetState(WorldLocationState state)
			{
				WorldLocation.SetState(state);
				UITracker?.SetState(state);
			}
		}

		public void Init(WorldPlaythroughData playthroughData)
		{
			m_PlaythroughData = playthroughData;

			m_PlaythroughData.DataIntegrityCheck();

			foreach (WorldMapLevelParamData level in m_PlaythroughData.GetAllLevels()) {
				var locationBind = new LocationBind() {
					LevelID = level.LevelID
				};

				locationBind.WorldLocation = GameObject.Instantiate(WorldMapLocationPrefab, LocationsRoot);
				locationBind.WorldLocation.name = "L-" + level.LevelID;
				locationBind.WorldLocation.transform.localPosition = new Vector3(level.WorldMapPosition.x, 0f, level.WorldMapPosition.y);
				locationBind.WorldLocation.transform.localRotation= Quaternion.identity;
				locationBind.WorldLocation.transform.localScale = Vector3.one;
				locationBind.WorldLocation.Setup(level);


				if (TrackerController && UILocationTrackerPrefab) {
					locationBind.UITracker = GameObject.Instantiate(UILocationTrackerPrefab);
					locationBind.UITracker.name = "UI-" + level.LevelID;
					locationBind.UITracker.Setup(level);
					locationBind.UITracker.Clicked += LoadLocation;

					TrackerController.StartTracking(locationBind.WorldLocation.transform, (RectTransform) locationBind.UITracker.transform);
				}

				m_Locations.Add(locationBind);
			}

			foreach(LocationBind locationBind in m_Locations) {
				WorldLocationState state = m_PlaythroughData.GetLocationState(locationBind.LevelID);
				locationBind.SetState(state);
			}
		}

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			StartCoroutine(RevealUnlockedLocations());
		}

		public void OnLevelUnloading()
		{
		}

		public IEnumerator RevealUnlockedLocations()
		{
			foreach(LocationBind locationBind in m_Locations) {
				WorldLocationState state = m_PlaythroughData.GetLocationState(locationBind.LevelID);

				if (state == WorldLocationState.Unlocked) {
					m_PlaythroughData.RevealUnlockedLevel(locationBind.LevelID);
					locationBind.SetState(WorldLocationState.Revealed);

					float duration = 0.5f;
					Sequence seq = DOTween.Sequence();
					seq.Append(locationBind.WorldLocation.transform.DOScale(0.2f, duration).From());
					seq.Insert(0f, locationBind.UITracker.transform.DOScale(0.2f, duration).From());

					yield return seq;
				}
			}
		}

		// TODO: Not used - locations are purely UI for now.
		public void TryWorldSelect(Vector2 screenPos)
		{
			Ray ray = Camera.GetComponentInChildren<Camera>().ScreenPointToRay(screenPos);

			if (Physics.Raycast(ray, out RaycastHit hitInfo)) {
				var location = hitInfo.collider.GetComponentInParent<WorldMapLocation>();
				if (location) {
					LoadLocation(location.LevelID);
				}
			}
		}

		private void LoadLocation(string levelID)
		{
			m_PlaythroughData.SetCurrentLevel(levelID);

			var supervisor = m_PlaythroughData.PrepareSupervisor();
			Game.GameManager.Instance.SwitchLevelAsync(supervisor);
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

			foreach (LocationBind locationBind in m_Locations) {
				if (locationBind.UITracker.State.IsVisible()) {
					locationBind.UITracker.SetZoomLevel(ZoomNormalized);
				}
			}
		}
	}
}