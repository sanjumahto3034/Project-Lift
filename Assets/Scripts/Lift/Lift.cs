using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;


#if UNITY_EDITOR
using UnityEditor;
#endif

public struct LiftStateCalled
{
    public int floorId;
    public LiftMoveDirection direction;

    public static LiftStateCalled Create(int floorID, LiftMoveDirection direction) => new LiftStateCalled()
    {
        floorId = floorID,
        direction = direction
    };

    public override bool Equals(object obj)
    {
        if (!(obj is LiftStateCalled)) return false;
        return Equals((LiftStateCalled)obj);
    }

    public bool Equals(LiftStateCalled other) => floorId == other.floorId && direction == other.direction;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + floorId.GetHashCode();
            hash = hash * 23 + direction.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(LiftStateCalled lhs, LiftStateCalled rhs) => lhs.Equals(rhs);
    public static bool operator !=(LiftStateCalled lhs, LiftStateCalled rhs) => !(lhs == rhs);
}

public class Lift : MonoBehaviour
{
    public int FloorId { get; private set; } = 0;

    [SerializeField] private Transform _leftDoor;
    [SerializeField] private Transform _rightDoor;
    [SerializeField] private LiftManager _liftManager;
    [SerializeField] private float liftSpeed = 2.0f;

    public bool IsWorking { get; private set; } = false;
    public LiftMoveDirection CurrentDirection = LiftMoveDirection.None;

    public Action<int> onFloorReach;
    public Action<int> onFloorNumberUpdate;

    private SortedSet<int> upQueue = new();
    private SortedSet<int> downQueue = new(Comparer<int>.Create((a, b) => b.CompareTo(a)));

    [SerializeField] private GameObject _obstacleDector;
    [SerializeField] private TMP_Text _liftNumber;
    private void Awake()
    {
        _obstacleDector.SetActive(false);
        onFloorNumberUpdate += (int id) => _liftNumber.text = id.ToString();
    }

    public void LiftCall(int floorId, LiftMoveDirection direction)
    {
        if (!IsWorking && floorId == FloorId)
        {
            OpenDoor();
            return;
        }

        if (direction == LiftMoveDirection.Up)
            upQueue.Add(floorId);
        else
            downQueue.Add(floorId);

        if (!IsWorking)
            StartCoroutine(eGoToNextFloor());
    }

    public void GoToFloor(int floorId)
    {
        if (!IsWorking && floorId == FloorId)
        {
            OpenDoor();
            return;
        }

        if (floorId > FloorId)
            upQueue.Add(floorId);
        else if (floorId < FloorId)
            downQueue.Add(floorId);

        if (!IsWorking)
            StartCoroutine(eGoToNextFloor());
    }

    private IEnumerator eGoToNextFloor()
    {
        IsWorking = true;

        while (upQueue.Count > 0 || downQueue.Count > 0)
        {
            int? targetFloor = GetNextTargetFloor();
            if (targetFloor.HasValue)
                yield return StartCoroutine(eGoToFloorDynamic(targetFloor.Value));
            else
                break;
        }

        IsWorking = false;
        CurrentDirection = LiftMoveDirection.None;
    }

    private int? GetNextTargetFloor()
    {
        if (CurrentDirection == LiftMoveDirection.None)
        {
            if (upQueue.Count > 0)
            {
                CurrentDirection = LiftMoveDirection.Up;
                return upQueue.Min;
            }
            else if (downQueue.Count > 0)
            {
                CurrentDirection = LiftMoveDirection.Down;
                return downQueue.Max;
            }
        }
        else if (CurrentDirection == LiftMoveDirection.Up)
        {
            foreach (int floor in upQueue)
                if (floor > FloorId)
                    return floor;

            if (downQueue.Count > 0)
            {
                CurrentDirection = LiftMoveDirection.Down;
                return downQueue.Max;
            }
        }
        else if (CurrentDirection == LiftMoveDirection.Down)
        {
            foreach (int floor in downQueue)
                if (floor < FloorId)
                    return floor;

            if (upQueue.Count > 0)
            {
                CurrentDirection = LiftMoveDirection.Up;
                return upQueue.Min;
            }
        }

        return null;
    }
    private IEnumerator eGoToFloorDynamic(int targetFloor)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(startPos.x, _liftManager.FirstFloorPoint.y + (_liftManager.RoomHeight * targetFloor), startPos.z);
        float duration = Vector3.Distance(startPos, endPos) / liftSpeed;
        float elapsed = 0f;

