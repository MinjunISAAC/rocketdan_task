// ----- System
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
        [SerializeField] protected CapsuleCollider2D _collider2D = null;
        [SerializeField] protected Rigidbody2D _rigidbody = null;
        [SerializeField] protected CircleCollider2D _circleCollider2D = null;

        [Space(1.5f)]
        [Header("2. 이동 관련 변수")]
        [SerializeField] protected float _moveSpeed = 2f;

        [Space(1.5f)]
        [Header("3. 레이캐스트 설정")]
        [SerializeField] protected RaycastSettings _leftConfig = new RaycastSettings(1f, 0f, 0f, 0f);
        [SerializeField] protected RaycastSettings _rightConfig = new RaycastSettings(1f, 0f, 0f, 0f);
        [SerializeField] protected RaycastSettings _downConfig = new RaycastSettings(1f, 0f, 0f, 0f);
        [SerializeField] private float _downRaycastOffset = 0.5f;
        [SerializeField] protected RaycastSettings _upConfig = new RaycastSettings(1f, 0f, 0f, 0f);
        [SerializeField] protected RaycastSettings _attackConfig = new RaycastSettings(1.5f, 0f, 0f, 0f);

        [Space(1.5f)]
        [Header("4. 애니메이션 설정")]
        [SerializeField] protected Animator _animator = null;

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const string ANIM_MOVE = "Move";
        private const string ANIM_OVERCOME = "Overcome";
        private const string ANIM_ATTACK = "Attack";
        private const string ANIM_DEAD = "Dead";
        private const float OVERCOME_DELAY = 1f;
        private const float LANDING_TIMEOUT = 3f;
        private const float ATTACK_INTERVAL = 1f;
        private const float DOWN_FORCE = 8f;

        [SerializeField] protected EEnemyState _currState = EEnemyState.Unknown;
        [SerializeField] protected EEnemyState _prevState = EEnemyState.Unknown;

        protected Coroutine _coState = null;

        private float _overcomeDelayTimer = -1f;
        private float _landingTimeoutTimer = -1f;

        private int _layerValue = 0;

        // 타겟팅 변수들
        [SerializeField] private EnemyBase _leftTarget = null;
        [SerializeField] private EnemyBase _rightTarget = null;
        [SerializeField] private GameObject _downTarget = null;
        [SerializeField] private EnemyBase _upTarget = null;
        [SerializeField] private BoxBase _attackTarget = null;

        // --------------------------------------------------
        // Enums
        // --------------------------------------------------
        public enum RaycastType
        {
            Unknown = 0,
            Left = 0,
            Right = 1,
            Up = 2,
            Down = 3,
            Attack = 4,
        }

        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public EEnemyState State { get; private set; } = EEnemyState.Unknown;
        public CapsuleCollider2D Collider => _collider2D;
        public Rigidbody2D Rigidbody => _rigidbody;
        public EnemyBase RightTarget => _rightTarget;

        // --------------------------------------------------
        // Methods - Event
        // --------------------------------------------------
        protected virtual void Awake() { BindComponents(); }

        protected virtual void Start()
        {
            ChangeState(EEnemyState.Move);
        }

        protected virtual void FixedUpdate()
        {
            UpdateTarget<EnemyBase>(RaycastType.Left, ref _leftTarget);
            UpdateTarget<EnemyBase>(RaycastType.Right, ref _rightTarget);
            UpdateTarget<EnemyBase>(RaycastType.Up, ref _upTarget);
            UpdateDownTarget();
            UpdateAttackTarget();

            if (_currState == EEnemyState.Attack && _upTarget != null && _downTarget != null)
            {
                var currentPos = transform.position;
                var targetPos = currentPos + Vector3.right * 0.15f;
                var newPos = Vector3.Lerp(currentPos, targetPos, Time.fixedDeltaTime * 2f);
                transform.position = newPos;

                if (Vector3.Distance(currentPos, targetPos) < 0.01f)
                    ChangeState(EEnemyState.Move);
            }
        }

        // --------------------------------------------------
        // Methods - Bind Group
        // --------------------------------------------------
        protected virtual void BindComponents()
        {
            if (_rigidbody == null)
            {
                if (TryGetComponent(out Rigidbody2D rigidbody))
                    _rigidbody = rigidbody;
            }

            if (_collider2D == null)
            {
                if (TryGetComponent(out CapsuleCollider2D collider2D))
                    _collider2D = collider2D;
            }

            if (_circleCollider2D == null)
            {
                if (TryGetComponent(out CircleCollider2D circleCollider2D))
                    _circleCollider2D = circleCollider2D;
            }
        }

        // --------------------------------------------------
        // Methods - State Group
        // --------------------------------------------------
        public virtual void ChangeState(EEnemyState state)
        {
            if (_currState == state)
                return;

            if (_coState != null)
            {
                StopCoroutine(_coState);
                _coState = null;
            }

            StopAllCoroutines();
            _landingTimeoutTimer = -1f;

            _prevState = _currState;
            _currState = state;

            switch (state)
            {
                case EEnemyState.Move: _coState = StartCoroutine(Co_Move()); break;
                case EEnemyState.Overcome: _coState = StartCoroutine(Co_Overcome()); break;
                case EEnemyState.Attack: _coState = StartCoroutine(Co_Attack()); break;
                case EEnemyState.Dead: _coState = StartCoroutine(Co_Dead()); break;
                default: break;
            }
        }

        protected virtual IEnumerator Co_Move()
        {
            _animator.SetTrigger(ANIM_MOVE);

            while (_currState == EEnemyState.Move)
            {
                var currentVelocity = _rigidbody.velocity;
                currentVelocity.x = -_moveSpeed;
                _rigidbody.velocity = currentVelocity;

                yield return new WaitForFixedUpdate();
            }
        }

        protected virtual IEnumerator Co_Overcome()
        {
            _animator.SetTrigger(ANIM_OVERCOME);

            var moveDis = 0f;
            if (_leftTarget != null)
            {
                var targetCollider = _leftTarget.Collider;
                if (targetCollider != null)
                    moveDis = (targetCollider.size.y + targetCollider.size.x);
            }

            if (_rightTarget != null || _upTarget != null)
            {
                _overcomeDelayTimer = Time.time + OVERCOME_DELAY;
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, -_moveSpeed * 2f);
                yield return new WaitForFixedUpdate();
                ChangeState(EEnemyState.Move);
                yield break;
            }

            if (_downTarget == null)
            {
                _overcomeDelayTimer = Time.time + OVERCOME_DELAY;
                _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, -_moveSpeed * 2f);
                yield return new WaitForFixedUpdate();
                ChangeState(EEnemyState.Move);
            }

            var targetPos = (Vector2)transform.position + Vector2.up * moveDis;
            var startTime = Time.time;
            var moveDuration = 1.5f;
            var jumpDir = new Vector2(-1 * _leftTarget.Collider.size.x, _leftTarget.Collider.size.x).normalized;
            while (_currState == EEnemyState.Overcome)
            {
                var elapsedTime = Time.time - startTime;
                var progress = elapsedTime / moveDuration;

                if (progress < 1f)
                {
                    var newPos = Vector2.Lerp((Vector2)transform.position, targetPos, progress);
                    _rigidbody.MovePosition(newPos);
                }
                else
                    break;

                if (_leftTarget == null)
                {
                    _overcomeDelayTimer = Time.time + OVERCOME_DELAY;
                    yield return new WaitForFixedUpdate();
                    ChangeState(EEnemyState.Move);
                    break;
                }

                if (_downTarget == null)
                {
                    _overcomeDelayTimer = Time.time + OVERCOME_DELAY;
                    yield return new WaitForFixedUpdate();
                    ChangeState(EEnemyState.Move);
                    break;
                }

                yield return new WaitForFixedUpdate();
            }

            _rigidbody.velocity = jumpDir * _moveSpeed * 2f;
            _overcomeDelayTimer = Time.time + OVERCOME_DELAY;
            _landingTimeoutTimer = Time.time + LANDING_TIMEOUT;

            ChangeState(EEnemyState.Move);
        }

        protected virtual IEnumerator Co_Attack()
        {
            while (_currState == EEnemyState.Attack)
            {
                if (_attackTarget == null || !_attackTarget.IsAlive)
                {
                    ChangeState(EEnemyState.Move);
                    yield break;
                }

                var downEnemy = GetDownEnemyBase();
                if (downEnemy != null)
                    PushEnemyChain(downEnemy, 0);

                _animator.SetTrigger(ANIM_ATTACK);
                if (_attackTarget != null)
                    _attackTarget.Hit(10f);

                var currentVelocity = _rigidbody.velocity;
                currentVelocity.x = -_moveSpeed * 2f;
                _rigidbody.velocity = currentVelocity;


                yield return new WaitForSeconds(ATTACK_INTERVAL);
            }
        }

        protected virtual IEnumerator Co_Dead()
        {
            _animator.SetTrigger(ANIM_DEAD);
            yield return null;
        }

        // --------------------------------------------------
        // Methods - Trigger Group
        // --------------------------------------------------
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            var boxBase = other.GetComponent<BoxBase>();
            if (boxBase != null && boxBase.IsAlive)
            {
                _attackTarget = boxBase;
                if (_currState == EEnemyState.Move)
                    ChangeState(EEnemyState.Attack);
            }
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            var boxBase = other.GetComponent<BoxBase>();
            if (boxBase != null && boxBase.IsAlive)
            {
                if (_attackTarget != boxBase)
                    _attackTarget = boxBase;

                if (_currState == EEnemyState.Move)
                    ChangeState(EEnemyState.Attack);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            var boxBase = other.GetComponent<BoxBase>();
            if (boxBase != null && _attackTarget == boxBase)
            {
                _attackTarget = null;
                if (_currState == EEnemyState.Attack)
                    ChangeState(EEnemyState.Move);
            }
        }

        protected virtual void UpdateAttackTarget()
        {
            var setting = GetRaycastSetting(RaycastType.Attack);
            var baseDirection = GetBaseDirection(RaycastType.Attack);
            var origin = setting.GetOrigin(transform.position);
            var direction = setting.GetDirection(baseDirection);

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, setting.distance);

            BoxBase foundBox = null;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject != gameObject)
                {
                    foundBox = hit.collider.GetComponent<BoxBase>();
                    if (foundBox != null && foundBox.IsAlive)
                        break;
                }
            }

            if (foundBox != null)
            {
                if (_attackTarget != foundBox)
                {
                    _attackTarget = foundBox;
                    if (_currState == EEnemyState.Move)
                        ChangeState(EEnemyState.Attack);
                }
            }
            else
            {
                if (_attackTarget != null)
                {
                    _attackTarget = null;
                    if (_currState == EEnemyState.Attack)
                        ChangeState(EEnemyState.Move);
                }
            }

