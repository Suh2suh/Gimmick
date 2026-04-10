using KinematicCharacterController;
using UnityEngine;

namespace REIW
{
    /// <summary>
    /// 해당 컴포넌트를 오브젝트에 부착 시,
    /// 캐릭터가 오브젝트 움직임(Kinematic)에 따라 함께 이동/회전합니다
    /// </summary>
    [RequireComponent(typeof(PhysicsMover))]
    public abstract class GimmickPlatformBase : GimmickPropBase, IMoverController
    {
        /// <summary> 플랫폼 이동 시, 실질적으로 조작하는 포지션 (해당 포지션으로 플랫폼이 이동함) </summary>
        protected Vector3 UpdatePosition { get; set; }
        
        /// <summary> 플랫폼 회전 시, 실질적으로 조작하는 쿼터니언 (해당 쿼터니언대로 플랫폼이 회전함) </summary>
        protected Quaternion UpdateRotation { get; set; }
        
        /// <summary> 플랫폼 이동 플래그 | True: UpdatePosition으로 이동 / False: Object의 현재 위치 유지 </summary>
        protected virtual bool UseUpdatePosition { get; set; } = true;
        
        /// <summary> 플랫폼 회전 플래그 | True: UpdateRotation으로 회전 / False: Object의 현재 회전 유지 </summary>
        protected virtual bool UseUpdateRotation { get; set; } = true;
        
        [LineTitle("PLATFORM BASE")]
        [SerializeField] protected PhysicsMover _physicsMover;
        /// <summary> 플랫폼의 충돌 콜라이더 - isTrigger = false </summary>
        [SerializeField] protected Collider _platformCollider;
        /// <summary> 플랫폼의 캐릭터 탐지 콜라이더 - isTrigger = true </summary>
        [SerializeField] protected BoxCollider _playerDetectCollider;
        [SerializeField] protected MeshRenderer _platformModelRenderer;
        
        
        protected sealed override void AwakeProp()
        {
            // * PhysicsMover 초기화
            if (_physicsMover == null)
            {
                _physicsMover = GetComponent<PhysicsMover>();
            }
            _physicsMover.MoverController = this;
            UpdatePosition = transform.position;
            UpdateRotation = transform.rotation;

            // * Collider Size 설정: 기믹 모델 기준
            if (_platformCollider is BoxCollider boxCollider)
                boxCollider.SetSizeAsMesh(_platformModelRenderer);
            else if (_platformCollider is SphereCollider sphereCollider)
                sphereCollider.SetSizeAsMesh(_platformModelRenderer);
            
            if (_playerDetectCollider != null)  // 플레이어가 올라설 수 없는 경우 (움직이는 벽) => collider 설정하지 않음
            {
                _playerDetectCollider.SetSizeAsMesh(_platformModelRenderer, 1f, 2f, 1f);
                _playerDetectCollider.center = new Vector3(0, 2, 0);
            }

            AwakePlatform();
        }
        protected virtual void AwakePlatform() {}
        

        #region @suhlee [임시 코드]: 플랫폼 위에 서있을 시 Damper 변경 (Detect 방식 변경하든지... 필요)
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                LocalCharacter.Instance.CharacterAnimation.Movement.SetMovingPlatformStateGrounderIK(true);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                LocalCharacter.Instance.CharacterAnimation.Movement.SetMovingPlatformStateGrounderIK(true);
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                LocalCharacter.Instance.CharacterAnimation.Movement.SetMovingPlatformStateGrounderIK(false);
            }
        }

        #endregion

        
        /// <summary>
        /// Physics Mover에서 계산되는 Update() Function
        /// </summary>
        public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            BeforeUpdateMovement();

            // 이동 불가일 시, 이동 정지
            goalPosition = UseUpdatePosition ? UpdatePosition : transform.position;
            // 회전 불가일 시, 회전 정지
            goalRotation = UseUpdateRotation ? UpdateRotation : transform.rotation;

            AfterUpdateMovement();
        }

        protected virtual void BeforeUpdateMovement() {}
        protected virtual void AfterUpdateMovement() {}
    }
}
