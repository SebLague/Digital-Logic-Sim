using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateGroup : MonoBehaviour
{

    public event System.Action<int> onGroupSizeSettingPressed;

    public TMP_InputField groupSizeInput;
    public Button setSizeButton;
    public GameObject menuHolder;
    private bool menuActive;

    int groupSizeValue;

    // Start is called before the first frame update
    void Start ()
    {
        menuActive = false;
        groupSizeValue = 8;
        setSizeButton.onClick.AddListener (SetGroupSize);
        groupSizeInput.onValueChanged.AddListener (SetCurrentText);
    }

    void SetCurrentText (string groupSize) {
        groupSizeValue = int.Parse(groupSize);
    }

    public void CloseMenu () {
        onGroupSizeSettingPressed.Invoke(groupSizeValue);
        menuActive = false;
        menuHolder.SetActive(false);
    }

    public void OpenMenu ()
    {
        menuActive = true;
        menuHolder.SetActive(true);
    }

    void SetGroupSize () {
        if (menuActive) {
            CloseMenu();
        } else {
            OpenMenu();
        }

    }
}
