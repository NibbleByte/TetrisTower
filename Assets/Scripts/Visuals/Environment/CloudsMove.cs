using UnityEngine;

namespace TetrisTower.Visuals.Environment
{
	public class CloudsMove : MonoBehaviour
	{
		public Vector3 MoveSpeed;

		void Update()
		{
			transform.position += MoveSpeed * Time.deltaTime;
		}
	}

}