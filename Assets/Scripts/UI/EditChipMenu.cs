using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;
using System;

public class EditChipMenu : MonoBehaviour
{
    public TMP_InputField chipNameField;
    public Button doneButton;
    public Button deleteButton;
    public GameObject panel;
    public ChipBarUI chipBarUI;

    private Manager manager;
    private string nameBeforeChanging;

    private bool init = false;

    public void Init()
    {
        if (init)
        {
            return;
        }
        chipBarUI = GameObject.Find("Chip Bar").GetComponent<ChipBarUI>();
        chipNameField.onValueChanged.AddListener(ChipNameFieldChanged);
        doneButton.onClick.AddListener(FinishCreation);
        deleteButton.onClick.AddListener(DeleteChip);
        UnityEngine.Debug.Log("Adding listener");
        manager = FindObjectOfType<Manager>();
        FindObjectOfType<ChipInteraction>().editChipMenu = this;
        panel.gameObject.SetActive(false);
        init = true;
    }

    public void EditChip(Chip chip)
    {
        panel.gameObject.SetActive(true);
        GameObject chipUI = GameObject.Find("Create (" + chip.chipName + ")");
        this.gameObject.transform.position = chipUI.transform.position + new Vector3(7.5f, -1.2f, 0);
        float xVal = Math.Min(this.gameObject.transform.position.x, 13.9f);
        xVal = Math.Max(xVal, -0.1f);
        this.gameObject.transform.position = new Vector3(xVal, this.gameObject.transform.position.y, this.gameObject.transform.position.z);
        chipNameField.text = chip.chipName;
        nameBeforeChanging = chip.chipName;
        doneButton.interactable = true;
        deleteButton.interactable = ChipSaver.IsSafeToDelete(nameBeforeChanging);
    }

    public void ChipNameFieldChanged(string value)
    {
        string formattedName = value.ToUpper();
        doneButton.interactable = IsValidChipName(formattedName.Trim());
        chipNameField.text = formattedName;
    }

    public bool IsValidRename(string chipName)
    {
        if (nameBeforeChanging == chipName)
        {
            // Name has not changed
            return true;
        }
        if (!IsValidChipName(chipName))
        {
            // Name is either empty, AND or NOT
            return false;
        }
        SavedChip[] savedChips = SaveSystem.GetAllSavedChips();
        for (int i = 0; i < savedChips.Length; i++)
        {
            if (savedChips[i].name == chipName)
            {
                // Name already exists in custom chip
                return false;
            }
        }
        return true;
    }

    public bool IsValidChipName(string chipName)
    {
        return chipName != "AND" && chipName != "NOT" && chipName.Length != 0;
    }

    public void DeleteChip()
    {
        ChipSaver.Delete(nameBeforeChanging);
        CloseEditChipMenu();
        EditChipBar();
    }

    public void EditChipBar()
    {
        chipBarUI.ReloadBar();
        SaveSystem.LoadAll(manager);
    }

    public void FinishCreation()
    {
        if (chipNameField.text != nameBeforeChanging)
        {
            // Chip has been renamed
            ChipSaver.Rename(nameBeforeChanging, chipNameField.text.Trim());
            EditChipBar();
        }
        CloseEditChipMenu();
    }

    public void CloseEditChipMenu()
    {
        panel.gameObject.SetActive(false);
    }

}
