
namespace REIW
{
    ////////////////////////////////////////////
    /// 클라이언트 테스트 스크립트           ///  description: https://www.notion.so/voyagergames/Gimmick-31f229b506f2809da194e520f5d9e204?source=copy_link
    ////////////////////////////////////////////
    public class DUMMY_GimmickNetwork
    {
        #region Prop
            [System.Serializable]
            public class PropPacket
            {
                /// <summary>
                /// 프랍 식별자
                /// </summary>
                public int GimmickID;
                
                /// <summary>
                /// 프랍 작동 시작 시간 (가장 최근에 작동 시작했던 시간)
                /// </summary>
                public long ActuationStartTimeMs; 

                
                /// <summary>
                /// 프랍 상태 (작동 가능/ 작동 중/ 작동 불가)
                /// </summary>
                public GimmickPropState State; 

                
                /// <summary>
                /// 프랍 작동 중단점 Index
                /// </summary>
                public byte SubPhaseIndex;
            }
            
            
            // 0. PVE 맵 진입 시
            public void ReqPropInfo(int gimmickID)
            {
            }

            public void AckPropInfo(PropPacket propInfo)
            {
                var prop = GimmickManager.Instance.GetPropByGimmickID(propInfo.GimmickID);
                // 0. Prop 초기화
                // 1. 상태에 따라 Prop별 분기 처리
            }
            
            
            // 1. PVE 맵 내부
            // [ 프랍 상태 변경 시 ]
            public void ReqNotiPropStateChanged(int gimmickID, GimmickPropState state)
            {
                // ★ 최초 발신 클라이언트의 데이터로 1회만 저장해야 함...
            }

            public void AckNotiPropStateChanged(PropPacket propInfo)  
            {
                var prop = GimmickManager.Instance.GetPropByGimmickID(propInfo.GimmickID);
                // 0. 상태에 따라 Prop별 분기 처리
                // 1. e.g.)
                //    switch (state)
                //    {
                //       idle: 미처리 - (씬에 스폰된 상태 그대로니까... 필요없음)
                //       actuate: 들어온 작동 시작 시간 기반 보정 - (네트워크 딜레이)
                //       dead: 작동 불가 처리 - (platform-suicide 모드일 시 setActive(false))
                //    }
            }

            // [ 프랍 중단점 도착 시 ]
            public void ReqNotiPropSubPhase(int gimmickID, int subPhaseIndex)
            {
                // ★ 최초 발신 클라이언트의 데이터로 1회만 저장해야 함...
            }
            // => AckNotiPropSubPhase() { do nothing(); }
        
        
        #endregion

        #region Trigger
            [System.Serializable]
            public class TriggerPacket
            {
                public int GimmickID;
                
                /// <summary>
                /// 트리거 Animation 상태 (작동 상태)
                /// </summary>
                public GimmickTriggerAnimState State;
            }
            
            
            // 0. PVE 맵 진입 시
            public void ReqTriggerInfo(int gimmickID)
            {
                // renetwork~.req~;
            }

            public void AckTriggerInfo(TriggerPacket triggerInfo)
            {
                // State에 따라 애니 변경
            }
            
            
            // 1. PVE 맵 내부
            public void ReqNotiTriggerStateChanged(int gimmickID, GimmickTriggerAnimState state)
            {
                // renetwork~.req~;
            }

            public void AckTriggerStateChanged(TriggerPacket triggerInfo)
            {
                var trigger = GimmickManager.Instance.GetTriggerByGimmickID(triggerInfo.GimmickID);
                // trigger 애니 상태 변경
                // ㄴ 위 함수 들어오는 시점부터, Prop은 trigger작동일 때, debug모드가 아닐 시에는
                //    trigger 상태 변경 콜백으로 작동되지 않는다...
                
                //    => 서버에서
                //       a) 트리거와 연결된 기믹들에 대하여 PropPacket 생성
                //       b) NotiPropStateChanged()를 뿌려주고 => 작동시킨다
            }

            
        #endregion
    }
}