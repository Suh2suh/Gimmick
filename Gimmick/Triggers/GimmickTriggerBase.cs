using UnityEngine;

namespace REIW
{
    public abstract class GimmickTriggerBase : MonoBehaviour
    {
        public virtual GimmickTriggerAnimState CurrentAnimState { get => _currentAnimState; set => _currentAnimState = value; }
        public virtual bool IsInteractable { get => _isInteractable; protected set => _isInteractable = value; }
        
        [LineTitle("TRIGGER BASE")]
        public int GimmickID;

        [Space(5)]
        [SerializeField, ReadOnly] protected GimmickTriggerAnimState _currentAnimState;
        [SerializeField, ReadOnly] protected bool _isInteractable;

        [LineSubtitle("Debug Option")]
        [SerializeField, Space(5)] private bool _debugMode;
        protected bool IsDebugMode => _debugMode;

        private void Awake()
        {
            Initialize();
            void Initialize()
            {
                // @suhlee TODO: 서버 연결 시, 서버에서 내려준 ID를 사용해야 함
                GimmickID = GimmickManager.Instance.GenerateGimmickID_LocalTest();
                GimmickManager.Instance.Register(GimmickID, this);
            }
            
            AwakeTrigger();
        }
        protected virtual void AwakeTrigger() {}

        public abstract void Trigger();
    }
}
