using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
public class EEPROM : BuiltinChip {
    public EEPROM_File_Browser EEPROM_File;
    string outbit;
    string address;
    int base10address;
	protected override void Awake () {
		base.Awake ();
	}

	protected override void ProcessOutput () {
        if(EEPROM_File.EEPROM_path != null) {
            address = "";
            outbit = "";
            for(int i = 0; i < inputPins.Length; i++) {
                address += inputPins[i].State;
            }
            base10address = Convert.ToInt32(address);
            if(File.ReadAllText(EEPROM_File.EEPROM_path).Length < base10address) {
                outbit = File.ReadAllText(EEPROM_File.EEPROM_path)[base10address].ToString();
		        ChipUtil.setPins(outbit, outputPins);
            } else {
                ChipUtil.setPins("0", outputPins);
            }
        }
	}

}