        onFloorNumberUpdate?.Invoke(FloorId); // Immediate update when starting movement

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);

            int liveFloor = GetClosestFloorToCurrentPosition();
            onFloorNumberUpdate?.Invoke(liveFloor); // 🔄 Dynamic update while moving

            if (CurrentDirection == LiftMoveDirection.Up && upQueue.Contains(liveFloor) && liveFloor > FloorId && liveFloor < targetFloor)
            {
                upQueue.Remove(liveFloor);
                yield return StartCoroutine(eGoToFloorDynamic(liveFloor));
                yield break;
            }
            else if (CurrentDirection == LiftMoveDirection.Down && downQueue.Contains(liveFloor) && liveFloor < FloorId && liveFloor > targetFloor)
            {
                downQueue.Remove(liveFloor);
                yield return StartCoroutine(eGoToFloorDynamic(liveFloor));
                yield break;
            }

            yield return null;
        }

        transform.position = endPos;
        FloorId = targetFloor;

        onFloorNumberUpdate?.Invoke(FloorId); // Final update when target reached
        Debug.Log($"Lift reached floor {targetFloor}");

        if (CurrentDirection == LiftMoveDirection.Up)
            upQueue.Remove(targetFloor);
        else
            downQueue.Remove(targetFloor);

        OpenDoor();
    }

    private int GetClosestFloorToCurrentPosition()
    {
        float liftY = transform.position.y;
        float minDistance = float.MaxValue;
        int closestFloor = FloorId;

        for (int i = 0; i < _liftManager.NoOfFloor; i++)
        {
            float floorY = _liftManager.FirstFloorPoint.y + (_liftManager.RoomHeight * i);
            float dist = Mathf.Abs(floorY - liftY);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestFloor = i;
            }
        }

        return closestFloor;
    }

    public void OpenDoor()
    {
        _obstacleDector.SetActive(false);
        onFloorReach?.Invoke(FloorId);
        StopAllCoroutines();
        StartCoroutine(eOpenDoor());
    }

    public void CloseDoor()
    {
        StopAllCoroutines();
        StartCoroutine(eCloseDoor());
    }

    private IEnumerator eOpenDoor()
    {
        IsWorking = true;
        float scale = _leftDoor.localScale.x;
        while (scale >= 0)
        {
            scale -= Time.deltaTime;
            _leftDoor.localScale = new Vector3(Mathf.Max(0, scale), 1, 1);
            _rightDoor.localScale = new Vector3(Mathf.Max(0, scale), 1, 1);
            yield return null;
        }

        _leftDoor.localScale = Vector3.zero;
        _rightDoor.localScale = Vector3.zero;
        yield return new WaitForSeconds(3f);
        CloseDoor();
    }

    private IEnumerator eCloseDoor()
    {
        float scale = _leftDoor.localScale.x;
        _obstacleDector.SetActive(true);
        while (scale < 1)
        {
            scale += Time.deltaTime;
            _leftDoor.localScale = new Vector3(Mathf.Min(1, scale), 1, 1);
            _rightDoor.localScale = new Vector3(Mathf.Min(1, scale), 1, 1);
            yield return null;
        }

        _leftDoor.localScale = Vector3.one;
        _rightDoor.localScale = Vector3.one;

        if (upQueue.Count > 0 || downQueue.Count > 0)
            StartCoroutine(eGoToNextFloor());
        else
        {
            IsWorking = false;
            CurrentDirection = LiftMoveDirection.None;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Lift))]
    public sealed class LiftEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(20);
            Lift lift = (Lift)target;
            if (!Application.isPlaying) return;

            if (GUILayout.Button(nameof(lift.OpenDoor), GUILayout.Height(30)))
                lift.OpenDoor();
            if (GUILayout.Button(nameof(lift.CloseDoor), GUILayout.Height(30)))
                lift.CloseDoor();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (int f in upQueue)
        {
            Vector3 pos = new Vector3(transform.position.x + 1, _liftManager.FirstFloorPoint.y + (_liftManager.RoomHeight * f), transform.position.z);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
        }

        Gizmos.color = Color.red;
        foreach (int f in downQueue)
        {
            Vector3 pos = new Vector3(transform.position.x - 1, _liftManager.FirstFloorPoint.y + (_liftManager.RoomHeight * f), transform.position.z);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
        }
    }
#endif
}
