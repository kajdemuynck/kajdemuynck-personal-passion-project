using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [SerializeField] GameObject[] inventoryObj;
    private Dictionary<string, GameObject> inventoryItems = new Dictionary<string, GameObject>();
    private string prefix = "Item";

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

        foreach (GameObject obj in inventoryObj)
        {
            string key = obj.name.Substring(prefix.Length).ToLower();
            inventoryItems.Add(key, obj);
        }

        SetInventory(new List<string>() {});
    }

    public void SetItemInInventory(string item)
    {
        List<string> inventoryList = GetInventory();

        if (inventoryItems.Keys.Contains(item))
        {
            if (!inventoryList.Contains(item))
                inventoryList.Add(item);
            else
                inventoryList.Remove(item);
        }

        SetInventory(inventoryList);
    }

    public bool CheckInventory(string item)
    {
        return GetInventory().Contains(item);
    }

    private List<string> GetInventory()
    {
        object inventoryObj = PhotonNetwork.LocalPlayer.CustomProperties["inventory"];
        List<string> inventoryList = ((string) inventoryObj).Split(';').ToList();
        return inventoryList;
    }

    private void SetInventory(List<string> inventoryList)
    {
        string inventoryStr = String.Join(";", inventoryList);
        Hashtable hash = new Hashtable();
        hash.Add("inventory", inventoryStr);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        foreach (string key in inventoryItems.Keys)
            inventoryItems[key].SetActive(inventoryList.Contains(key));
    }

    public void HideInventory()
    {
        gameObject.SetActive(false);
    }
}
