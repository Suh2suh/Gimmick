using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    /// <summary>
    /// @suhlee TODO: 점프량 계산식(GetJumpHeightMultifier)은 임시이므로, 기획 측에서 전달해주어야함
    ///               ㄴ 점프 레벨 별로 보정값은 수식으로 들어올 예정...
    /// </summary>
    public class Prop_Trampoline : GimmickPropBase
    {
        [LineTitle("PROP_ TRAMPOLINE")]
        [Tooltip("점프 강도 | 0: 일반 점프와 동일")][SerializeField] 
        private int _jumpLevel;
        
        [Tooltip("점프 배수 (수식 오기 전 임시) | Level이 1단계 높아질수록 0.2씩 증가")][SerializeField]
        private float _jumpHeightStep = 0.2f;
        
        [LineCustom(10, 10, true)]
        [Tooltip("Trampoline의 플레이어 인식용 Collider |  MeshRenderer의 상단 표면에 TrampolineSurface의 center을 자동 부착 (얹는다는 느낌)")][SerializeField]
        private BoxCollider _trampolineSurface;
        
        [Tooltip("Trampoline Mesh의 Renderer")][SerializeField] 
        private MeshRenderer _trampolineMeshRenderer;


        // ==================================================
        // [LOGIC] LIFE CYCLE
        // ==================================================        
        protected override void AwakeProp()
        {
            // Collider (_trampolineSurface) 세팅: size, center
            _trampolineSurface.SetSizeAsMesh(_trampolineMeshRenderer, _trampolineSurface.size.x * 0.95f, 0.1f, _trampolineSurface.size.z * 0.95f);
            var meshWorldPos = _trampolineMeshRenderer.transform.TransformPoint(_trampolineMeshRenderer.localBounds.center.x, 
                                                                                        _trampolineMeshRenderer.localBounds.max.y,
                                                                                        _trampolineMeshRenderer.localBounds.center.z);
            var centerPos = _trampolineSurface.transform.InverseTransformPoint(meshWorldPos);
            _trampolineSurface.center = new Vector3(0, centerPos.y, 0f);  // mesh의 상단 표면에 얹음
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================        
        protected override async UniTask Actuate()
        {
            _trampolineSurface.enabled = true;
        }

        public override async UniTask RequestCancel()
        {
            IsCancelReserved = true;
            
            _trampolineSurface.enabled = false;
            
            CurrentState = GimmickPropState.Ready;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (CurrentState == GimmickPropState.Actuating && other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                LocalCharacter.Instance.StartJump(false);
                LocalCharacter.Instance.AddVelocity(transform.up * GetJumpHeightMultifier(_jumpLevel));
            }
        }

        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================        
        /// <summary> 임시 계산식 </summary>
        private float GetJumpHeightMultifier(int jumpLevel)
        {
            return (1 + jumpLevel * _jumpHeightStep);
        }
        
    }
}
