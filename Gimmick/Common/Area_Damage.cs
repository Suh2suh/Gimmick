using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    /// <summary>
    /// enable-disable, 위치 이동 등 콜라이더 정보가 변하는 경우,
    /// 본 오브젝트 혹은 부모에 kinematic Rigidbody 부착 필요 (충돌트리 통째로 재계산 방지)
    /// </summary>
    public class Area_Damage : MonoBehaviour
    {
        [LineTitle("AREA_ DAMAGE")]
        [Tooltip("데미지 반경")] [SerializeField]
        float _damageRadius;
        
        [Tooltip("플레이어에게 줄 데미지")] [SerializeField][Space(5)]
        private float _damage = 5f;
        [Tooltip("데미지 간격")] [SerializeField] 
        private float _damageIntervalSec = 0.1f;

        [LineCustom(10, 10, true)]
        [Tooltip("영역 콜라이더")] [SerializeField][Space(5)] 
        private Collider _areaCollider;
        
        private bool _isPlayerInArea = false;
        private bool _isDamageIntervalRunning = false;
        
        private CancellationTokenSource _objectDisableTokenSource;

        
        // ==================================================
        // [LOGIC] Life Cycle
        // ==================================================
        private void Awake()
        {
            _damage = Mathf.Clamp(_damage, 0f, _damage);
        }

        private void OnEnable()
        {
            _objectDisableTokenSource?.Dispose();
            _objectDisableTokenSource = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            _isPlayerInArea = false;
            
            _objectDisableTokenSource?.Cancel();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                _isPlayerInArea = true;

                if (!_isDamageIntervalRunning)
                {
                    _isDamageIntervalRunning = true;
                    
                    DamagePlayerInterval().Forget();       
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                _isPlayerInArea = false;
            }
        }
        

        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        private async UniTask DamagePlayerInterval()
        {
            try
            {
                bool CanDamagePlayer() => !_objectDisableTokenSource.IsCancellationRequested && _areaCollider.enabled;
                while (CanDamagePlayer() && _isPlayerInArea)
                {
                    DamagePlayer();

                    await UniTask.Delay(TimeSpan.FromSeconds(_damageIntervalSec),
                        DelayType.Realtime, PlayerLoopTiming.Update, _objectDisableTokenSource.Token);
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                _isDamageIntervalRunning = false;
            }
        }

        private void DamagePlayer()
        {
            // Local만 판정하면 된다
            LogUtil.Log($"Damage [{_damage}] to Local Player!".Color(Color.red));
        }
        
        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        public void SetActive(bool isActive)
        {
            this._areaCollider.enabled = isActive;
            
            this.enabled = isActive;
        }
        
        public void SetDamageAreaRadius(float radius)
        {
            _damageRadius = radius;
            
            // < @suhlee 임시 반경 조절
            // TODO: 추후에 폭발 영역에 렌더러 빠질 경우(= 애니로 들어올 경우) 에는 Collider Radius 및 애니메이션 반경만 조절할 것
            //       애니 어떻게 들어오나 보고... 데미지 존 스케일을 늘릴지 아님 스케일 두고 collider Radius만 늘릴지 생각해보기
            _areaCollider.transform.localScale = new Vector3(_damageRadius, _damageRadius, _damageRadius);
            // >
        }
    }
}
