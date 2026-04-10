using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace REIW
{
    public class Prop_ProjectileLauncher : GimmickPropBase
    {
        [LineTitle("PROP_ PROJECTILE LAUNCHER")]
        [SerializeField] GimmickPropRepeatMode _repeatMode = GimmickPropRepeatMode.OneShot;
        [Space(5)]
        [Tooltip("해당 런처에서 발사되는 Projectile prefab")]
        [SerializeField] private GimmickProjectileBase _targetProjectile;
        [Tooltip("Projectile 발사지점")]
        [SerializeField] private Transform _fireLaunchPoint;
        [Tooltip("발사 방향 | Local 기준 (Vector3.zero일 경우, forward로 설정됨)")]
        [SerializeField] private Vector3 _fireAxisLocal;
        [Tooltip("발사 간격 (초)")]
        [SerializeField] private float _fireRate = 0.5f;
        
        [LineSubtitle("- RepeatMode - OneShot")]
        [SerializeField] private int _fireCountOnOneShot = 10;
        
        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            if (IsDebugMode)
                Debug.Log($"[Gimmick_ProjectileLauncher] {GimmickID} | Request Actuate!".Color(Color.yellow));
            
            if (_repeatMode == GimmickPropRepeatMode.OneShot)
                await FireProjectilesOnCount(_fireCountOnOneShot);
            else
                await FireProjectilesOnLoop();
            
            CurrentState = GimmickPropState.Ready;
        }
        
        private async UniTask FireProjectilesOnCount(int fireCount) => await FireProjectiles(isLoop: false, fireCount);
        private async UniTask FireProjectilesOnLoop() => await FireProjectiles(isLoop: true);
        private async UniTask FireProjectiles(bool isLoop, int fireCount = 1)
        {
            if (IsDebugMode)
                Debug.Log($"[Gimmick_ProjectileLauncher] {GimmickID} | Start Fire Projectiles...".Color(Color.yellow));
            
            int leftCount = fireCount;
            while (leftCount > 0)
            {
                if (IsCancelReserved || destroyCancellationToken.IsCancellationRequested)
                    return;
                
                FireProjectile();
                await UniTask.WaitForSeconds(_fireRate, false, PlayerLoopTiming.Update, destroyCancellationToken);

                if (!isLoop)
                    leftCount--;
                
                if (IsDebugMode)
                    Debug.Log($"[Gimmick_ProjectileLauncher] {GimmickID} | Fire! {fireCount - leftCount} / {fireCount}".Color(Color.yellow));
            }
        }

        private void FireProjectile()
        {
            GimmickProjectileBase projectile = 
                Instantiate
                (
                    original: _targetProjectile, 
                    position: _fireLaunchPoint.position,
                    rotation: Quaternion.LookRotation(_fireLaunchPoint.forward, _fireLaunchPoint.up),
                    parent: _fireLaunchPoint.transform
                ); // 발사대 기준으로 forward 설정

// #if UNITY_EDITOR
            // 에디터에서 런처 발사 축 확인용 (Inspector에서 축 변경할 때 대비, 발사할 때마다 계산)
            projectile.Fire(TransformDirectionFireAxis(_fireAxisLocal));
// #else
            // projectile.Fire(_fireAxisLocal);
// #endif
        }

        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        private Vector3 TransformDirectionFireAxis(Vector3 localFireAxis)
            => localFireAxis == Vector3.zero ? _fireLaunchPoint.TransformDirection(Vector3.forward)
                                             : _fireLaunchPoint.TransformDirection(localFireAxis).normalized;
        

        
        
        #region Gizmo Drawing
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_fireLaunchPoint == null)
                return;
            
            Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);
            Gizmos.DrawSphere(_fireLaunchPoint.transform.position, 0.1f);
            
            Gizmos.color = Color.red;
            Vector3 fireAxisGlobal = TransformDirectionFireAxis(_fireAxisLocal);
            Gizmos.DrawLine(_fireLaunchPoint.transform.position, _fireLaunchPoint.transform.position + fireAxisGlobal * 1.5f);
        }
#endif
        #endregion
    }
}
