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
        isOpen = true;
        if (defaultSelection != null && defaultSelection.activeSelf)
        {
            if (defaultSelection.GetComponent<Button>())
                defaultSelection.GetComponent<Button>().Select();
            else if (defaultSelection.GetComponent<TMP_InputField>())
                defaultSelection.GetComponent<TMP_InputField>().Select();
        }
        // else find a button within the screen to activate
        else
        {
            SelectAnyButton(transform);
        }

        gameObject.SetActive(true);
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
                Debug.Log("Found button");
            }
            SelectAnyButton(child);
        }
    }
}
