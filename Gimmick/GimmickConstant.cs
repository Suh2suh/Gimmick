// 임시로 Enum 클라에서 생성
// 추후에는 기획에서 테이블 제작 완료 시, 해당 cs파일은 제거될 예정

namespace REIW
{
    ///////////////////////////////////////////////////////////////////////
    /// Gimmick Prop  - {(e.g.) 움직이는 발판, 기울어지는 다리...}    ///
    ///////////////////////////////////////////////////////////////////////
    public enum GimmickPropState
    {
        /// <summary>
        /// 작동 X, 작동 가능
        /// </summary>
        Ready,
        
        /// <summary>
        /// 작동 O
        /// </summary>
        Actuating,
        
        /// <summary>
        /// 작동 X, 작동 불가
        /// </summary>
        Dead
    }
    
    public enum GimmickPropActuationMode
    {
        /// <summary>
        /// 인던 생성 시 자동 반복
        /// </summary>
        Auto,
        
        /// <summary>
        /// 트리거 등 외부 요인에 의해서 작동
        /// </summary>
        External
    }
    
    public enum GimmickPropMovementPattern
    {
        OneWay,
        Loop,
        PingPong,
    }

    // Watermill, ProjectileLauncher, LavaFountain 등
    public enum GimmickPropRepeatMode
    {
        /// <summary>
        /// 작동 트리거 → 1회 작동
        /// </summary>
        OneShot,
        
        /// <summary>
        /// 작동 트리거 → 지속 작동
        /// </summary>
        Infinite,
    }
    
    
    ///////////////////////////////////////////////////////////////////////
    /// Gimmick Trigger - {(e.g.) 레버, 발판...}                       ///
    ///////////////////////////////////////////////////////////////////////
    public enum GimmickTriggerAnimState
    {
        /// <summary>
        /// 작동 불가 상태
        /// 플레이어 인터렉션: 불가
        /// Animation: Loop
        /// </summary>
        DeActivation,
        
        /// <summary>
        /// 작동 가능 상태
        /// 플레이어 인터렉션: 가능
        /// Animation: Loop 
        /// </summary>
        Idle,
        
        /// <summary>
        /// 동작 상태 (대기 → 활성)
        /// 플레이어 인터렉션: 불가
        /// Animation: One-shot   
        /// </summary>
        Action,
        
        /// <summary>
        /// 작동 중 상태
        /// 플레이어 인터렉션: 불가
        /// Animation: Loop
        /// </summary>
        Activation,
        
        /// <summary>
        /// 복귀 상태 (활성 → 대기)
        /// 플레이어 인터렉션: 불가
        /// Animation: One-shot
        /// </summary>
        Return
    }
}
