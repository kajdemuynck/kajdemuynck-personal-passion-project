using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ItemPickupMoney : ItemPickup
{
    private int value;

    public override void CheckInteraction(PlayerInput playerInput)
    {
        if (Mouse.current.leftButton.isPressed)
        {
            CollectMoney();
        }

        base.CheckInteraction(playerInput);
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
