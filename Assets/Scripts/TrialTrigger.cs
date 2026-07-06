using UnityEngine;

public class TrialTrigger : MonoBehaviour
{
    // Stores whether this trigger has been activated.
    public bool WasTriggered { get; private set; }

    // Stores whether the participant is currently inside this trigger.
    public bool IsInside { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        // Checks whether the entering object is the player.
        if (!other.CompareTag("Player"))
            return;

        // Stores that the player is inside the trigger.
        IsInside = true;

        // Stores that the trigger was activated.
        WasTriggered = true;
    }

    private void OnTriggerExit(Collider other)
    {
        // Checks whether the exiting object is the player.
        if (!other.CompareTag("Player"))
            return;

        // Stores that the player is no longer inside the trigger.
        IsInside = false;
    }

    public void ResetTrigger()
    {
        // Resets the trigger activation state.
        WasTriggered = false;
    }
}