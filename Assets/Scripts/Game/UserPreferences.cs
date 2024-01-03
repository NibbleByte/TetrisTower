using FlyingWormConsole3.FullSerializer;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TetrisTower.Game
{
	public interface IUserPreferences
	{
		event Action Changed;

		public enum TouchInputControlMethod
		{
			Drag,
			Swipes,
			OnScreenControls
		}

		bool DownIsDrop { get; }
		TouchInputControlMethod TouchInputControls { get; }


	}

	namespace Implementation
	{
		[JsonObject(MemberSerialization.Fields)]
		public class UserPreferences : IUserPreferences, IDisposable
		{
			[field:JsonIgnore]
			public event Action Changed;

			[field:JsonProperty(nameof(DownIsDrop))]
			public bool DownIsDrop { get; set; } = true;

			[field:JsonProperty(nameof(TouchInputControls))]
			public IUserPreferences.TouchInputControlMethod TouchInputControls { get; set; } = IUserPreferences.TouchInputControlMethod.Drag;



			public void ApplyFrom(UserPreferences other)
			{
				var serialized = JsonConvert.SerializeObject(other, typeof(UserPreferences), new JsonSerializerSettings() {
					TypeNameHandling = TypeNameHandling.Auto,
					//Formatting = Formatting.Indented,
				});

				JsonConvert.PopulateObject(serialized, this, new JsonSerializerSettings() {
					TypeNameHandling = TypeNameHandling.Auto,
				});

				ApplyChanges();
			}

			public UserPreferences Clone()
			{
				var clone = new UserPreferences();
				clone.ApplyFrom(this);
				return clone;
			}

			public void ApplyChanges()
			{
				Changed?.Invoke();
			}

			public void Dispose()
			{
				ApplyChanges();
			}
		}

	}
}