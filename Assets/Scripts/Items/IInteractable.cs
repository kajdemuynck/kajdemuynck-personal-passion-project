using UnityEngine;

public interface IInteractable
{
    bool Interact(RaycastHit hit, bool isInteracting);
}