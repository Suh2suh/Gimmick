using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Platform_CircularMove : GimmickPlatformBase
    {
        protected override bool UseUpdatePosition => true;
        protected override bool UseUpdateRotation => false;
        
        [LineTitle("PLATFORM_ CIRCULAR MOVE")]
        [Tooltip("이동 패턴 | One Way: 0-N 정지 | Loop: 0-N-0 무한 반복 | PingPong: 0-N-0(순) 후 0-N-0(역) 반복")] 
        [SerializeField] private GimmickPropMovementPattern _movementPattern = GimmickPropMovementPattern.OneWay;
        
        [Tooltip("목적지 도달 시 파괴 여부 | OneWay 패턴만 사용")] 
        [SerializeField] private bool _shouldDespawnOnDead = false;
        
        [LineSubtitle("- Path")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _centralPoint;
        [SerializeField, Range(0, 180f)] private float _normalVectorDegree = 0f;
        [SerializeField] private float _defaultSpeed = 10f;
        [SerializeField] private float _defaultWaitTime = 0f;
        [SerializeField] private bool _isMovingClockwise = true;

        [Space(5)]
        [SerializeField] private List<WayPointData> _wayPoints = new();
        [Serializable] public class WayPointData
        {
            [field: SerializeField] public Transform WayPointTransform { get; private set; }
            [field: SerializeField] public float SpeedToReach { get; private set; } = 10f;
            [field: SerializeField] public float WaitTime { get; private set; }
            [HideInInspector] public Vector3 CalculatedPosition;
            [HideInInspector] public float AngleFromStart; 
        }

        [SerializeField, ReadOnly] private long _mainPhaseDurationMs;
        
        // 운행 시간표: 각 노드가 고유한 물리 데이터와 누적 시간을 가짐
        private List<TimelineNode> _timeline = new();
        private class TimelineNode
        {
            public int StartWp;
            public int GoalWp;
            public float AngleDelta;
            public long MoveDurationMs;
            public long WholeDurationMs;
            public long PathEndMs; 
        }

        private Vector3 _radiusVector;
        private Vector3 _rotateAxis;
        private Vector3 _circlePathCenterPos;
        private float _radius;

        protected override void AwakePlatform()
        {
            if (_startPoint == null || _centralPoint == null)
            {
                CurrentState = GimmickPropState.Dead;
                return;
            }
            _circlePathCenterPos = _centralPoint.position;
            RebuildWayPoints();
            InitPath();
        }

        private void InitPath()
        {
            _timeline.Clear();
            int totalStations = _wayPoints.Count + 1; 
            if (totalStations <= 1) return;

            // PingPong: 순방향 한 바퀴(total) + 역방향 한 바퀴(total) = 2 * total
            int maxLoopCount = _movementPattern switch
            {
                GimmickPropMovementPattern.OneWay => totalStations - 1,
                GimmickPropMovementPattern.Loop => totalStations,
                GimmickPropMovementPattern.PingPong => totalStations * 2,
                _ => totalStations
            };

            int startWp = 0;
            long currentTimelineMs = 0;

            for (int i = 0; i < maxLoopCount; i++)
            {
                int goalWp;
                bool isForwardPhase = true;

                if (_movementPattern == GimmickPropMovementPattern.PingPong)
                {
                    isForwardPhase = i < totalStations; // 절반은 순방향, 절반은 역방향
                    if (isForwardPhase)
                    {
                        goalWp = (startWp + 1) % totalStations;
                    }
                    else
                    {
                        goalWp = startWp - 1;
                        if (goalWp < 0) goalWp = totalStations - 1;
                    }
                }
                else
                {
                    goalWp = (startWp + 1) % totalStations;
                    if (_movementPattern == GimmickPropMovementPattern.OneWay && startWp == totalStations - 1) break;
                }

                // 물리 계산 (방향성에 따른 AngleDelta 추출)
                bool actualClockwise = isForwardPhase ? _isMovingClockwise : !_isMovingClockwise;
                TimelineNode node = CalculateNodePhysics(startWp, goalWp, actualClockwise);
                
                currentTimelineMs += node.WholeDurationMs;
                node.PathEndMs = currentTimelineMs;
                _timeline.Add(node);

                startWp = goalWp;
            }
            _mainPhaseDurationMs = currentTimelineMs;
        }

        private TimelineNode CalculateNodePhysics(int startWp, int goalWp, bool clockwise)
        {
            float startAngle = GetStationAngle(startWp);
            float endAngle = GetStationAngle(goalWp);
            float speed = GetStationSpeed(goalWp);

            float angleDiff = Mathf.DeltaAngle(startAngle, endAngle);
            if (clockwise && angleDiff <= 0) angleDiff += 360f;
            else if (!clockwise && angleDiff >= 0) angleDiff -= 360f;

            double arcLength = (2.0 * Math.PI * (double)_radius) * (Math.Abs((double)angleDiff) / 360.0);
            long moveMs = (long)Math.Round((arcLength / Math.Max(0.001, (double)speed)) * 1000.0);
            long wholeMs = moveMs + (long)Math.Round((double)GetStationWaitTime(goalWp) * 1000.0);

            return new TimelineNode
            {
                StartWp = startWp,
                GoalWp = goalWp,
                AngleDelta = angleDiff,
                MoveDurationMs = moveMs,
                WholeDurationMs = wholeMs
            };
        }

        protected override async UniTask Actuate()
        {
            if (_timeline.Count == 0) return;

            // 동기화 시점 보정
            long prevActuationDuration = 0;
            int clampedSubPhase = Mathf.Clamp(DoneSubPhase, 0, _timeline.Count);
            if (clampedSubPhase > 0) prevActuationDuration = _timeline[clampedSubPhase - 1].PathEndMs;
            ActuationStartTimeMs -= prevActuationDuration;

            try
            {
                while (CurrentState == GimmickPropState.Actuating)
                {
                    long elapsedTimeMs = Math.Max(0, ReNetworkUtility.NowServerTimeMs - ActuationStartTimeMs);
                    if (_movementPattern != GimmickPropMovementPattern.OneWay) elapsedTimeMs %= _mainPhaseDurationMs;
                    
                    int subPhaseIndex = GetStartSubPhase(elapsedTimeMs);
                    var node = _timeline[subPhaseIndex];
                    
                    long prevSubPhaseEndMs = (subPhaseIndex == 0) ? 0 : _timeline[subPhaseIndex - 1].PathEndMs;
                    long elapsedSubPhaseTimeMs = elapsedTimeMs - prevSubPhaseEndMs;
                    
                    if (elapsedSubPhaseTimeMs < node.MoveDurationMs)
                    {
                        double progress = (double)elapsedSubPhaseTimeMs / node.MoveDurationMs;
                        float currentAngle = GetStationAngle(node.StartWp) + (node.AngleDelta * (float)progress);
                        
                        Quaternion rot = Quaternion.AngleAxis(currentAngle, _rotateAxis);
                        UpdatePosition = _circlePathCenterPos + (rot * _radiusVector);
                    }
                    else
                    {
                        UpdatePosition = GetStationPosition(node.GoalWp);
                        DoneSubPhase = subPhaseIndex + 1;

                        if (_movementPattern == GimmickPropMovementPattern.OneWay && subPhaseIndex == _timeline.Count - 1)
                        {
                            CurrentState = GimmickPropState.Dead;
                            if (_shouldDespawnOnDead) gameObject.SetActive(false);
                            break;
                        }
                        if (IsCancelReserved) break;
                    }
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
                }
            }
            finally
            {
                if (CurrentState != GimmickPropState.Dead) CurrentState = GimmickPropState.Ready;
            }
        }
        
        private int GetStartSubPhase(long elapsedMainPhaseMs)
        {
            for (int i = 0; i < _timeline.Count; i++)
            {
                if (elapsedMainPhaseMs < _timeline[i].PathEndMs) return i;
            }
            return Math.Max(0, _timeline.Count - 1);
        }

        [ContextMenu("Rebuild WayPoints")]
        public void RebuildWayPoints()
        {
            if (_startPoint == null || _centralPoint == null) return;
            _circlePathCenterPos = _centralPoint.position;
            _radiusVector = _startPoint.position - _circlePathCenterPos;
            _radius = _radiusVector.magnitude;
            _rotateAxis = CalculateRotateAxis(_radiusVector);

            foreach (var wp in _wayPoints)
            {
                if (wp.WayPointTransform == null) continue;
                Vector3 toWP = wp.WayPointTransform.position - _circlePathCenterPos;
                Vector3 projected = Vector3.ProjectOnPlane(toWP, _rotateAxis).normalized * _radius;
                wp.CalculatedPosition = _circlePathCenterPos + projected;
                wp.WayPointTransform.position = wp.CalculatedPosition;
                float angle = Vector3.SignedAngle(_radiusVector, projected, _rotateAxis);
                if (_isMovingClockwise) { if (angle < 0) angle += 360f; }
                else { angle = -angle; if (angle < 0) angle += 360f; }
                wp.AngleFromStart = angle;
            }
            _wayPoints.Sort((a, b) => a.AngleFromStart.CompareTo(b.AngleFromStart));
            foreach (var wp in _wayPoints)
            {
                Vector3 dir = wp.CalculatedPosition - _circlePathCenterPos;
                wp.AngleFromStart = Vector3.SignedAngle(_radiusVector, dir, _rotateAxis);
            }
        }
        
        private Vector3 CalculateRotateAxis(Vector3 radiusVector)
        {
            Vector3 side = Vector3.Cross(radiusVector.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(radiusVector.normalized, Vector3.forward);
            Vector3 zeroDegreeVector = Vector3.Cross(radiusVector.normalized, side.normalized);
            return Quaternion.AngleAxis(_normalVectorDegree, radiusVector.normalized) * zeroDegreeVector;
        }

        private float GetStationAngle(int index) => index == 0 ? 0f : _wayPoints[index - 1].AngleFromStart;
        private Vector3 GetStationPosition(int index) => index == 0 ? _startPoint.position : _wayPoints[index - 1].CalculatedPosition;
        private float GetStationSpeed(int index) => index == 0 ? _defaultSpeed : _wayPoints[index - 1].SpeedToReach;
        private float GetStationWaitTime(int index) => index == 0 ? _defaultWaitTime : _wayPoints[index - 1].WaitTime;

#if UNITY_EDITOR
        private void OnValidate() { if (!Application.isPlaying) RebuildWayPoints(); }
        private void OnDrawGizmos()
        {
            if (_centralPoint == null || _startPoint == null) return;
            Vector3 center = _centralPoint.position;
            Vector3 rVec = _startPoint.position - center;
            float radius = rVec.magnitude;
            Vector3 axis = CalculateRotateAxis(rVec);
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(center, axis, radius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_startPoint.position, 0.2f);
            if (_wayPoints != null)
            {
                for (int i = 0; i < _wayPoints.Count; i++)
                {
                    if (_wayPoints[i].WayPointTransform == null) continue;
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(GetStationPosition(i + 1), 0.15f);
                }
            }
        }
#endif
    }
}