#if UNITY_EDITOR
            setting.DrawRay(origin, direction, foundBox != null);
#endif
        }

        private EnemyBase GetDownEnemyBase()
        {
            if (_downTarget != null)
                return _downTarget.GetComponent<EnemyBase>();
            return null;
        }

        private void PushEnemyChain(EnemyBase enemy, int depth)
        {
            if (enemy == null || depth > 10)
                return;

            StartCoroutine(Co_PushEnemy(enemy, depth));

            var rightEnemy = enemy.RightTarget;
            if (rightEnemy != null)
                PushEnemyChain(rightEnemy, depth + 1);
        }

        private IEnumerator Co_PushEnemy(EnemyBase enemy, int depth)
        {
            var pushDistance = 0.5f - depth * 0.05f;
            var pushDuration = 0.3f - depth * 0.02f;
            var startPos = enemy.transform.position;
            var targetPos = startPos + Vector3.right * pushDistance;
            var elapsedTime = 0f;

            while (elapsedTime < pushDuration)
            {
                elapsedTime += Time.fixedDeltaTime;
                var progress = elapsedTime / pushDuration;
                var newPos = Vector3.Lerp(startPos, targetPos, progress);
                enemy.transform.position = newPos;
                yield return new WaitForFixedUpdate();
            }

            enemy.transform.position = targetPos;
        }

        protected virtual IEnumerator Co_WaitForLanding()
        {
            while (Time.time < _landingTimeoutTimer)
            {
                if (_downTarget != null)
                {
                    _landingTimeoutTimer = -1f;
                    yield break;
                }

                var currentVelocity = _rigidbody.velocity;
                currentVelocity.y -= 9.8f * Time.fixedDeltaTime;
                _rigidbody.velocity = currentVelocity;

                yield return new WaitForFixedUpdate();
            }

            _landingTimeoutTimer = -1f;
            ChangeState(EEnemyState.Move);
        }

        // --------------------------------------------------
        // Methods - Raycast Group
        // --------------------------------------------------
        #region [Raycast Group]
        protected virtual RaycastSettings GetRaycastSetting(RaycastType type)
        {
            switch (type)
            {
                case RaycastType.Left: return _leftConfig;
                case RaycastType.Right: return _rightConfig;
                case RaycastType.Up: return _upConfig;
                case RaycastType.Down: return _downConfig;
                case RaycastType.Attack: return _attackConfig;
                default: return _leftConfig;
            }
        }

        protected virtual Vector2 GetBaseDirection(RaycastType type)
        {
            switch (type)
            {
                case RaycastType.Left: return Vector2.left;
                case RaycastType.Right: return Vector2.right;
                case RaycastType.Up: return Vector2.up;
                case RaycastType.Down: return Vector2.down;
                case RaycastType.Attack: return Vector2.left;
                default: return Vector2.left;
            }
        }

        protected virtual RaycastHit2D CastRay(RaycastType type)
        {
            var setting = GetRaycastSetting(type);
            var baseDirection = GetBaseDirection(type);

            return setting.PerformRaycast(transform.position, baseDirection, gameObject);
        }

        protected virtual void UpdateTarget<T>(RaycastType type, ref T targetVariable) where T : Component
        {
            var setting = GetRaycastSetting(type);
            var baseDirection = GetBaseDirection(type);

            var origin = setting.GetOrigin(transform.position);
            var direction = setting.GetDirection(baseDirection);

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, setting.distance);

            T foundComponent = null;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject != gameObject)
                {
                    foundComponent = hit.collider.GetComponent<T>();
                    if (foundComponent != null)
                        break;
                }
            }

            if (foundComponent != null && targetVariable == null)
            {
                targetVariable = foundComponent;

                if (foundComponent is EnemyBase)
                {
                    switch (type)
                    {
                        case RaycastType.Left:
                            if (Time.time >= _overcomeDelayTimer && _rightTarget == null && _upTarget == null)
                                ChangeState(EEnemyState.Overcome);
                            break;
                        case RaycastType.Right:
                            _rightTarget = foundComponent as EnemyBase;
                            break;
                        case RaycastType.Up:
                            _upTarget = foundComponent as EnemyBase;
                            break;
                    }
                }
            }
            else if (foundComponent != null && targetVariable != null)
            {
                if (foundComponent is EnemyBase)
                {
                    switch (type)
                    {
                        case RaycastType.Left:
                            if (Time.time >= _overcomeDelayTimer && _rightTarget == null && _upTarget == null)
                                ChangeState(EEnemyState.Overcome);
                            break;
                        case RaycastType.Right:
                            _rightTarget = foundComponent as EnemyBase;
                            break;
                        case RaycastType.Up:
                            _upTarget = foundComponent as EnemyBase;
                            break;
                    }
                }
            }
            else if (foundComponent == null && targetVariable != null)
            {
                targetVariable = null;
            }

