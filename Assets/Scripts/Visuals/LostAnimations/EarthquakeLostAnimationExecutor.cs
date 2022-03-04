using System.Collections;
using System.Collections.Generic;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.LostAnimations
{
	public class EarthquakeLostAnimationExecutor : MonoBehaviour, ILostAnimationExecutor
	{
		public int Chance => m_Chance;

		[SerializeField]
		private int m_Chance = 10;

		public float StartDelay = 5f;
		public float WatchDuration = 10f;

		public float CameraRotateSpeed = 5f;
		public Vector2 CameraShakeRange = new Vector2(-1f, 1f);

		public Vector2 ForceRange = new Vector2(20, 100);
		public ForceMode ForceMode = ForceMode.Impulse;
		public Vector2 TorqueRange = new Vector2(-10, 10);
		public ForceMode TorqueMode = ForceMode.Impulse;

		private bool m_Interrupted = false;

		public IEnumerator Execute(ConeVisualsGrid visualsGrid, Transform fallingVisualsContainer, List<KeyValuePair<GridCoords, ConeVisualsBlock>> blocks)
		{
			m_Interrupted = false;

			StartCoroutine(OperateCamera(fallingVisualsContainer));

			var camera = fallingVisualsContainer.GetComponentInChildren<Camera>().transform;
			Vector3 cameraInitialPos = camera.localPosition;

			float startTime = Time.time;

			while(Time.time < startTime + StartDelay) {
				if (m_Interrupted)
					yield break;

				camera.localPosition = cameraInitialPos + new Vector3(Random.Range(CameraShakeRange.x, CameraShakeRange.y), Random.Range(CameraShakeRange.x, CameraShakeRange.y), 0f);
				yield return null;
			}

			camera.localPosition = cameraInitialPos;

			var templateRigidbody = GetComponent<Rigidbody>();

			for(int i = 0; i < blocks.Count; ++i) {
				var pair = blocks[i];

				var rigidbody = pair.Value.gameObject.AddComponent<Rigidbody>();
				rigidbody.mass = templateRigidbody.mass;
				rigidbody.angularDrag = templateRigidbody.angularDrag;
				rigidbody.drag = templateRigidbody.drag;
				rigidbody.isKinematic = false;

				int multiplier = i % 2 == 0 ? 1 : -1;
				rigidbody.AddForce(pair.Value.transform.forward * multiplier * Random.Range(ForceRange.x, ForceRange.y), ForceMode.Impulse);
				rigidbody.AddTorque(Random.Range(TorqueRange.x, TorqueRange.y), Random.Range(TorqueRange.x, TorqueRange.y), Random.Range(TorqueRange.x, TorqueRange.y), TorqueMode);
			}

			yield return new WaitForSeconds(WatchDuration);
		}

		public void Interrupt()
		{
			m_Interrupted = true;
		}

		private IEnumerator OperateCamera(Transform fallingVisualsContainer)
		{
			while(true) {
				if (m_Interrupted)
					yield break;

				var eulerAngles = fallingVisualsContainer.eulerAngles;
				eulerAngles.y += CameraRotateSpeed * Time.deltaTime;
				fallingVisualsContainer.eulerAngles = eulerAngles;

				yield return null;
			}
		}
	}
}