using UnityEngine;

public class TrialTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    public bool WasTriggered { get; private set; }

    public void ResetTrigger()
    {
        WasTriggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            WasTriggered = true;
        }
    }
}