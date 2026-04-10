using UnityEngine;
using UnityEngine.Serialization;

namespace REIW
{
    /// <summary>
    /// 해당 컴포넌트가 부착된 오브젝트는,
    /// 오브젝트 윗방향 벡터(local.Y)와 지면 법선 벡터(global.Y)의 각도차에 비례한 속도로 캐릭터를 미끄러뜨림
    /// </summary>
    public class SlipOnAngle : MonoBehaviour
    {
        [LineTitle("AREA_ SLIP ON ANGLE")]
        [Tooltip("미끄러짐 활성화/비활성화")] [SerializeField]
        private bool _enableSlip = true;
        
        [Tooltip("미끄러지기 시작하는 각도")] [SerializeField] 
        private float _slipStartAngle = 20f;
        
        [FormerlySerializedAs("_platformFriction")] [Tooltip("마찰력 (0~1) | 0 => 매우 미끄러움, 1 => 미끄러지지 않음")] [SerializeField][Range(0, 1)]
        private float _friction = 0f;
        
#if UNITY_EDITOR
        [LineSubtitle("- Debug Option")]
        [Tooltip("ID = 0: 로그 출력 안 함")]
        [SerializeField] private int _debugID = 0; 
#endif
        
        private static readonly float _speedMultiplier = 50f;
        
        
        private void OnCollisionStay(Collision other)
        {
            if (_enableSlip && other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                var planeAngle = Vector3.Angle(Vector3.up, this.transform.up);
                if (planeAngle >= _slipStartAngle)
                {
                    var slipForce = Vector3.ProjectOnPlane(Vector3.down * 9.81f, this.transform.up) * (1 - _friction);
                    LocalCharacter.Instance.AddVelocity(slipForce * _speedMultiplier * Time.deltaTime);
                }
#if UNITY_EDITOR
                if (_debugID > 0)
                {
                    LogUtil.Log($"[SlipperyPlatform({_debugID})] 지면 기준 플랫폼 각도: {planeAngle}");
                }
#endif
            }
        }
    }
}
