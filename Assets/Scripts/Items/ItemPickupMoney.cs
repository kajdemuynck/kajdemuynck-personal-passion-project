using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickupMoney : ItemPickup
{
    private int value;
    public string category;

    private void Start()
    {
        type = "money";
    }

    public override bool Interact(RaycastHit hit, bool isInteracting)
    {
        if (base.Interact(hit, isInteracting))
        {
            if (isInteracting)
                CollectMoney();

            return true;
        }
        else
            return false;
    }

    public void CollectMoney()
    {
        int money = (int)PhotonNetwork.LocalPlayer.CustomProperties["money"];
        money += value;
        Hashtable hash = new Hashtable();
        hash.Add("money", money);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        Debug.Log(money);
    }

    public void SetValue(int _value)
    {
        value = _value;
        description = string.Format("Worth {0} dollars", value);
    }
}
