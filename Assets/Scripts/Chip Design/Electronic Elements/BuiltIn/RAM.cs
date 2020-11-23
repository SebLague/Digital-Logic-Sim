using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RAM : MonoBehaviour {

	void Start () {
		Bit[] bits = new Bit[5];
		for (int i = 0; i < bits.Length; i++) {
			//Debug.Log ((bits[i] | bits[0]));
		}
	}

	public void ProcessChip () {
		BitArray x = new BitArray(5);
		var n = x[3];
	}

	public struct Bit {
		public bool value;

		public Bit (bool value) {
			this.value = value;
		}

		public Bit (int value) {
			this.value = value == 1;
		}

		public static implicit operator bool (Bit bit) => bit.value;
		public static implicit operator int (Bit bit) => (bit.value) ? 1 : 0;
	}
}