using UnityEngine;

public class ClayInteractionTest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("🔥 Hand touched clay: " + other.name);
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log("👋 Hand is interacting with clay");
    }
}