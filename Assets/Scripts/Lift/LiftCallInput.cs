using System;
using TMPro;
using UnityEngine;

public sealed class LiftCallInput : MonoBehaviour
{
    [SerializeField] private int _floorId;
    public int FloorId => _floorId;
    [SerializeField] private Lift _lift;

    [SerializeField] private TMP_Text _liftExistNumererLbl;
    public Action onReach;
    private void Awake()
    {
        _lift.onFloorReach += OnReach;
        _lift.onFloorNumberUpdate += OnLiftNumberUpdate;
    }
    public void OnLiftNumberUpdate(int id)
    {
        _liftExistNumererLbl.text = id.ToString();

    }
    private void OnReach(int id)
    {
        if (_floorId == id)
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
