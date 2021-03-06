using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Menu : MonoBehaviour
{
    public string menuName;
    public bool isOpen;
    public GameObject defaultSelection;

    public void Open()
    {
        gameObject.SetActive(true);

        isOpen = true;
        if (defaultSelection != null && defaultSelection.activeSelf)
        {
            if (defaultSelection.GetComponent<Button>())
            {
                defaultSelection.GetComponent<Button>().Select();
                TMP_Text txt = defaultSelection.GetComponent<Button>().GetComponentInChildren<TMP_Text>();
                if (txt != null)
                    txt.color = new Color(1, 1, 1, 1);
            }
            else if (defaultSelection.GetComponent<TMP_InputField>())
                defaultSelection.GetComponent<TMP_InputField>().Select();
        }
        // else find a button within the screen to activate
        else
        {
            SelectAnyButton(transform);
        }
    }

    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }

    private void SelectAnyButton(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.activeSelf && child.GetComponent<Button>())
            {
                child.GetComponent<Button>().Select();
            }
            SelectAnyButton(child);
        }
    }
}
