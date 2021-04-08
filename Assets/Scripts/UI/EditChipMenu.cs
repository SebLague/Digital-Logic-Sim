using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using SFB;
using System.Linq;

public class EditChipMenu : MonoBehaviour
{
    public TMP_InputField chipNameField;
    public Button doneButton;
    public Button deleteButton;
    public Button viewButton;
    public Button exportButton;
    public GameObject panel;
    public ChipBarUI chipBarUI;

    private Manager manager;
    private Chip currentChip;
    private string nameBeforeChanging;
    public bool isActive;

    private bool init = false;
    private bool focused = false;

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
        viewButton.onClick.AddListener(ViewChip);
        exportButton.onClick.AddListener(ExportChip);
        manager = FindObjectOfType<Manager>();
        FindObjectOfType<ChipInteraction>().editChipMenu = this;
        panel.gameObject.SetActive(false);
        init = true;
        isActive = false;
    }

    public void EditChip(Chip chip)
    {
        panel.gameObject.SetActive(true);
        isActive = true;
        GameObject chipUI = GameObject.Find("Create (" + chip.chipName + ")");
        this.gameObject.transform.position = chipUI.transform.position + new Vector3(7.5f, -0.65f, 0);
        float xVal = Math.Min(this.gameObject.transform.position.x, 13.9f);
        xVal = Math.Max(xVal, -0.1f);
        this.gameObject.transform.position = new Vector3(xVal, this.gameObject.transform.position.y, this.gameObject.transform.position.z);
        chipNameField.text = chip.chipName;
        nameBeforeChanging = chip.chipName;
        doneButton.interactable = true;
        deleteButton.interactable = ChipSaver.IsSafeToDelete(nameBeforeChanging);
        viewButton.interactable = chip.canBeEdited;
        exportButton.interactable = chip.canBeEdited;
        focused = true;
        currentChip = chip;
    }

    public void ChipNameFieldChanged(string value)
    {
        string formattedName = value.ToUpper();
        doneButton.interactable = IsValidChipName(formattedName.Trim());
        chipNameField.interactable = IsValidChipName(formattedName.Trim());
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
        String[] notValidArray = {
            "AND", "NOT", "OR", "XOR", "HDD",
            "4 BIT ENCODER", "4 BIT DECODER",
            "8 BIT ENCODER", "8 BIT DECODER",
            "16 BIT ENCODER", "16 BIT DECODER"
        };

        // If chipName is in notValidArray then is not a valid name
        if (notValidArray.Any(chipName.Contains)) {
            return false;
        } else if (chipName.Length != 0) {
            return false;
        } else {
            return true;
        }
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
        isActive = false;
        focused = false;
        currentChip = null;
    }

    public void ViewChip()
    {
        if (currentChip != null) {
            manager.ViewChip(currentChip);
            CloseEditChipMenu();
        }
    }

    public void ExportChip()
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Export chip design", "", currentChip.chipName + ".dls", "dls");
        if (path.Length != 0) {
            ChipSaver.Export(currentChip, path);
        }
    }

    public void Update()
    {
        if (focused) {
            if (Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(1) ||
                Input.GetMouseButtonDown(2))
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

                if(hit.collider != null) {
                    // If click is outside the panel
                    if (hit.collider.name != panel.name) {
                        CloseEditChipMenu();
                    }
                }
            }
        }
    }
}
