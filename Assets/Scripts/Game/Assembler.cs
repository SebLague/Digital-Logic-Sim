using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Assembler : MonoBehaviour {

	public EmulatedRAM emulatedRAM;

	public Program programToRun;

	static string[] assemblyInstructions = {
		"A=RAM[X]", // 0
		"A=X", // 1
		"A=A+B", // 2
		"A=A-B", // 3
		"B=RAM[X]", // 4
		"B=X", // 5
		"RAM[X]=A", // 6
		"RAM[X]=B", // 7
		"RAM[X]=A+B", // 8
		"RAM[X]=A-B", // 9
		"JMP[X]", // 10
		"JMP[X]IFNEG", // 11
		"", // 12
		"", // 13
		"", // 14
		"HALT" // 15
	};

	void Awake () {
		if (Application.isPlaying) {
			emulatedRAM.LoadInstructions (programToRun.machineValues);
		}
	}

	public static string BinaryStringFromByte (int byteValue) {
		if (byteValue < 0) {
			byteValue = ~(byteValue - 1);
		}
		char[] first4Bits = { '0', '0', '0', '0' };
		char[] last4Bits = { '0', '0', '0', '0' };

		for (int j = 0; j < 4; j++) {
			if (byteValue.GetBit (j) == 1) {
				last4Bits[3 - j] = '1';
			}
			if (byteValue.GetBit (j + 4) == 1) {
				first4Bits[3 - j] = '1';
			}
		}

		return new string (first4Bits) + " " + new string (last4Bits);

	}

	public static void Assemble (Program program) {
		program.machineValues = new int[program.assembly.Length];

		for (int i = 0; i < program.assembly.Length; i++) {
			string line = program.assembly[i];
			line = line.Replace (" ", "");
			if (line.Contains ("//")) {
				line = line.Substring (0, line.IndexOf ("//"));
			}
			if (!string.IsNullOrEmpty (line)) {
				string numeralString = "";
				int numeralValue = 0;
				for (int charIndex = 0; charIndex < line.Length; charIndex++) {
					bool isNegSign = (line[charIndex] == '-' && charIndex > 0 && line[charIndex - 1] == '=');
					if (char.IsDigit (line[charIndex]) || isNegSign) {
						numeralString += line[charIndex];
					}
				}
				if (numeralString.Length > 0) {
					line = line.Replace (numeralString, "X");
					numeralValue = int.Parse (numeralString);
				}

				bool instructionFound = false;

				for (int j = 0; j < assemblyInstructions.Length; j++) {
					if (line.Equals (assemblyInstructions[j])) {
						program.machineValues[i] = j << 4;
						if (numeralValue < 0) {
							numeralValue = ~(numeralValue - 1);
						}
						program.machineValues[i] |= numeralValue;
						instructionFound = true;
						break;
					}
				}

				if (!instructionFound) {
					Debug.Log ("No instruction found: " + line);
				}
			}
		}

		// Set binary string
		program.machineCodeString = new string[program.assembly.Length];
		for (int i = 0; i < program.assembly.Length; i++) {
			program.machineCodeString[i] = BinaryStringFromByte (program.machineValues[i]);
		}
	}

	public static void CalculateMachineCodeValues (Program program) {
		program.machineValues = new int[program.machineCodeString.Length];
		for (int i = 0; i < program.machineCodeString.Length; i++) {
			int value = 0;
			string formattedMachineCodeString = program.machineCodeString[i].Replace (" ", "");
			for (int j = 0; j < formattedMachineCodeString.Length; j++) {
				if (formattedMachineCodeString[j] == '1') {
					value |= 1 << (j);
				}
			}
			program.machineValues[i] = value;

		}

	}

}