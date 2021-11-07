using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickup : MonoBehaviourPunCallbacks
{
    private int value = 20;
    private HUD hud;

    private void Start()
    {
        hud = GameObject.Find("HUD").GetComponent<HUD>();
        Debug.Log(hud.name);
    }

    private void OnMouseOver()
    {
        Transform camera = Camera.main.transform;
        float dist = Vector3.Distance(camera.position, transform.position);
        if (dist <= 2)
            hud.ShowValue(value);
        else
            hud.HideValue();
    }

    private void OnMouseExit()
    {
        hud.HideValue();
    }

    private void OnMouseDown()
    {
        int money = (int)PhotonNetwork.LocalPlayer.CustomProperties["money"];
        money += value;
        Hashtable hash = new Hashtable();
        hash.Add("money", money);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        Debug.Log(money);
        hud.HideValue();
        Destroy(gameObject);
    }

    public void SetValue(int _value)
    {
        value = _value;
    }
}
