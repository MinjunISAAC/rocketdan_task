// ----- System
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;


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
        [SerializeField] protected Collider2D _myCollider = null;

        [Header("2. 이동 설정")]
        [SerializeField] protected float _idleMoveSpeed = 2.0f;

        [Header("3. 점프 설정")]
        [SerializeField] protected float _jumpForce = 5.0f;
        [SerializeField] protected float _jumpHorizontalSpeed = 2.0f;
        [SerializeField] protected float _pushForce = 3.0f; 
        [SerializeField] protected LayerMask _groundLayerMask = -1;
        [SerializeField] protected float _groundCheckDistance = 0.5f;

        [Header("4. 공격 설정")]
        [SerializeField] protected float _attackCooldown = 2.0f;

        [Header("4. 애니메이션 설정")]
        [SerializeField] protected Animator _animator = null;

        [Header("5. 레이 캐스트 설정")]
        [SerializeField] protected float _raycastRange = 0.75f;
        [SerializeField] protected float _raycastHeight = 0.5f;
        [SerializeField] protected float _attackRaycastRange = 1.0f;
        [SerializeField] protected float _attackRaycastHeight = 0.3f;
        [SerializeField] protected float _attackRaycastAngle = 0f; // -45도 ~ 45도

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const string ANIM_IDLE = "Idle";
        private const string ANIM_JUMP = "Jump";
        private const string ANIM_ATTACK = "Attack";
        private const string ANIM_HIT = "Hit";
        private const string ANIM_DIE = "Die";

        private const float JUMP_COOLDOWN_TIME = 0.5f;
        private const int MAX_LAYER_INDEX = 3;
        
        private EEnemyType _enemyType = EEnemyType.Unknown;
        private int _layerIndex = 0;

        protected EEnemyState _currState = EEnemyState.Unknown;
        protected EEnemyState _prevState = EEnemyState.Unknown;

        protected Coroutine _coState = null;

        protected RaycastHit2D _raycastHit;
        protected bool _hasObject = false;
        protected bool _hasCapsuleCollider = false;
        protected float _colliderHeight = 0f;
        
        protected RaycastHit2D _attackRaycastHit;
        protected bool _hasAttackTarget = false;

        protected bool _isJumping = false;
        protected float _jumpStartTime = 0f;
        protected float _jumpCurrentHeight = 0f;

        protected bool _isGrounded = false;

        protected float _jumpTime = 0f;
        protected bool _isJumpDelayed = false;
        
        protected float _attackTime = 0f;
        protected bool _isAttackDelayed = false;

        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public EEnemyState CurrentState => _currState;

        // --------------------------------------------------
        // Method - Events
        // --------------------------------------------------
        // ----- 사용 중
        protected virtual void Start()
        {
            ChangeState(EEnemyState.Idle, null);
        }

        protected virtual void Update()
        {
            CheckForwardObject();
            CheckAttackTarget();
            CheckJumpReady();
            CheckAttackReady();
        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void OnDestroy()
        {

        }

        // --------------------------------------------------
        // Method - Normal
        // --------------------------------------------------
        #region [Spawn]
        public void Spawn(EEnemyType enemyType, int layerIndex, Transform spawnPosition, Transform enemyParent)
        {
            _enemyType = enemyType;
            _layerIndex = layerIndex;

            SetObjectLayer(layerIndex);
            SetGroundLayerMask(layerIndex);
            SetRigidbodyLayer(layerIndex);
            SetSpriteRendererOrder(layerIndex);

            transform.position = new Vector3(spawnPosition.position.x, spawnPosition.position.y, layerIndex);
            transform.rotation = spawnPosition.rotation;

            transform.SetParent(enemyParent);
        }

        private void SetObjectLayer(int layerIndex)
        {
            var layerName = $"EnemyLine_{layerIndex}";
            var layerNumber = LayerMask.NameToLayer(layerName);
            
            if (layerNumber == -1)
                return;

            gameObject.layer = layerNumber;
        }

        private void SetGroundLayerMask(int layerIndex)
        {
            var groundLayerName = $"Ground_{layerIndex}";
            var groundLayerNumber = LayerMask.NameToLayer(groundLayerName);
            
            if (groundLayerNumber == -1)
                return;

            _groundLayerMask = 1 << groundLayerNumber;
        }

        private void SetRigidbodyLayer(int layerIndex)
        {
            if (_rigidbody2D == null)
                return;

            var excludeLayers = new List<int>();

            for (int i = 0; i < MAX_LAYER_INDEX; i++)
            {
                if (i != layerIndex)
                {
                    var enemyLayerName = $"EnemyLine_{i}";
                    var enemyLayerNumber = LayerMask.NameToLayer(enemyLayerName);
                    if (enemyLayerNumber != -1)
                        excludeLayers.Add(enemyLayerNumber);
                }
            }

            for (int i = 0; i < MAX_LAYER_INDEX; i++)
            {
                if (i != layerIndex)
                {
                    var groundLayerName = $"Ground_{i}";
                    var groundLayerNumber = LayerMask.NameToLayer(groundLayerName);
                    if (groundLayerNumber != -1)
                        excludeLayers.Add(groundLayerNumber);
                }
            }

            foreach (var excludeLayer in excludeLayers)
            {
                var currentLayer = gameObject.layer;
                Physics2D.IgnoreLayerCollision(currentLayer, excludeLayer, true);
            }
        }

        private void SetSpriteRendererOrder(int layerIndex)
        {
            var orderOffset = layerIndex * 10;
            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

            foreach (var spriteRenderer in spriteRenderers)
            {
                var originalOrder = spriteRenderer.sortingOrder;
                spriteRenderer.sortingOrder = originalOrder + (30 - orderOffset);
            }
        }
        #endregion

        #region [State]
        public void ChangeState(EEnemyState state, Action doneCallBack)
        {
            if (_currState == state)
                return;

            if (_coState != null)
            {
                StopCoroutine(_coState);
                _coState = null;
            }

            _prevState = _currState;
            _currState = state;

            switch (state)
            {
                case EEnemyState.Idle: _coState = StartCoroutine(Co_IdleState(doneCallBack)); break;
                case EEnemyState.Jump: _coState = StartCoroutine(Co_JumpState(doneCallBack)); break;
                case EEnemyState.Attack: _coState = StartCoroutine(Co_AttackState(doneCallBack)); break;
                case EEnemyState.Hit: _coState = StartCoroutine(Co_HitState(doneCallBack)); break;
                case EEnemyState.Die: _coState = StartCoroutine(Co_DieState(doneCallBack)); break;
            }
        }

        protected virtual IEnumerator Co_IdleState(Action doneCallBack = null)
        {
            _animator.SetTrigger(ANIM_IDLE);

            _isJumping = false;
            _jumpStartTime = 0f;
            _jumpCurrentHeight = 0f;
            _isGrounded = false;

            while (_currState == EEnemyState.Idle)
            {
                var moveDirection = Vector2.left;
                var newVelocity = new Vector2(moveDirection.x * _idleMoveSpeed, _rigidbody2D.velocity.y);
                _rigidbody2D.velocity = newVelocity;

                yield return null;
            }

            _rigidbody2D.velocity = new Vector2(0, _rigidbody2D.velocity.y);
        }

        protected virtual IEnumerator Co_JumpState(Action doneCallBack = null)
        {
            _isJumping = true;
            _jumpStartTime = Time.time;
            _isGrounded = false;

            if (_rigidbody2D != null && _rigidbody2D.gravityScale <= 0)
                _rigidbody2D.gravityScale = 1f;

            if (_rigidbody2D != null)
            {
                var JumpForce = GetJumpForce();
                var jumpVelocity = new Vector2(0f, JumpForce);
                _rigidbody2D.velocity = jumpVelocity;

                PushEnemy();
            }

            var jumpStartY = transform.position.y;
            var elapsed = 0f;
            var maxJumpTime = 2.0f;
            var isRising = true;
            var hasPeaked = false;
            var horizontalSpeedApplied = false;

            _animator.SetTrigger(ANIM_JUMP);

            while (_currState == EEnemyState.Jump && elapsed < maxJumpTime)
            {
                elapsed += Time.deltaTime;

                if (_rigidbody2D != null)
                {
                    var currentY = transform.position.y;
                    var velocityY = _rigidbody2D.velocity.y;

                    _jumpCurrentHeight = currentY - jumpStartY;

                    if (isRising && velocityY <= 0.1f)
                    {
                        isRising = false;
                        hasPeaked = true;
                    }

                    if (!horizontalSpeedApplied && _jumpCurrentHeight >= _colliderHeight)
                    {
                        var currentVelocity = _rigidbody2D.velocity;
                        _rigidbody2D.velocity = new Vector2(-_jumpHorizontalSpeed, currentVelocity.y);
                        horizontalSpeedApplied = true;
                    }

                    if (hasPeaked)
                    {
                        _isGrounded = CheckGround();

                        if (_isGrounded)
                            break;
                    }

                    if (currentY < jumpStartY - 5f)
                        break;
                }

                yield return null;
            }

            if (_currState == EEnemyState.Jump)
            {
                _jumpTime = Time.time;
                _isJumpDelayed = true;

                _isJumping = false;
                _jumpStartTime = 0f;
                _jumpCurrentHeight = 0f;
                _colliderHeight = 0f;

                _isGrounded = false;

                ChangeState(EEnemyState.Idle, null);
            }
        }

        protected virtual IEnumerator Co_AttackState(Action doneCallBack = null)
        {
            _animator.SetTrigger(ANIM_ATTACK);
            
            var attackDuration = 1.0f;
            var elapsed = 0f;
            var callbackInvoked = false;

            while (_currState == EEnemyState.Attack && elapsed < attackDuration)
            {
                elapsed += Time.deltaTime;
                
                if (!callbackInvoked && elapsed >= attackDuration * 0.5f)
                {
                    doneCallBack?.Invoke();
                    callbackInvoked = true;
                }
                
                if (_rigidbody2D != null)
                    _rigidbody2D.velocity = new Vector2(0f, _rigidbody2D.velocity.y);
                
                yield return null;
            }
            
            if (_currState == EEnemyState.Attack)
            {
                _attackTime = Time.time;
                _isAttackDelayed = true;
                
                ChangeState(EEnemyState.Idle, null);
            }
        }

        protected virtual IEnumerator Co_HitState(Action doneCallBack = null)
        {
            yield return null;
        }

        protected virtual IEnumerator Co_DieState(Action doneCallBack = null)
        {
            yield return null;
        }
        #endregion

        #region [Jump]
        private bool CheckGround()
        {
            var rayOrigin = transform.position;
            var rayDirection = new Vector2(-1f, -1f).normalized; 

            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, rayDirection, _groundCheckDistance);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    var isGroundLayer = (_groundLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0;
                    var isEnemyBase = hit.collider.gameObject.GetComponent<EnemyBase>() != null;

                    if (isGroundLayer || isEnemyBase)
                        return true;
                }
            }

            return false;
        }

        private void CheckJumpReady()
        {
            if (_isJumpDelayed)
            {
                if (Time.time - _jumpTime >= JUMP_COOLDOWN_TIME)
                    _isJumpDelayed = false;
            }
        }

        private void CheckAttackReady()
        {
            if (_isAttackDelayed)
            {
                if (Time.time - _attackTime >= _attackCooldown)
                    _isAttackDelayed = false;
            }
        }

        private float GetColliderHeight(GameObject targetObject)
        {
            var capsuleCollider = targetObject.GetComponent<CapsuleCollider2D>();
            if (capsuleCollider != null)
                return capsuleCollider.size.y;

            var boxCollider = targetObject.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
                return boxCollider.size.y;

            CircleCollider2D circleCollider = targetObject.GetComponent<CircleCollider2D>();
            if (circleCollider != null)
                return circleCollider.radius * 2f;

            return 1f; 
        }

        private void PushEnemy()
        {
            if (!_hasObject || _raycastHit.collider == null)
                return;

            var targetObject = _raycastHit.collider.gameObject;
            var targetEnemy = targetObject.GetComponent<EnemyBase>();
            if (targetEnemy == null)
                return;

            var targetRigidbody = targetObject.GetComponent<Rigidbody2D>();
            if (targetRigidbody == null)
                return;

            var myColliderHeight = GetMyColliderHeight();
            var colliderHeight = GetTargetColliderHeight(targetObject);

            if (myColliderHeight >= colliderHeight)
            {
                var pushForce = new Vector2(-_pushForce, 0f);
                targetRigidbody.AddForce(pushForce, ForceMode2D.Impulse);
            }
        }

        private float GetMyColliderHeight()
        {
            if (_myCollider is CapsuleCollider2D capsule)
                return capsule.size.y;

            if (_myCollider is BoxCollider2D box)
                return box.size.y;

            if (_myCollider is CircleCollider2D circle)
                return circle.radius * 2f;

            return 1f;
        }

        private float GetTargetColliderHeight(GameObject targetObject)
        {
            var capsuleCollider = targetObject.GetComponent<CapsuleCollider2D>();
            if (capsuleCollider != null)
                return capsuleCollider.size.y;

            var boxCollider = targetObject.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
                return boxCollider.size.y;

            var circleCollider = targetObject.GetComponent<CircleCollider2D>();
            if (circleCollider != null)
                return circleCollider.radius * 2f;

            return 1f;
        }

        private float GetJumpForce()
        {
            if (!_hasObject || _raycastHit.collider == null)
                return _jumpForce; 
                
            var targetObject = _raycastHit.collider.gameObject;
            var targetColliderHeight = GetTargetColliderHeight(targetObject);

            if (targetColliderHeight > 0f)
            {
                var heightMultiplier = 1f + (targetColliderHeight - 1f) * 0.3f;
                var calculatedForce = _jumpForce * heightMultiplier;
                var minForce = _jumpForce * 0.5f;
                var finalForce = Mathf.Max(calculatedForce, minForce);

                return finalForce;
            }

            return _jumpForce;
        }
        #endregion

        #region [Raycast]
        private void CheckForwardObject()
        {
            var rayOrigin = new Vector2(transform.position.x, transform.position.y + _raycastHeight);
            var rayDirection = Vector2.left;

            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, rayDirection, _raycastRange);

            _raycastHit = new RaycastHit2D();
            var isFoundValidHit = false;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    if (IsGroundLayer(hit.collider.gameObject))
                        continue;

                    _raycastHit = hit;
                    isFoundValidHit = true;
                    break;
                }
            }

            var rayEnd = rayOrigin + rayDirection * _raycastRange;

            if (isFoundValidHit)
            {
                var hitObject = _raycastHit.collider.gameObject;
                var targetEnemyBase = hitObject.GetComponent<EnemyBase>();
                if (targetEnemyBase != null)
                {
                    if (IsSameEnemyLayer(hitObject))
                    {
                        _hasObject = true;
                        _colliderHeight = GetColliderHeight(hitObject);

                        if (_currState == EEnemyState.Idle && _colliderHeight > 0f && !_isJumping && !_isJumpDelayed)
                            ChangeState(EEnemyState.Jump, null);
                    }
                    else
                    {
                        _hasObject = false;
                        _hasCapsuleCollider = false;
                        _colliderHeight = 0f;
                    }
                }
                else
                {
                    _hasObject = false;
                    _hasCapsuleCollider = false;
                    _colliderHeight = 0f;
                }
            }
            else
            {
                _hasObject = false;
                _hasCapsuleCollider = false;
                _colliderHeight = 0f;
            }
        }

        private bool IsSameEnemyLayer(GameObject targetObject)
        {
            var targetEnemy = targetObject.GetComponent<EnemyBase>();
            if (targetEnemy == null)
                return false;

            var myLayer = gameObject.layer;
            var targetLayer = targetObject.layer;

            var isSameLayer = myLayer == targetLayer;
            Debug.Log($"{gameObject.name}: IsSameEnemyLayer 체크 - {targetObject.name} (myLayer: {myLayer}, targetLayer: {targetLayer}, isSame: {isSameLayer})");
            
            return isSameLayer;
        }

        private bool IsEnemyLayer(GameObject targetObject)
        {
            var targetEnemy = targetObject.GetComponent<EnemyBase>();
            if (targetEnemy == null)
                return false;

            var targetLayer = targetObject.layer;
            var isEnemyLayer = targetLayer == LayerMask.NameToLayer("EnemyLine_0") ||
                              targetLayer == LayerMask.NameToLayer("EnemyLine_1") ||
                              targetLayer == LayerMask.NameToLayer("EnemyLine_2");

            Debug.Log($"{gameObject.name} -> {targetObject.name}: isEnemyLayer = {isEnemyLayer} (layer: {targetLayer})");
            return isEnemyLayer;
        }

        private void CheckAttackTarget()
        {
            var rayOrigin = new Vector2(transform.position.x, transform.position.y + _attackRaycastHeight);
            
            var angleRad = _attackRaycastAngle * Mathf.Deg2Rad;
            var baseDirection = Vector2.left; 
            var rayDirection = new Vector2(
                baseDirection.x * Mathf.Cos(angleRad) - baseDirection.y * Mathf.Sin(angleRad),
                baseDirection.x * Mathf.Sin(angleRad) + baseDirection.y * Mathf.Cos(angleRad)
            );

            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, rayDirection, _attackRaycastRange);
            _attackRaycastHit = new RaycastHit2D();

            var isAttack = false;
            var boxBase = default(BoxBase);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    if (IsEnemyLayer(hit.collider.gameObject))
                        continue;

                    if (IsGroundLayer(hit.collider.gameObject))
                        continue;

                    boxBase = hit.collider.gameObject.GetComponent<BoxBase>();
                    if (boxBase != null)
                    {
                        _attackRaycastHit = hit;
                        isAttack = true;
                        Debug.Log($"{gameObject.name}: 공격 레이캐스트로 BoxBase 발견! - {hit.collider.gameObject.name} (각도: {_attackRaycastAngle}도)");
                        break;
                    }
                }
            }

            if (isAttack)
            {
                _hasAttackTarget = true;
                
                if (_currState == EEnemyState.Idle && !_isJumping && !_isJumpDelayed && !_isAttackDelayed)
                {
                    ChangeState(EEnemyState.Attack, () => 
                    {
                        if (boxBase != null)
                            boxBase.Hit(10f);
                    });
                }
            }
            else
                _hasAttackTarget = false;
        }

        private bool IsGroundLayer(GameObject targetObject)
        {
            var targetLayer = targetObject.layer;
            var layerName = LayerMask.LayerToName(targetLayer);
            var isGroundLayerByMask = (_groundLayerMask.value & (1 << targetLayer)) != 0;
            var isGroundLayerByName = layerName != null && layerName.StartsWith("Ground");
            var isGroundLayer = isGroundLayerByMask || isGroundLayerByName;
            
            return isGroundLayer;
        }
        #endregion
    }
}