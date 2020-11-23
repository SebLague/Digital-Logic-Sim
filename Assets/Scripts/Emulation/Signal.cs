public struct Signal {
	public int value;

	public Signal (int value) {
		this.value = value;
	}

	public void SetValue (int value) {
		this.value = value;
	}

	public int this [int i] {
		get {
			return (this.value >> i) & 1;
		}
		set {
			this.value &= ~(1 << i);
			this.value |= value << i;
		}
	}

	public static implicit operator int (Signal signal) {
		return signal.value;
	}

	public override string ToString () {
		string s = "";
		for (int i = 0; i < 16; i++) {
			s += this [i];
			if (i == 7) {
				s += " ";
			}
		}
		return s;
	}
}