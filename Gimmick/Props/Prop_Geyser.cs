using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    [RequireComponent(typeof(BoxCollider))]
    public class Prop_Geyser : GimmickPropBase
    {
        [LineTitle("PROP_ GEYSER")]
        [Tooltip("간헐천 콜라이더 비율 | 간헐천 내에서 트램펄린처럼 뛰어오르기 시작하는 지점")] [SerializeField, Range(0.5f, 1f)]
        private float _geyserColliderRatio = 0.8f;
        
        [Tooltip("간헐천 모델 상승 속도")] [SerializeField]
        private float _geyserModelAnimSpeed = 0.2f;

        [Tooltip("간헐천 진입 시 플레이어 상승 속도")] [SerializeField]
        private float _geyserMoveSpeed = 20f;
        
        [LineCustom(10, 10, isDashed: true)]
        [Tooltip("간헐천 Trigger Collider | 기믹의 BoxCollider(Trigger) 삽입")] [SerializeField]
        private BoxCollider _geyserCollider;
        
        [Tooltip("간헐천 모델 Rendererer")] [SerializeField]
        private MeshRenderer _geyserModelRenderer;
        
        [Tooltip("간헐천 끝 지점 | 간헐천이 끝나는 지점")] [SerializeField]
        private Transform _geyserEndPoint;
        
        [Tooltip("간헐천 모델 | 자식 계층의 'Model' 삽입 - 하위의 간헐천 메쉬는 Bottom Pivot으로 잡혀있어야 합니다")] [SerializeField]
        private Transform _geyserModelGroup;
        
        [Tooltip("간헐천 모델 Top Pivot | 자식 계층의 'Model - Top Pivot' 삽입")] [SerializeField]
        private Transform _geyserModelTopPivot;
        
        
        // ==================================================
        // [LOGIC] LIFE CYCLE
        // ==================================================        
        protected override void AwakeProp()
        {
            _geyserCollider.SetSizeAsMesh(_geyserModelRenderer, 0.95f, 1f, 0.95f);

            float localTotalHeight = transform.InverseTransformPoint(_geyserEndPoint.position).y;
            float colliderYSize = localTotalHeight * _geyserColliderRatio;
            float colliderCenterY = colliderYSize / 2f;
            
            _geyserCollider.center = new Vector3(0, colliderCenterY, 0);
            _geyserCollider.size = new Vector3(_geyserCollider.size.x, colliderYSize, _geyserCollider.size.z);
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            _geyserCollider.enabled = true;

            Vector3 plusScale = new Vector3(0, 1, 0) * _geyserModelAnimSpeed;
            while (_geyserModelTopPivot.position.y < _geyserEndPoint.position.y)
            {
                if (IsCancelReserved) 
                    return;
                
                _geyserModelGroup.localScale += plusScale;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }
            
            // 취소하지 않는 한, 계속 작동됨
        }

        public override async UniTask RequestCancel()
        {
            IsCancelReserved = true;
            
            _geyserCollider.enabled = false;
            
            Vector3 plusScale = new Vector3(0, 1, 0) * _geyserModelAnimSpeed;
            while (_geyserModelGroup.localScale.y - plusScale.y > 1f)
            {
                _geyserModelGroup.localScale -= plusScale;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }

            CurrentState = GimmickPropState.Ready;
        }

        
        private void OnTriggerEnter(Collider other)
        {
            if (CurrentState == GimmickPropState.Actuating && other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                if (LocalCharacter.Instance.CharacterAnimation.Movement.IsGrounded)
                {
                    LocalCharacter.Instance.SetPositionAndRotation(
                        LocalCharacter.Transform.position + new Vector3(0, 0.75f, 0), 
                        LocalCharacter.Transform.rotation
                    );
                }

                LocalCharacter.Instance.SetGravity(LocalCharacter.Instance.Gravity / 2);
                LocalCharacter.Instance.StartJump(false);
                LocalCharacter.Instance.AddVelocity(-LocalCharacter.Instance.Gravity.normalized * _geyserMoveSpeed);
                
                // Debug.Log("Geyser Enter".Color(Color.red));
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                if (LocalCharacter.Transform.position.y <= _geyserEndPoint.transform.position.y)
                {
                    LocalCharacter.Instance.AddVelocity(-LocalCharacter.Instance.Gravity.normalized * _geyserMoveSpeed * Time.fixedDeltaTime);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                LocalCharacter.Instance.SetGravity(LocalCharacter.Instance.Gravity);
                
                // Debug.Log("Geyser Exit".Color(Color.cyan));
            }
        }
    }
}
