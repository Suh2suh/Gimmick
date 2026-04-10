using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Platform_LinearMove : GimmickPlatformBase
    {
        protected override bool UseUpdatePosition => true;
        protected override bool UseUpdateRotation => false;
        
        [LineTitle("Platform_LinearMove")]
        [Tooltip("이동 패턴 | One Way: WayPoint 0 - ... - WayPoint N - 정지 | Loop: WayPoint 0 - ... - WayPoint N - WayPoint 0 - ..." +
                 "            Loop :  WayPoint 0 - ... - WayPoint N - ... - WayPoint 0 - ...")] [SerializeField] 
        private GimmickPropMovementPattern _movementPattern = GimmickPropMovementPattern.OneWay;
        
        [Tooltip("목적지 도달 시 파괴 여부 | OneWay 패턴만 사용")] [SerializeField] 
        private bool _shouldDespawnOnDead = false;
        
        [LineCustom(10, 10, true)]
        [Tooltip("WayPoint 이동 시 가감속 옵션 | 설정/해제에 따른 도달 시간은 동일")] [SerializeField]
        private bool _useAcceleration = false;
        
        [Tooltip("가감속 유지 시간 | 가감속 옵션 설정 시 사용")] [SerializeField]
        private float _accelerationTime = 0.1f;
        
        [LineCustom(10, 10, true)]
        [Tooltip("플랫폼 이동 경로 | 서브 페이즈의 기준 (0-1: SubPhase 1, 1-2: SuhPhase 2...)")] [SerializeField][Space(5)] 
        private List<WayPointData> _wayPoints = new();
        [Serializable] private class WayPointData
        {
            public Vector3 Position => WayPointTransform.position;
            [HideInInspector] public int Index;
            [field: SerializeField] public Transform WayPointTransform { get; private set; }
            [field: SerializeField] public float SpeedToReach { get; private set; } = 10f;  // m/sec 
            [field: SerializeField] public float WaitTime { get; private set; }  // sec
        }
        
        [Tooltip("메인 페이즈 작동 소요 시간 | 전체 서브페이즈 작동 소요 시간의 집합")] [SerializeField, ReadOnly][Space(5)]
        private long _mainPhaseDurationMs;
        
        private Dictionary<(int startWp, int goalWp), PathData> _wayPointPath = new();
        private List<(int startWp, int goalWp)> _pathSequence = new();
        
        private long _accelerationTimeMs;

        
        // ==================================================
        // [LOGIC] Initialization
        // ==================================================
        protected override void AwakePlatform()
        {
            RebuildWayPoints();
            InitPath();

            _accelerationTimeMs = (long)Mathf.Round(Mathf.Max(0, _accelerationTime * 1000));
        }
        
        private void InitPath()
        {
            _wayPointPath.Clear();
            _pathSequence.Clear();

            int prevWp = 0, startWp = 0;
            // 정거장 수에 따른 최대 경로 확보 (PingPong 고려)
            int maxLoopCount = (_wayPoints.Count - 1) * 2; 
            if (_movementPattern == GimmickPropMovementPattern.OneWay) maxLoopCount = _wayPoints.Count - 1;

            long pathEndMs = 0;
            for (int i = 0; i < maxLoopCount; i++)
            {
                var goalWayPoint = GetNextStationIndexInternal(prevWp, startWp);
                if (goalWayPoint == null) break;

                var segment = (startWp, goalWayPoint.Index);
                if (!_wayPointPath.ContainsKey(segment))
                {
                    PathData pathData = new PathData();
                    float distance = Vector3.Distance(GetWayPointData(startWp).Position, goalWayPoint.Position);
                    
                    pathData.MoveDurationMs = (long)Math.Round(((double)distance / Math.Max(0.01, (double)goalWayPoint.SpeedToReach)) * 1000.0);
                    pathData.WholeDurationMs = pathData.MoveDurationMs + (long)Math.Round((double)goalWayPoint.WaitTime * 1000.0);
                    
                    pathEndMs += pathData.WholeDurationMs;
                    pathData.PathEndMs = pathEndMs;
                    
                    _wayPointPath[segment] = pathData;
                    _pathSequence.Add(segment);

                    prevWp = startWp;
                    startWp = goalWayPoint.Index;
                }
                else break;
            }
            _mainPhaseDurationMs = _wayPointPath.Values.Sum(p => p.WholeDurationMs);
        }
        
        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            // * <동기화 시점 보정>
            // ** 직전 작동에서 완료된 SubPhase까지의 시간을 소거
            long prevActuationDuration = 0;
            int clampedSubPhase = Mathf.Clamp(DoneSubPhase, 0, _pathSequence.Count);
            for (int i = 0; i < clampedSubPhase; i++)
            {
                var key = _pathSequence[i];
                prevActuationDuration += _wayPointPath[key].WholeDurationMs;
            }
            ActuationStartTimeMs -= prevActuationDuration;

            try
            {
                while (CurrentState == GimmickPropState.Actuating)
                {
                    // * <시작 위치 구하기>
                    // ** elapsedTime 추출 & 메인 페이즈 소요시간 기준으로 나누기
                    //    ㄴ Auto 타입의 경우, (ServerServingTime == ActuationStartTimeMs)이므로 elapsedTime이 천문학적으로 커지기에
                    long elapsedTimeMs = Math.Max(0, ReNetworkUtility.NowServerTimeMs - ActuationStartTimeMs);
                    elapsedTimeMs %= _mainPhaseDurationMs;
                    
                    // ** 이동을 시작할 정거장 구하기 
                    int subPhaseIndex = GetStartSubPhase(elapsedMainPhaseMs: elapsedTimeMs);
                    
                    int startWayPoint = _pathSequence[subPhaseIndex].startWp;
                    int goalWayPoint = _pathSequence[subPhaseIndex].goalWp;
                    PathData currentPath = _wayPointPath[(startWayPoint, goalWayPoint)];
                    
                    // ** 해당 정거장의 진행도 구하기
                    long prevSubPhaseEndMs = (subPhaseIndex == 0) ? 0 : _wayPointPath[_pathSequence[subPhaseIndex - 1]].PathEndMs;
                    long elapsedSubPhaseTimeMs = elapsedTimeMs - prevSubPhaseEndMs;
                    
                    // * <정거장 이동 및 대기>
                    bool shouldMove = (elapsedSubPhaseTimeMs < currentPath.MoveDurationMs);
                    if (shouldMove)
                    {
                        // ** 이동 구간
                        double progress = CalculateProgress(elapsedSubPhaseTimeMs, currentPath.MoveDurationMs);

                        Vector3 startPos = _wayPoints[startWayPoint].Position;
                        Vector3 endPos = _wayPoints[goalWayPoint].Position;
                        
                        UpdatePosition = Vector3.Lerp(startPos, endPos, (float)progress);
                    }
                    else
                    {
                        // ** 대기 구간
                        UpdatePosition = _wayPoints[goalWayPoint].Position;
                        DoneSubPhase = subPhaseIndex + 1;

                        bool shouldDie = (_movementPattern == GimmickPropMovementPattern.OneWay && goalWayPoint == _wayPoints[^1].Index);
                        if (shouldDie)
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
                if (CurrentState != GimmickPropState.Dead)
                {
                    CurrentState = GimmickPropState.Ready;
                }
            }
        }
        
        private int GetStartSubPhase(long elapsedMainPhaseMs)
        {
            for (int i = 0; i < _pathSequence.Count; i++)
            {
                if (elapsedMainPhaseMs < _wayPointPath[_pathSequence[i]].PathEndMs)
                    return i;
            }
            return 0;
        }
        
        private double CalculateProgress(long elapsedMs, long moveDurationMs)
        {
            double elapsed = (double)elapsedMs;
            double total = (double)moveDurationMs;

            if (_useAcceleration && total >= _accelerationTimeMs * 2.0)
            {
                double accTime = (double)_accelerationTimeMs;
                double maxHeight = 1.0 / (total - accTime);

                if (elapsed < accTime)
                    return 0.5 * (elapsed * (maxHeight * elapsed / accTime));
                
                if (elapsed > total - accTime)
                {
                    double remainingTime = total - elapsed;
                    return 1.0 - (0.5 * (remainingTime * (remainingTime * maxHeight / accTime)));
                }

                return (0.5 * accTime * maxHeight) + ((elapsed - accTime) * maxHeight);
            }

            return Math.Clamp(elapsed / total, 0.0, 1.0);
        }
        
        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        [ContextMenu("Rebuild WayPoints")]
        private void RebuildWayPoints()
        {
            _wayPoints = _wayPoints.Where(wp => wp.WayPointTransform != null).ToList();
            for (int i = 0; i < _wayPoints.Count; i++) _wayPoints[i].Index = i;
        }
        
        
        private WayPointData GetNextStationIndexInternal(int prevIdx, int curIdx)
        {
            int nextIdx = -1;
            int count = _wayPoints.Count;

            switch (_movementPattern)
            {
                case GimmickPropMovementPattern.OneWay:
                    nextIdx = curIdx + 1;
                    if (nextIdx >= count) return null;
                    break;
                case GimmickPropMovementPattern.Loop:
                    nextIdx = (curIdx + 1) % count;
                    break;
                case GimmickPropMovementPattern.PingPong:
                    if (curIdx == 0) nextIdx = 1;
                    else if (curIdx == count - 1) nextIdx = curIdx - 1;
                    else nextIdx = (curIdx > prevIdx ? curIdx + 1 : curIdx - 1); 
                    break;
                default: return null;
            }
            return GetWayPointData(nextIdx);
        }

        private WayPointData GetWayPointData(int index)
            => (index >= 0 && index < _wayPoints.Count ? _wayPoints[index] : null);
        
        
        
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_wayPoints == null || _wayPoints.Count == 0) return;
            bool hasMissing = false;
            for (int i = 0; i < _wayPoints.Count; i++)
            {
                if (_wayPoints[i]?.WayPointTransform == null) { hasMissing = true; continue; }
                Vector3 curPos = _wayPoints[i].Position;
                Gizmos.color = hasMissing ? Color.gray : (i == 0 ? Color.cyan : Color.red);
                Gizmos.DrawSphere(curPos, 0.05f);
                if (i < _wayPoints.Count - 1)
                {
                    if (_wayPoints[i + 1]?.WayPointTransform != null)
                    {
                        Gizmos.color = hasMissing ? Color.red : Color.yellow;
                        Gizmos.DrawLine(curPos, _wayPoints[i + 1].Position);
                    }
                    else hasMissing = true;
                }
                else if (_movementPattern == GimmickPropMovementPattern.Loop && !hasMissing)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(curPos, _wayPoints[0].Position);
                }
            }
        }
#endif
    }
}