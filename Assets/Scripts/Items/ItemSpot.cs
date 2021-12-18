using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpot : MonoBehaviour
{
    public int id;
    public int size;
    public string category;
    private AudioSource audioSource;
    [SerializeField] AudioClip[] audioClips;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.enabled = true;
        id = CreateID();
    }

    private int CreateID()
    {
        string idChild = gameObject.transform.GetSiblingIndex().ToString("000");
        string idParent = gameObject.transform.parent.GetSiblingIndex().ToString("000");
        return int.Parse(string.Format("{0}{1}", idParent, idChild));
    }

    public void PlaySound(AudioClip audioClip = null)
    {
        if (audioSource != null)
        {
            if (audioClip == null)
                audioSource.clip = audioClips[Random.Range(0, audioClips.Length)];
            else
                audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}
