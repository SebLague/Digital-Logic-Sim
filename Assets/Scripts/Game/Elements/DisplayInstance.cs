using System.Collections.Generic;
using DLS.Description;
using DLS.Simulation;
using Seb.Types;

namespace DLS.Game
{
	public class DisplayInstance
	{
		public List<DisplayInstance> ChildDisplays;
		public DisplayDescription Desc;
		public ChipType DisplayType;
		public Bounds2D LastDrawBounds;

		// Cached sim references to speed up rendering
		public SimChip SimChip;
		public SimPin SimPin;
	}
}