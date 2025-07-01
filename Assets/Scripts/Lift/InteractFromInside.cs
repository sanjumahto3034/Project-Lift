using TMPro;
using UnityEngine;

public class InteractFromInside : MonoBehaviour, IInteract
{
    [SerializeField] private Lift _lift;
    [SerializeField] private int _floorId;
    [SerializeField] private bool _isDisable;
    private void Awake()
    {
        _lift.onFloorReach += OnReach;
    }
    public void Interact()
    {
        if (_isDisable)
            return;

        GetComponent<MeshRenderer>().material.color = Color.green;
        this._lift.GoToFloor(_floorId);
    }
    private void OnReach(int id)
    {
        if (id == _floorId)
        {
            GetComponent<MeshRenderer>().material.color = Color.white;
        }
    }
    public void Init(Lift lift, int floorId)
    {
        _isDisable = false;
        this._floorId = floorId;
        this._lift = lift;
        GetComponent<MeshRenderer>().sharedMaterial.color = Color.white;
    }
    public void DisabelLiftButton()
    {
        _isDisable = true;
        GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Strikethrough;
        GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;
    }
}
