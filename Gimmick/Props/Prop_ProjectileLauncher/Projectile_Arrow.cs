using UnityEngine;

namespace REIW
{
    public class Projectile_Arrow : GimmickProjectileBase
    {
        protected override void OnHit(bool isPlayer)
        {
            // < N초 후 피해 등... 처리
            // write something here
            // >
            
            // 파괴 애니 처리
            // <
            // 
            // >
            
            Destroy(this.gameObject);
        }
    }
}
