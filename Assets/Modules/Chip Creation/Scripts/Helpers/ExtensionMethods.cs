using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DLS.ChipCreation
{
	public static class ExtensionMethods
	{
		public static Vector3 WithZ(this Vector3 v, float z)
		{
			return new Vector3(v.x, v.y, z);
		}

		public static Vector3 WithZ(this Vector2 v, float z)
		{
			return new Vector3(v.x, v.y, z);
		}
	}
}