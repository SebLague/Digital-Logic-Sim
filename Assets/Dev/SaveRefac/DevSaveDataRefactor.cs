using UnityEngine;

namespace DLS.Dev
{
	public class DevSaveDataRefactor : MonoBehaviour
	{
		void Awake()
		{
			RenameBuiltinChip();
		}


		void RenameBuiltinChip()
		{
			/*
			Debug.Log("Renaming...");

			Dictionary<string, string> RenameLookup = new()
			{
				{ "7-SEG", "7-SEGMENT" },
			};

			ProjectDescription[] projects = Loader.LoadAllProjectDescriptions();

			for (int projectIndex = 0; projectIndex < projects.Length; projectIndex++)
			{
				ProjectDescription project = projects[projectIndex];
				string projectName = project.ProjectName;

				// ---- Update Subchips ----
				ChipDescription[] chips = Loader.LoadAllSavedChips(projectName);

				for (int i = 0; i < chips.Length; i++)
				{
					for (int j = 0; j < chips[i].SubChips.Length; j++)
					{
						if (RenameLookup.TryGetValue(chips[i].SubChips[j].Name, out string newName))
						{
							chips[i].SubChips[j].Name = newName;
						}
					}

					Saver.SaveChip(chips[i], projectName);
				}

				// ---- Update Starred list ----
				for (int i = 0; i < project.StarredList.Count; i++)
				{
					if (project.StarredList[i].IsCollection) continue;
					if (RenameLookup.TryGetValue(project.StarredList[i].Name, out string newName))
					{
						project.StarredList[i] = new StarredItem(newName, false);
					}
				}

				// ---- Update Collections list ----
				for (int i = 0; i < project.ChipCollections.Count; i++)
				{
					for (int j = 0; j < project.ChipCollections[i].Chips.Count; j++)
					{
						if (RenameLookup.TryGetValue(project.ChipCollections[i].Chips[j], out string newName))
						{
							project.ChipCollections[i].Chips[j] = newName;
						}
					}
				}

				Saver.SaveProjectDescription(project);
			}
			*/
		}
	}
}