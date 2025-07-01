using UnityEngine;

namespace Game
{
    /// <summary>
    /// EnemyBase 디버깅을 위한 헬퍼 스크립트
    /// </summary>
    public class EnemyDebugger : MonoBehaviour
    {
        [Header("디버깅 설정")]
        [SerializeField] private EnemyBase _targetEnemy = null;
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _raycastColor = Color.green;
        [SerializeField] private Color _hitColor = Color.red;
        [SerializeField] private Color _groundColor = Color.magenta;

        private void OnDrawGizmos()
        {
            if (!_showGizmos || _targetEnemy == null)
                return;

            DrawEnemyDebugInfo();
        }

        private void DrawEnemyDebugInfo()
        {
            var enemyTransform = _targetEnemy.transform;
            var enemyPosition = enemyTransform.position;

            // 1. 앞쪽 레이캐스트 (장애물 감지)
            DrawForwardRaycast(enemyPosition);

            // 2. 바닥 체크 레이캐스트
            DrawGroundRaycast(enemyPosition);

            // 3. 현재 상태 표시
            DrawStateInfo(enemyPosition);
        }

        private void DrawForwardRaycast(Vector3 enemyPosition)
        {
            // EnemyBase의 private 변수들을 reflection으로 접근
            var raycastHeight = GetPrivateField<float>("_raycastHeight");
            var raycastRange = GetPrivateField<float>("_raycastRange");
            var hasObject = GetPrivateField<bool>("_hasObject");
            var raycastHit = GetPrivateField<RaycastHit2D>("_raycastHit");

            var rayOrigin = new Vector3(enemyPosition.x, enemyPosition.y + raycastHeight, enemyPosition.z);
            var rayEnd = rayOrigin + Vector3.left * raycastRange;

            // 레이캐스트 라인
            Gizmos.color = hasObject ? _hitColor : _raycastColor;
            Gizmos.DrawLine(rayOrigin, rayEnd);

            // 시작점과 끝점
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rayOrigin, 0.05f);

            Gizmos.color = hasObject ? _hitColor : _raycastColor;
            Gizmos.DrawWireSphere(rayEnd, 0.05f);

            // 히트 포인트
            if (hasObject && raycastHit.collider != null)
            {
                Gizmos.color = _hitColor;
                Gizmos.DrawWireSphere(raycastHit.point, 0.1f);
            }
        }

        private void DrawGroundRaycast(Vector3 enemyPosition)
        {
            var groundCheckDistance = GetPrivateField<float>("_groundCheckDistance");
            var isGrounded = GetPrivateField<bool>("_isGrounded");

            var groundRayOrigin = enemyPosition;
            var groundRayDirection = new Vector3(-1f, -1f, 0f).normalized; // 왼쪽 아래 45도
            var groundRayEnd = groundRayOrigin + groundRayDirection * groundCheckDistance;

            Gizmos.color = isGrounded ? _groundColor : Color.gray;
            Gizmos.DrawLine(groundRayOrigin, groundRayEnd);

            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            Gizmos.DrawWireSphere(groundRayOrigin, 0.03f);

            Gizmos.color = isGrounded ? _groundColor : Color.gray;
            Gizmos.DrawWireSphere(groundRayEnd, 0.03f);
        }

        private void DrawStateInfo(Vector3 enemyPosition)
        {
            var currentState = _targetEnemy.CurrentState;
            var isJumping = GetPrivateField<bool>("_isJumping");
            var isJumpDelayed = GetPrivateField<bool>("_isJumpDelayed");

            // 상태 정보를 월드 좌표에 표시
            var infoPosition = enemyPosition + Vector3.up * 2f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(infoPosition, 
                $"State: {currentState}\n" +
                $"Jumping: {isJumping}\n" +
                $"JumpDelayed: {isJumpDelayed}");
            #endif
        }

        private T GetPrivateField<T>(string fieldName)
        {
            var field = typeof(EnemyBase).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
                return (T)field.GetValue(_targetEnemy);
            
            return default(T);
        }

        [ContextMenu("Set Target Enemy")]
        private void SetTargetEnemy()
        {
            _targetEnemy = GetComponent<EnemyBase>();
            if (_targetEnemy == null)
                _targetEnemy = GetComponentInParent<EnemyBase>();
        }
    }
} 