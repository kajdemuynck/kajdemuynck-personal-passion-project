using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    PhotonView pv;

    private List<ItemPickup> itemsPickup;
    private List<ItemPickupMoney> itemsPickupMoney;

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

        pv = GetComponent<PhotonView>();
    }

    public void Start()
    {
        itemsPickup = new List<ItemPickup>(FindObjectsOfType<ItemPickup>());
        itemsPickupMoney = new List<ItemPickupMoney>();
        SortList();

        if (PhotonNetwork.IsMasterClient)
        {
            InitItemPickupsMoney();
        }

        PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void SortList()
    {
        itemsPickup.Sort(delegate (ItemPickup a, ItemPickup b) {
            return (a.id).CompareTo(b.id);
        });

        foreach (ItemPickup item in itemsPickup)
        {
            if (item.gameObject.GetComponent<ItemPickupMoney>())
            {
                itemsPickupMoney.Add(item.GetComponent<ItemPickupMoney>());
            }
        }
    }

    public void InitItemPickupsMoney()
    {
        int[] values = new int[itemsPickupMoney.Count];
        for (int i = 0; i < values.Length; i++)
            values[i] = (int)Random.Range(10f, 20f);

        pv.RPC("RPC_InitItemPickupsMoney", RpcTarget.All, values);
    }

    [PunRPC]
    private void RPC_InitItemPickupsMoney(int[] values)
    {
        for (int i = 0; i < itemsPickupMoney.Count; i++)
            itemsPickupMoney[i].SetValue(values[i]);
    }

    public void RemoveItem(GameObject item)
    {
        pv.RPC("RPC_RemoveItem", RpcTarget.All, itemsPickup.IndexOf(item.GetComponent<ItemPickup>()));
    }

    [PunRPC]
    private void RPC_RemoveItem(int index)
    {
        GameObject item = itemsPickup[index].gameObject;
        item.GetComponent<ItemPickup>().isPickedUp = true;
        itemsPickup.Remove(item.GetComponent<ItemPickup>());
        Destroy(item);
    }
}
