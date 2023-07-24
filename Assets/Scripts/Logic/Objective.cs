using DevLocker.GFrame.Input;
using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace TetrisTower.Logic
{
	public enum ObjectiveStatus
	{
		InProgress,
		Completed,
		Failed,
	}

	/// <summary>
	/// Objectives base class. Based on the returned status player may win or lose the level.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public interface Objective
	{
		ObjectiveStatus Status { get; }

		void Init(PlayerStatesContext context);
		void Deinit();

		string GetDisplayText();

		void Validate(UnityEngine.Object context);
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(Objective), true)]
	public class ObjectiveDrawer : SerializeReferenceCreatorDrawer<Objective>
	{
	}
#endif

}