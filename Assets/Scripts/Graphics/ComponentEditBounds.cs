using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ComponentEditBounds : MonoBehaviour {

	public float thickness = 0.1f;
	public Material material;
	public Transform inputSignalArea;
	public Transform outputSignalArea;

	Mesh quadMesh;
	Matrix4x4[] trs;

	void Start () {
		if (Application.isPlaying) {
			MeshShapeCreator.CreateQuadMesh (ref quadMesh);
			CreateMatrices ();
		}
	}

	void UpdateSignalAreaSizeAndPos (Transform signalArea) {
		signalArea.position = new Vector3 (signalArea.position.x, transform.position.y, signalArea.position.z);
		signalArea.localScale = new Vector3 (signalArea.localScale.x, transform.localScale.y, 1);
	}

	void CreateMatrices () {
		Vector3 centre = transform.position;
		float width = Mathf.Abs (transform.localScale.x);
		float height = Mathf.Abs (transform.localScale.y);

		Vector3[] edgeCentres = {
			centre + Vector3.left * width / 2,
			centre + Vector3.right * width / 2,
			centre + Vector3.up * height / 2,
			centre + Vector3.down * height / 2
		};

		Vector3[] edgeScales = {
			new Vector3 (thickness, height + thickness, 1),
			new Vector3 (thickness, height + thickness, 1),
			new Vector3 (width + thickness, thickness, 1),
			new Vector3 (width + thickness, thickness, 1)
		};

		trs = new Matrix4x4[4];
		for (int i = 0; i < 4; i++) {
			trs[i] = Matrix4x4.TRS (edgeCentres[i], Quaternion.identity, edgeScales[i]);
		}
	}

	void Update () {
		if (!Application.isPlaying) {
			MeshShapeCreator.CreateQuadMesh (ref quadMesh);
			CreateMatrices ();
			UpdateSignalAreaSizeAndPos (inputSignalArea);
			UpdateSignalAreaSizeAndPos (outputSignalArea);
		}

		for (int i = 0; i < 4; i++) {
			Graphics.DrawMesh (quadMesh, trs[i], material, 0);
		}
	}
}