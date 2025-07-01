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

        [Header("4. 애니메이션 설정")]
        [SerializeField] protected Animator _animator = null;

        [Header("5. 레이 캐스트 설정")]
        [SerializeField] protected float _raycastRange = 0.75f;
        [SerializeField] protected float _raycastHeight = 0.5f;


        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const string ANIM_IDLE = "Idle";
        private const string ANIM_JUMP = "Jump";
        private const string ANIM_ATTACK = "Attack";
        private const string ANIM_HIT = "Hit";
        private const string ANIM_DIE = "Die";

        private const float JUMP_COOLDOWN_TIME = 0.5f;

        protected EEnemyState _currState = EEnemyState.Unknown;
        protected EEnemyState _prevState = EEnemyState.Unknown;

        protected Coroutine _coState = null;

        protected RaycastHit2D _raycastHit;
        protected bool _hasObject = false;
        protected bool _hasCapsuleCollider = false;
        protected float _colliderHeight = 0f;

        protected bool _isJumping = false;
        protected float _jumpStartTime = 0f;
        protected float _jumpCurrentHeight = 0f;

        protected bool _isGrounded = false;

        protected float _jumpTime = 0f;
        protected bool _isJumpDelayed = false;

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
            CheckJumpReady();
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

        protected virtual IEnumerator Co_JumpState()
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

        protected virtual IEnumerator Co_AttackState()
        {
            yield return null;
        }

        protected virtual IEnumerator Co_HitState()
        {
            yield return null;
        }

        protected virtual IEnumerator Co_DieState()
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
        #endregion
    }
}