using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickup : MonoBehaviourPunCallbacks
{
    public int id;
    public string description;
    public bool isDestroyed = false;
    public int interactionDistance = 2;

    ItemManager itemManager;

    protected void Start()
    {
        itemManager = GameObject.Find("ItemManager").GetComponent<ItemManager>();
    }

    public virtual void CheckInteraction()
    {
        if (Input.GetMouseButtonDown(0))
        {
            itemManager.RemoveItem(gameObject);
        }
    }

    //protected void OnMouseOver()
    //{
    //    if (GetDistanceFromCamera(transform.position) <= interactionDistance && !isDestroyed)
    //        hud.ShowDescription(description);
    //    else
    //        hud.HideDescription();
    //}

    //protected void OnMouseExit()
    //{
    //    hud.HideDescription();
    //}

    //protected virtual void OnMouseDown()
    //{
    //    if (GetDistanceFromCamera(transform.position) <= interactionDistance)
    //    {
    //        RemoveItem();
    //    }
    //}

    //protected float GetDistanceFromCamera(Vector3 pos)
    //{
    //    Transform camera = Camera.main.transform;
    //    float dist = Vector3.Distance(camera.position, pos);
    //    return dist;
    //}
}
