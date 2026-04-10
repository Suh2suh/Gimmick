using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Trigger_Event : GimmickTriggerBase
    {
        [LineTitle("TRIGGER_ EVENT")]
        [Tooltip("이벤트 수신 시 작동시킬 프랍들")] [SerializeField] 
        private List<GimmickPropBase> _targetProps = new();
        
        public override void Trigger()
        {
            foreach (var prop in _targetProps)
            {
                prop.RequestActuate().Forget();
            }
        }
    }
}
