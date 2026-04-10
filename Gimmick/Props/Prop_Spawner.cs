using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    public class Prop_Spawner : GimmickPropBase
    {
        [Header("Setting - Prop_Spawner")]
        [SerializeField]
        private List<SpawnData> _spawnDatas;
        [System.Serializable] private class SpawnData
        {
            [field: SerializeField] public GameObject SpawnTarget { get; private set; }
            [field: SerializeField] public Transform SpawnPosition { get; private set; }
            [field: SerializeField] public Vector3 SpawnRotation { get; private set; }
        }

        [SerializeField] 
        private float _spawnInterval = 3f;
        
        
        protected override async UniTask Actuate()
        {
            while (!IsCancelReserved)
            {
                foreach (var spawnData in _spawnDatas)
                {
                    var spawnedObject = Instantiate(spawnData.SpawnTarget, spawnData.SpawnPosition.position, Quaternion.identity);
                    if (!spawnedObject.activeSelf)
                        spawnedObject.SetActive(true);
                }

                await UniTask.WaitForSeconds(_spawnInterval);
            }
            
            CurrentState = GimmickPropState.Ready;
        }
    }
}
