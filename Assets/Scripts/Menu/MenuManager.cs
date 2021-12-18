using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [SerializeField] Menu[] menus;
    [SerializeField] TMP_Text titleMain;
    [SerializeField] GameObject quitButtonMain;
    [SerializeField] GameObject quitButtonDisconnected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
        //DontDestroyOnLoad(gameObject);

        if (Application.platform != RuntimePlatform.OSXPlayer
            && Application.platform != RuntimePlatform.WindowsPlayer
            && Application.platform != RuntimePlatform.LinuxPlayer)
        {
            titleMain.alignment = TextAlignmentOptions.Center;
            quitButtonMain.SetActive(false);
            quitButtonDisconnected.SetActive(false);
        }
    }

    public void OpenMenu(string name)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].menuName == name)
            {
                menus[i].Open();
            }
            else if (menus[i].isOpen)
            {
                menus[i].Close();
            }
        }
    }
}
