using System.Linq;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
public enum LiftMoveDirection { None, Up, Down }
public sealed class LiftManager : MonoBehaviour
{
    private const int MIN_NO_OF_FLOORS = 3;
    private const int MAX_NO_OF_FLOORS = 10;

    [Range(MIN_NO_OF_FLOORS, MAX_NO_OF_FLOORS)]
    [SerializeField] private int _numberOfFloor = 3;
    public int NoOfFloor => _numberOfFloor;

    [SerializeField] private float _floorGap = 2.5f;
    public float FloorGap => _floorGap;

    [SerializeField] private float _roomHeight = 5;
    public float RoomHeight => _roomHeight;

    [SerializeField] private Transform _pillers;
    [SerializeField] private LiftCallInput[] _liftCallInputs;
    [SerializeField] private GameObject _prefabOfOutsideRemote;
    [SerializeField] private Transform _remoteFloorFirstPoint;
    [SerializeField] private Lift _lift;

    [SerializeField] private GameObject _prefabOfPlatform;


    [SerializeField] private Transform _parentOfLiftInsideRemote;
    public Vector3 FirstFloorPoint;
    private void Awake()
    {
        FirstFloorPoint = _lift.transform.position;
    }
    public void OnFloorReach()
    {

    }
    public void Recreate()
    {

        int insideRemoteCounter = 0;
        foreach (Transform child in _parentOfLiftInsideRemote)
        {
            child.GetComponent<InteractFromInside>().Init(_lift, insideRemoteCounter);
            insideRemoteCounter++;
        }

        for (int i = 0; i < MAX_NO_OF_FLOORS; i++)
        {
            InteractFromInside inside = _parentOfLiftInsideRemote.GetChild(i).GetComponent<InteractFromInside>();
            if (i >= NoOfFloor) inside.DisabelLiftButton();
        }

        if (_liftCallInputs != null)
        {
            foreach (var item in _liftCallInputs.Where(n => n != null))
            {
                var target = item.gameObject;
                if (target != null)
                {
                    if (Application.isPlaying) Destroy(target);
                    else DestroyImmediate(target);
                }
            }
        }

        _liftCallInputs = new LiftCallInput[_numberOfFloor];

        Vector3 position = _remoteFloorFirstPoint.position;
        for (int i = 0; i < _numberOfFloor; i++)
        {
            GameObject go = Instantiate(_prefabOfOutsideRemote);
            go.name = $"Call Floor {i}";
            go.transform.position = position;
            position += new Vector3(0, _roomHeight, 0);
            _liftCallInputs[i] = go.GetComponent<LiftCallInput>();
            _liftCallInputs[i].Init(i, _lift);
            if (i > 0)
            {
                GameObject baseGO = Instantiate(_prefabOfPlatform);
                float y = _lift.transform.position.y + i * _roomHeight;
                baseGO.transform.position = new Vector3(_lift.transform.position.x, y, _lift.transform.position.z);
                baseGO.transform.parent = go.transform;
            }
        }

        _pillers.transform.localScale = new Vector3(1, _numberOfFloor * _roomHeight, 1);
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(LiftManager))]
    public sealed class LiftManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(20);
            LiftManager baseClass = (LiftManager)target;
            if (GUILayout.Button(nameof(baseClass.Recreate), GUILayout.Height(30)))
                baseClass.Recreate();
        }
    }
#endif
}