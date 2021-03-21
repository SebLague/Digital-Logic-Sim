using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

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
        string path = EditorUtility.OpenFilePanel(
            "Import chip design",
            "",
            "dls"
        );
        ChipLoader.Import(path);
        EditChipBar();
    }

    void EditChipBar()
    {
        chipBarUI.ReloadBar();
        SaveSystem.LoadAll(manager);
    }
}