#if UNITY_EDITOR
            setting.DrawRay(origin, direction, foundComponent != null);
#endif
        }

        protected virtual void UpdateDownTarget()
        {
            if (_circleCollider2D == null)
                return;

            var circleCenter = (Vector2)transform.position + _circleCollider2D.offset;
            var circleRadius = _circleCollider2D.radius;
            var hitColliders = Physics2D.OverlapCircleAll(circleCenter, circleRadius);

            GameObject foundObject = null;
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject != gameObject)
                {
                    var enemy = hitCollider.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        foundObject = hitCollider.gameObject;
                        break;
                    }

                    if (hitCollider.gameObject.layer == LayerMask.NameToLayer($"Ground_{_layerValue}"))
                    {
                        foundObject = hitCollider.gameObject;
                        break;
                    }
                }
            }

            if (foundObject != null && _downTarget == null)
                _downTarget = foundObject;
            else if (foundObject == null && _downTarget != null)
                _downTarget = null;

#if UNITY_EDITOR
            if (_circleCollider2D != null)
            {
                var center = (Vector2)transform.position + _circleCollider2D.offset;
                var radius = _circleCollider2D.radius;
                var color = foundObject != null ? Color.red : Color.green;
                Debug.DrawLine(center + Vector2.left * radius, center + Vector2.right * radius, color);
                Debug.DrawLine(center + Vector2.up * radius, center + Vector2.down * radius, color);
            }
