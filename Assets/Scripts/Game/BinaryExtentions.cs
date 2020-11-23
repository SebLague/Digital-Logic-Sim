using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BinaryExtentions {

	public static int GetBit (this int value, int index) {
		return value >> index & 1;
	}

	public static int GetBitRange (this int value, int lsbIndex, int msbIndex) {
		int rangeValue = 0;
		for (int i = lsbIndex; i <= msbIndex; i++) {
			rangeValue |= (value >> lsbIndex) & (1 << i);
		}
		return rangeValue;
	}

	public static void SetBit (ref this int value, int index, int bitValue) {
		value |= bitValue << index;
	}
}