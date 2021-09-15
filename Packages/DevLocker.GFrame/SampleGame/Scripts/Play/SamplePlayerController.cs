using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Sample player controller responsible for moving the "player".
	/// It handles the two player's modes:
	/// - Jumper - moves left and right and can jump with gravity pulling it down.
	/// - Chopper - gravity is disabled and the player can move freely in all directions (i.e. flying).
	/// </summary>
	public class SamplePlayerController : MonoBehaviour
	{
		public float JumperSpeed = 2f;
		public float JumperJumpForce = 4f;

		public float ChopperSpeed = 3;

		public bool IsJumper => !m_Rigidbody.isKinematic;

		private Rigidbody m_Rigidbody;
		private Vector3 m_Velocity;

		void Awake()
		{
			m_Rigidbody = GetComponent<Rigidbody>();
		}

		#region Jumper

		public void JumperMovement(float movement)
		{
			m_Velocity = new Vector3(movement, 0f, 0f);
		}

		public void JumperJump()
		{
			if (Mathf.Abs(m_Rigidbody.velocity.y - 0f) < 0.01f) {
				m_Rigidbody.AddForce(Vector3.up * JumperJumpForce, ForceMode.Impulse);
			}
		}

		public void SwitchToChopper()
		{
			m_Rigidbody.isKinematic = true;
			m_Velocity = Vector3.zero;

			Game.SampleLevelsManager.Instance.SetLevelState(new SamplePlayChopperState());
		}

		#endregion


		#region Chopper

		public void ChopperMovement(Vector2 movement)
		{
			m_Velocity = movement;
		}

		public void SwitchToJumper()
		{
			m_Rigidbody.isKinematic = false;
			m_Velocity = Vector3.zero;

			Game.SampleLevelsManager.Instance.SetLevelState(new SamplePlayJumperState());
		}

		#endregion

		void FixedUpdate()
		{
			float speed = IsJumper ? JumperSpeed : ChopperSpeed;

			m_Rigidbody.MovePosition(m_Rigidbody.position + m_Velocity * Time.fixedDeltaTime * speed);
		}
	}
}