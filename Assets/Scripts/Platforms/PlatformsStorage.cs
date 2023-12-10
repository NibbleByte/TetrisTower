using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TetrisTower.Platforms
{
	/// <summary>
	/// Wraps up some platform calls for file storage, relating to the current user.
	/// </summary>
	public static class PlatformsStorage
	{
		private const string ZIP_DATA_NAME = "Data";

		private static string GetFullPath(string relativeFilePath) => Path.Combine(Application.persistentDataPath, relativeFilePath);


		public static async Task WriteFileAsync(string relativeFilePath, string content)
		{
			string fullPath = GetFullPath(relativeFilePath);

			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

			await File.WriteAllTextAsync(fullPath, content);
		}

		public static async Task WriteZipFileAsync(string relativeFilePath, string content)
		{
			Exception threadException = null;

			string fullPath = GetFullPath(relativeFilePath);

			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

			var zipThread = new Thread(() => {

				try {
					using (var zip = new Ionic.Zip.ZipFile()) {
						zip.AddEntry(ZIP_DATA_NAME, content);
						zip.Save(fullPath);
					}
				} catch (Exception ex) {
					threadException = ex;
				}

			});
			zipThread.Name = "Zip-Writer";
			zipThread.Start();

			while (zipThread.IsAlive && threadException == null) {
				await Task.Yield();
			}

			if (threadException != null) {
				throw threadException;
			}
		}

		public static async Task<string> ReadFileAsync(string relativeFilePath)
		{
			return await File.ReadAllTextAsync(GetFullPath(relativeFilePath));
		}

		public static async Task<string> ReadZipFileAsync(string relativeFilePath)
		{
			string fullPath = GetFullPath(relativeFilePath);
			string content = null;
			Exception threadException = null;

			var zipThread = new Thread(() => {

				try {
					using (var raw = new MemoryStream())
					using (var zip = Ionic.Zip.ZipFile.Read(fullPath)) {
						zip.Entries.First(e => e.FileName == ZIP_DATA_NAME).Extract(raw);

						raw.Position = 0; // reset position so we start reading from the start.

						using (var rawReader = new StreamReader(raw)) {
							content = rawReader.ReadToEnd();
						}
					}
				} catch (Exception ex) {
					threadException = ex;
				}

			});
			zipThread.Name = "Zip-Reader";
			zipThread.Start();

			while (zipThread.IsAlive && threadException == null && content == null) {
				await Task.Yield();
			}

			if (threadException != null)
				throw threadException;

			if (content == null)
				throw new IOException("No content found.");

			return content;
		}


		public static bool FileExists(string relativeFilePath)
		{
			string fullPath = GetFullPath(relativeFilePath);

			return File.Exists(fullPath);
		}

		public static async Task<bool> DeleteFileAsync(string relativeFilePath)
		{
			if (!FileExists(relativeFilePath))
				return await Task.FromResult(false);

			string fullPath = GetFullPath(relativeFilePath);

			File.Delete(fullPath);

			return await Task.FromResult(true);
		}

		public static async Task<string[]> ListFilesAsync(string relativeFolderPath, params string[] extensions)
		{
			if (extensions.Length == 0)
				throw new ArgumentException("Must specify extensions to look for");

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