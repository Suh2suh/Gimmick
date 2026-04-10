using UnityEngine;

namespace REIW
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class GimmickProjectileBase : MonoBehaviour
    {
        public enum PathType
        {
            Linear,
            Parabolic  // 이 경우, linearDirection에 맞춰서 포물선으로...
        }
        
        
        [LineTitle("PROJECTILE BASE")]
        [Tooltip("발사 경로 타입")] [SerializeField]
        protected PathType _pathType;
        
        [Tooltip("발사 속도")] [SerializeField]
        protected float _speed = 10f;
        
        [Space(5)]
        [Tooltip("부여 데미지")] [SerializeField] 
        protected float _damage = 10f;
        
        [Tooltip("데미지 속성")] [SerializeField]
        protected int _damageType = 0;  // 속성
        
        [Tooltip("데미지 반경 (발사체 콜라이더 크기 조절용)")] [SerializeField]
        protected float _damageRadius = 5f;
        
        [LineCustom(10, 10, isDashed: true)]
        [Tooltip("발사체 Rigidbody")] [SerializeField]
        protected Rigidbody _rigidbody;
        [Tooltip("발사체 콜라이다")] [SerializeField] 
        protected SphereCollider _collider;


        // ==================================================
        // [LOGIC] LIFE CYCLE
        // ==================================================
        private void Awake()
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = (_pathType == PathType.Parabolic);
            
            _collider.radius = _damageRadius;
        }
        
        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        public void Fire(Vector3 normalizedDirectionGlobal)
        {
            gameObject.transform.LookAt(this.transform.position + normalizedDirectionGlobal);
            _rigidbody.isKinematic = false;

            if (_pathType == PathType.Linear)
            {
                _rigidbody.AddForce(normalizedDirectionGlobal * _speed);
            }
            else
            {
                _rigidbody.AddForce(normalizedDirectionGlobal * _speed + Vector3.up * 0.5f * _speed);
            }
        }

        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER ||
                other.gameObject.layer == Layer.LAYER_GROUND) // 임시: 발사체 및 발사대 외 콜라이더만 충돌처리
            {
                OnHit(other.gameObject.layer == Layer.LAYER_PLAYER);
            }
        }
        
        /// <summary>
        /// 발사체가 어떤 물체에 맞았을 때, 호출
        /// 발사체 위치 (arrow 스폰 지점) 기준 local linear 방향 필요 (Vector3)
        /// </summary>
        protected virtual void OnHit(bool isPlayer)
        {
            if (isPlayer)
            {
                LogUtil.Log($"[ProjectileBase - {transform.name}] => HIT PLAYER!");    
            }
            
            // < N초 후 피해 등... 처리
            // write something here
            // >
            
            // 파괴 애니 처리
            // <
            // 
            // >
        }
    }
}
