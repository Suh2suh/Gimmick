using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Prop_TimerBomb : GimmickPropBase
    {
        [LineTitle("PROP_ TIMERBOMB")]
        [Tooltip("플레이어에게 데미지를 주는 데미지 존")] [SerializeField] 
        private Area_Damage _damageArea;
        
        [Tooltip("데미지 존 반경")] [SerializeField][Space(5)]
        private float _damageAreaRadius = 1f;
        
        [Tooltip("폭발 시 데미지 존 유지 시간 범위")] [SerializeField][Space(5)]
        private Vector2 _damageZoneStaySecRange = new Vector2(1.5f, 1.5f);
        
        [Tooltip("다음 폭발까지의 대기시간 범위")] [SerializeField][Space(5)]
        private Vector2 _explosionIntervalSecRange = new Vector2(4.5f, 4.5f);

        [LineSubtitle("- For Develop")]
        [Tooltip("클라이언트 확인용 폭발 영역 렌더러")] [SerializeField][Space(10)]
        private Renderer _renderer_FOR_DEVELOP;
        
        
        // ==================================================
        // [LOGIC] Life Cycle
        // ==================================================
        protected override void AwakeProp()
        {
            if (_damageZoneStaySecRange[0] <= 0)
                _damageZoneStaySecRange[0] = 1.5f;
            if (_explosionIntervalSecRange[0] <= 0)
                _explosionIntervalSecRange[0] = 4.5f;
            
            /* @suhlee 임시*/ _renderer_FOR_DEVELOP.enabled = false;

            _damageArea.SetDamageAreaRadius(_damageAreaRadius);
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            while (!IsCancelReserved)
            {
                // <폭발 커지는 애니 재생
                /* @suhlee 임시*/ _renderer_FOR_DEVELOP.enabled = true;
                // >
                
                _damageArea.SetActive(true);
                
                float damageZoneStaySec = Random.Range(_damageZoneStaySecRange[0], _damageZoneStaySecRange[1]);
                if (IsDebugMode)
                {
                    Debug.Log($"[Prop_TimerBomb({GimmickID})] 데미지존 유지시간(초): {damageZoneStaySec}".Color(Color.yellow));
                }
                await UniTask.WaitForSeconds(damageZoneStaySec, false, PlayerLoopTiming.Update, destroyCancellationToken);
                
                _damageArea.SetActive(false);
                
                // <폭발 작아지는 애니 재생
                /* @suhlee 임시*/ _renderer_FOR_DEVELOP.enabled = false;
                // >
                
                float explosionIntervalSec = Random.Range(_explosionIntervalSecRange[0], _explosionIntervalSecRange[1]);
                if (IsDebugMode)
                {
                    Debug.Log($"[Prop_TimerBomb({GimmickID})] 다음 데미지존 활성화까지의 대기시간(초): {explosionIntervalSec}".Color(Color.yellow));
                }
                await UniTask.WaitForSeconds(explosionIntervalSec, false, PlayerLoopTiming.Update, destroyCancellationToken);
            }
            
            CurrentState = GimmickPropState.Ready;
        }
    }
}
