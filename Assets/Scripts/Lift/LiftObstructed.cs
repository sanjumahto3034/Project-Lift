using UnityEngine;

public class LiftObstructed : MonoBehaviour
{
    [SerializeField] private Lift _lift;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
            _lift.OpenDoor();
    }
}
