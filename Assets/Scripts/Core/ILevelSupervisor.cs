using System.Collections;

namespace TetrisTower.Core
{
	public interface ILevelSupervisor
	{
		IEnumerator Load();

		IEnumerator Unload();
	}

}