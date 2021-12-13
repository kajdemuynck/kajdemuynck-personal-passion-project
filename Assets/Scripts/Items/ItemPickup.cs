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
        //TouchControls.Instance.grabButton.onClick.AddListener(TakeItem);
    }

    public virtual bool Interact(RaycastHit hit, bool isInteracting)
    {
        if (hit.distance <= interactionDistance)
        {
            GameplayManager.Instance.ShowDescription(description);

            if (isInteracting)
                TakeItem();

            return true;
        }
        else
            return false;
    }

    private void TakeItem()
    {
        Inventory.Instance.SetItemInInventory(type);
        ItemManager.Instance.RemoveItem(gameObject);
    }

    private int CreateID()
    {
        string idChild = gameObject.transform.GetSiblingIndex().ToString("000");
        string idParent = gameObject.transform.parent.GetSiblingIndex().ToString("000");
        return int.Parse(string.Format("{0}{1}", idChild, idParent));
    }
}
