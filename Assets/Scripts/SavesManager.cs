using DevLocker.GFrame.MessageBox;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.SystemUI;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Playthroughs;
using TetrisTower.TowerLevels.Replays;

namespace TetrisTower.Saves
{
	public static class SavesManager
	{
		public static JsonConverter[] GetConverters(GameConfig config) => new JsonConverter[] {
				new BlocksSkinSetConverter(config.AssetsRepository),
				new LevelParamAssetConverter(config.AssetsRepository),
				new WorldLevelsSetConverter(config.AssetsRepository),
				new GridShapeTemplateConverter(config.AssetsRepository),
				new Core.RandomXoShiRo128starstarJsonConverter(),
		};

		private const string ReplaysFolder = "Replays/";
		private const string PlaythroughsFolder = "Saves/";
		private const string PreferencesFolder = "Preferences/";

		private const string ReplayExtension = ".jrep";
		private const string PlaythroughsExtension = ".wsav";
		private const string PreferencesExtension = ".pref";

		public static TReturn Clone<TReturn>(object data, GameConfig config)
		{
			var serialized = Serialize<TReturn>(data, config);

			// No need to have the json "TypeNameHandling = Auto" of the root object serialized, as we specify the type in the generics parameter.
			return Deserialize<TReturn>(serialized, config);
		}

		public static TReturn Clone<TData, TReturn>(object data, GameConfig config)
		{
			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			var serialized = JsonConvert.SerializeObject(data, typeof(TData), new JsonSerializerSettings() {
				Converters = GetConverters(config),
				TypeNameHandling = TypeNameHandling.Auto,
				//Formatting = Formatting.Indented,
			});

			return Deserialize<TReturn>(serialized, config);
		}

		public static string Serialize<TData>(object data, GameConfig config)
		{
			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			return JsonConvert.SerializeObject(data, typeof(TData), new JsonSerializerSettings() {
				Converters = GetConverters(config),
				TypeNameHandling = TypeNameHandling.Auto,
				//Formatting = Formatting.Indented,
			});
		}

		public static TReturn Deserialize<TReturn>(string serializeData, GameConfig config)
		{
			// No need to have the json "TypeNameHandling = Auto" of the root object serialized, as we specify the type in the generics parameter.
			return JsonConvert.DeserializeObject<TReturn>(serializeData, new JsonSerializerSettings() {
				Converters = GetConverters(config),
				TypeNameHandling = TypeNameHandling.Auto,
			});
		}

		public static async void SaveReplay(string name, ReplayRecording recording, GameConfig config)
		{
			try {
				GameManager.Instance.GetManager<BlockingOperationOverlayController>().Block(recording);

				string content = Serialize<ReplayRecording>(recording, config);
				await Platforms.PlatformsStorage.WriteFileAsync(Path.Combine(ReplaysFolder, name + ReplayExtension), content);

				GameManager.Instance.GetManager<ToastNotificationsController>().ShowNotification("Replay saved!");

			} catch (Exception ex) {

				UnityEngine.Debug.LogException(ex);

				MessageBox.Instance.ShowSimple("Save Failed", $"Replay failed to save!", MessageBoxIcon.Error, MessageBoxButtons.OK, (Action) null);

			} finally {

				GameManager.Instance.GetManager<BlockingOperationOverlayController>().Unblock(recording);
			}
		}

		public static async Task<ReplayRecording> LoadReplay(string name, GameConfig config)
		{
			string content = await Platforms.PlatformsStorage.ReadFileAsync(Path.Combine(ReplaysFolder, name + ReplayExtension));
			return Deserialize<ReplayRecording>(content, config);
		}

		public static async void SavePlaythrough(int slot, WorldPlaythroughData playthroughData, GameConfig config)
		{
			try {
				GameManager.Instance.GetManager<BlockingOperationOverlayController>().Block(playthroughData);

				string content = Serialize<WorldPlaythroughData>(playthroughData, config);
				await Platforms.PlatformsStorage.WriteFileAsync(Path.Combine(PlaythroughsFolder, $"Story_{slot}{PlaythroughsExtension}"), content);

				GameManager.Instance.GetManager<ToastNotificationsController>().ShowNotification("Replay Saved!");

			} catch (Exception ex) {

				UnityEngine.Debug.LogException(ex);

				MessageBox.Instance.ShowSimple("Save Failed", $"Replay failed to save!", MessageBoxIcon.Error, MessageBoxButtons.OK, (Action)null);

			} finally {

				GameManager.Instance.GetManager<BlockingOperationOverlayController>().Unblock(playthroughData);
			}
		}

		public static async Task<ReplayRecording> LoadPlaythrough(int slot, GameConfig config)
		{
			string content = await Platforms.PlatformsStorage.ReadFileAsync(Path.Combine(PlaythroughsFolder, $"Story_{slot}{PlaythroughsExtension}"));
			return Deserialize<ReplayRecording>(content, config);
		}
	}

}