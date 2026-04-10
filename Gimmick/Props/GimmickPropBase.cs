using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace REIW
{
    public abstract class GimmickPropBase : MonoBehaviour
    {
        #region Setting - PropBase
        [LineTitle("PROP BASE")]
        [Tooltip("해당 GimmickProp의 고유 ID (현재는 미사용 - 클라 임시 처리)")]
        public int GimmickID;
        
        [Tooltip("해당 GimmickProp이 작동을 시작하는 방식. | Auto: 게임 시작 시 자동 작동 / Trigger: GimmickTrigger에 의해서 작동")][field: SerializeField]
        public GimmickPropActuationMode ActuationMode { get; private set; } = GimmickPropActuationMode.Auto;
        
        [Tooltip("기믹 상태")] [SerializeField, ReadOnly]
        private GimmickPropState _currentState = GimmickPropState.Ready;

        [Tooltip("직전 작동 시작 시간")] [SerializeField, ReadOnly, Space(5)]
        private long _actuationStartTimeMs;

        [Tooltip("직전 작동 중단점 Index")][SerializeField, ReadOnly] 
        private int _doneSubPhase = 0;
        
        [Tooltip("네트워크 패킷")] [SerializeField, KinematicCharacterController.ReadOnly] 
        protected DUMMY_GimmickNetwork.PropPacket _propPacket;

        private bool _isCancelReserved = false;

        #endregion

        #region Setting - Debug
        [LineSubtitle("- Debug Option")]
        [Tooltip("디버깅 모드")] [SerializeField] 
        private bool _debugMode;
        protected bool IsDebugMode => _debugMode;

        #endregion
        
        public GimmickPropState CurrentState
        {
            get => _currentState;
            protected set
            {
                if (_currentState == value)
                    return;
                
                _currentState = value;
                OnStateChanged?.Invoke(_currentState);
            }
        }
        
        protected long ActuationStartTimeMs
        {
            get => _actuationStartTimeMs;
            set => _actuationStartTimeMs = value;
        }

        protected int DoneSubPhase
        {
            get => _doneSubPhase;
            set
            {
                if (_doneSubPhase == value)
                    return;
                
                _doneSubPhase = value;
                OnSubPhaseDone?.Invoke(_doneSubPhase);
            }
        }
        
        public bool IsCancelReserved
        {
            get => _isCancelReserved;
            protected set
            {
                if (_isCancelReserved == value) 
                    return; 
                
                _isCancelReserved = value;
                OnCancelReservationChanged?.Invoke(_isCancelReserved);
            }
        }
        
        /// <summary> 상태 변경 (e.g. 준비 => 작동, 작동 => 작동 불가) </summary>
        public event Action<GimmickPropState> OnStateChanged;
        
        /// <summary> 취소 요청 상태 변경 </summary>
        public event Action<bool> OnCancelReservationChanged;
        
        /// <summary> 서브 페이즈 종료 (e.g. 플랫폼에서 이동 도중, 중간 정거장에 도달했을 때) </summary>
        public event Action<int> OnSubPhaseDone;
        
        
        // ==================================================
        // [LOGIC] Life Cycle
        // ==================================================
        private void Awake()
        {
            Initialize();
            AwakeProp();
        }
        
        private void Initialize()
        {    
            // @suhlee TODO: 서버 연결 시, 서버에서 내려준 ID를 사용해야 함
            GimmickID = GimmickManager.Instance.GenerateGimmickID_LocalTest();
            GimmickManager.Instance.Register(GimmickID, this);
        }
        
        protected virtual void AwakeProp() {}
        
        protected virtual void Start()
        {
            if (ActuationMode == GimmickPropActuationMode.Auto)
            {
                ActuationStartTimeMs = ReNetworkUtility.ServerServingTimeMs;
                RequestActuate().Forget();
            }
        }

        private void OnDestroy()
        {
            _isCancelReserved = true;
            GimmickManager.Instance.Unregister(this);
        }

        
        // ==========================================
        // [LOGIC] CORE
        // ==========================================
        public async UniTask RequestActuate()
        {
            if (CurrentState is GimmickPropState.Actuating or GimmickPropState.Dead)
                return;
            CurrentState = GimmickPropState.Actuating;
            
            if (ActuationMode == GimmickPropActuationMode.External)
            {
                ActuationStartTimeMs = ReNetworkUtility.NowServerTimeMs;
            }

            _isCancelReserved = false;  // withoutNotify: 단순 전처리 초기화이므로, 프로퍼티 콜백 미사용
            await Actuate();
        }
        
        protected virtual async UniTask Actuate() => LogUtil.Log($"[GimmickProp] 작동 로직이 구현되지 않았습니다 - {this.GetType()}");
        
        
        public virtual async UniTask RequestCancel()
        {
            IsCancelReserved = true;
        }

        public void UndoCancel()
        {
            IsCancelReserved = false;
        }
        
        

       
        #region [For Develop] will be removed
            [ContextMenu("ACTUATE_TEST")] private void Actuate_TEST() => Actuate().Forget();
            [ContextMenu("CANCEL_TEST")] private void Cancel_TEST() => RequestCancel().Forget();
        #endregion
        
    }
}
