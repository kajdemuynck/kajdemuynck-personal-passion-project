using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    public bool isLookingAtItem = false;
    PhotonView pv;
    PlayerManager pm;
    private HUD hud;
    public LayerMask EnvironmentLayer;
    private Ray ray;
    private RaycastHit hit;

    private PlayerInput playerInput;

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
        DontDestroyOnLoad(gameObject);

        playerInput = GetComponent<PlayerInput>();
        pv = GetComponent<PhotonView>();
        hud = GameObject.Find("HUD").GetComponent<HUD>();
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

    public void Update()
    {
        isLookingAtItem = false;

        if (CheckMouseOver())
        {
            foreach (ItemPickup item in itemsPickup)
            {
                if (!(hit.collider.gameObject.GetComponent<ItemPickupMoney>() && pm.role == "agent"))
                {
                    if (hit.collider.gameObject == item.gameObject && !item.isPickedUp && hit.distance <= item.interactionDistance)
                    {
                        isLookingAtItem = true;
                        hud.ShowDescription(item.description);
                        item.CheckInteraction(playerInput);
                        break;
                    }
                }
            }

            if (!isLookingAtItem)
                hud.HideDescription();
        }
        else
        {
            hud.HideDescription();
        }

        //Debug.Log(isLookingAtItem);
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

    public void SetPlayerManager(int pvid)
    {
        pm = PhotonView.Find(pvid).GetComponent<PlayerManager>();
    }

    protected bool CheckMouseOver()
    {
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 0.5 = center of the screen
            ray.origin = Camera.main.transform.position;
            return Physics.Raycast(ray, out hit, 10f, ~EnvironmentLayer);
            //PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
        }
        else
        {
            return false;
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
        //if (!pv.IsMine)
        //    return;

        GameObject item = itemsPickup[index].gameObject;
        item.GetComponent<ItemPickup>().isPickedUp = true;
        itemsPickup.Remove(item.GetComponent<ItemPickup>());
        Destroy(item);
    }
}
