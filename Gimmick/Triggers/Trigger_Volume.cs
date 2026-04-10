using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace REIW
{
    public class Trigger_Volume : GimmickTriggerBase
    {
        [LineTitle("TRIGGER_ VOLUME")]
        [SerializeField] private Collider _triggerCollider;
        
        [FormerlySerializedAs("_targetGimmicks")]
        [SerializeField] private List<GimmickPropBase> _targetProps = new();
        
        protected override void AwakeTrigger()
        {
            if (_triggerCollider == null)
            {
                _triggerCollider = GetComponent<Collider>();
            }
        }
        
        
        private void OnTriggerEnter(Collider other)
        {
            // 화살-돌-폭탄 등 특정 지점에 맞추었을 때 작동해야 하는 경우, Layer 확장 필요
            if (other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                Trigger();
            }
        }
        
        public override void Trigger()
        {
            foreach (var targetGimmick in _targetProps)
            {
                targetGimmick.RequestActuate().Forget();
            }
        }
    }
}
