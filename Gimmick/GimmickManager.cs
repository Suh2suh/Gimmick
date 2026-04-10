using System.Collections.Generic;
using UnityEngine;

namespace REIW
{
    public class GimmickManager : MonoBehaviour
    {
        // [Singleton]
        public static GimmickManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GimmickManager>();

                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("GimmickManager");
                        _instance = obj.AddComponent<GimmickManager>();
                    }
                }

                return _instance;
            }
        }
        private static GimmickManager _instance;

        // [Gimmick ID: 기믹] 
        /*[For DEBUG]*/ [SerializeField] private UDictionary<int /*GimmickID*/, GimmickTriggerBase> _triggerByGimmickID = new();
        /*[For DEBUG]*/ [SerializeField] private UDictionary<int /*GimmickID*/, GimmickPropBase> _propByGimmickID = new();
        // private Dictionary<int /*GimmickID*/, GimmickTriggerBase> _triggerByGimmickID = new();
        // private Dictionary<int /*GimmickID*/, GimmickPropBase> _propByGimmickID = new();
        
        // ㄴ 어차피 trigger/prop은 각각의 객체이고, 패킷도 다르게 들어올 가능성 ↑ => 일단 따로 관리한다
        private HashSet<int> _validGimmickIDs = new();


        private void OnDestroy() => _instance = null;

        public GimmickPropBase GetPropByGimmickID(int gimmickID)
            /*[For DEBUG]*/ => _propByGimmickID.TryGetValue(gimmickID, out var prop) ? prop : null;
            // => _propByGimmickID.GetValueOrDefault(gimmickID);
        public GimmickTriggerBase GetTriggerByGimmickID(int gimmickID) 
            /*[For DEBUG]*/ => _triggerByGimmickID.TryGetValue(gimmickID, out var trigger) ? trigger : null;
            // => _triggerByGimmickID.GetValueOrDefault(gimmickID);
        
        public void Register(int gimmickID, GimmickTriggerBase trigger)
        {
            if (_triggerByGimmickID.ContainsKey(gimmickID))  // 비정상적인 상황 (ID는 애초에 서버 측에서 겹치면 안 됨)
                LogUtil.Log($"[Gimmick Register] 이미 존재하는 GimmickID입니다 - {gimmickID}".Color(Color.yellow));
            _triggerByGimmickID[gimmickID] = trigger;
            _validGimmickIDs.Add(gimmickID);
        }
        public void Register(int gimmickID, GimmickPropBase prop)
        {
            if (_propByGimmickID.ContainsKey(gimmickID))  // 비정상적인 상황 (ID는 애초에 서버 측에서 겹치면 안 됨)
                LogUtil.Log($"[Gimmick Register] 이미 존재하는 GimmickID입니다 - {gimmickID}".Color(Color.yellow));
            _propByGimmickID[gimmickID] = prop;
            _validGimmickIDs.Add(gimmickID);
        }
        
        public void Unregister(GimmickTriggerBase trigger)
        {
            if (_triggerByGimmickID.ContainsKey(trigger.GimmickID))
            {
                _triggerByGimmickID.Remove(trigger.GimmickID);
                _validGimmickIDs.Remove(trigger.GimmickID);
            }
            else {} // 비정상적인 상황 (Register 되지 않으면 안 됨)
        }
        public void Unregister(GimmickPropBase prop)
        {
            if (_triggerByGimmickID.ContainsKey(prop.GimmickID))
            {
                _triggerByGimmickID.Remove(prop.GimmickID);
                _validGimmickIDs.Remove(prop.GimmickID);
            }
            else {} // 비정상적인 상황 (Register 되지 않으면 안 됨)
        }

        #region [For Local Test]

        public int GenerateGimmickID_LocalTest()
        {
            int gimmickID = Random.Range(int.MinValue, int.MaxValue);
            while (_validGimmickIDs.Contains(gimmickID))
            {
                gimmickID = Random.Range(int.MinValue, int.MaxValue);
            }
            
            return gimmickID;
        }
        

        #endregion
    }
}
