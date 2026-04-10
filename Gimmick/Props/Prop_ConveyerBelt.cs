using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace REIW
{
    [RequireComponent(typeof(BoxCollider))]
    public class Prop_ConveyerBelt : GimmickPropBase
    {
        [LineTitle("PROP_ CONVEYER BELT")]
        [Tooltip("작동 방식")] [SerializeField]
        private GimmickPropRepeatMode _repeatMode = GimmickPropRepeatMode.Infinite;
        
        [Tooltip("작동 대기 시간")] [SerializeField]
        private float _warmupSec = 3f;
        
        [Tooltip("이동 패턴")] [SerializeField][Space(5)]
        private List<MovePattern> _movePatterns = new();
        [System.Serializable] private class MovePattern
        {
            [Tooltip("이동 방향 | Local 기준, (0~360)")] [field: SerializeField, Range(0, 360)] 
            public float MoveAngle { get; private set; } = 0f;
            
            [Tooltip("이동 속도")] [field: SerializeField] 
            public float MoveSpeed { get; private set; } = 10f;
            
            [Tooltip("작동 시간")] [field: SerializeField] 
            public float StaySec { get; private set; } = 3f;
        }
        
        [LineCustom(10, 10, true)]
        [Tooltip("컨베이어 벨트 콜라이더 - Trigger용")] [SerializeField]
        private BoxCollider _conveyerBeltColliderTrigger;
        
        [Tooltip("컨베이어 벨트 렌더러")] [SerializeField, Space(5)]
        private MeshRenderer _conveyerBeltRenderer;
        
        
        private static readonly float _speedMultiplier = 20f;  // KCC Input이 없을 때, (Velocity == 0) 이므로 보정계수 필요 

        private int _currentPatternIndex = 0;
        private Vector3 _currentMoveDirection = Vector3.zero;
        private float _currentSpeed = 0f;

        
        // ==================================================
        // [LOGIC] Initialization
        // ==================================================
        protected override void AwakeProp()
        {
            _currentPatternIndex = 0;
            RebuildConveyerBeltCollider();
        }

        
        // ==================================================
        // [LOGIC] CORE
        // ==================================================
        protected override async UniTask Actuate()
        {
            await UniTask.WaitForSeconds(_warmupSec, false, PlayerLoopTiming.Update, destroyCancellationToken);
            
            _currentPatternIndex = 0;
            if (!IsIndexValid(_currentPatternIndex))
            {
                CurrentState = GimmickPropState.Ready;
                return;
            }
            
            _conveyerBeltColliderTrigger.enabled = true;
            
            while (!IsCancelReserved)
            {
                MovePattern currentPattern = GetMovePattern(_currentPatternIndex);
                _currentMoveDirection = GetMoveDirection(currentPattern.MoveAngle);
                _currentSpeed = currentPattern.MoveSpeed;
                
                await UniTask.WaitForSeconds(currentPattern.StaySec, false, PlayerLoopTiming.Update, destroyCancellationToken);

                int nextPatternIndex = _currentPatternIndex + 1;
                if (!IsIndexValid(nextPatternIndex))
                {
                    if (_repeatMode == GimmickPropRepeatMode.OneShot)
                        break;
                    else
                        nextPatternIndex = 0;
                }
                _currentPatternIndex = nextPatternIndex;
            }

            _conveyerBeltColliderTrigger.enabled = false;
            CurrentState = GimmickPropState.Ready;
        }

        private void OnTriggerStay(Collider other)
        {
            if (CurrentState == GimmickPropState.Actuating && other.gameObject.layer == Layer.LAYER_PLAYER)
            {
                LocalCharacter.Instance.AddVelocity(_currentMoveDirection * _currentSpeed * _speedMultiplier * Time.deltaTime);
            }
        }
        
        
        // ==================================================
        // [LOGIC] UTILITY
        // ==================================================
        private MovePattern GetMovePattern(int index) => (index >= _movePatterns.Count) ? null : _movePatterns[index];
        private Vector3 GetMoveDirection(float moveAngle) => Quaternion.AngleAxis(moveAngle, transform.up) * transform.forward;
        private bool IsIndexValid(int index) => index >= 0 && index < _movePatterns.Count;


        [ContextMenu("Rebuild ConveyerBelt Collider")]
        private void RebuildConveyerBeltCollider()
        {
            _conveyerBeltColliderTrigger.SetSizeAsMesh(_conveyerBeltRenderer, 1f, 0.1f, 1f);
            var meshWorldPos = _conveyerBeltRenderer.transform.TransformPoint(_conveyerBeltRenderer.localBounds.center.x, 
                                                                                      _conveyerBeltRenderer.localBounds.max.y,
                                                                                      _conveyerBeltRenderer.localBounds.center.z);
            var centerPos = _conveyerBeltColliderTrigger.transform.InverseTransformPoint(meshWorldPos);
            _conveyerBeltColliderTrigger.center = new Vector3(0, centerPos.y, 0f);  // mesh의 상단 표면에 얹음
        }
        
        
#if UNITY_EDITOR
        #region Gizmo Drawing
private void OnDrawGizmos()
        {
            if (_movePatterns == null || _movePatterns.Count == 0) return;

            // 1. 배치 설정 (기존의 절반 수준)
            float columnSpacing = 1.25f; // 가로 간격 (2.5 -> 1.25)
            float rowSpacing = 1.25f;    // 세로 간격 (2.5 -> 1.25)
            int itemsPerRow = 4;        

            // 벨트 표면에서 살짝만 띄움
            Vector3 baseOrigin = transform.position + (Vector3.up * 0.8f); 
            
            Vector3 startPos = baseOrigin 
                               - (transform.right * (columnSpacing * (itemsPerRow - 1) * 0.5f))
                               + (transform.forward * (rowSpacing * (Mathf.CeilToInt(_movePatterns.Count / (float)itemsPerRow) - 1) * 0.5f));

            for (int i = 0; i < _movePatterns.Count; i++)
            {
                var pattern = _movePatterns[i];
                if (pattern == null) continue;

                int row = i / itemsPerRow;
                int col = i % itemsPerRow;

                Vector3 slotPos = startPos 
                                  + (transform.right * (col * columnSpacing)) 
                                  - (transform.forward * (row * rowSpacing));

                bool isCurrent = (Application.isPlaying && i == _currentPatternIndex);
                Color mainColor = isCurrent ? Color.red : Color.yellow;
                
                Vector3 worldDir = GetMoveDirection(pattern.MoveAngle);
                
                // 2. 화살표 크기도 절반으로 축소
                DrawDetailedPattern(slotPos, worldDir, mainColor, isCurrent, i, pattern);
                
                Gizmos.color = mainColor;
                Gizmos.DrawSphere(slotPos, 0.03f); // 포인트 점도 더 작게
            }
        }

        private void DrawDetailedPattern(Vector3 pos, Vector3 direction, Color color, bool highlight, int index, MovePattern pattern)
        {
            UnityEditor.Handles.color = color;
            // 스케일 기본값을 0.5로 낮춰서 전체적으로 작게 만듦
            float baseScale = 0.5f; 
            float scale = highlight ? baseScale * 1.5f : baseScale;
            
            float lineLength = 1.0f * scale;
            Vector3 endPos = pos + direction * lineLength;

            // --- 화살표 그리기 ---
            UnityEditor.Handles.DrawLine(pos, endPos);
            float headSize = 0.3f * scale;
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
            UnityEditor.Handles.DrawLine(endPos, endPos + right * headSize);
            UnityEditor.Handles.DrawLine(endPos, endPos + left * headSize);

            // --- 정보 텍스트 (줄어든 간격에 맞춰 폰트도 조정) ---
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = highlight ? 11 : 9; // 폰트 사이즈 하향
            labelStyle.fontStyle = highlight ? FontStyle.Bold : FontStyle.Normal;
            labelStyle.alignment = TextAnchor.UpperCenter;

            // 텍스트가 화살표랑 너무 겹치지 않게 오프셋 조정
            string infoText = $"<color=white>[{index}]</color> {pattern.MoveSpeed:F1}m/s ({pattern.StaySec:F1}s)";

            // 텍스트를 화살표 시작점 아래쪽으로 배치하여 시야 확보
            UnityEditor.Handles.Label(pos - (Vector3.up * 0.1f), infoText, labelStyle);
        }
        
        #endregion
#endif
    }
}
