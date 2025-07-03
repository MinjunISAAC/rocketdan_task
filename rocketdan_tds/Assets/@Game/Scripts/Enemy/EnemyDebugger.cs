#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Game
{
    public class EnemyDebugger : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [SerializeField] private EnemyBase _targetEnemy = null;
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _raycastColor = Color.green;
        [SerializeField] private Color _hitColor = Color.red;
        [SerializeField] private Color _groundColor = Color.magenta;
        [SerializeField] private Color _attackRaycastColor = Color.yellow;
        [SerializeField] private Color _attackHitColor = new Color(1f, 0.5f, 0f);

        // --------------------------------------------------
        // Methods - Event
        // --------------------------------------------------
        private void OnDrawGizmos()
        {
            if (!_showGizmos || _targetEnemy == null)
                return;

            DrawEnemyDebugInfo();
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        private void DrawEnemyDebugInfo()
        {
            var enemyTransform = _targetEnemy.transform;
            var enemyPosition = enemyTransform.position;

            DrawForwardRaycast(enemyPosition);
            DrawAttackRaycast(enemyPosition);
            DrawGroundRaycast(enemyPosition);
            DrawStateInfo(enemyPosition);
        }

        private void DrawForwardRaycast(Vector3 enemyPosition)
        {
            try
            {
                var raycastHeight = GetPrivateField<float>("_raycastHeight");
                var raycastRange = GetPrivateField<float>("_raycastRange");
                var hasObject = GetPrivateField<bool>("_hasObject");
                var raycastHit = GetPrivateField<RaycastHit2D>("_raycastHit");
                var rayOrigin = new Vector3(enemyPosition.x, enemyPosition.y + raycastHeight, enemyPosition.z);
                var rayEnd = rayOrigin + Vector3.left * raycastRange;

                Gizmos.color = hasObject ? _hitColor : _raycastColor;
                Gizmos.DrawLine(rayOrigin, rayEnd);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(rayOrigin, 0.05f);

                Gizmos.color = hasObject ? _hitColor : _raycastColor;
                Gizmos.DrawWireSphere(rayEnd, 0.05f);

                if (hasObject && raycastHit.collider != null)
                {
                    Gizmos.color = _hitColor;
                    Gizmos.DrawWireSphere(raycastHit.point, 0.1f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"DrawForwardRaycast 오류: {e.Message}");
            }
        }

        private void DrawAttackRaycast(Vector3 enemyPosition)
        {
            try
            {
                var attackRaycastHeight = GetPrivateField<float>("_attackRaycastHeight");
                var attackRaycastRange = GetPrivateField<float>("_attackRaycastRange");
                var attackRaycastAngle = GetPrivateField<float>("_attackRaycastAngle");
                var hasAttackTarget = GetPrivateField<bool>("_hasAttackTarget");
                var attackRaycastHit = GetPrivateField<RaycastHit2D>("_attackRaycastHit");
                var rayOrigin = new Vector3(enemyPosition.x, enemyPosition.y + attackRaycastHeight, enemyPosition.z);
                var angleRad = attackRaycastAngle * Mathf.Deg2Rad;
                var baseDirection = Vector2.left; // 기본 방향 (왼쪽)
                var rotatedDirection = new Vector2(
                    baseDirection.x * Mathf.Cos(angleRad) - baseDirection.y * Mathf.Sin(angleRad),
                    baseDirection.x * Mathf.Sin(angleRad) + baseDirection.y * Mathf.Cos(angleRad)
                );
                var rayDirection = new Vector3(rotatedDirection.x, rotatedDirection.y, 0f);
                var rayEnd = rayOrigin + rayDirection * attackRaycastRange;

                Gizmos.color = hasAttackTarget ? _attackHitColor : _attackRaycastColor;
                Gizmos.DrawLine(rayOrigin, rayEnd);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(rayOrigin, 0.04f);

                Gizmos.color = hasAttackTarget ? _attackHitColor : _attackRaycastColor;
                Gizmos.DrawWireSphere(rayEnd, 0.04f);

                if (hasAttackTarget && attackRaycastHit.collider != null)
                {
                    Gizmos.color = _attackHitColor;
                    Gizmos.DrawWireSphere(attackRaycastHit.point, 0.08f);
                }

                #if UNITY_EDITOR
                var infoPosition = rayOrigin + Vector3.up * 0.5f;
                Handles.Label(infoPosition, $"Attack\nAngle: {attackRaycastAngle:F1}°");
                #endif
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"DrawAttackRaycast 오류: {e.Message}");
            }
        }

        private void DrawGroundRaycast(Vector3 enemyPosition)
        {
            try
            {
                var groundCheckDistance = GetPrivateField<float>("_groundCheckDistance");
                var isGrounded = GetPrivateField<bool>("_isGrounded");

                var groundRayOrigin = enemyPosition;
                var groundRayDirection = new Vector3(0f, -1f, 0f).normalized; // 왼쪽 아래 45도
                var groundRayEnd = groundRayOrigin + groundRayDirection * groundCheckDistance;

                Gizmos.color = isGrounded ? _groundColor : Color.gray;
                Gizmos.DrawLine(groundRayOrigin, groundRayEnd);

                Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
                Gizmos.DrawWireSphere(groundRayOrigin, 0.03f);

                Gizmos.color = isGrounded ? _groundColor : Color.gray;
                Gizmos.DrawWireSphere(groundRayEnd, 0.03f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"DrawGroundRaycast 오류: {e.Message}");
            }
        }

        private void DrawStateInfo(Vector3 enemyPosition)
        {
            try
            {
                var currentState = _targetEnemy.CurrentState;
                var isJumping = GetPrivateField<bool>("_isJumping");
                var isJumpDelayed = GetPrivateField<bool>("_isJumpDelayed");
                var hasAttackTarget = GetPrivateField<bool>("_hasAttackTarget");
                var infoPosition = enemyPosition + Vector3.up * 2f;
                
                #if UNITY_EDITOR
                Handles.Label(infoPosition, 
                    $"State: {currentState}\n" +
                    $"Jumping: {isJumping}\n" +
                    $"JumpDelayed: {isJumpDelayed}\n" +
                    $"AttackTarget: {hasAttackTarget}");
                #endif
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"DrawStateInfo 오류: {e.Message}");
            }
        }

        private T GetPrivateField<T>(string fieldName)
        {
            try
            {
                var field = typeof(EnemyBase).GetField(fieldName, 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null && _targetEnemy != null)
                    return (T)field.GetValue(_targetEnemy);
                
                return default(T);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GetPrivateField 오류 ({fieldName}): {e.Message}");
                return default(T);
            }
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
#endif