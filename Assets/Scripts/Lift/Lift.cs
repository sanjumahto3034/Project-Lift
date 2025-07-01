using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


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
    [SerializeField] private float liftSpeed = 2.0f; // Units per second

    public bool IsWorking { get; private set; } = false;
    public LiftMoveDirection CurrentDirection = LiftMoveDirection.None;

    private SortedSet<int> upQueue = new();
    private SortedSet<int> downQueue = new(Comparer<int>.Create((a, b) => b.CompareTo(a))); // Descending
    public Action<int> onFloorReach;

    [SerializeField] private GameObject _obstacleDector;
    private void Awake()
    {
        _obstacleDector.SetActive(false);
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
        else
            downQueue.Add(floorId);

        if (!IsWorking)
            StartCoroutine(eGoToNextFloor());
    }

    private IEnumerator eGoToNextFloor()
    {
        IsWorking = true;

        while (upQueue.Count > 0 || downQueue.Count > 0)
        {
            int nextFloor = -1;

            if (CurrentDirection == LiftMoveDirection.Up || CurrentDirection == LiftMoveDirection.None)
            {
                if (upQueue.Count > 0)
                {
                    nextFloor = upQueue.Min;
                    upQueue.Remove(nextFloor);
                    CurrentDirection = LiftMoveDirection.Up;
                }
                else if (downQueue.Count > 0)
                {
                    nextFloor = downQueue.Max;
                    downQueue.Remove(nextFloor);
                    CurrentDirection = LiftMoveDirection.Down;
                }
            }
            else if (CurrentDirection == LiftMoveDirection.Down)
            {
                if (downQueue.Count > 0)
                {
                    nextFloor = downQueue.Max;
                    downQueue.Remove(nextFloor);
                    CurrentDirection = LiftMoveDirection.Down;
                }
                else if (upQueue.Count > 0)
                {
                    nextFloor = upQueue.Min;
                    upQueue.Remove(nextFloor);
                    CurrentDirection = LiftMoveDirection.Up;
                }
            }

            if (nextFloor != -1)
                yield return StartCoroutine(eGoToFloor(nextFloor));
        }

        IsWorking = false;
        CurrentDirection = LiftMoveDirection.None;
    }

    private IEnumerator eGoToFloor(int floorId)
    {
        if (floorId < 0 || floorId >= _liftManager.NoOfFloor)
        {
            Debug.LogWarning("Invalid floor ID.");
            yield break;
        }

        if (floorId == FloorId)
        {
            OpenDoor();
            yield break;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(
            startPos.x,
            _liftManager.FirstFloorPoint.y + (_liftManager.RoomHeight * floorId),
            startPos.z
        );

        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / liftSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        FloorId = floorId;
        Debug.Log($"Lift reached floor {floorId}");
        OpenDoor();
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
#endif
}
