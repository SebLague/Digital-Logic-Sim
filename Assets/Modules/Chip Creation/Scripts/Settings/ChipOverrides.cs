using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DLS.ChipCreation
{
	[CreateAssetMenu(menuName = "DLS/Overrides")]
	public class ChipOverrides : ScriptableObject
	{
		[SerializeField] bool useChipOverrides;
		[SerializeField] bool useVideoChipOverrides;
		[SerializeField] bool useVideoChipsInBuild;
		[SerializeField] ChipOverride[] chipOverrides;
		[SerializeField] ChipOverride[] videoChipOverrides;

		public Dictionary<string, ChipBase> CreateLookup()
		{
			var lookup = new Dictionary<string, ChipBase>(System.StringComparer.OrdinalIgnoreCase);
			if (chipOverrides != null && useChipOverrides)
			{
				foreach (var entry in chipOverrides.Where(x => x.prefab != null))
				{
					lookup.Add(entry.chipName, entry.prefab);
				}
			}

			if (videoChipOverrides != null && useVideoChipOverrides && (useVideoChipsInBuild || Application.isEditor))
			{
				foreach (var entry in videoChipOverrides.Where(x => x.prefab != null))
				{
					lookup.Add(entry.chipName, entry.prefab);
				}
			}
			return lookup;
		}

		[System.Serializable]
		public struct ChipOverride
		{
			public string chipName;
			public ChipBase prefab;

			public bool IsValidMatch(string name)
			{
				return string.Equals(name, chipName, System.StringComparison.OrdinalIgnoreCase) && prefab != null;
			}
		}
	}
}