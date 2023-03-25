using DevLocker.GFrame.Input;
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
	public class SamplePlayerController : MonoBehaviour, ILevelLoadedListener
	{
		public float JumperSpeed = 2f;
		public float JumperJumpForce = 4f;

		public float ChopperSpeed = 3;

		public bool IsJumper => !m_Rigidbody.isKinematic;

		private Rigidbody m_Rigidbody;
		private Vector3 m_Velocity;

		private IPlayerContext m_PlayerContext;

		void Awake()
		{
			m_Rigidbody = GetComponent<Rigidbody>();
		}

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			// Implementation for ILevelLoadListener
			// Add here logic that should happen after the level was loaded.

			contextReferences.SetByType(out m_PlayerContext);
		}

		public void OnLevelUnloading()
		{
			// Implementation for ILevelLoadListener
			// Add here logic that should happen before the level was unloaded.
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

		public async void SwitchToChopper()
		{
			m_Rigidbody.isKinematic = true;
			m_Velocity = Vector3.zero;

			await m_PlayerContext.GetPlayerLevelStateStack().SetStateAsync(new SamplePlayChopperState());
		}

		#endregion


		#region Chopper

		public void ChopperMovement(Vector2 movement)
		{
			m_Velocity = movement;
		}

		public async void SwitchToJumper()
		{
			m_Rigidbody.isKinematic = false;
			m_Velocity = Vector3.zero;

			await m_PlayerContext.GetPlayerLevelStateStack().SetStateAsync(new SamplePlayJumperState());
		}

		#endregion

		void FixedUpdate()
		{
			float speed = IsJumper ? JumperSpeed : ChopperSpeed;

			m_Rigidbody.MovePosition(m_Rigidbody.position + m_Velocity * Time.fixedDeltaTime * speed);
		}
	}
}