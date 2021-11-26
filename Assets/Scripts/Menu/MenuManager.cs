using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [SerializeField] Menu[] menus;

    private void Awake()
    {
        Instance = this;
        //if (Instance == null)
        //{
        //    Instance = this;
        //}
        //else if (Instance != this)
        //{
        //    Destroy(Instance.gameObject);
        //    Instance = this;
        //}
        //DontDestroyOnLoad(gameObject);
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
