using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Prop_LavaFountain : GimmickPropBase
    {
        [LineTitle("PROP_ LAVA FOUNTAIN")]
        [SerializeField]
        private GimmickPropRepeatMode _repeatMode = GimmickPropRepeatMode.Infinite;
        
        [Tooltip("용암 분수 뚜껑")] [SerializeField]
        private PropSub_FountainLid _fountainLid;
        
        [Tooltip("용암 발사 끝 지점 | 용암이 올라가기를 멈추는 지점")] [SerializeField]
        private Transform _lavaGoalPoint;
        
        [Tooltip("용암 모델 그룹")] [SerializeField]
        private Transform _lavaModelGroup;
        
        [Tooltip("용암 모델 Top Pivot | LavaModel 자식 계층의 'Top Pivot' 삽입")] [SerializeField]
        private Transform _lavaModelTopPivot;

        [Tooltip("용암 모델 상승 속도")] [SerializeField]
        private float _lavaModelAnimSpeed = 0.2f;

        [Tooltip("최대 높이에서의 대기시간")] [SerializeField]
        private float _waitTimeOnTop = 5f;

        [LineSubtitle("- GimmickPropRepeatMode - Infinite Option")]
        [Tooltip("재작동 대기시간 | Infinite일 시에만")] [SerializeField]
        private float _actuationLoopDuration = 5f;

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            do
            {
                Vector3 plusScale = new Vector3(0, 1, 0) * _lavaModelAnimSpeed;
                bool IsLavaReachTop() => (_lavaModelTopPivot.position.y >= _lavaGoalPoint.position.y);
                while (!IsLavaReachTop())
                {
                    if (IsCancelReserved)
                        break;

                    _lavaModelGroup.localScale += plusScale;
                    _fountainLid.SetLavaTopPivot(_lavaModelTopPivot.position);
                    
                    if (!IsLavaReachTop())
                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
                }

                if (!IsCancelReserved)
                {
                    _fountainLid.NotiLidOnTop();

                    await UniTask.WaitForSeconds(_waitTimeOnTop, false, PlayerLoopTiming.Update, destroyCancellationToken);
                }

                while (_lavaModelGroup.localScale.y - plusScale.y > 1f)
                {
                    _lavaModelGroup.localScale -= plusScale;
                    _fountainLid.SetLavaTopPivot(_lavaModelTopPivot.position);

                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, destroyCancellationToken);
                }
                _lavaModelGroup.localScale = Vector3.one;

                if (ShouldActuateAgain())
                    await UniTask.WaitForSeconds(3f, false, PlayerLoopTiming.Update, destroyCancellationToken);

            } while (ShouldActuateAgain());
            bool ShouldActuateAgain() => (_repeatMode == GimmickPropRepeatMode.Infinite && !IsCancelReserved);
            
            CurrentState = GimmickPropState.Ready;
        }
    }
}
