using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(InteractionLever))]
    public class Trigger_Interaction : GimmickTriggerBase
    {
        private enum OperationType
        {
            /// <summary>
            /// 사용자가 명시적으로 취소 요청을 하지 않을 시, Prop 작동 유지
            /// e.g.) 레버를 원위치로 복귀시켰을 때에만 취소 요청 전송
            /// </summary>
            Sustained,
        
            /// <summary>
            /// 기믹 프랍이 1 subPhase만큼 작동 완료한 후에, 프랍 작동 취소 요청
            /// => 1 subPhase의 기준은 기믹프랍에서 지정 (OnSubPhaseDone 이벤트 호출)
            /// </summary>
            AutoRelease
        }
        
        [Header("Setting - GimmickTrigger_Interaction")]
        [Tooltip("해당 GimmickTrigger(레버)를 Trigger시에 작동시킬 기믹 프랍")] [SerializeField] 
        private GimmickPropBase _targetProp;
        [Tooltip("해당 GimmickTrigger(레버)의 작동 방식 | Stay: 작동 유지 / Cycle: 1사이클 작동 완료 후 Cancel")] [SerializeField]
        private OperationType _operationType = OperationType.Sustained;
        [Space(5)]
        [Tooltip("[임시] 해당 GimmickTrigger(레버)의 상태를 표시하는 메터리얼")] [SerializeField] 
        private MeshRenderer _interactableChecker;
 
        public override GimmickTriggerAnimState CurrentAnimState
        {
            get => _currentAnimState;
            set
            {
                if (_currentAnimState == value)
                    return;
                
                _currentAnimState = value;
                switch (_currentAnimState)
                {
                    case GimmickTriggerAnimState.DeActivation:
                        _interactableChecker.material.color = Color.black;
                        break;
                    case GimmickTriggerAnimState.Idle:
                        _interactableChecker.material.color = Color.white;
                        break;
                    case GimmickTriggerAnimState.Action:
                        _interactableChecker.material.color = Color.cyan;
                        break;
                    case GimmickTriggerAnimState.Activation:
                        _interactableChecker.material.color = Color.blue;
                        break;
                    case GimmickTriggerAnimState.Return:
                        _interactableChecker.material.color = Color.gray;
                        break;
                }
            }
        }

        /// <summary> 인터렉션 가능 여부 </summary>
        public override bool IsInteractable
        {
            get => _isInteractable;
            protected set
            {
                if (IsDebugMode)
                    Debug.Log($"[Gimmick_TriggerInteraction] IsInteractable: {value}".Color(Color.yellow));
                
                _isInteractable = value;
                OnIsInteractableChanged?.Invoke();
            }
        }
        public event Action OnIsInteractableChanged;
        
        /// <summary> 인터렉션 존 내부에 위치했는지 여부 => 나 뿐만이 아니라 다른 플레이어도 감지해야댐 </summary>
        public bool IsInInteractionZone { get; set; }
        
        
        protected override void AwakeTrigger()
        {
            CurrentAnimState = GimmickTriggerAnimState.DeActivation;
            
            if (_targetProp.ActuationMode != GimmickPropActuationMode.External)
                return;

            IsInteractable = true;
            _targetProp.OnStateChanged += OnPropStateChanged;
            _targetProp.OnCancelReservationChanged += OnPropCancelReservationChanged;
            _targetProp.OnSubPhaseDone += OnPropSubPhaseDone;
        }

        private void OnDestroy()
        {
            if (_targetProp.ActuationMode != GimmickPropActuationMode.External)
                return;
            
            _targetProp.OnStateChanged -= OnPropStateChanged;
            _targetProp.OnCancelReservationChanged -= OnPropCancelReservationChanged;
            _targetProp.OnSubPhaseDone -= OnPropSubPhaseDone;
        }

        public override void Trigger()
        {
            if (_targetProp == null)
            {
                LogUtil.Log($"[GimmickTrigger_Interaction] TargetGimmickProp is null! (name: {gameObject.name} | instanceID: {gameObject.GetInstanceID()})".Color(Color.red));
                return;
            }

            Action leverAction = _targetProp.CurrentState switch
            {
                GimmickPropState.Ready => ActuateGimmickProp,
                GimmickPropState.Actuating => _targetProp.IsCancelReserved ? _targetProp.UndoCancel : CancelGimmickProp,
                _ => null,
            };
            leverAction?.Invoke();
        }

        private void ActuateGimmickProp()
        {
            if (_operationType == OperationType.AutoRelease)
                IsInteractable = false;  // 로컬 단에서 1회 방지
            
            CurrentAnimState = GimmickTriggerAnimState.Activation;
            _targetProp.RequestActuate().Forget();
        }

        private void CancelGimmickProp()
        {
            if (_operationType == OperationType.AutoRelease 
                && _targetProp.CurrentState == GimmickPropState.Actuating)
                return;
            
            CurrentAnimState = GimmickTriggerAnimState.Idle;
            _targetProp.RequestCancel().Forget();
        }


        private void OnPropStateChanged(GimmickPropState currentState)
        {
            switch (currentState)
            {
                case GimmickPropState.Ready:
                {
                    // if (prevState != GimmickPropState.Actuating)
                        // return;
                    
                    if (_operationType == OperationType.AutoRelease)
                    {
                        IsInteractable = true;
                    }
                    CurrentAnimState = (IsInteractable && IsInInteractionZone) ? GimmickTriggerAnimState.Idle : GimmickTriggerAnimState.DeActivation;
                }
                break;

                case GimmickPropState.Actuating:
                {
                    if (_operationType == OperationType.AutoRelease)
                    {
                        IsInteractable = false;  // OtherPlayer가 레버 돌렸을 때에도 실행해줘야함
                    }
                    CurrentAnimState = GimmickTriggerAnimState.Activation;
                }
                break;

                case GimmickPropState.Dead:
                {
                    IsInteractable = false;
                    
                    CurrentAnimState = GimmickTriggerAnimState.DeActivation;
                }
                break;
            }
        }

        private void OnPropCancelReservationChanged(bool isCancelReserved)
        {
            if (_targetProp.CurrentState != GimmickPropState.Actuating)
                return;
            
            CurrentAnimState = isCancelReserved ? ((IsInteractable && IsInInteractionZone) ? GimmickTriggerAnimState.Idle : GimmickTriggerAnimState.DeActivation)
                                                : GimmickTriggerAnimState.Activation;
        }

        private void OnPropSubPhaseDone(int subPhase)
        {
            if (_operationType == OperationType.AutoRelease)
            {
                _targetProp.RequestCancel().Forget();
            }
        }
    }
}
