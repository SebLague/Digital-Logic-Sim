using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshShapeCreator {

	// Create 1x1 quad mesh
	public static void CreateQuadMesh (ref Mesh mesh) {
		InitMesh (ref mesh);
		Vector3[] verts = {
			(Vector3.left + Vector3.up) * 0.5f,
			(Vector3.right + Vector3.up) * 0.5f,
			(Vector3.left + Vector3.down) * 0.5f,
			(Vector3.right + Vector3.down) * 0.5f
		};
		int[] tris = {
			0,
			1,
			2,
			1,
			3,
			2
		};

		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.RecalculateBounds ();
	}

	static void InitMesh (ref Mesh mesh) {
		if (mesh == null) {
			mesh = new Mesh ();
		}
		mesh.Clear ();
	}
}