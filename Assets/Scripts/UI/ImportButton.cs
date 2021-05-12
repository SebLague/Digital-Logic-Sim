using System;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class ImportButton : MonoBehaviour
{
    public Button importButton;
    public Manager manager;
    public ChipBarUI chipBarUI;
    void Start()
    {
        importButton.onClick.AddListener(ImportChip);
    }

    void ImportChip() {
        var extensions = new[] {
            new ExtensionFilter("Chip design", "dls"),
        };


        StandaloneFileBrowser.OpenFilePanelAsync("Import chip design", "", extensions, true, (string[] paths) => {
            try {
                if (paths[0] != null && paths[0] != "") {

                ChipLoader.Import(paths[0]);
                EditChipBar();
                }
            } catch (IndexOutOfRangeException) {}
         });
        
    }

    void EditChipBar()
    {
        chipBarUI.ReloadBar();
        SaveSystem.LoadAll(manager);
    }
}
