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

            if (pc.pv.IsMine && pc.pm.role == "robber" && !pc.pm.hasEscaped)
            {
                timer += Time.deltaTime;

                if (timer >= holdTime)
                    pc.pm.Escape();
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.GetComponent<PlayerController>())
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();

            if (pc.pv.IsMine && !pc.pm.hasEscaped)
                timer = 0;
        }
    }
}
