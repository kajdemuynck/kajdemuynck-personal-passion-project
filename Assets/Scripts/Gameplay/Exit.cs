using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour
{
    private float timer;
    private float holdTime = 1f;

    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();

            if (pc.pv.IsMine && pc.pm.role == "robber" && !pc.pm.hasFinishedSpree)
            {
                timer += Time.deltaTime;

                if (timer >= holdTime)
                    pc.pm.FinishSpree();
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();

            if (pc.pv.IsMine && !pc.pm.hasFinishedSpree)
                timer = 0;
        }
    }
}
