// MIT License Copyright(c) 2022 Filip Slavov, https://github.com/NibbleByte/UnityWiseSVN

using DevLocker.VersionControl.WiseGit.Preferences;
using UnityEditor;

namespace DevLocker.VersionControl.WiseGit.Branches
{
	/// <summary>
	/// Starts the database if enabled.
	/// </summary>
	[InitializeOnLoad]
	internal static class GitBranchesDatabaseStarter
	{
		// HACK: If this was the SVNBranchesDatabase itself it causes exceptions on assembly reload.
		//		 The static constructor gets called during reload because the instance exists.
		static GitBranchesDatabaseStarter()
		{
			var playerPrefs = GitPreferencesManager.Instance.PersonalPrefs;
			var projectPrefs = GitPreferencesManager.Instance.ProjectPrefs;

			// HACK: Just touch the SVNBranchesDatabase instance to initialize it.
			if (playerPrefs.EnableCoreIntegration && projectPrefs.EnableBranchesDatabase && GitBranchesDatabase.Instance.IsActive)
				return;
		}
	}
}
