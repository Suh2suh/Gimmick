using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Prop_Watermill : GimmickPropBase
    {
        [LineTitle("PROP_ WATERMILL")]
        [Tooltip("WatermillType | Once: Trigger 시, 설정값만큼 1회 회전 / Loop: 설정값만큼 회전 => 대기 => 회전 => 대기...")]
        [SerializeField]
        private GimmickPropRepeatMode _repeatMode = GimmickPropRepeatMode.OneShot; 
        
        [Tooltip("회전축 | Local Space")]
        [SerializeField] private Vector3 _rotationAxis = Vector3.right;
        
        [Tooltip("1회 회전 각도")]
        [SerializeField] private float _rotationAngle = 90f;
        
        [Tooltip("회전 속도")]
        [SerializeField] private float _rotationSpeed = 20f;
        
        [Tooltip("프랍 모델 렌더러")][SerializeField]
        private MeshRenderer _modelRenderer;
        
        [Tooltip("프랍 콜라이더")][SerializeField] 
        private Collider _watermillCollider;
        
        [LineSubtitle("- GimmickPropRepeatMode - Loop Option")]
        [Tooltip("1회 회전 후 대기시간 (Loop 타입일 경우)")]
        [SerializeField] private float _stayTime = 1f;
        
        
        // ==================================================
        // [LOGIC] Life Cycle
        // ==================================================
        protected override void AwakeProp()
        {
            if (_modelRenderer && _watermillCollider)
            {
                if (_watermillCollider is BoxCollider boxCollider)
                    boxCollider.SetSizeAsMesh(_modelRenderer);
                else if (_watermillCollider is SphereCollider sphereCollider)
                    sphereCollider.SetSizeAsMesh(_modelRenderer);
            }
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            while (!IsCancelReserved)
            {
                Quaternion to = gameObject.transform.rotation * Quaternion.AngleAxis(_rotationAngle, _rotationAxis);
                await RotateAsync(to, _rotationSpeed);
                
                await UniTask.WaitForSeconds(_stayTime, ignoreTimeScale: false, PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
                
                if (_repeatMode == GimmickPropRepeatMode.OneShot)
                    break;
            }

            CurrentState = GimmickPropState.Ready;
        }

        private async UniTask RotateAsync(Quaternion to, float speed)
        {
            Quaternion from = transform.rotation;
            
            float slerpT = 0f;
            float totalAngle = Quaternion.Angle(transform.rotation, to);
            while (!Mathf.Approximately(Quaternion.Angle(transform.rotation, to), 0f))
            {
                slerpT += Time.deltaTime * (speed / totalAngle);
                transform.rotation = Quaternion.Slerp(from, to, slerpT);
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }
            
            transform.rotation = to;
        }

        
        
        
#if UNITY_EDITOR
        #region Gizmo Drawing
        private static readonly Color arcColor = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.3f);
        private void OnDrawGizmos()
        {
            // 1. 월드 좌표 및 회전축 설정
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Vector3 center = transform.position;
            Vector3 worldAxis = transform.TransformDirection(_rotationAxis.normalized);
            
            // 2. 현재 오브젝트의 로컬 평면(Right/Forward) 중 축과 겹치지 않는 기준점 찾기
            Vector3 localBaseVec = Vector3.right;
            if (Mathf.Abs(Vector3.Dot(_rotationAxis.normalized, localBaseVec)) > 0.9f)
            {
                localBaseVec = Vector3.forward;
            }

            // 3. 현재 오브젝트의 실제 회전 상태가 반영된 '0도' 시작 방향
            Vector3 worldBaseVec = transform.TransformDirection(localBaseVec);
            Vector3 orthoStartDir = Vector3.Cross(worldAxis, Vector3.Cross(worldBaseVec, worldAxis)).normalized;

            // 4. 시각화 설정
            float radius = 1.5f; 
            
            // 5. 회전축 (흰색 라인)
            Gizmos.color = Color.white;
            UnityEditor.Handles.DrawLine(center - worldAxis * 1.5f, center + worldAxis * 1.5f);

            // 6. "다음 회전 범위"만 표시 (Next Step Arc)
            // 현재 transform.rotation을 기준으로 _rotationAngle만큼만 부채꼴을 그림
            UnityEditor.Handles.color = arcColor;
            UnityEditor.Handles.DrawSolidArc(center, worldAxis, orthoStartDir, _rotationAngle, radius);
            
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawWireArc(center, worldAxis, orthoStartDir, _rotationAngle, radius);

            // 7. 현재 정면/평면 기준선 (빨간색)
            Gizmos.color = Color.red;
            Gizmos.DrawRay(center, orthoStartDir * (radius * 1.2f));

            // 8. 목표 지점(끝점) 표시
            Vector3 endDir = Quaternion.AngleAxis(_rotationAngle, worldAxis) * orthoStartDir;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(center, endDir * radius);
        }

        #endregion
        
#endif
    }
}
