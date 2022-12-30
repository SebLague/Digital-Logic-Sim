using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SebUtils
{
	public static class VectorHelper
	{

		public static Vector2[] Vector3sToVector2s(IList<Vector3> vector3s)
		{
			Vector2[] vector2s = new Vector2[vector3s.Count];
			for (int i = 0; i < vector2s.Length; i++)
			{
				vector2s[i] = vector3s[i];
			}
			return vector2s;
		}


		public static Vector3[] Vector2sToVector3s(IList<Vector2> vector2s, float z = 0)
		{
			Vector3[] vector3s = new Vector3[vector2s.Count];
			for (int i = 0; i < vector3s.Length; i++)
			{
				vector3s[i] = new Vector3(vector2s[i].x, vector2s[i].y, z);
			}
			return vector3s;
		}

		public static Vector3 WithX(Vector3 vec, float x)
		{
			return new Vector3(x, vec.y, vec.z);
		}

		public static Vector3 WithY(Vector3 vec, float y)
		{
			return new Vector3(vec.x, y, vec.z);
		}

		public static Vector3 WithZ(Vector2 vec, float z)
		{
			return new Vector3(vec.x, vec.y, z);
		}
	}
}