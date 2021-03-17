using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateButton : MonoBehaviour
{
    public event System.Action onChipUpdatePressed;

    public Button updateButton;

    public void Start() {
        updateButton.onClick.AddListener(ChipUpdatePressed);
    }

    void ChipUpdatePressed() {
        onChipUpdatePressed?.Invoke();
    }
}
