using System;
using Cysharp.Threading.Tasks;
using REIW.EventLock;
using UnityEngine;

namespace REIW
{
    public class Prop_PlayerLauncher : GimmickPropBase
    {
        private enum CannonType
        {
            /// <summary>
            /// 플레이어가 발사 축 조절 가능
            /// </summary>
            Control,
            
            /// <summary>
            /// 발사 축 고정
            /// </summary>
            Fix
        }
        
        
        [LineTitle("PROP_ PLAYER LAUNCHER")]
        [Tooltip("캐논 타입 | Control: 발사 축 조절 가능 / Fix: 발사 축 고정")][SerializeField] 
        private CannonType _cannonType = CannonType.Control;
        [Space(5)]
        [Tooltip("플레이어를 고정할 위치")][SerializeField]
        private Transform _playerLaunchPoint;
        [Space(5)]
        [Tooltip("발사 대기 시간")][SerializeField]
        private float _launchWaitSec = 1f;
        [Space(5)]
        [Tooltip("발사 강도 | 해당 강도만큼 Velocity Add")][SerializeField]
        private float _launchForce = 10f;
        [Tooltip("발사 유지 시간 | 해당 시간만큼 Velocity Add")][SerializeField]
        private float _launchStaySec = 4f;
        
        private MoveLock _moveLock = new();
        public class MoveLock : ICheckEventLockState
        {
            public eEventLockType CurrentEventLockType => eEventLockType.CharacterMoveAllAction;
            public eEventLockType ReleaseEventLockType => eEventLockType.None;
        }
        
        private static readonly float _speedMultiplier = 60f;  // 50~70
        private bool _canCancel = false;
        
        
        [LineSubtitle("- Control Type Option")]
        [Tooltip("1회 각도 조절 시 이동 범위")][SerializeField]
        private float _controlAnglePerOne = 3f;
        [Tooltip("최대 이동 범위 | x, z축")][SerializeField]
        private float _maxAngle = 40f;
        [Tooltip("캐논 각도")][SerializeField, ReadOnly]
        private Vector2 _rotateDirection = Vector2.zero;
        
        private float _initialYAngle;


        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        protected override void AwakeProp()
        {
            // ground 보다 0.75정도 땅에서 띄워줘야, 발사 가능 (KCC ground 판정 때문에...)
            if (_playerLaunchPoint.position.y < 0.75f)
                _playerLaunchPoint.position = new Vector3(_playerLaunchPoint.position.x, 0.75f, _playerLaunchPoint.position.z);

            _initialYAngle = transform.localRotation.eulerAngles.y;
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            if (ActuationMode == GimmickPropActuationMode.Auto)
                return;
            
            // * Pre Process
            //   조작 불가 처리
            //   애니 Airbone으로 | ∵ Dash/Jump해서 캐논 진입 시, 해당 애니 그대로 멈춰버림 => Idle은 IsGround일 때만 진입 가능하므로... 추후 논의 필요 
            LocalCharacter.Instance.CharacterEventLockController.AddEventLockState(_moveLock);
            //   위치 고정
            LocalCharacter.Instance.SetPositionAndRotation(_playerLaunchPoint.position, Quaternion.identity);
            LocalCharacter.Instance.SetClientCharacterRotation(transform.rotation);  // TODO: 회전은 클라 캐릭터만 돌리면 네트워크/Local 회전 중첩 이슈 => 추후 논의 필요 <= @suhlee 

            // * Process
            _canCancel = true;
            _rotateDirection = new Vector2(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.z);
            
            float sec = 0f;
            while (sec < _launchWaitSec)
            {
                if (IsCancelReserved)
                {
                    CurrentState = GimmickPropState.Ready;
                    return;
                }
                
                if (_cannonType == CannonType.Control)  // Control Type: 각도 조절 타입
                {
                    Vector2 moveInput = InputController.Singleton.Move;
                    int horizontalInput = Math.Sign(moveInput.x) * -1;  // horizontal Axis 누를 시, 우측키 => redAxis - 좌측키 => -redAxis
                    int verticalInput = Math.Sign(moveInput.y);
                    
                    float xAngle = _rotateDirection.x + (verticalInput * _controlAnglePerOne);
                    float zAngle = _rotateDirection.y + (horizontalInput * _controlAnglePerOne);  // z축 "기준" 회전이기 때문에, 좌우 회전
                    
                    xAngle = (xAngle > 180f) ? xAngle - 360f : (xAngle < -180f) ? xAngle + 360f : xAngle;  // 각도 튄 것 정규화
                    zAngle = (zAngle > 180f) ? zAngle - 360f : (zAngle < -180f) ? zAngle + 360f : zAngle;
                    if (Mathf.Abs(xAngle) <= _maxAngle) _rotateDirection.x = xAngle;
                    if (Mathf.Abs(zAngle) <= _maxAngle) _rotateDirection.y = zAngle;
                    
                    transform.localRotation = Quaternion.Euler(_rotateDirection.x, _initialYAngle, _rotateDirection.y);
                    // LocalCharacter.Instance.SetClientCharacterRotation(transform.rotation);
                }
                sec += Time.deltaTime;
                if (sec < _launchWaitSec) await UniTask.Yield(destroyCancellationToken);
            }
            
            //   플레이어 발사 & 플레이어가 특정 거리(높이) 도달 전까지 대기
            _canCancel = false;
            LocalCharacter.Instance.SetClientCharacterRotation(Quaternion.identity);  // 회전 (0,0,0)으로 복귀
            
            sec = 0f;
            while (sec < _launchStaySec)
            {
                Vector3 launchVelocity = transform.up * _launchForce;  // 중력 무시 필요 X => 중력은 UpdateVelocity에서 계산됨 => MoveEventLock일 시 자동 무시됨
                
                LocalCharacter.Instance.AddVelocity(launchVelocity * _speedMultiplier * Time.deltaTime);
                
                sec += Time.deltaTime;
                if (sec < _launchStaySec) await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
            }
            
            // * Post Process
            LocalCharacter.Instance.CharacterEventLockController.RemoveEventLockState(_moveLock);
            CurrentState = GimmickPropState.Ready;
        }

        public override async UniTask RequestCancel()
        {
            if (CurrentState != GimmickPropState.Actuating || !_canCancel)  // 플레이어가 캐논 안에 들어가있을 때에만 취소 가능
                return;
            IsCancelReserved = true;
            
            LocalCharacter.Instance.SetClientCharacterRotation(Quaternion.identity); 
            LocalCharacter.Instance.CharacterEventLockController.RemoveEventLockState(_moveLock);
        }


        
        
#if UNITY_EDITOR
        #region Gizmo Drawing
        private void OnDrawGizmos()
        {
            UnityEditor.Handles.color = Color.yellow;

            UnityEditor.Handles.DrawLine(_playerLaunchPoint.position, _playerLaunchPoint.position + transform.up * 3f);
        }

        #endregion
#endif
        
    }
}
