using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickup : MonoBehaviourPunCallbacks
{
    protected string description;
    protected bool isDestroyed = false;
    protected int interactionDistance = 2;
    private HUD hud;

    protected virtual void Start()
    {
        hud = GameObject.Find("HUD").GetComponent<HUD>();
    }

    protected void OnMouseOver()
    {
        if (GetDistanceFromCamera(transform.position) <= interactionDistance && !isDestroyed)
            hud.ShowDescription(description);
        else
            hud.HideDescription();
    }

    protected void OnMouseExit()
    {
        hud.HideDescription();
    }

    protected virtual void OnMouseDown()
    {
        if (GetDistanceFromCamera(transform.position) <= interactionDistance)
        {
            RemoveItem();
        }
    }

    protected float GetDistanceFromCamera(Vector3 pos)
    {
        Transform camera = Camera.main.transform;
        float dist = Vector3.Distance(camera.position, pos);
        Debug.Log(dist);
        return dist;
    }

    private void RemoveItem()
    {
        isDestroyed = true;
        hud.HideDescription();
        Destroy(gameObject);
    }
}
