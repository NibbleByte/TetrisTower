using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.Utils;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
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
		public Transform LinksRoot;

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
		public LineRenderer WorldMapLocationsLinkPrefab;
		public UI.WorldMapLocationUITracker UILocationTrackerPrefab;

		private WorldPlaythroughData m_PlaythroughData;

		private List<LocationBind> m_Locations = new List<LocationBind>();
		private List<LinksBind> m_Links = new List<LinksBind>();

		private struct LinksBind
		{
			public WorldLevelsLink Link;
			public LineRenderer LineRenderer;
		}

		private struct LocationBind
		{
			public string LevelID => LevelParam.LevelID;
			public WorldMapLevelParamData LevelParam;
			public WorldMapLocation WorldLocation;
			public UI.WorldMapLocationUITracker UITracker;

			public void SetState(WorldPlaythroughData.WorldLevelAccomplishment accomplishment)
			{
				WorldLocation.SetState(accomplishment.State);
				UITracker?.SetState(accomplishment.State, accomplishment.HighestScore, LevelParam.CalculateStarsEarned(accomplishment.HighestScore));
			}
		}

		public void Init(WorldPlaythroughData playthroughData, GameConfig gameConfig)
		{
			m_PlaythroughData = playthroughData;

			m_PlaythroughData.DataIntegrityCheck();

			foreach (WorldMapLevelParamData level in m_PlaythroughData.GetAllLevels()) {
				var locationBind = new LocationBind() {
					LevelParam = level,
				};

				locationBind.WorldLocation = GameObject.Instantiate(WorldMapLocationPrefab, LocationsRoot);
				locationBind.WorldLocation.name = "L-" + level.LevelID;
				locationBind.WorldLocation.transform.localPosition = new Vector3(level.WorldMapPosition.x, 0f, level.WorldMapPosition.y);
				locationBind.WorldLocation.transform.localRotation= Quaternion.identity;
				locationBind.WorldLocation.transform.localScale = WorldMapLocationPrefab.transform.localScale;
				locationBind.WorldLocation.Setup(level);


				if (TrackerController && UILocationTrackerPrefab) {
					locationBind.UITracker = GameObject.Instantiate(UILocationTrackerPrefab);
					locationBind.UITracker.name = "UI-" + level.LevelID;
					locationBind.UITracker.Setup(level, gameConfig.StarEarnedSprite, gameConfig.StarMissingSprite);
					locationBind.UITracker.Clicked += OnLocationSelected;

					TrackerController.StartTracking(locationBind.WorldLocation.transform, (RectTransform) locationBind.UITracker.transform);
				}

				m_Locations.Add(locationBind);
			}

			foreach(LocationBind locationBind in m_Locations) {
				WorldLocationState state = m_PlaythroughData.GetLocationState(locationBind.LevelID);
				locationBind.SetState(m_PlaythroughData.GetAccomplishment(locationBind.LevelID));
			}

			foreach(WorldLevelsLink link in m_PlaythroughData.GetAllLevelLinks()) {
				var linksBind = new LinksBind() {
					Link = link,
				};

				LocationBind location1 = GetLocationBind(link.LevelID1);
				LocationBind location2 = GetLocationBind(link.LevelID2);

				linksBind.LineRenderer = GameObject.Instantiate(WorldMapLocationsLinkPrefab, LinksRoot);
				linksBind.LineRenderer.transform.ResetTransform();
				linksBind.LineRenderer.SetPositions(new Vector3[] {
					// Root is raised a bit above the ground so no z-fighting is happening.
					location1.WorldLocation.transform.localPosition,
					location2.WorldLocation.transform.localPosition
				});

				bool shouldDisplay =
					location1.WorldLocation.State >= WorldLocationState.Revealed &&
					location2.WorldLocation.State >= WorldLocationState.Revealed;

				linksBind.LineRenderer.gameObject.SetActive(shouldDisplay);

				m_Links.Add(linksBind);
			}
		}

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			StartCoroutine(RevealReachedLocations());
		}

		public void OnLevelUnloading()
		{
		}

		public IEnumerator RevealReachedLocations()
		{
			foreach(LocationBind locationBind in m_Locations) {
				WorldLocationState state = m_PlaythroughData.GetLocationState(locationBind.LevelID);

				bool playAnimation = false;

				// Cheat just unlocked these - play the animation.
				if (state == WorldLocationState.Unlocked && locationBind.WorldLocation.State < WorldLocationState.Unlocked) {
					locationBind.SetState(m_PlaythroughData.GetAccomplishment(locationBind.LevelID));

					playAnimation = true;
				}

				if (state == WorldLocationState.Reached) {
					m_PlaythroughData.RevealReachedLevel(locationBind.LevelID);

					// Auto-unlock if possible.
					if (m_PlaythroughData.CanUnlock(locationBind.LevelID, out _)) {
						m_PlaythroughData.UnlockRevealedLevel(locationBind.LevelID);
					}

					// Can change in one of two states.
					locationBind.SetState(m_PlaythroughData.GetAccomplishment(locationBind.LevelID));

					playAnimation = true;
				}

				if (playAnimation) {

					float duration = 0.5f;
					Sequence seq = DOTween.Sequence();
					seq.Append(locationBind.WorldLocation.transform.DOScale(0.2f, duration).From());
					seq.Insert(0f, locationBind.UITracker.PlayRevealAnimation(duration));


					// Show the links too
					foreach(var link in GetRevealedLinksFor(locationBind.LevelID)) {

						if (link.LineRenderer.gameObject.activeSelf) // Should not happen?
							continue;

						link.LineRenderer.gameObject.SetActive(true);
						var endColor = new Color2(link.LineRenderer.startColor, link.LineRenderer.endColor);
						var startColor = endColor;
						startColor.ca.a = 0f;
						startColor.cb.a = 0f;
						link.LineRenderer.startColor = startColor.ca;
						link.LineRenderer.endColor = startColor.cb;
						seq.Insert(0f, link.LineRenderer.DOColor(startColor, endColor, duration));
					}

					yield return seq;
				}
			}
		}

		public void RefreshLocation(string levelID)
		{
			LocationBind locationBind = GetLocationBind(levelID);
			locationBind.SetState(m_PlaythroughData.GetAccomplishment(locationBind.LevelID));

			foreach (var link in GetRevealedLinksFor(locationBind.LevelID)) {
				link.LineRenderer.gameObject.SetActive(true);
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

		private LocationBind GetLocationBind(string levelID) => m_Locations.FirstOrDefault(lb => lb.LevelID == levelID);

		private IEnumerable<LinksBind> GetLinksFor(string levelID)
		{
			foreach (LinksBind linksBind in m_Links) {
				if (linksBind.Link.LevelID1 == levelID)
					yield return linksBind;

				if (linksBind.Link.LevelID2 == levelID)
					yield return linksBind;
			}
		}

		private IEnumerable<LinksBind> GetRevealedLinksFor(string levelID)
		{
			if (GetLocationBind(levelID).WorldLocation.State < WorldLocationState.Revealed) {
				yield break;
			}

			foreach (var link in GetLinksFor(levelID)) {

				string otherLocationID = link.Link.LevelID1 == levelID
					? link.Link.LevelID2
					: link.Link.LevelID1
					;

				var otherLocation = GetLocationBind(otherLocationID);
				if (otherLocation.WorldLocation.State >= WorldLocationState.Revealed) {
					yield return link;
				}
			}
		}

		private void OnLocationSelected(string levelID)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed || UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed) {
				FindAnyObjectByType<WorldMapDebugAPI>().CompleteLevel(levelID);
				return;
			}
#endif
			WorldLocationState state = m_PlaythroughData.GetLocationState(levelID);
			switch(state) {
				case WorldLocationState.Revealed:
					UnlockLocation(levelID);
					break;

				case WorldLocationState.Unlocked:
				case WorldLocationState.Completed:
					LoadLocation(levelID);
					break;

				default: // Do nothing.
					break;
			}
		}

		private void UnlockLocation(string levelID)
		{
			LocationBind locationBind = GetLocationBind(levelID);

			if (m_PlaythroughData.CanUnlock(levelID, out _)) {
				m_PlaythroughData.UnlockRevealedLevel(levelID);
				RefreshLocation(levelID);
				locationBind.UITracker?.PlayUnlockAnimation(0.08f);
			} else {
				locationBind.UITracker?.PlayRejectShake(0.2f);
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

#if UNITY_EDITOR
				Vector3 localPos = locationBind.WorldLocation.transform.localPosition;
				Vector2 mapPos = locationBind.LevelParam.WorldMapPosition;
				if (localPos.x != mapPos.x || localPos.z != mapPos.y) {
					locationBind.WorldLocation.transform.localPosition = new Vector3(mapPos.x, 0f, mapPos.y);
				}
#endif
			}
		}
	}
}