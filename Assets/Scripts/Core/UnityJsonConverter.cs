using Newtonsoft.Json;
using System;
using UnityEngine;

namespace TetrisTower.Core
{
	public class Vector3JsonConverter : JsonConverter<Vector3>
	{
		public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
		{
			writer.WriteStartArray();
			{
				serializer.Serialize(writer, value.x);
				serializer.Serialize(writer, value.y);
				serializer.Serialize(writer, value.z);
			}
			writer.WriteEndArray();
		}

		public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			reader.Read();
			existingValue.x = serializer.Deserialize<float>(reader);
			reader.Read();
			existingValue.y = serializer.Deserialize<float>(reader);
			reader.Read();
			existingValue.z = serializer.Deserialize<float>(reader);

			reader.Read();
			if (reader.TokenType != JsonToken.EndArray)
				throw new InvalidOperationException("Array end missing.");

			return existingValue;
		}
	}

	public class Vector2JsonConverter : JsonConverter<Vector2>
	{
		public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
		{
			writer.WriteStartArray();
			{
				serializer.Serialize(writer, value.x);
				serializer.Serialize(writer, value.y);
			}
			writer.WriteEndArray();
		}

		public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			reader.Read();
			existingValue.x = serializer.Deserialize<float>(reader);
			reader.Read();
			existingValue.y = serializer.Deserialize<float>(reader);

			reader.Read();
			if (reader.TokenType != JsonToken.EndArray)
				throw new InvalidOperationException("Array end missing.");

			return existingValue;
		}
	}
}