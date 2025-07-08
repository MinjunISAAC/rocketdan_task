// ----- System
using System;
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;
using UnityEngine.UI;

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

        [Space(1.5f)]
        [Header("2. 이동 설정")]
        [SerializeField] protected float _idleMoveSpeed = 2.0f;

        [Space(1.5f)]
        [Header("3. 점프 설정")]
        [SerializeField] protected float _jumpForce = 5.0f;
        [SerializeField] protected float _jumpHorizontalSpeed = 2.0f;
        [SerializeField] protected float _pushForce = 3.0f;
        [SerializeField] protected LayerMask _groundLayerMask = -1;
        [SerializeField] protected float _groundCheckDistance = 0.5f;

        [Space(1.5f)]
        [Header("4. 데미지 설정")]
        [SerializeField] protected float _hitKnockbackForce = 2.0f;
        [SerializeField] protected float _hitKnockbackDuration = 0.3f;
        [SerializeField] private UI_DamageFx _damageFx = null;

        [Space(1.5f)]
        [Header("5. 공격 설정")]
        [SerializeField] protected float _attackCooldown = 2.0f;

        [Space(1.5f)]
        [Header("6. 애니메이션 설정")]
        [SerializeField] protected Animator _animator = null;

        [Space(1.5f)]
        [Header("7. 렌더링 그룹")]
        [SerializeField] private List<SpriteRenderer> _spriteRendererList = null;
        [SerializeField] private Material _whiteOutMaterial = null;
        [SerializeField] private GameObject _model = null;
        [SerializeField] private ParticleSystem _dieEffect = null;

        [Space(1.5f)]
        [Header("8. 고유 능력 옵션")]
        [SerializeField] private Slider _hpSlider = null;
        [SerializeField] private Image _IMG_FillShadow = null;

        [Space(1.5f)]
        [Header("9. 레이 캐스트 설정")]
        [SerializeField] protected float _raycastRange = 0.75f;
        [SerializeField] protected float _raycastHeight = 0.5f;
        [SerializeField] protected float _attackRaycastRange = 1.0f;
        [SerializeField] protected float _attackRaycastHeight = 0.3f;
        [SerializeField] protected float _attackRaycastAngle = 0f;

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        protected const string ANIM_IDLE = "Idle";
        protected const string ANIM_JUMP = "Jump";
        protected const string ANIM_ATTACK = "Attack";
        protected const string ANIM_HIT = "Hit";
        protected const string ANIM_DIE = "Die";

        protected const float JUMP_COOLDOWN_TIME = 1f;
        protected const int MAX_LAYER_INDEX = 3;

        protected float _maxHp = 100f;
        protected float _currHp = 100f;

        protected EEnemyType _enemyType = EEnemyType.Unknown;
        protected int _layerIndex = 0;

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

        protected Coroutine _coWhiteEffect = null;
        protected List<Material> _originalMaterialList = new List<Material>();
        protected float _fadeInDuration = 0.1f;
        protected float _fadeOutDuration = 0.3f;

        protected Coroutine _coKnockBack = null;

        private bool _isFirstGround = true;

        // HP 슬라이더 그림자 애니메이션 관련
        private Coroutine _coHpShadowAnimation = null;
        private float _hpShadowAnimationDuration = 0.375f;

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

        protected virtual void FixedUpdate() 
        {
            CheckForwardObject();
            CheckAttackTarget();
            CheckJumpReady();
            CheckAttackReady();
        }

        protected virtual void OnDisable()
        {
            if (_currState == EEnemyState.Jump)
            {
                _jumpTime = Time.time;
                _isJumpDelayed = true;
                _isJumping = false;
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject == gameObject)
                return;

            var enemyBase = other.gameObject.GetComponent<EnemyBase>();
            if (enemyBase != null && IsSameEnemyLayer(other.gameObject) && _currState != EEnemyState.Jump)
                PushEnemyBack(other.gameObject);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            if (other.gameObject == gameObject)
                return;

            var enemyBase = other.gameObject.GetComponent<EnemyBase>();
            if (enemyBase != null && IsSameEnemyLayer(other.gameObject) && _currState != EEnemyState.Jump)
                PushEnemyBack(other.gameObject);
        }

        // --------------------------------------------------
        // Method - Normal
        // --------------------------------------------------
        #region [Spawn]
        public void Spawn(EEnemyType enemyType, int maxHealth, int layerIndex, Transform spawnPosition, Transform enemyParent)
        {
            _enemyType = enemyType;
            _layerIndex = layerIndex;

            _maxHp = maxHealth;
            _currHp = _maxHp;

            SetObjectLayer(layerIndex);
            SetGroundLayerMask(layerIndex);
            SetRigidbodyLayer(layerIndex);
            SetColliderPhysics();
            SetSpriteRendererOrder(layerIndex);
            SetHp();

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

        private void SetColliderPhysics()
        {
            if (_myCollider == null)
                return;

            if (_myCollider is BoxCollider2D boxCollider)
                boxCollider.usedByEffector = false;
            else if (_myCollider is CapsuleCollider2D capsuleCollider)
                capsuleCollider.usedByEffector = false;
            else if (_myCollider is CircleCollider2D circleCollider)
                circleCollider.usedByEffector = false;

            if (_rigidbody2D != null)
            {
                var physicsMaterial = new PhysicsMaterial2D("EnemySuperSlippery");
                physicsMaterial.friction = 0.15f;
                _rigidbody2D.sharedMaterial = physicsMaterial;
            }
        }
        #endregion

        #region [State]
        public void ChangeState(EEnemyState state, Action doneCallBack)
        {
            if (_currState == state)
                return;

            if (_currState == EEnemyState.Jump && state != EEnemyState.Jump)
            {
                _jumpTime = Time.time;
                _isJumpDelayed = true;
                _isJumping = false;
            }

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
            _raycastHit = new RaycastHit2D();
            _hasObject = false;
            _hasCapsuleCollider = false;

            while (_currState == EEnemyState.Idle)
            {
                var moveDirection = Vector2.left;
                var newVelocity = new Vector2(moveDirection.x * _idleMoveSpeed, _rigidbody2D.velocity.y);
                _rigidbody2D.velocity = newVelocity;

                if (!CheckGround() && !_isFirstGround)
                {
                    var pushForce = new Vector2(1f, 10f);
                    _rigidbody2D.AddForce(pushForce, ForceMode2D.Impulse);
                    _isFirstGround = false;
                }

                yield return null;
            }
        }

        protected virtual IEnumerator Co_JumpState(Action doneCallBack = null)
        {
            _isJumping = true;
            _jumpStartTime = Time.time;
            _isGrounded = false;

            if (_rigidbody2D != null && _rigidbody2D.gravityScale <= 0)
                _rigidbody2D.gravityScale = 1f;

            _animator.SetTrigger(ANIM_JUMP);

            var jumpElapsed = 0f;
            var maxJumpTime = 10.0f;
            var objectDisappeared = false;
            var originalHasObject = _hasObject;
            var originalRaycastHit = _raycastHit;

            while (_currState == EEnemyState.Jump && jumpElapsed < maxJumpTime && !objectDisappeared)
            {
                jumpElapsed += Time.deltaTime;

                if (_rigidbody2D != null)
                    _rigidbody2D.velocity = new Vector2(0f, GetJumpForce() * 0.75f);

                _isGrounded = CheckGround();

                if (originalHasObject && originalRaycastHit.collider != null)
                {
                    var rayOrigin = new Vector2(transform.position.x, transform.position.y + _raycastHeight);
                    var rayDirection = Vector2.left;
                    var hits = Physics2D.RaycastAll(rayOrigin, rayDirection, _raycastRange);
                    var foundObject = false;

                    foreach (var hit in hits)
                    {
                        if (hit.collider != null && hit.collider.gameObject != gameObject)
                        {
                            var targetEnemyBase = hit.collider.gameObject.GetComponent<EnemyBase>();
                            if (targetEnemyBase != null && IsSameEnemyLayer(hit.collider.gameObject))
                            {
                                foundObject = true;
                                break;
                            }
                        }
                    }

                    if (!foundObject)
                    {
                        objectDisappeared = true;
                        break;
                    }
                }
                else if (!originalHasObject)
                {
                    objectDisappeared = true;
                    break;
                }

                yield return null;
            }

            if (objectDisappeared && _currState == EEnemyState.Jump)
            {
                if (_rigidbody2D != null)
                    _rigidbody2D.velocity = new Vector2(-_jumpHorizontalSpeed * 4f, _rigidbody2D.velocity.y * 0.75f);
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

                _raycastHit = new RaycastHit2D();
                _hasObject = false;
                _hasCapsuleCollider = false;

                if (_rigidbody2D != null)
                    _rigidbody2D.velocity = new Vector2(-_idleMoveSpeed, _rigidbody2D.velocity.y);

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

                if (!CheckGround() && !_isFirstGround)
                {
                    var pushForce = new Vector2(1f, 10f);
                    _rigidbody2D.AddForce(pushForce, ForceMode2D.Impulse);
                    _isFirstGround = false;
                }

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
            _animator.SetTrigger(ANIM_HIT);

            var hitDuration = 0.5f;
            var elapsed = 0f;

            while (_currState == EEnemyState.Hit && elapsed < hitDuration)
            {
                elapsed += Time.deltaTime;

                if (_coKnockBack == null && _rigidbody2D != null)
                    _rigidbody2D.velocity = new Vector2(-_idleMoveSpeed, _rigidbody2D.velocity.y);

                yield return null;
            }

            if (_currState == EEnemyState.Hit)
                ChangeState(EEnemyState.Idle, null);
        }

        protected virtual IEnumerator Co_DieState(Action doneCallBack = null)
        {
            _animator.SetTrigger(ANIM_DIE);

            var dieDuration = 1.0f;
            var elapsed = 0f;

            if (_rigidbody2D != null)
                _rigidbody2D.velocity = Vector2.zero;

            _myCollider.enabled = false;
            _model.gameObject.SetActive(false);
            _dieEffect.Play();

            while (_currState == EEnemyState.Die && elapsed < dieDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_currState == EEnemyState.Die)
            {
                _model.gameObject.SetActive(true);
                _myCollider.enabled = true;
                gameObject.SetActive(false);
            }
        }
        #endregion

        #region [Jump]
        private bool CheckGround()
        {
            var rayOrigin = transform.position;
            var diagonalRayDirection = new Vector2(-1f, -1f).normalized;
            RaycastHit2D[] diagonalHits = Physics2D.RaycastAll(rayOrigin, diagonalRayDirection, _groundCheckDistance);

            foreach (RaycastHit2D hit in diagonalHits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    var isGroundLayer = (_groundLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0;
                    if (isGroundLayer)
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

        private float GetJumpForce()
        {
            if (!_hasObject || _raycastHit.collider == null)
                return _jumpForce;

            var targetObject = _raycastHit.collider.gameObject;
            var targetColliderHeight = GetColliderHeight(targetObject);

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

        private void PushEnemyBack(GameObject targetEnemy)
        {
            var targetRigidbody = targetEnemy.GetComponent<Rigidbody2D>();
            if (targetRigidbody != null)
            {
                var pushForce = new Vector2(0.35f, 0f);
                targetRigidbody.AddForce(pushForce, ForceMode2D.Impulse);
            }
        }
        #endregion

        #region [Raycast]
        private void CheckForwardObject()
        {
            var rayOrigin = new Vector2(transform.position.x, transform.position.y + _raycastHeight);
            var rayDirection = Vector2.left;

            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, rayDirection, _raycastRange);

            var isFoundValidHit = false;
            var closestHit = new RaycastHit2D();
            var closestDistance = float.MaxValue;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    if (IsGroundLayer(hit.collider.gameObject))
                        continue;

                    var targetEnemyBase = hit.collider.gameObject.GetComponent<EnemyBase>();
                    if (targetEnemyBase != null && IsSameEnemyLayer(hit.collider.gameObject))
                    {
                        var distance = Vector2.Distance(rayOrigin, hit.point);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestHit = hit;
                            isFoundValidHit = true;
                        }
                    }
                }
            }

            if (isFoundValidHit)
            {
                _raycastHit = closestHit;
                var hitObject = _raycastHit.collider.gameObject;
                var targetEnemyBase = hitObject.GetComponent<EnemyBase>();

                _hasObject = true;
                _colliderHeight = GetColliderHeight(hitObject);

                var canJump = _colliderHeight > 0f && !_isJumping && !_isJumpDelayed;
                if (canJump)
                {
                    if (_currState == EEnemyState.Idle)
                        ChangeState(EEnemyState.Jump, null);
                    else if (_currState == EEnemyState.Hit)
                        ChangeState(EEnemyState.Jump, null);
                }
            }
            else
            {
                // 적이 감지되지 않았을 때만 초기화 (겹쳐있을 때는 유지)
                if (!_hasObject)
                {
                    _raycastHit = new RaycastHit2D();
                    _hasCapsuleCollider = false;
                    _colliderHeight = 0f;
                }
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

        #region [Damage]
        private void SetHp()
        {
            _currHp = _maxHp;

            if (_hpSlider != null)
            {
                _hpSlider.maxValue = _maxHp;
                _hpSlider.value = _currHp;
                UpdateHpSliderVisibility();
                
                // 초기 HP 설정 시 그림자도 동기화
                if (_IMG_FillShadow != null)
                    _IMG_FillShadow.fillAmount = _hpSlider.value / _hpSlider.maxValue;
            }
        }

        public void Hit(float damage)
        {
            _damageFx.Show((int)damage);
            _currHp -= damage;

            StartWhiteEffect();
            StartKnockbackEffect();
            UpdateHpSlider();

            if (_currHp <= 0)
                ChangeState(EEnemyState.Die, null);
            else
                ChangeState(EEnemyState.Hit, null);
        }

        private void UpdateHpSlider()
        {
            if (_hpSlider != null)
            {
                _hpSlider.value = _currHp;
                UpdateHpSliderVisibility();
                
                // HP 그림자 애니메이션 시작
                StartHpShadowAnimation();
            }
        }

        private void UpdateHpSliderVisibility()
        {
            if (_hpSlider != null)
            {
                var shouldShow = _currHp > 0f && _currHp < _maxHp;
                _hpSlider.gameObject.SetActive(shouldShow);
            }
        }

        private void StartHpShadowAnimation()
        {
            if (_IMG_FillShadow == null)
                return;

            if (_coHpShadowAnimation != null)
                StopCoroutine(_coHpShadowAnimation);

            _coHpShadowAnimation = StartCoroutine(Co_HpShadowAnimation());
        }

        private IEnumerator Co_HpShadowAnimation()
        {
            if (_IMG_FillShadow == null || _hpSlider == null)
            {
                Debug.LogWarning("HP Shadow Animation: _IMG_FillShadow or _hpSlider is null");
                yield break;
            }

            var startFillAmount = _IMG_FillShadow.fillAmount;
            var targetFillAmount = _hpSlider.value / _hpSlider.maxValue;
            var elapsedTime = 0f;

            Debug.Log($"HP Shadow Animation Start: {startFillAmount} -> {targetFillAmount}");

            while (elapsedTime < _hpShadowAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / _hpShadowAnimationDuration;
                
                // 가속도를 주는 보간 함수 사용 (EaseInQuad)
                var acceleratedProgress = progress * progress;
                var currentFillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, acceleratedProgress);
                _IMG_FillShadow.fillAmount = currentFillAmount;

                Debug.Log($"HP Shadow Animation Progress: {progress:F2}, FillAmount: {currentFillAmount:F2}");

                yield return null;
            }

            _IMG_FillShadow.fillAmount = targetFillAmount;
            Debug.Log($"HP Shadow Animation End: {_IMG_FillShadow.fillAmount}");
            _coHpShadowAnimation = null;
        }

        private void StartKnockbackEffect()
        {
            if (_coKnockBack != null)
                StopCoroutine(_coKnockBack);

            _coKnockBack = StartCoroutine(Co_KnockbackEffect());
        }

        private IEnumerator Co_KnockbackEffect()
        {
            if (_rigidbody2D == null)
                yield break;

            var knockbackVelocity = new Vector2(_hitKnockbackForce, 0f);
            _rigidbody2D.velocity = knockbackVelocity;

            var elapsedTime = 0f;
            while (elapsedTime < _hitKnockbackDuration)
            {
                elapsedTime += Time.deltaTime;

                var remainingTime = _hitKnockbackDuration - elapsedTime;
                var knockbackMultiplier = remainingTime / _hitKnockbackDuration;
                var currentKnockback = new Vector2(_hitKnockbackForce * knockbackMultiplier, 0f);

                _rigidbody2D.velocity = currentKnockback;
                yield return null;
            }

            _rigidbody2D.velocity = new Vector2(-_idleMoveSpeed, _rigidbody2D.velocity.y);

            _coKnockBack = null;
        }

        private void StartWhiteEffect()
        {
            if (_coWhiteEffect != null)
                StopCoroutine(_coWhiteEffect);

            _coWhiteEffect = StartCoroutine(Co_WhiteEffect());
        }

        private IEnumerator Co_WhiteEffect()
        {
            _originalMaterialList.Clear();
            foreach (var spriteRenderer in _spriteRendererList)
            {
                if (spriteRenderer != null)
                {
                    _originalMaterialList.Add(spriteRenderer.material);
                    spriteRenderer.material = _whiteOutMaterial;
                }
            }

            yield return StartCoroutine(Co_FadeWhiteAmount(0f, 0.65f, _fadeInDuration));
            yield return StartCoroutine(Co_FadeWhiteAmount(0.65f, 0f, _fadeOutDuration, () =>
            {
                for (int i = 0; i < _spriteRendererList.Count && i < _originalMaterialList.Count; i++)
                {
                    if (_spriteRendererList[i] != null)
                        _spriteRendererList[i].material = _originalMaterialList[i];
                }
                _coWhiteEffect = null;
            }));
        }

        private IEnumerator Co_FadeWhiteAmount(float startValue, float endValue, float duration, Action doneCallBack = null)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                var progress = elapsedTime / duration;
                var smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                var currentValue = Mathf.Lerp(startValue, endValue, smoothProgress);

                foreach (var spriteRenderer in _spriteRendererList)
                {
                    if (spriteRenderer != null && spriteRenderer.material != null)
                        spriteRenderer.material.SetFloat("_WhiteAmount", currentValue);
                }

                yield return null;
            }

            foreach (var spriteRenderer in _spriteRendererList)
            {
                if (spriteRenderer != null && spriteRenderer.material != null)
                    spriteRenderer.material.SetFloat("_WhiteAmount", endValue);
            }

            doneCallBack?.Invoke();
        }
        #endregion
    }
}