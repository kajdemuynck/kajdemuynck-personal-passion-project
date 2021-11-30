using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
//using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickup : MonoBehaviourPunCallbacks, IInteractable
{
    public int id;
    public string type;
    public string description = "[ITEM]";
    public bool isPickedUp = false;
    public float interactionDistance = 2f;

    protected void Awake()
    {
        id = CreateID();
        //itemManager = GameObject.Find("ItemManager").GetComponent<ItemManager>();
    }

    public virtual void Interact(RaycastHit hit, bool isInteracting)
    {
        if (hit.distance <= interactionDistance)
        {
            HUD.Instance.ShowDescription(description);

            if (isInteracting)
            {
                Inventory.Instance.SetItemInInventory(type);
                ItemManager.Instance.RemoveItem(gameObject);
            }
        }
        else
            HUD.Instance.HideDescription();
    }

    private int CreateID()
    {
        string idChild = gameObject.transform.GetSiblingIndex().ToString("000");
        string idParent = gameObject.transform.parent.GetSiblingIndex().ToString("000");
        return int.Parse(string.Format("{0}{1}", idChild, idParent));
    }
}
