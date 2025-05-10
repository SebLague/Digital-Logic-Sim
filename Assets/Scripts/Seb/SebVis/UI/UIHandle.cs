using System;

namespace Seb.Vis.UI
{
	public readonly struct UIHandle : IEquatable<UIHandle>
	{
		public readonly string stringID;
		public readonly int intID;
		readonly int hashCode;

		public UIHandle(string stringID, int intID = 0)
		{
			this.stringID = stringID;
			this.intID = intID;
			hashCode = HashCode.Combine(stringID, intID);
		}

		public bool Equals(UIHandle other) => intID == other.intID && string.Equals(stringID, other.stringID, StringComparison.Ordinal);

		public override int GetHashCode() => hashCode;
	}
}