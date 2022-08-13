using UnityEngine;

namespace DevLocker.GFrame.Pools
{
	/// <summary>
	/// Simple pool for a RectTransform prefab instances.
	/// Use to avoid hiccups when instantiating often prefabs of the same type.
	/// Don't forget to release your instance when you're done with it.
	/// </summary>
	public class RectTransformPrefabsPool : GenericComponentPrefabsPool<RectTransform>
	{

	}
}