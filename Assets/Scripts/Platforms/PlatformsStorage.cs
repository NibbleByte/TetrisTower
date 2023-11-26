using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace TetrisTower.Platforms
{
	/// <summary>
	/// Wraps up some platform calls for file storage, relating to the current user.
	/// </summary>
	public static class PlatformsStorage
	{
		private static string GetFullPath(string relativeFilePath) => Path.Combine(Application.persistentDataPath, relativeFilePath);

		public static async Task WriteFileAsync(string relativeFilePath, string content)
		{
			string fullPath = GetFullPath(relativeFilePath);

			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

			await File.WriteAllTextAsync(fullPath, content);
		}

		public static async Task<string> ReadFileAsync(string relativeFilePath)
		{
			return await File.ReadAllTextAsync(GetFullPath(relativeFilePath));
		}

		public static bool FileExists(string relativeFilePath)
		{
			string fullPath = Path.Combine(Application.persistentDataPath, GetFullPath(relativeFilePath));

			return File.Exists(fullPath);
		}

		public static async Task<string[]> ListFilesAsync(string relativeFolderPath, params string[] extensions)
		{
			string fullPath = GetFullPath(relativeFolderPath);

			if (!Directory.Exists(fullPath))
				return Array.Empty<string>();

			string[] found = Directory.GetFiles(fullPath)
				.Where(path => Array.IndexOf(extensions, Path.GetExtension(path)) != -1)
				.ToArray()
				;

			return await Task.FromResult(found);

		}
	}
}