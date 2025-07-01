using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloorUICanvas : MonoBehaviour
{
    [SerializeField] private GameObject _prefabOfFloorIdButton;
    [SerializeField] private RectTransform _parentOfFoorId;

    [Space]
    [SerializeField] private GameObject _prefabOfDirectFloorCall;
    [SerializeField] private RectTransform _parentOfDirectFloorCall;

    [Space]
    [SerializeField] private LiftManager _liftManager;
    [SerializeField] private Lift _lift;

    private Dictionary<int, GameObject> liftFloorIdButton = new();
    private Dictionary<int, GameObject> liftDirectCallButton = new();
    private readonly Color directFloorCalledColor = new(0.9308f, 0.9308f, 0.9308f);
    private void Awake()
    {
        _lift.onFloorReach += OnFloorReach;
        for (int i = 0; i < _liftManager.NoOfFloor; i++)
        {
            int id = i;
            string floorId = id.ToString();
            GameObject _floorIdBtnGO = Instantiate(_prefabOfFloorIdButton, _parentOfFoorId);
            _floorIdBtnGO.transform.GetComponentInChildren<TMP_Text>().text = floorId;
            _floorIdBtnGO.name = floorId;

            GameObject _floorDirectCall = Instantiate(_prefabOfDirectFloorCall, _parentOfDirectFloorCall);
            _floorDirectCall.transform.GetComponentInChildren<TMP_Text>().text = floorId;
            _floorDirectCall.name = floorId;

            liftFloorIdButton[id] = _floorIdBtnGO;
            liftDirectCallButton[id] = _floorDirectCall;

            var buttonDirectFloorId = liftFloorIdButton[id].GetComponent<Button>();
            var buttonUp = liftDirectCallButton[id].transform.GetChild(1).GetComponent<Button>();
            var buttonDown = liftDirectCallButton[id].transform.GetChild(2).GetComponent<Button>();

            buttonDirectFloorId.onClick.AddListener(() =>
            {
                buttonDirectFloorId.GetComponent<Image>().color = Color.green;
                _lift.GoToFloor(id);
            });

            buttonUp.onClick.AddListener(() =>
            {
                buttonUp.GetComponent<Image>().color = Color.green;
                _lift.LiftCall(id, LiftMoveDirection.Up);
            });
            buttonDown.onClick.AddListener(() =>
            {
                buttonDown.GetComponent<Image>().color = Color.green;
                _lift.LiftCall(id, LiftMoveDirection.Down);
            });
        }
    }


    public void OnFloorReach(int floorID)
    {
        liftFloorIdButton[floorID].GetComponent<Image>().color = Color.white;
        liftDirectCallButton[floorID].transform.GetChild(1).GetComponent<Image>().color = directFloorCalledColor;
        liftDirectCallButton[floorID].transform.GetChild(2).GetComponent<Image>().color = directFloorCalledColor;
    }
}
