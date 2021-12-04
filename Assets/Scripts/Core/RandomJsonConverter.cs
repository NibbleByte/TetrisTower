using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TetrisTower.Core
{
	public class RandomJsonConverter : JsonConverter<Random>
	{
		public override void WriteJson(JsonWriter writer, Random value, JsonSerializer serializer)
		{
			var binaryFormatter = new BinaryFormatter();
			using (var temp = new MemoryStream()) {
				binaryFormatter.Serialize(temp, value);
				writer.WriteValue(Convert.ToBase64String(temp.ToArray()));
			}
		}

		public override Random ReadJson(JsonReader reader, Type objectType, Random existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			byte[] bytes = Convert.FromBase64String((string)reader.Value);

			var binaryFormatter = new BinaryFormatter();
			using (var temp = new MemoryStream(bytes)) {
				return (Random)binaryFormatter.Deserialize(temp);
			}
		}
	}
}