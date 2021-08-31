using System;

namespace TetrisTower.Game
{
	[Serializable]
	public class PlayerOptions
	{
		public enum TouchInputControlMethod
		{
			Drag,
			Swipes,
			OnScreenControls
		}

		public TouchInputControlMethod TouchInputControls = TouchInputControlMethod.Swipes;
	}
}