using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Platform_Tilt : GimmickPlatformBase
    {
        protected override bool UseUpdatePosition => false;
        protected override bool UseUpdateRotation => true;
        
        private enum TiltType
        {
            TiltingTrap,
            TiltingSeesaw
        }
        
        
        [LineTitle("PLATFORM_TILT")]
        [Tooltip("TiltingTrap: 기울였다가 복귀, TiltingSeesaw: Tilt 세팅 기준으로, 대칭 반복")][SerializeField][Space(5)]
        private TiltType _tiltType = TiltType.TiltingTrap;
        
        [Tooltip("어떤 축을 기준으로 기울일 것인가?: 해당 오브젝트의 anchor을 원점으로 축을 설정" +
                 " - ※ anchor을 다르게 하고 싶을 시, 상위 부모 오브젝트에 해당 컴포넌트 부착 필요")] [SerializeField]
        private Vector3 _tiltingAxis;
        
        [Tooltip("어떤 방향으로 기울일 것인가?: Seesaw의 경우, 해당 방향을 시작으로 시계/반시계 방향으로 반복")][SerializeField]
        private bool _isClockwise = true;
        
        [Tooltip("어떤 각도까지 기울일 것인가?")] [SerializeField][Range(1, 360)] 
        private float _tiltingAngle;
        
        [Tooltip("기울이는 속도: Lerp 속도")] [SerializeField][Space(5)]
        private float _tiltingSpeed = 10f;
        
        private Quaternion _initialRotation;
        private Quaternion _goalRotation1;
        
        
        [LineSubtitle("- Tilting Trap Option")] 
        [Tooltip("회전 횟수")] [SerializeField]
        private int _tiltCount = 1;
        [Tooltip("목표 회전 각도 도달 후의 대기시간")] [SerializeField] 
        private int _stayTime;
        
        private Quaternion _goalRotation2;
        
        
        // ==================================================
        // [LOGIC] Initialization
        // ==================================================
        protected override void AwakePlatform()
        {
            _initialRotation = transform.rotation;
            Quaternion clockwiseQuaternion = _initialRotation * Quaternion.AngleAxis(_tiltingAngle, _tiltingAxis);
            Quaternion counterClockwiseQuaternion = _initialRotation * Quaternion.AngleAxis(-_tiltingAngle, _tiltingAxis);
            _goalRotation1 = _isClockwise ? clockwiseQuaternion : counterClockwiseQuaternion;
            _goalRotation2 = _isClockwise ? counterClockwiseQuaternion : clockwiseQuaternion;
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            if (_tiltType == TiltType.TiltingTrap)
            {
                if (_tiltCount == 0) 
                    LogUtil.Log($"[Gimmick_Tilt] 기울이기 횟수가 0회입니다. 인스펙터 확인 필요 (instanceID: {gameObject.GetInstanceID()}).".Color(Color.yellow));
                
                for (int i = 0; i < _tiltCount; i++)
                {
                    await TiltToAsync(_initialRotation, _goalRotation1, _tiltingSpeed);
                    await UniTask.WaitForSeconds(_stayTime, ignoreTimeScale: false, PlayerLoopTiming.Update, destroyCancellationToken);
                    await TiltToAsync(transform.rotation, _initialRotation, _tiltingSpeed);
                    await UniTask.WaitForSeconds(_stayTime, ignoreTimeScale: false, PlayerLoopTiming.Update, destroyCancellationToken);
                    
                    if (IsCancelReserved)
                        break;
                }
            }
            else
            {
                // 대칭 반복 이동
                while (gameObject?.GetCancellationTokenOnDestroy().IsCancellationRequested == false)
                {
                    await TiltToAsync(_initialRotation, _goalRotation1, _tiltingSpeed); 
                    await UniTask.WaitForSeconds(_stayTime, ignoreTimeScale: false, PlayerLoopTiming.Update, destroyCancellationToken);
                    await TiltToAsync(_goalRotation1, _goalRotation2, _tiltingSpeed);
                    await UniTask.WaitForSeconds(_stayTime, ignoreTimeScale: false, PlayerLoopTiming.Update, destroyCancellationToken);
                    await TiltToAsync(_goalRotation2, _initialRotation, _tiltingSpeed);
                    
                    if (IsCancelReserved)
                        break;
                }
            }

            CurrentState = GimmickPropState.Ready;
        }

        private async UniTask TiltToAsync(Quaternion from, Quaternion to, float speed)
        {
            UpdateRotation = from;
            
            float slerpT = 0f;
            float totalAngle = Quaternion.Angle(from, to);
            while (!Mathf.Approximately(Quaternion.Angle(UpdateRotation, to), 0f))
            {
                slerpT += Time.deltaTime * (speed / totalAngle);
                UpdateRotation = Quaternion.Slerp(from, to, slerpT);
                
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }

            UpdateRotation = to;
        }
        
        
        
        
#if UNITY_EDITOR
        #region Gizmo Drawing
        
        private static readonly Color arcColorFirst = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.3f);
        private static readonly Color arcColorSecond = new Color(Color.green.r, Color.green.g, Color.green.b, 0.3f);
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && destroyCancellationToken.IsCancellationRequested) 
                return;

            // 1. 월드 좌표 및 월드 회전축 설정
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Vector3 center = transform.position;
            Vector3 worldAxis = transform.TransformDirection(_tiltingAxis.normalized);
            
            // 2. [수정] 회전축과 시작 평면 벡터가 평행한지 체크
            // 기본 시작 방향은 로컬 Right이지만, 축이 Right라면 Forward를 대신 사용합니다.
            Vector3 localBaseVec = Vector3.right;
            if (Mathf.Abs(Vector3.Dot(_tiltingAxis.normalized, localBaseVec)) > 0.9f)
            {
                localBaseVec = Vector3.forward;
            }

            // 3. 축에 수직인 시작 방향(World) 계산
            Vector3 worldBaseVec = transform.TransformDirection(localBaseVec);
            Vector3 orthoStartDir = Vector3.Cross(worldAxis, Vector3.Cross(worldBaseVec, worldAxis)).normalized;

            // 회전축
            Gizmos.color = Color.white;
            UnityEditor.Handles.DrawLine(center, center + worldAxis * 2f); // 회전축
            UnityEditor.Handles.DrawLine(center, center + worldAxis * -2f); // 회전축
            
            float handleSize = 2.0f;
            float firstAngle = _isClockwise ? _tiltingAngle : -_tiltingAngle;
            UnityEditor.Handles.color = arcColorFirst;
            UnityEditor.Handles.DrawSolidArc(center, worldAxis, orthoStartDir, firstAngle, handleSize);
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawWireArc(center, worldAxis, orthoStartDir, firstAngle, handleSize);
            if (_tiltType == TiltType.TiltingSeesaw)
            {
                UnityEditor.Handles.color = arcColorSecond;
                UnityEditor.Handles.DrawSolidArc(center, worldAxis, orthoStartDir, -firstAngle, handleSize);
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.DrawWireArc(center, worldAxis, orthoStartDir, -firstAngle, handleSize);
            }

            // 기준선 표시
            Gizmos.color = Color.red;
            Gizmos.DrawRay(center, orthoStartDir * handleSize);
        }
        #endregion
        
#endif
    }
}