#endif
        }
        #endregion

        #region [Editor Group]
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawRaycast(RaycastType.Left);
            DrawRaycast(RaycastType.Right);
            DrawRaycast(RaycastType.Up);
            DrawRaycast(RaycastType.Attack);
            DrawDownRaycasts();
        }

        private void DrawRaycast(RaycastType type)
        {
            var setting = GetRaycastSetting(type);
            if (!setting.showDebug)
                return;

            var baseDirection = GetBaseDirection(type);
            var origin = setting.GetOrigin(transform.position);
            var dir = setting.GetDirection(baseDirection);

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, setting.distance);
            var hasHit = false;

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject != gameObject)
                {
                    hasHit = true;
                    break;
                }
            }

            setting.DrawRay(origin, dir, hasHit);
        }

        private void DrawDownRaycasts()
        {
            // Circle Collider 2D 시각화
            if (_circleCollider2D != null)
            {
                var center = (Vector2)transform.position + _circleCollider2D.offset;
                var radius = _circleCollider2D.radius;
                var color = Color.green;

                // 십자 모양으로 원형 영역 표시
                Debug.DrawLine(center + Vector2.left * radius, center + Vector2.right * radius, color);
                Debug.DrawLine(center + Vector2.up * radius, center + Vector2.down * radius, color);
            }
        }
#endif
        #endregion
    }
}