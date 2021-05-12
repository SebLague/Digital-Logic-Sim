using System;
using UnityEngine;
using SFB;

public class EEPROM_File_Browser : MonoBehaviour
{
    [System.NonSerialized]public string EEPROM_path;
    
    private void OnMouseOver() {
        if(Input.GetKey(KeyCode.KeypadPlus)) {
            SelectEEPROMFile();
        }
    }

    void SelectEEPROMFile() {
        var extensions = new[] {
            new ExtensionFilter("Plain Text ", "txt"),
        };
        StandaloneFileBrowser.OpenFilePanelAsync("Select EEPROM File (.txt)", "", extensions, true, (string[] paths) => {
            try {
                if (paths[0] != null && paths[0] != "") {
                    EEPROM_path = paths[0];
                }
            } catch(IndexOutOfRangeException) {}
         });
    }
}