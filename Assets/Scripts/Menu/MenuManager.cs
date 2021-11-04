using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    private Menu[] menus;

    private void Awake()
    {
        Instance = this;
        menus = GetComponentsInChildren<Menu>();
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
