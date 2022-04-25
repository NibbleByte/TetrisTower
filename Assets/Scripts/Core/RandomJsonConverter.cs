using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Xoshiro.PRNG32;

namespace TetrisTower.Core
{
	// Use this as System.Random is no longer serializable.
	// https://issuetracker.unity3d.com/issues/error-is-thrown-when-attempting-to-serialize-system-dot-random
	// https://sourceforge.net/p/xoshiroprng-net/code/ci/default/tree/
	public class RandomXoShiRo128starstarJsonConverter : JsonConverter<Random>
	{
		private static FieldInfo[] m_Fields;

		private static void CacheFields()
		{
			if (m_Fields == null) {
				m_Fields = typeof(XoShiRo128starstar).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			}
		}

		public override void WriteJson(JsonWriter writer, Random value, JsonSerializer serializer)
		{
			CacheFields();

			if (value == null) {
				writer.WriteNull();
				return;
			}

			writer.WriteStartArray();
			foreach(var field in m_Fields) {
				uint stateValue = (uint) field.GetValue(value);
				writer.WriteValue(stateValue.ToString());
			}
			writer.WriteEndArray();
		}

		public override Random ReadJson(JsonReader reader, Type objectType, Random existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			CacheFields();

			if (hasExistingValue)
				return existingValue;

			if (reader.TokenType == JsonToken.Null)
				return null;

			List<uint> intValues = new List<uint>();

			while (reader.Read()) {
				if (reader.TokenType == JsonToken.EndArray)
					break;

				intValues.Add(uint.Parse((string)reader.Value));
			}

			var value = new XoShiRo128starstar();
			for(int i = 0; i < m_Fields.Length; ++i) {
				m_Fields[i].SetValue(value, intValues[i]);
			}

			return value;
		}
	}



	// More info: https://issuetracker.unity3d.com/issues/error-is-thrown-when-attempting-to-serialize-system-dot-random
	[Obsolete("No longer used as new versions of dotnet are deprecating BinaryFormatter. System.Random is no logner marked as [Serializable]")]
	public class RandomJsonConverter : JsonConverter<Random>
	{
		public override void WriteJson(JsonWriter writer, Random value, JsonSerializer serializer)
		{
			if (value == null) {
				writer.WriteNull();
				return;
			}

			var binaryFormatter = new BinaryFormatter();
			using (var temp = new MemoryStream()) {
				binaryFormatter.Serialize(temp, value);
				writer.WriteValue(Convert.ToBase64String(temp.ToArray()));
			}
		}

		public override Random ReadJson(JsonReader reader, Type objectType, Random existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (hasExistingValue)
				return existingValue;

			if (reader.Value == null)
				return null;

			byte[] bytes = Convert.FromBase64String((string)reader.Value);

			var binaryFormatter = new BinaryFormatter();
			using (var temp = new MemoryStream(bytes)) {
				return (Random)binaryFormatter.Deserialize(temp);
			}
		}
	}
}