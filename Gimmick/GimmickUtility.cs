using UnityEngine;

namespace REIW
{
    public static class GimmickUtility
    {
        ////////////////////////////////////////////////////////////////
        /// Set Collider's Size as Mesh                              ///
        /// ////////////////////////////////////////////////////////////
        public static void SetSizeAsMesh(this BoxCollider boxCollider, MeshRenderer meshRenderer, float sizeRatioX, float sizeRatioY, float sizeRatioZ, bool setCenterAsMesh = false)
        {
            var meshBounds = meshRenderer.localBounds;
            var meshScale = meshRenderer.transform.localScale;
            var meshBoundSize = new Vector3(meshBounds.size.x * meshScale.x * sizeRatioX, 
                                            meshBounds.size.y * meshScale.y * sizeRatioY, 
                                            meshBounds.size.z * meshScale.z * sizeRatioZ);
            boxCollider.size = meshBoundSize;

            if (setCenterAsMesh)
            {
                boxCollider.center = meshBounds.center;
            }
        }
        public static void SetSizeAsMesh(this BoxCollider boxCollider, MeshRenderer meshRenderer) => SetSizeAsMesh(boxCollider, meshRenderer, 1f, 1f, 1f, false);
        public static void SetSizeAsMesh(this BoxCollider boxCollider, MeshRenderer meshRenderer, bool setCenterAsMesh) => SetSizeAsMesh(boxCollider, meshRenderer, 1f, 1f, 1f, setCenterAsMesh);
        
        
        public static void SetSizeAsMesh(this SphereCollider sphereCollider, MeshRenderer meshRenderer, float sizeRatio, bool setCenterAsMesh = false)
        {
            var meshBounds = meshRenderer.localBounds;
            var meshScale = meshRenderer.transform.localScale;
            var meshBoundSize = new Vector3(meshBounds.size.x * meshScale.x, meshBounds.size.y * meshScale.y, meshBounds.size.z * meshScale.z);
            
            sphereCollider.radius = Mathf.Max(meshBoundSize.x / 2, meshBoundSize.y / 2, meshBoundSize.z / 2) * sizeRatio;

            if (setCenterAsMesh)
            {
                sphereCollider.center = meshBounds.center;
            }
        }
        public static void SetSizeAsMesh(this SphereCollider sphereCollider, MeshRenderer meshRenderer) => SetSizeAsMesh(sphereCollider, meshRenderer, 1f, false);
        public static void SetSizeAsMesh(this SphereCollider sphereCollider, MeshRenderer meshRenderer, bool setCenterAsMesh) => SetSizeAsMesh(sphereCollider, meshRenderer, 1f, setCenterAsMesh);

        
        public static void SetSizeAsMesh(this CapsuleCollider capsuleCollider, MeshRenderer meshRenderer, float sizeRatio = 1f, bool setCenterAsMesh = false)
        {
            var meshBounds = meshRenderer.localBounds;
            var meshScale = meshRenderer.transform.localScale;
            var meshBoundSize = new Vector3(meshBounds.size.x * meshScale.x, meshBounds.size.y * meshScale.y, meshBounds.size.z * meshScale.z);
            
            float height = meshBoundSize.y;
            float radius = Mathf.Max(meshBoundSize.x, meshBoundSize.z) / 2f;
            capsuleCollider.height = height * sizeRatio;
            capsuleCollider.radius = radius * sizeRatio;

            if (setCenterAsMesh)
            {
                capsuleCollider.center = meshBounds.center;
            }
        }
        public static void SetSizeAsMesh(this CapsuleCollider capsuleCollider, MeshRenderer meshRenderer) => SetSizeAsMesh(capsuleCollider, meshRenderer, 1f, false);
        public static void SetSizeAsMesh(this CapsuleCollider capsuleCollider, MeshRenderer meshRenderer, bool setCenterAsMesh) => SetSizeAsMesh(capsuleCollider, meshRenderer, 1f, setCenterAsMesh);
        
        
        
        ////////////////////////////////////////////////////////////////
        /// Set Collider's Size as Mesh                              ///
        /// ////////////////////////////////////////////////////////////
        /// <summary>
        /// Animation Curve의 Time, Value를 각각 0~1 사이로 정규화 
        /// </summary>
        public static void NormalizeCurveData(AnimationCurve curve)
        {
            if (curve == null || curve.length < 2) return;

            Keyframe[] keys = curve.keys;
    
            // 1. 현재 커브의 시간(Time)과 값(Value)의 최소/최대값 파악
            float minTime = keys[0].time;
            float maxTime = keys[^1].time;
    
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].value < minValue) minValue = keys[i].value;
                if (keys[i].value > maxValue) maxValue = keys[i].value;
            }

            float timeRange = maxTime - minTime;
            float valueRange = maxValue - minValue;

            // 2. 모든 키프레임을 0~1 범위로 재설정
            for (int i = 0; i < keys.Length; i++)
            {
                // 시간 정규화 (0~1)
                if (timeRange > 0)
                    keys[i].time = (keys[i].time - minTime) / timeRange;
                else
                    keys[i].time = 0f;

                // 값 정규화 (0~1)
                if (valueRange > 0)
                    keys[i].value = (keys[i].value - minValue) / valueRange;
                else
                    keys[i].value = 0f; // 모든 값이 같다면 0(혹은 1)으로 평탄화
            }

            // 3. 수정된 키프레임들을 다시 커브에 할당
            curve.keys = keys;
        }
    }
}
