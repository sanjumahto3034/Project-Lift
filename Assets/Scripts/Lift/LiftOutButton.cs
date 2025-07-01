using UnityEngine;

public sealed class LiftOutButton : MonoBehaviour, IInteract
{
    public LiftMoveDirection direction;


    private void Awake()
    {
        GetComponent<MeshRenderer>().material.color = Color.white;
        GetComponentInParent<LiftCallInput>().onReach += () => GetComponent<MeshRenderer>().material.color = Color.white;
    }

    public void Interact()
    {
        GetComponent<MeshRenderer>().material.color = Color.green;
        GetComponentInParent<LiftCallInput>().CallLift(direction);
    }
}