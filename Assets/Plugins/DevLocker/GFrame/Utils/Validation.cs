using System.Reflection;
using UnityEngine;

namespace DevLocker.GFrame.Utils
{
	public static class Validation
	{
		public static bool ValidateMissingObject(Object source, Object objValue, string fieldName = null)
		{
			if (!ReferenceEquals(objValue, null) && objValue == null) {
				if (!string.IsNullOrEmpty(fieldName)) {
					fieldName = $".{fieldName}";
				}
				Debug.LogError($"\"{source.name}\" of {source.GetType().Name}{fieldName} references missing / deleted object.", source);

				return false;
			}

			return true;
		}
	}
}
