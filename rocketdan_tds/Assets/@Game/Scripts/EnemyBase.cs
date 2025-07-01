// ----- System
using System;
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class EnemyBase : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [Header("1. 물리 컴포넌트 그룹")]
        [SerializeField] protected Rigidbody2D _rigidbody2D = null;

        [Header("2. 이동 설정")]
        [SerializeField] protected float _idleMoveSpeed = 2.0f;
        
        [Header("3. 점프 설정")]
        [SerializeField] protected float _jumpForce = 5.0f;
        [SerializeField] protected bool _autoJumpOnObstacle = true; // 장애물 감지 시 자동 점프
        [SerializeField] protected float _jumpHeightMargin = 0.5f; // 점프 높이 여유분
        [SerializeField] protected float _maxJumpHeight = 5.0f; // 최대 점프 높이 제한
        [SerializeField] protected float _landingStabilityTime = 0.4f; // 착지 안정성 확인 시간
        [SerializeField] protected float _landingStabilityThreshold = 0.1f; // 착지 안정성 높이 임계값
        [SerializeField] protected float _jumpCooldownTime = 1.0f; // 점프 후 재점프 딜레이 시간
   
        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        [SerializeField] protected EEnemyState _currState = EEnemyState.Unknown;
        [SerializeField] protected EEnemyState _prevState = EEnemyState.Unknown;

        protected Coroutine _coState = null;

        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public EEnemyState CurrentState => _currState;

        // --------------------------------------------------
        // Method - Events
        // --------------------------------------------------
        protected virtual void Start()
        {
            // 초기화 시 쿨다운 없음
            _lastJumpCompletedTime = 0f;
            _isJumpOnCooldown = false;
            
            ChangeState(EEnemyState.Idle, null);
        }



        protected virtual void OnDisable() 
        {

        }

        protected virtual void OnDestroy()
        {

        }



        // --------------------------------------------------
        // Method - Normals
        // --------------------------------------------------
        #region [State]
        public void ChangeState(EEnemyState state, Action doneCallBack)
        {
            if (_currState == state)
                return;

            // 기존 상태 코루틴 정지
            if (_coState != null)
            {
                StopCoroutine(_coState);
                _coState = null;
            }

            _prevState = _currState;
            _currState = state;

            switch (state)
            {
                case EEnemyState.Idle: _coState = StartCoroutine(Co_IdleState()); break;
                case EEnemyState.Jump: _coState = StartCoroutine(Co_JumpState()); break;
                case EEnemyState.Attack: _coState = StartCoroutine(Co_AttackState()); break;
                case EEnemyState.Hit: _coState = StartCoroutine(Co_HitState()); break;
                case EEnemyState.Die: _coState = StartCoroutine(Co_DieState()); break;
            }

            doneCallBack?.Invoke();
        }

        protected virtual IEnumerator Co_IdleState()
        {
            // Idle 상태 진입 시 점프 상태 리셋 (쿨다운은 유지)
            _isJumping = false;
            _jumpStartTime = 0f;
            _jumpCurrentHeight = 0f;
            
            // 안정성 체크 리셋
            _lastStableY = 0f;
            _stableStartTime = 0f;
            _isCheckingStability = false;
            
            while (_currState == EEnemyState.Idle)
            {
                Vector2 moveDirection = Vector2.left;
                _rigidbody2D.velocity = new Vector2(moveDirection.x * _idleMoveSpeed, _rigidbody2D.velocity.y);
                
                yield return null;
            }
            
            _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
        }

        protected virtual IEnumerator Co_JumpState()
        {
            // 점프 상태 초기화
            _isJumping = true;
            _jumpStartTime = Time.time;
            
            // 안정성 체크 초기화
            _lastStableY = 0f;
            _stableStartTime = 0f;
            _isCheckingStability = false;
            
            // 중력 설정 확인 및 경고
            if (_rigidbody2D != null && _rigidbody2D.gravityScale <= 0)
            {
                _rigidbody2D.gravityScale = 1f;
            }
            
            // 점프 실행 (위쪽으로 velocity 설정)
            if (_rigidbody2D != null)
            {
                // 콜라이더 높이 기반 점프 힘 계산
                float targetJumpForce = CalculateJumpForceForHeight(_colliderHeight + _jumpHeightMargin);
                
                // 점프 시에는 X축 이동 유지하면서 Y축에만 점프 힘 적용
                Vector2 jumpVelocity = new Vector2(-_idleMoveSpeed, targetJumpForce);
                _rigidbody2D.velocity = jumpVelocity;
            }
            
            // 개선된 착지 감지 시스템
            float jumpStartY = transform.position.y;
            float elapsed = 0f;
            float maxJumpTime = 2.0f; // 최대 2초로 단축
            bool isRising = true;
            bool hasPeaked = false;
            
            // 안정성 체크 초기화
            _lastStableY = jumpStartY;
            _stableStartTime = 0f;
            _isCheckingStability = false;
            
            while (_currState == EEnemyState.Jump && elapsed < maxJumpTime)
            {
                elapsed += Time.deltaTime;
                
                if (_rigidbody2D != null)
                {
                    float currentY = transform.position.y;
                    float velocityY = _rigidbody2D.velocity.y;
                    
                    // 실시간 점프 높이 업데이트
                    _jumpCurrentHeight = currentY - jumpStartY;
                    
                    // 상승 중에서 하강으로 전환 감지 (최고점 도달)
                    if (isRising && velocityY <= 0.1f)
                    {
                        isRising = false;
                        hasPeaked = true;
                    }
                    
                    // 착지 감지 방법 1: 시작 높이 근처로 돌아옴
                    if (hasPeaked && currentY <= jumpStartY + 0.2f)
                    {
                        break;
                    }
                    
                    // 착지 감지 방법 2: 높이 안정성 체크 (상자 위 착지 등)
                    if (hasPeaked)
                    {
                        float heightDifference = Mathf.Abs(currentY - _lastStableY);
                        
                        if (heightDifference <= _landingStabilityThreshold)
                        {
                            // 높이가 안정적이면 타이머 시작/지속
                            if (!_isCheckingStability)
                            {
                                _isCheckingStability = true;
                                _stableStartTime = Time.time;
                            }
                            
                            // 설정된 시간 동안 안정되었으면 착지로 인정
                            if (Time.time - _stableStartTime >= _landingStabilityTime)
                            {
                                break;
                            }
                        }
                        else
                        {
                            // 높이가 변하면 안정성 체크 리셋
                            if (_isCheckingStability)
                            {
                                _isCheckingStability = false;
                            }
                            _lastStableY = currentY;
                        }
                    }
                    
                    // 응급 상황: 너무 아래로 떨어지면 강제 종료
                    if (currentY < jumpStartY - 1f)
                    {
                        break;
                    }
                }
                
                yield return null;
            }
            
            // 점프 완료 후 Idle로 복귀
            if (_currState == EEnemyState.Jump)
            {
                // 점프 쿨다운 시작
                _lastJumpCompletedTime = Time.time;
                _isJumpOnCooldown = true;
                
                // 점프 상태 리셋
                _isJumping = false;
                _jumpStartTime = 0f;
                _jumpCurrentHeight = 0f;
                _colliderHeight = 0f;
                
                // 안정성 체크 리셋
                _lastStableY = 0f;
                _stableStartTime = 0f;
                _isCheckingStability = false;
                
                ChangeState(EEnemyState.Idle, null);
            }
        }

        protected virtual IEnumerator Co_AttackState()
        {
            Debug.Log($"[{gameObject.name}] Attack State Started");
            yield return null;
        }

        protected virtual IEnumerator Co_HitState()
        {
            Debug.Log($"[{gameObject.name}] Hit State Started");
            yield return null;
        }

        protected virtual IEnumerator Co_DieState()
        {
            Debug.Log($"[{gameObject.name}] Die State Started");
            yield return null;
        }
        #endregion

        // --------------------------------------------------
        // Method - Raycast Detection
        // --------------------------------------------------
        [Header("[레이캐스트 감지]")]
        [SerializeField] protected float _raycastDistance = 0.75f; // 레이캐스트 거리
        [SerializeField] protected float _raycastHeightOffset = 0.5f; // 레이캐스트 높이 오프셋
        [SerializeField] protected LayerMask _obstacleLayerMask = -1; // 감지할 레이어

        // Raycast Variables
        protected RaycastHit2D _raycastHit;
        protected bool _hasObstacle = false;
        protected bool _hasCapsuleCollider = false;
        protected float _colliderHeight = 0f;
        
        // Jump Status Variables  
        protected bool _isJumping = false;
        protected float _jumpStartTime = 0f;
        protected float _jumpCurrentHeight = 0f;
        
        // Landing Detection Variables
        protected float _lastStableY = 0f;
        protected float _stableStartTime = 0f;
        protected bool _isCheckingStability = false;
        
        // Jump Cooldown Variables
        protected float _lastJumpCompletedTime = 0f;
        protected bool _isJumpOnCooldown = false;

        protected virtual void Update()
        {
            CheckObstacleWithRaycast();
            UpdateJumpCooldown();
        }
        
        private void UpdateJumpCooldown()
        {
            // 점프 쿨다운 체크
            if (_isJumpOnCooldown)
            {
                if (Time.time - _lastJumpCompletedTime >= _jumpCooldownTime)
                {
                    _isJumpOnCooldown = false;
                }
            }
        }

        private void CheckObstacleWithRaycast()
        {
            // 레이캐스트 시작점 (높이 오프셋 적용)
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y + _raycastHeightOffset);
            Vector2 rayDirection = Vector2.left;
            
            // 모든 충돌체를 가져와서 자신을 제외한 첫 번째 오브젝트 찾기
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, rayDirection, _raycastDistance, _obstacleLayerMask);
            
            // 자신이 아닌 첫 번째 오브젝트 찾기
            _raycastHit = new RaycastHit2D();
            bool foundValidHit = false;
            
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    _raycastHit = hit;
                    foundValidHit = true;
                    break;
                }
            }
            
            Vector2 rayEnd = rayOrigin + rayDirection * _raycastDistance;
            
            if (foundValidHit)
            {
                _hasObstacle = true;
                GameObject hitObject = _raycastHit.collider.gameObject;
                
                AnalyzeCollider(hitObject);
                
                if (_autoJumpOnObstacle && _currState == EEnemyState.Idle && _colliderHeight > 0f && !_isJumping && !_isJumpOnCooldown)
                    ChangeState(EEnemyState.Jump, null);
            }
            else
            {
                _hasObstacle = false;
                _hasCapsuleCollider = false;
                _colliderHeight = 0f;
            }
        }

        private float CalculateJumpForceForHeight(float targetHeight)
        {
            if (targetHeight <= 0f)
                return _jumpForce;
            
            // 최대 점프 높이 제한 적용
            if (targetHeight > _maxJumpHeight)
                return _jumpForce;
            
            float gravity = Mathf.Abs(Physics2D.gravity.y) * _rigidbody2D.gravityScale;
            float calculatedForce = Mathf.Sqrt(2f * gravity * targetHeight);
            
            float minForce = 2f;
            float finalForce = Mathf.Max(calculatedForce, minForce);
            
            return finalForce;
        }

        private void AnalyzeCollider(GameObject targetObject)
        {
            CapsuleCollider2D capsuleCollider = targetObject.GetComponent<CapsuleCollider2D>();
            _hasCapsuleCollider = capsuleCollider != null;
            
            if (_hasCapsuleCollider)
            {
                _colliderHeight = capsuleCollider.size.y;
                return;
            }
            
            BoxCollider2D boxCollider = targetObject.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                _colliderHeight = boxCollider.size.y;
                return;
            }
            
            CircleCollider2D circleCollider = targetObject.GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                _colliderHeight = circleCollider.radius * 2f; // 지름 = 높이
                return;
            }
            
            PolygonCollider2D polygonCollider = targetObject.GetComponent<PolygonCollider2D>();
            if (polygonCollider != null)
            {
                _colliderHeight = polygonCollider.bounds.size.y;
                return;
            }
            
            EdgeCollider2D edgeCollider = targetObject.GetComponent<EdgeCollider2D>();
            if (edgeCollider != null)
            {
                _colliderHeight = edgeCollider.bounds.size.y;
                return;
            }
            
            Collider2D[] allColliders = targetObject.GetComponents<Collider2D>();
            if (allColliders.Length > 0)
            {
                float maxHeight = 0f;
                
                foreach (var col in allColliders)
                {
                    if (col.bounds.size.y > maxHeight)
                        maxHeight = col.bounds.size.y;
                }
                
                _colliderHeight = maxHeight;
            }
            else
            {
                _colliderHeight = 0f;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {

            Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + _raycastHeightOffset, transform.position.z);
            Vector3 rayEnd = rayOrigin + Vector3.left * _raycastDistance;
            
            Gizmos.color = _hasObstacle ? Color.red : Color.green;
            Gizmos.DrawLine(rayOrigin, rayEnd);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rayOrigin, 0.05f);
            
            Gizmos.color = _hasObstacle ? Color.red : Color.green;
            Gizmos.DrawWireSphere(rayEnd, 0.05f);
            
            if (_hasObstacle && _raycastHit.collider != null)
            {
                Gizmos.color = _hasCapsuleCollider ? Color.yellow : Color.red;
                Gizmos.DrawWireSphere(_raycastHit.point, 0.1f);
                
                if (_hasCapsuleCollider)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(_raycastHit.point, Vector3.one * 0.2f);
                }
            }
        }
#endif
    }
}