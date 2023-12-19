using Newtonsoft.Json;
using System;
using UnityEngine;

namespace TetrisTower.Core
{
	public class SerializableAsset : ScriptableObject
	{
		public string SEID;	// As in SerializationId

#if UNITY_EDITOR
		private void OnValidate()
		{
			string guid;
			long localId;
			UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out guid, out localId);
			bool isSubAsset = UnityEditor.AssetDatabase.IsSubAsset(this);
			string actualSEID = isSubAsset ? $"{guid}|{localId}" : guid;

			if (SEID != actualSEID) {
				SEID = actualSEID;
				UnityEditor.EditorUtility.SetDirty(this);
			}
		}
#endif
	}

    public class SerializableAssetConverter<T> : JsonConverter<T> where T : SerializableAsset
    {
        public AssetsRepository Repository { get; private set; }

        public SerializableAssetConverter(AssetsRepository repository)
        {
            Repository = repository;
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            writer.WriteValue(value.SEID);
        }

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string seid = (string)reader.Value;

            return Repository.GetAssetById<T>(seid);
        }
    }
}