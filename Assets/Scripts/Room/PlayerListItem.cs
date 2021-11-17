using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerListItem : MonoBehaviourPunCallbacks
{
    [SerializeField] TMP_Text usernameText;
    [SerializeField] TMP_Text pingText;

    Player player;
    private Color32[] colors = new Color32[3] { new Color32(95, 215, 127, 255), new Color32(255, 191, 63, 255), new Color32(255, 71, 71, 255) };
    //private List<string> list = new List<string> { };

    public void Setup(Player _player)
    {
        player = _player;
        usernameText.text = _player.NickName;
        StartCoroutine(UpdatePing());
    }

    private IEnumerator UpdatePing()
    {
        while (true)
        {
            if (player.CustomProperties.ContainsKey("ping"))
            {
                int ping = int.Parse(player.CustomProperties["ping"].ToString());
                pingText.text = ping.ToString();

                if (ping < 50)
                {
                    pingText.color = colors[0];
                }
                else if (ping > 100)
                {
                    pingText.color = colors[2];
                }
                else
                {
                    pingText.color = colors[1];
                }
            }
            yield return new WaitForSeconds(2f);
        }

    }

    public override void OnLeftRoom()
    {
        Destroy(gameObject);

    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (player == otherPlayer)
        {
            Destroy(gameObject);
        }
    }
}
