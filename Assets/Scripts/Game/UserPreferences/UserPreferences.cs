using Newtonsoft.Json;
using System;

namespace TetrisTower.Game.Preferences
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

		#region Audio

		float MasterVolume { get; }

		bool MusicMute { get; }
		float MusicVolume { get; }

		bool SoundsMute { get; }
		float SoundsVolume { get; }
		float AmbienceVolume { get; }


		#endregion


		#region Input

		bool DownIsDrop { get; }
		TouchInputControlMethod TouchInputControls { get; }

		#endregion
	}

	public interface IPreferencesManager
	{
		void Init(GameContext gameContext);
	}

	namespace Implementation
	{
		[JsonObject(MemberSerialization.Fields)]
		public class UserPreferences : IUserPreferences, IDisposable
		{
			[field:JsonIgnore]
			public event Action Changed;

			#region Audio

			[field: JsonProperty(nameof(MasterVolume))]
			public float MasterVolume { get; set; } = 1f;

			[field: JsonProperty(nameof(MusicMute))]
			public bool MusicMute { get; set; } = false;

			[field: JsonProperty(nameof(MusicVolume))]
			public float MusicVolume { get; set; } = 1f;



			[field: JsonProperty(nameof(SoundsMute))]
			public bool SoundsMute { get; set; } = false;

			[field: JsonProperty(nameof(SoundsVolume))]
			public float SoundsVolume { get; set; } = 1f;

			[field: JsonProperty(nameof(AmbienceVolume))]
			public float AmbienceVolume { get; set; } = 1f;

			#endregion

			#region Input

			[field:JsonProperty(nameof(DownIsDrop))]
			public bool DownIsDrop { get; set; } = false;

			[field:JsonProperty(nameof(TouchInputControls))]
			public IUserPreferences.TouchInputControlMethod TouchInputControls { get; set; } = IUserPreferences.TouchInputControlMethod.Drag;

			#endregion



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