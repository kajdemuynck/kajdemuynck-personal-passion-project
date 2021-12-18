using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    PhotonView pv;

    private List<ItemPickup> itemsPickup;
    //private List<ItemPickupMoney> itemsPickupMoney;
    private List<ItemSpot> emptySpots;
    [SerializeField] GameObject[] itemsPickupMoneyObj;

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
        emptySpots = new List<ItemSpot>(FindObjectsOfType<ItemSpot>()).FindAll(item => !(item.GetComponentInChildren<ItemPickup>() != null && item.GetComponentInChildren<ItemPickupMoney>() == null));
        SortSpotList();

        if (PhotonNetwork.IsMasterClient)
        {
            InitItemPickupsMoney();
        }

        PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void SortItemList()
    {
        itemsPickup.Sort(delegate (ItemPickup a, ItemPickup b) {
            return (a.id).CompareTo(b.id);
        });
    }

    private void SortSpotList()
    {
        emptySpots.Sort(delegate (ItemSpot a, ItemSpot b) {
            return (a.id).CompareTo(b.id);
        });
    }

    public void InitItemPickupsMoney()
    {
        int[] values = new int[emptySpots.Count];
        int[] types = new int[emptySpots.Count];

        for (int i = 0; i < emptySpots.Count; i++)
        {
            values[i] = (int)Random.Range(10f, 20f);
            do types[i] = Random.Range(0, itemsPickupMoneyObj.Length);
            while (emptySpots[i].category != itemsPickupMoneyObj[types[i]].GetComponent<ItemPickupMoney>().category || emptySpots[i].size != itemsPickupMoneyObj[types[i]].GetComponent<ItemPickupMoney>().size);
        }

        pv.RPC("RPC_InitItemPickupsMoney", RpcTarget.All, values, types);
    }

    [PunRPC]
    private void RPC_InitItemPickupsMoney(int[] values, int[] types)
    {
        for (int i = 0; i < emptySpots.Count; i++)
        {
            GameObject obj;
            if (emptySpots[i].GetComponentInChildren<ItemPickupMoney>() == null)
                obj = Instantiate(itemsPickupMoneyObj[types[i]], emptySpots[i].transform);
            else
                obj = emptySpots[i].GetComponentInChildren<ItemPickupMoney>().gameObject;
            obj.GetComponent<ItemPickupMoney>().SetValue(values[i]);
        }

        itemsPickup = new List<ItemPickup>(FindObjectsOfType<ItemPickup>());
        SortItemList();
    }

    public void RemoveItem(GameObject item)
    {
        pv.RPC("RPC_RemoveItem", RpcTarget.All, itemsPickup.IndexOf(item.GetComponent<ItemPickup>()));
    }

    [PunRPC]
    private void RPC_RemoveItem(int index)
    {
        ItemPickup itemPickup = itemsPickup[index];
        itemPickup.transform.parent.GetComponent<ItemSpot>().PlaySound(itemPickup.GetAudioClip());
        itemPickup.isPickedUp = true;
        itemsPickup.Remove(itemPickup);
        Destroy(itemPickup.gameObject);
    }
}
