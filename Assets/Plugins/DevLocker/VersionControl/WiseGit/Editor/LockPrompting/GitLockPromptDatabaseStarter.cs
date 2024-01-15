// MIT License Copyright(c) 2022 Filip Slavov, https://github.com/NibbleByte/UnityWiseSVN

using DevLocker.VersionControl.WiseGit.Preferences;
using UnityEditor;

namespace DevLocker.VersionControl.WiseGit.LockPrompting
{
	/// <summary>
	/// Starts the database if enabled.
	/// </summary>
	[InitializeOnLoad]
	internal static class GitLockPromptDatabaseStarter
	{
		// HACK: If this was the SVNAutoLockingDatabase itself it causes exceptions on assembly reload.
		//		 The static constructor gets called during reload because the instance exists.
		static GitLockPromptDatabaseStarter()
		{
			TryStartIfNeeded();
		}

		internal static void TryStartIfNeeded()
		{
			var playerPrefs = GitPreferencesManager.Instance.PersonalPrefs;
			var projectPrefs = GitPreferencesManager.Instance.ProjectPrefs;

			// HACK: Just touch the SVNAutoLockingDatabase instance to initialize it.
			if (playerPrefs.EnableCoreIntegration && projectPrefs.EnableLockPrompt && GitLockPromptDatabase.Instance.IsActive)
				return;
		}
	}
}
