using System;
using UnityEngine;


namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Defines focus layer for scopes to group by.
	/// Scopes activation is limited to the same focus layer.
	/// </summary>
	[CreateAssetMenu(fileName = "Unknown_UIScopeFocusLayer", menuName = "GFrame/UI Scope Focus Layer", order = 1014)]
	public class UIScopeFocusLayer : ScriptableObject, IComparable, IComparable<UIScopeFocusLayer>
	{
		public enum InputBehaviourType
		{
			PreserveInput = 0,
			IsolateScopeElementsAndUIInput = 2,
			IsolateScopeElementsOnlyInput = 4,
		}

		[Tooltip("Higher priority will prevent lower ones to steal the focus.")]
		public int Priority = 0;

		[Tooltip("The root scope of this focus layer decides how input is handled: preserve the input or push new input mask on the stack.")]
		public InputBehaviourType InputBehaviour = InputBehaviourType.IsolateScopeElementsAndUIInput;

		public bool Equals(UIScopeFocusLayer other)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (ReferenceEquals(other, null))
				return false;

			return Priority == other.Priority && InputBehaviour == other.InputBehaviour;
		}

		public override bool Equals(object obj)
		{
			return obj is UIScopeFocusLayer other ? Equals(other) : false;
		}

		public override int GetHashCode()
		{
			//https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode

			int hash = 17;
			unchecked {
				hash = hash * 23 + Priority.GetHashCode();
				hash = hash * 23 + InputBehaviour.GetHashCode();
			}

			return hash;
		}

		public static bool operator ==(UIScopeFocusLayer a, UIScopeFocusLayer b)
		{
			if (ReferenceEquals(a, b))
				return true;

			if (ReferenceEquals(a, null))
				return false;

			return a.Equals(b);
		}
		public static bool operator !=(UIScopeFocusLayer a, UIScopeFocusLayer b)
		{
			return !(a == b);
		}

		public static bool operator<(UIScopeFocusLayer left, UIScopeFocusLayer right)
		{
			// Empty is always considered lesser.
			if (ReferenceEquals(left, null) && !ReferenceEquals(right, null))
				return true;

			if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
				return false;

			return left.Priority < right.Priority;
		}

		public static bool operator<=(UIScopeFocusLayer left, UIScopeFocusLayer right)
		{
			return left < right || left == right ;
		}

		public static bool operator>(UIScopeFocusLayer left, UIScopeFocusLayer right)
		{
			// Empty is always considered lesser.
			if (!ReferenceEquals(left, null) && ReferenceEquals(right, null))
				return true;

			if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
				return false;

			return left.Priority > right.Priority;
		}

		public static bool operator >=(UIScopeFocusLayer left, UIScopeFocusLayer right)
		{
			return left > right || left == right;
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as UIScopeFocusLayer);
		}

		public int CompareTo(UIScopeFocusLayer other)
		{
			if (ReferenceEquals(other, null))
				return 1;

			return Priority.CompareTo(other.Priority);
		}
	}
}