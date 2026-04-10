using MagicaCloth2;
using UnityEngine;

namespace REIW
{
    [RequireComponent(typeof(BoxCollider))]
    public class Area_Windzone : MonoBehaviour
    {
        #region Setting - Windzone
        [LineTitle]
        [Header("AREA_ WINDZONE")]
        [Tooltip("기본 바람 세기")][SerializeField]
        private float _windPower = 10f;

        [Tooltip("시작 지점 대비 바람 세기 | 0: 가장 가까운 지점 / 1: 가장 먼 지점")] [SerializeField]
        private AnimationCurve _windPowerCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Tooltip("바람 방향 | z로 고정")][SerializeField, ReadOnly]
        private Vector3 _windDirection = Vector3.forward;

        [Tooltip("윈드존 영역")] [SerializeField]
        private BoxCollider _windzoneCollider;
        
        [LineCustom(10, 10, true)]
        [SerializeField] 
        private bool _enableMagicaWind = false;
        
        [Tooltip("Magica WindZone")] [SerializeField]
        private MagicaWindZone _magicaWindZone;

        
        private float _windZoneStartPointZ;
        private float _windZoneEndPointZ;

        private static readonly float _speedMultiplier = 20f;  // KCC Input이 없을 때, (Velocity == 0) 이므로 보정계수 필요
        
        #endregion
        
        
        // ==================================================
        // [LOGIC] Life Cycle
        // ==================================================
        private void Awake()
        {
            _windzoneCollider ??= transform.GetComponent<BoxCollider>();

            RebuildWindZonePoints();
            GimmickUtility.NormalizeCurveData(_windPowerCurve);

            if (_enableMagicaWind)
            {
                _magicaWindZone ??= transform.GetComponentInChildren<MagicaWindZone>();
                _magicaWindZone.size = _windzoneCollider.size;
                _magicaWindZone.transform.position = transform.TransformPoint(_windzoneCollider.center);
            }
            else
            {
                if (_magicaWindZone.enabled)
                    _magicaWindZone.enabled = false;
            }
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        public void SetActive(bool active)
        {
            if (active)
            {
                _windzoneCollider.enabled = true;
            }
            else
            {
                _windzoneCollider.enabled = false;
            }
        }
        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                float windPowerByDistance = 0f;
                
                Vector3 localedPlayerPos = transform.InverseTransformPoint(LocalCharacter.Instance.CharacterTransform.position);
                float playerPosZ = localedPlayerPos.z;
                float normalizedDistance = Mathf.InverseLerp(_windZoneStartPointZ, _windZoneEndPointZ, playerPosZ);  // T 구하기
                
                // animationCurve에서 해당 지점 구해서 * windPower
                float powerMultiplier = _windPowerCurve.Evaluate(normalizedDistance);
                windPowerByDistance = powerMultiplier * _windPower;
                
                // 속도 추가
                LocalCharacter.Instance.AddVelocity(transform.TransformDirection(_windDirection) * windPowerByDistance * _speedMultiplier * Time.deltaTime);
            }
        }
        
        
        // ==================================================
        // [LOGIC] Utility
        // ==================================================
        
        /// <summary>
        /// Windzone 콜라이더 Center/Size 가 실시간 변하는 경우, 때마다 실행 필요
        /// 내 Transform 전체가 바뀌는 건 무관함
        /// </summary>
        private void RebuildWindZonePoints()
        {
            float colliderCenterZ = _windzoneCollider.center.z;
            float colliderSizeZ = _windzoneCollider.size.z;
            _windZoneStartPointZ = colliderCenterZ - (colliderSizeZ / 2);
            _windZoneEndPointZ = colliderCenterZ + (colliderSizeZ / 2);
        }
        
        
#if UNITY_EDITOR
        #region Gizmo Drawing
        private void OnDrawGizmos()
        {
            if (_windzoneCollider == null) return;

            // 1. 영역 박스 (반투명하게)
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireCube(_windzoneCollider.center, _windzoneCollider.size);

            // 2. 커브 시각화 (막대기 형태)
            float sizeZ = _windzoneCollider.size.z;
            float centerZ = _windzoneCollider.center.z;
            float startZ = centerZ - (sizeZ / 2f);
            float endZ = centerZ + (sizeZ / 2f);

            for (float t = 0; t <= 1.01f; t += 0.05f) // 더 촘촘하게 0.05 간격
            {
                float localZ = Mathf.Lerp(startZ, endZ, t);
                
                // 바닥점 (콜라이더 하단)
                Vector3 basePos = _windzoneCollider.center;
                basePos.z = localZ;
                basePos.y -= _windzoneCollider.size.y / 2f; 

                // 천장점 (커브 값에 따른 높이)
                float powerRatio = _windPowerCurve.Evaluate(t);
                Vector3 topPos = basePos;
                topPos.y += _windzoneCollider.size.y * powerRatio;

                // 세기에 따라 색상 변경 (약하면 청록, 강하면 노랑)
                Gizmos.color = Color.Lerp(Color.cyan, Color.yellow, powerRatio);
                
                // 막대기 그리기
                Gizmos.DrawLine(basePos, topPos);
                
                // 끝점 표시 (흐름을 보여줌)
                Gizmos.DrawSphere(topPos, 0.02f);
            }
        }

        #endregion
#endif
    }
}
