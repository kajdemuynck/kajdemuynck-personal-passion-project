using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] TMP_Text roomCapacityText;

    public RoomInfo info;

    public void Setup(RoomInfo _info)
    {
        info = _info;
        roomNameText.text = _info.Name;
        roomCapacityText.text = string.Format("{0}/{1}", _info.PlayerCount, _info.MaxPlayers);
    }

    public void OnClick()
    {
        Launcher.Instance.JoinRoom(info);
    }
}
