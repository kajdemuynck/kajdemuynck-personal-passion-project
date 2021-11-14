using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickupMoney : ItemPickup
{
    private int value;

    protected override void Start()
    {
        base.Start();
        value = (int)Random.Range(10f, 20f);
        description = string.Format("Worth {0} dollars", value);
    }

    protected override void OnMouseDown()
    {
        if (base.GetDistanceFromCamera(transform.position) <= interactionDistance)
        {
            int money = (int)PhotonNetwork.LocalPlayer.CustomProperties["money"];
            money += value;
            Hashtable hash = new Hashtable();
            hash.Add("money", money);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
        base.OnMouseDown();
    }

    public void SetValue(int _value)
    {
        value = _value;
    }
}
