using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public bool isLookingAtItem = false;
    PhotonView pv;
    private HUD hud;
    public LayerMask EnvironmentLayer;
    private Ray ray;
    private RaycastHit hit;

    private List<ItemPickup> itemsPickup;
    private List<ItemPickupMoney> itemsPickupMoney;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        hud = GameObject.Find("HUD").GetComponent<HUD>();

        //itemsPickup = FindObjectsOfType<ItemPickup>();
        itemsPickup = new List<ItemPickup>(FindObjectsOfType<ItemPickup>());
        itemsPickupMoney = new List<ItemPickupMoney>(FindObjectsOfType<ItemPickupMoney>());
    }

    public void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int[] values = new int[itemsPickupMoney.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = (int)Random.Range(10f, 20f);

            InitItemPickupsMoney(values);
        }
    }

    public void Update()
    {
        isLookingAtItem = false;

        if (CheckMouseOver())
        {
            foreach (var item in itemsPickup)
            {
                if (hit.collider.gameObject == item.gameObject && !item.isDestroyed && hit.distance <= item.interactionDistance)
                {
                    isLookingAtItem = true;
                    hud.ShowDescription(item.description);
                    item.CheckInteraction();
                    break;
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

    public void RemoveItem(GameObject item)
    {
        pv.RPC("RPC_RemoveItem", RpcTarget.All, itemsPickup.IndexOf(item.GetComponent<ItemPickup>()));
    }

    protected bool CheckMouseOver()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // 0.5 = center of the screen
        ray.origin = Camera.main.transform.position;
        return Physics.Raycast(ray, out hit, 10f, ~EnvironmentLayer);
        //PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
    }

    public void InitItemPickupsMoney(int[] values)
    {
        pv.RPC("RPC_InitItemPickupsMoney", RpcTarget.All, values);
    }

    [PunRPC]
    private void RPC_InitItemPickupsMoney(int[] values)
    {
        //if (!pv.IsMine)
        //    return;

        for (int i = 0; i < itemsPickupMoney.Count; i++)
        {
            itemsPickupMoney[i].SetValue(values[i]);
        }
    }

    [PunRPC]
    private void RPC_RemoveItem(int index)
    {
        //if (!pv.IsMine)
        //    return;

        GameObject item = itemsPickup[index].gameObject;
        item.GetComponent<ItemPickup>().isDestroyed = true;
        itemsPickup.Remove(item.GetComponent<ItemPickup>());
        Destroy(item);
    }
}
