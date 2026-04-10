using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class PropSub_FountainLid : GimmickPlatformBase
    {
        protected override bool UseUpdatePosition => true;
        protected override bool UseUpdateRotation => true;

        [SerializeField, ReadOnly]
        private bool _isLavaOnTop = false;

        private Vector3 _lidGoalPosition; // lava의 topPivot
        private Vector3 _lavaTopPosition;
        
        private bool _isLidJumping = false;
        private bool _isAfterJumping = false;
        
        private Quaternion _baseRotation;

        private bool _isPlayerOnLid;

        
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                _isPlayerOnLid = true;
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);
            
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                _isPlayerOnLid = false;
            }
        }

        public void SetLavaTopPivot(Vector3 lavaTopPivot)
        {
            // 뚜껑의 중점이 용암의 TopPivot을 따라가기 때문에, 용암 모델이 뚜껑 위로 벗어나지 않도록
            // 용암 TopPivot의 위쪽을 추적한다
            _lidGoalPosition = lavaTopPivot + (Vector3.up * 0.5f);  

            // if (IsDebugMode)
            // {
                // Debug.Log($"[PropSub_FountainLid] Lava's TopPivot Current: {lavaTopPivot}".Color(Color.yellow));
            // }
        }
        
        
        public void NotiLidOnTop()
        {
            _isLavaOnTop = true;
            _lavaTopPosition = _lidGoalPosition;
            
            _isAfterJumping = false;
        }

        protected override void BeforeUpdateMovement()
        {
            if (_isLidJumping)
                return;
            

            // 점프가 끝난 후, 용암이 최상단에 있고 안착한 상태라면 회전
            bool isGroundedAtTop = _isAfterJumping && Vector3.Distance(_lidGoalPosition, _lavaTopPosition) < 0.05f;
            if (isGroundedAtTop)
            {
                // 앞뒤좌우로 흔들리는 연출
                float speed = 5f;
                float amount = 5f;
        
                float rotX = Mathf.Sin(Time.time * speed) * amount;
                float rotZ = Mathf.Cos(Time.time * speed * 1.1f) * amount;
                float rotY = Mathf.Sin(Time.time * speed * 0.7f) * (amount * 0.5f);

                UpdateRotation = Quaternion.Euler(rotX, rotY, rotZ);
                UpdatePosition = _lidGoalPosition;
            }
            else if (_isLavaOnTop && Vector3.Distance(transform.position, _lidGoalPosition) < 0.1f)
            {
                StartJumpLid().Forget();
                _isLavaOnTop = false; 
            }
            else
            {
                // 평상시: 용암을 추적하며, 회전이 있으면 0,0,0으로 천천히 복구됨 
                UpdatePosition = _lidGoalPosition;
                
                if (UpdateRotation != Quaternion.identity)
                {
                    UpdateRotation = Quaternion.RotateTowards(
                        UpdateRotation, 
                        Quaternion.identity, 
                        180f * Time.deltaTime
                    );
                }
            }
        }
        
        private async UniTask StartJumpLid()
        {
            _isLidJumping = true;
    
            float duration = 0.6f; 
            float peakHeight = 5f;
            // 점프 시작 시점의 용암 높이 = 상승 궤적의 기준점
            float startY = _lidGoalPosition.y;

            bool isPlayerJumed = false;
    
            float sec = 0f;
            while (sec < duration && !destroyCancellationToken.IsCancellationRequested)
            {
                sec += Time.deltaTime;
                float t = sec / duration;

                float jumpOffset = Mathf.Sin(t * Mathf.PI) * peakHeight;  // 가속도가 포함된 포물선 오프셋 (0 -> 1 -> 0)
                float currentY = startY + jumpOffset;
        
                // 실시간 용암 높이 (바닥 체크용)
                float currentLavaY = _lidGoalPosition.y;
                float finalY = 0f;

                if (t > 0.5f) // 하강: 용암 안착 여부를 판단
                {
                    // 궤적상 높이가 용암보다 낮아졌다면 용암에 닿은 것으로 간주
                    if (currentY <= currentLavaY)
                    {
                        finalY = currentLavaY;
                        UpdatePosition = new Vector3(UpdatePosition.x, finalY, UpdatePosition.z);
                        break; // 안착했으므로 루프 종료
                    }
                    else
                    {
                        // 아직 공중에 떠 있다면 궤적을 따라 내려감
                        finalY = currentY;
                    }
                }
                else
                {

                    if (!isPlayerJumed)
                    {
                        if (IsDebugMode)
                        {
                            Debug.Log($"[PropSub_FountainLid] PlayerOnLid: {_isPlayerOnLid} | IsPlayerGrounded?: {LocalCharacter.Instance.CharacterAnimation.Movement.IsGrounded}".Color(Color.yellow));
                        }
                    
                        if (_isPlayerOnLid && LocalCharacter.Instance.CharacterAnimation.Movement.IsGrounded)
                        {
                            LocalCharacter.Instance.StartJump(directly: true);
                            LocalCharacter.Instance.AddVelocity(Vector3.up * 15);
                        }
                        isPlayerJumed = true;       
                    }
                    
                    // 상승 중에는 용암이 내려가든 올라오든 무시하고 궤적 고수
                    // 단, 용암이 뚜껑을 치고 올라오는 비정상 상황 방지를 위해 최소 높이는 보장
                    finalY = Mathf.Max(currentY, currentLavaY);
                }

                UpdatePosition = new Vector3(UpdatePosition.x, finalY, UpdatePosition.z);
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }
    
            // 후처리
            UpdatePosition = _lidGoalPosition;
            _isLidJumping = false;
            _isAfterJumping = true;
        }
    }
}
