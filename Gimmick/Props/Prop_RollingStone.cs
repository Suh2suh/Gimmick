using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    /// <summary>
    /// 해당 기믹 컴포넌트를 SphereCollider에 부착 - 기믹 아래에 다른 SphereCollider 자식 콜라이더(damageZone)를 부착해야 캐릭터가 감지됩니다
    /// ※ 부모 콜라이더는 isTrigger = true로 설정해주세요
    /// ※ 자식 콜라이더는 isTrigger = false로 설정해주세요
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Prop_RollingStone : GimmickPropBase
    {
        [LineTitle("PROP_ ROLLING STONE")]
        [Tooltip("플레이어가 RollingStone에 접촉 시, RollingStone의 속도가 해당 속도 이상이라면 플레이어를 타격")] [SerializeField]
        private float _minSpeedToDamage = 5f;
        
        [Tooltip("플레이어 감지용 콜라이더의 크기를 현재 기믹 크기의 몇 배로 지정할 것인가?")][SerializeField, Range(1, 3)] 
        private float _damageZoneSizeRatio = 1.5f;
        
        [LineCustom(10, 10 ,true)]
        [Tooltip("RollingStone 기믹의 RigidBody")][SerializeField] 
        private Rigidbody _rollingStoneRigidbody;
        
        [Tooltip("기믹에 부착된 모델의 렌더러 | mesh bound를 기준으로 콜라이더 size를 설정")] [SerializeField][Space(5)]
        private MeshRenderer _modelMeshRenderer;
        
        [Tooltip("기믹 rigidbody 콜라이더")] [SerializeField]
        private SphereCollider _rigidbodyCollider;
        
        [Tooltip("RollingStone 기믹의 자식 오브젝트에 부착된 플레이어 감지용 콜라이더: 자식 콜라이더는 isTrigger = true로 설정되어야 함")][SerializeField]
        private SphereCollider _damageZoneCollider;
        
        private float _currentSpeed = 0f;  // For Debug
        
        
        // ==================================================
        // [LOGIC] Initialization
        // ==================================================
        protected override void AwakeProp()
        {
            if (_rollingStoneRigidbody == null)
                _rollingStoneRigidbody = GetComponent<Rigidbody>();

            float colliderRadius = GetColliderRadius();
            _rigidbodyCollider.radius = colliderRadius;
            _damageZoneCollider.radius = colliderRadius * _damageZoneSizeRatio;

            _rigidbodyCollider.isTrigger = false;
            _damageZoneCollider.isTrigger = true;

            // Trigger 전에는 공이 떨어지지 않도록 방지
            _rollingStoneRigidbody.isKinematic = true;
        }

        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        protected override async UniTask Actuate()
        {
            // TODO: 논의 필요
            //       ㄴ 구르는 돌 같은 경우, 언제 작동이 끝났다고 해야할지 모호함
            //           정해지기 전까지는, 기믹 작동 여부가 계속 보이는 GimmickTrigger_Interaction은 사용하면 안 됨
            
            // 공 떨어뜨리기 시작
            _rollingStoneRigidbody.isKinematic = false;
            
            // 대기
            // ㄴ ?: 시간/ 일정 속도로 떨어질 때까지/ Despawn되기 전 등...
        }

        private void OnTriggerEnter(Collider other)
        {
#if UNITY_EDITOR
            if (IsDebugMode)
                LogUtil.Log($"[Gimmick_RollingStone] - {gameObject.name} | OnTriggerEnter - {other.gameObject.name}".Color(Color.yellow));
#endif
            // 플레이어 피격 조건: RollingStone에 접촉 & RollingStone의 속도가 일정치 이상일 때 
            if (other.gameObject.layer == Layer.LAYER_PLAYER 
                && _rollingStoneRigidbody.linearVelocity.magnitude > _minSpeedToDamage)
            {
#if UNITY_EDITOR
                if (IsDebugMode) 
                    LogUtil.Log($"[Gimmick_RollingStone] - {gameObject.name} | Attack Player! - {other.gameObject.name}".Color(Color.red));
#endif
                
                // @suhlee [임시 코드] 플레이어가 RollingStone에 피격 시, 리스폰 
                LocalCharacter.Instance.Motor
                    .SetPositionAndRotation(GimmickTestSceneController.Instance ? GimmickTestSceneController.Instance.RespawnPoint.position : Vector3.zero,
                                            Quaternion.identity);
            }
        }
        
        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        private float GetColliderRadius()
        {
            var meshExtents = _modelMeshRenderer.bounds.extents;
            return Mathf.Max(meshExtents.x, meshExtents.y, meshExtents.z);  // bounds의 가장 긴 선분을 반환
        }

        
        
        
#if UNITY_EDITOR
        #region DEBUG
        private void Update()
        {
            if (_rollingStoneRigidbody == null)
                return;
            _currentSpeed = _rollingStoneRigidbody.linearVelocity.magnitude;
        }
        
        #endregion

        #region Gizemo Drawing

        private void OnDrawGizmos()
        {
            if (_modelMeshRenderer == null)
                return;
            
            float colliderRadius = GetColliderRadius();
            if (_rigidbodyCollider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_rigidbodyCollider.transform.position, colliderRadius);
            }
            if (_damageZoneCollider != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_damageZoneCollider.transform.position, colliderRadius * _damageZoneSizeRatio);
            }
        }

        #endregion
        
#endif
    }
}
