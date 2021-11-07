using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] TMP_Text itemValueText;

    public void ShowValue(int value)
    {
        itemValueText.gameObject.SetActive(true);
        itemValueText.text = string.Format("Worth {0} dollars", value);
    }

    public void HideValue()
    {
        itemValueText.gameObject.SetActive(false);
    }
}
