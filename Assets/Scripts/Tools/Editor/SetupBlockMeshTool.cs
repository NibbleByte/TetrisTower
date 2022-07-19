using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.AssetProcessingTools
{
	public static class SetupBlockMeshTool
	{
		[MenuItem("Assets/Tetris Tower/Setup Block Mesh", validate = true)]
		public static bool SetupBlockMeshValidate()
		{
			return Selection.activeObject is Mesh || Selection.activeObject is GameObject;
		}

		[MenuItem("Assets/Tetris Tower/Setup Block Mesh")]
		public static void SetupBlockMesh()
		{
			Mesh mesh = Selection.activeObject as Mesh;
			string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (mesh == null) {
				mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
			}

			if (mesh == null)
				return;

			assetPath = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath) + "-Mesh.asset");
			mesh = Object.Instantiate(mesh);

			float minX = float.MaxValue, maxX = float.MinValue;
			float minY = float.MaxValue, maxY = float.MinValue;
			float minZ = float.MaxValue, maxZ = float.MinValue;

			Vector3[] vertices = mesh.vertices;

			foreach(var vertex in vertices) {
				if (vertex.x < minX) minX = vertex.x;
				if (vertex.x > maxX) maxX = vertex.x;

				if (vertex.y < minY) minY = vertex.y;
				if (vertex.y > maxY) maxY = vertex.y;

				if (vertex.z < minZ) minZ = vertex.z;
				if (vertex.z > maxZ) maxZ = vertex.z;
			}

			float distX = maxX - minX;
			float distY = maxY - minY;
			float distZ = maxZ - minZ;


			// Paint vertex color channels 0 to 1 (looking at the front):
			//   R: left to right
			//   G: bottom to top
			//   B: back to front

			Color[] colors = new Color[vertices.Length];
			for(int i = 0; i < vertices.Length; ++i) {
				Vector3 vertex = vertices[i];
				float ratioX = (vertex.x - minX) / distX;
				float ratioY = (vertex.y - minY) / distY;
				float ratioZ = (vertex.z - minZ) / distZ;

				colors[i] = new Color(1f - ratioX, ratioY, ratioZ);
			}

			mesh.colors = colors;

			AssetDatabase.CreateAsset(mesh, assetPath);
			AssetDatabase.SaveAssets();

			// Do this to force the editor to flush the mesh to disk.
			// Else it is broken and in the project window the old version is displayed, although the new one was written to disk.
			// Reimport any shader or code seems to fix this.
			AssetDatabase.ImportAsset(AssetDatabase.FindAssets("t:Shader", new string[] { "Assets/Art" }).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(), ImportAssetOptions.ForceSynchronousImport);
		}
	}
}
