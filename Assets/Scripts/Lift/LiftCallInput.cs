using System;
using UnityEngine;

public sealed class LiftCallInput : MonoBehaviour
{
    [SerializeField] private int _floorId;
    public int FloorId => _floorId;
    [SerializeField] private Lift _lift;


    public Action onReach;
    private void Awake()
    {
        _lift.onFloorReach += OnReach;
    }
    private void OnReach(int id)
    {
        onReach?.Invoke();
    }
    public void CallLift(LiftMoveDirection direction)
    {
        _lift.LiftCall(_floorId, direction);
    }

    public void Init(int id, Lift lift)
    {
        _floorId = id;
        _lift = lift;
    }
}
