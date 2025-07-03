// ----- System 
using System;
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    public class HunterBase : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [Header("1. 무기 관련 그룹")]
        [SerializeField] protected GameObject _OBJ_Weapon = null;
        [SerializeField] protected GameObject _OBJ_ShotArea = null;
        [SerializeField] protected Transform _shotStartTrans = null;

        [SerializeField] protected BulletBase[] _bulletSet = new BulletBase[3];
        
        [SerializeField] protected BombBase _originBomb = null;

        [Space(1.5f)]
        [Header("2. 캐릭터 그룹")]
        [SerializeField] protected Animator _animator = null;

        [Space(1.5f)]
        [Header("3. 자동 조준 설정")]
        [SerializeField] protected bool _enableAutoAim = true;
        [SerializeField] protected float _autoAimSpeed = 5f;
        [SerializeField] protected float _weaponRotationOffset = 0f; 

        [Space(1.5f)]
        [Header("4. 공격 설정")]
        [SerializeField] protected float _attackCooldown = 1.0f;
        [SerializeField] protected float _bulletSpeed = 5f; 
        [SerializeField] protected float _bulletMaxDistance = 10f;
        [SerializeField] protected LayerMask _bulletHitLayerMask = -1;

        [Space(1.5f)]
        [Header("5. 디버그 그룹")]
#if UNITY_EDITOR
        [SerializeField] protected HunterDebugger _hunterDebugger = null;
#endif

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const string ANIM_IDLE = "Idle";
        private const string ANIM_ATTACK = "Attack";
        
        private EHunterState _currState = EHunterState.Unknown;
        private EHunterState _prevState = EHunterState.Unknown;

        private Coroutine _coState = null;

        private Transform _currentTarget = null;
        private bool _isAutoAiming = false;

        private float _lastAttackTime = 0f;
        private bool _canAttack = true;

        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public EHunterState CurrentState => _currState;

        // --------------------------------------------------
        // Method - Events
        // --------------------------------------------------
        private void Start()
        {
            if (_OBJ_ShotArea != null)
                _OBJ_ShotArea.SetActive(false);
            
            SetBullets();
            ChangeState(EHunterState.Idle, null);
        }

        private void Update()
        {
            HandleClickInput();
            
            if (_enableAutoAim)
                HandleAutoAim();
            HandleAttack();

            if (_currState == EHunterState.Idle && !Input.GetMouseButton(0))
            {
                if (FindNearestTarget() != null)
                    ChangeState(EHunterState.AutoAttack, null);
            }

            if (_currState == EHunterState.AutoAttack && !Input.GetMouseButton(0))
            {
                if (FindNearestTarget() == null)
                    ChangeState(EHunterState.Idle, null);
            }
        }

        // --------------------------------------------------
        // Method - Normal
        // --------------------------------------------------
        #region [State]
        public void ChangeState(EHunterState state, Action doneCallBack)
        {
            if (_currState == state)
                return;

            _prevState = _currState;
            _currState = state;
            
            switch (state)
            {
                case EHunterState.Idle: _coState = StartCoroutine(Co_IdleState(doneCallBack)); break;
                case EHunterState.AutoAttack: _coState = StartCoroutine(Co_AutoAttackState(doneCallBack)); break;
                case EHunterState.Attack: _coState = StartCoroutine(Co_AttackState(doneCallBack)); break;
                case EHunterState.Die: _coState = StartCoroutine(Co_DieState(doneCallBack)); break;
            }
        }

        protected virtual IEnumerator Co_IdleState(Action doneCallBack = null)
        {
            _animator.SetTrigger(ANIM_IDLE);
            yield return null;
        }

        protected virtual IEnumerator Co_AutoAttackState(Action doneCallBack = null)
        {
            yield return null;
        }
        
        protected virtual IEnumerator Co_AttackState(Action doneCallBack = null)
        {
            yield return null;
        }
        
        protected virtual IEnumerator Co_DieState(Action doneCallBack = null)
        {
            yield return null;
        }
        #endregion

        #region [Input]
        private void HandleClickInput()
        {
            if (IsPointerOverUI())
                return;
                
            if (Input.GetMouseButtonDown(0))
            {
#if UNITY_EDITOR
                if (_hunterDebugger != null)
                    _hunterDebugger.CreateClickGizmo();
#endif
                
                if (_OBJ_ShotArea != null)
                    _OBJ_ShotArea.SetActive(true);

                ChangeState(EHunterState.Attack, null);
            }
            
            if (Input.GetMouseButton(0))
            {
#if UNITY_EDITOR
                if (_hunterDebugger != null)
                    _hunterDebugger.CreateDragGizmo();
#endif
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                if (_OBJ_ShotArea != null)
                    _OBJ_ShotArea.SetActive(false);

                EvaluateState();
            }
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
                return false;
                
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void EvaluateState()
        {
            if (Input.GetMouseButton(0))
            {
                ChangeState(EHunterState.Attack, null);
                return;
            }

            var hasTarget = FindNearestTarget() != null;

            if (hasTarget)
                ChangeState(EHunterState.AutoAttack, null);
            else
                ChangeState(EHunterState.Idle, null);
        }

        private void RotateWeaponToMouse()
        {
            if (_OBJ_Weapon == null)
                return;

            var mousePosition = Input.mousePosition;
            mousePosition.z = 10f;
            var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f;

            var direction = (worldPosition - _OBJ_Weapon.transform.position).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle += _weaponRotationOffset;
            
            var currentRotation = _OBJ_Weapon.transform.eulerAngles;
            currentRotation.z = angle;
            _OBJ_Weapon.transform.eulerAngles = currentRotation;
        }
        #endregion

        #region [Attack]
        private void HandleAttack()
        {
            if (!_canAttack)
            {
                if (Time.time - _lastAttackTime >= _attackCooldown)
                    _canAttack = true;
                else
                    return;
            }

            if (_currState == EHunterState.Attack || _currState == EHunterState.AutoAttack)
                FireBullets();
        }

        private void FireBullets()
        {
            if (_bulletSet == null || _bulletSet.Length == 0 || _shotStartTrans == null)
                return;

            _animator.SetTrigger(ANIM_ATTACK);
            _lastAttackTime = Time.time;
            _canAttack = false;

            var fireDirection = GetFireDirection();
            for (int i = 0; i < _bulletSet.Length; i++)
            {
                if (_bulletSet[i] != null)
                {
                    var randomAngle = UnityEngine.Random.Range(-15f / 2f, 15f / 2f);
                    var bulletDirection = GetRotatedDirection(fireDirection, randomAngle);

                    StartCoroutine(FireBullet(_bulletSet[i], bulletDirection));
                }
            }
        }

        public void FireBomb()
        {
            if (_originBomb == null)
                return;

            var bomb = Instantiate(_originBomb, _shotStartTrans.position, Quaternion.identity);
            bomb.Fire(null);
            
            var bombRigidbody = bomb.GetComponent<Rigidbody2D>();
            if (bombRigidbody != null)
            {
                Vector2 throwDirection = new Vector2(1f, 0.5f).normalized;
                float throwForce = UnityEngine.Random.Range(2.5f, 4f);
                bombRigidbody.AddForce(throwDirection * throwForce, ForceMode2D.Impulse);
            }
        }

        private Vector3 GetFireDirection()
        {
            if (_currState == EHunterState.Attack)
            {
                var mousePosition = Input.mousePosition;
                mousePosition.z = 10f;
                
                var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
                worldPosition.z = 0f;
                
                return (worldPosition - _shotStartTrans.position).normalized;
            }
            else if (_currState == EHunterState.AutoAttack && _currentTarget != null)
                return (_currentTarget.position - _shotStartTrans.position).normalized;
            else
                return Vector3.left;
        }

        private Vector3 GetRotatedDirection(Vector3 baseDirection, float angle)
        {
            var angleRad = angle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(angleRad);
            var sin = Mathf.Sin(angleRad);
            
            return new Vector3(
                baseDirection.x * cos - baseDirection.y * sin,
                baseDirection.x * sin + baseDirection.y * cos,
                0f
            );
        }

        private IEnumerator FireBullet(BulletBase bullet, Vector3 direction)
        {
            if (bullet == null || _shotStartTrans == null)
                yield break;

            bullet.transform.position = _shotStartTrans.position;
            bullet.gameObject.SetActive(true);

            var startPosition = bullet.transform.position;
            var distanceTraveled = 0f;
            var timeAlive = 0f;
            var hasHitEnemy = false;

            while (bullet.gameObject.activeInHierarchy && distanceTraveled < _bulletMaxDistance && !hasHitEnemy)
            {
                bullet.transform.position += direction * _bulletSpeed * Time.deltaTime;
                distanceTraveled = Vector3.Distance(startPosition, bullet.transform.position);
                timeAlive += Time.deltaTime;

                if (CheckBulletHit(bullet.transform.position))
                    break;

                if (CheckEnemyCollision(bullet.transform.position))
                    break;

                yield return null;
            }

            bullet.gameObject.SetActive(false);
            bullet.transform.position = _shotStartTrans.position;
        }

        private bool IsEnemyLineLayer(GameObject targetObject)
        {
            var targetLayer = targetObject.layer;
            var isEnemyLineLayer = targetLayer == LayerMask.NameToLayer("EnemyLine_0") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_1") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_2");
            
            return isEnemyLineLayer;
        }

        private void SetBullets()
        {
            if (_bulletSet == null || _bulletSet.Length == 0)
                return;

            for (int i = 0; i < _bulletSet.Length; i++)
            {
                if (_bulletSet[i] != null)
                {
                    _bulletSet[i].gameObject.SetActive(false);
                    if (_shotStartTrans != null)
                        _bulletSet[i].transform.position = _shotStartTrans.position;
                }
            }
        }

        private bool CheckBulletHit(Vector3 bulletPosition)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(bulletPosition, 0.05f, _bulletHitLayerMask);
            
            foreach (Collider2D hit in hits)
            {
                if (hit != null && hit.gameObject != gameObject)
                {
                    if (hit.gameObject.GetComponent<BulletBase>() != null)
                        continue;
                        
                    return true;
                }
            }

            return false;
        }

        private bool CheckEnemyCollision(Vector3 bulletPosition)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(bulletPosition, 0.1f);
            
            foreach (Collider2D hit in hits)
            {
                if (hit != null && hit.gameObject != gameObject)
                {
                    var targetLayer = hit.gameObject.layer;
                    var isEnemyLineLayer = targetLayer == LayerMask.NameToLayer("EnemyLine_0") ||
                                          targetLayer == LayerMask.NameToLayer("EnemyLine_1") ||
                                          targetLayer == LayerMask.NameToLayer("EnemyLine_2");

                    if (isEnemyLineLayer)
                    {
                        var enemy = hit.gameObject.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            enemy.Hit(_bulletSet[0].Power); // 첫 번째 불릿의 파워 사용
                            Debug.Log($"불릿 Enemy 충돌: {enemy.name}");
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region [Auto Attack]
        private void HandleAutoAim()
        {
            if (_currState == EHunterState.Attack)
            {
                if (Input.GetMouseButton(0))
                    RotateWeaponToMouse();
                return;
            }

            if (_currState == EHunterState.AutoAttack)
            {
                if (_currentTarget == null || !IsTargetValid(_currentTarget))
                    _currentTarget = FindNearestTarget();

                if (_currentTarget != null)
                {
                    RotateWeaponToTarget(_currentTarget);
                    _isAutoAiming = true;
                }
                else
                    _isAutoAiming = false;
            }
            else
            {
                _isAutoAiming = false;
                _currentTarget = null;
            }
        }

        private Transform FindNearestTarget()
        {
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
                return null;

            var detectionRadius = circleCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            var center = transform.position;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, detectionRadius);
            
            var nearestTarget = default(Transform);
            var nearestDistance = float.MaxValue;

            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject == gameObject)
                    continue;

                if (!IsEnemyLineLayer(collider.gameObject))
                    continue;

                var distance = Vector3.Distance(center, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = collider.transform;
                }
            }

            return nearestTarget;
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null)
                return false;

            if (target.gameObject == null)
                return false;

            if (!IsEnemyLineLayer(target.gameObject))
                return false;

            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
                return false;

            var detectionRadius = circleCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            var distance = Vector3.Distance(transform.position, target.position);
            
            return distance <= detectionRadius;
        }

        private void RotateWeaponToTarget(Transform target)
        {
            if (_OBJ_Weapon == null || target == null)
                return;

            var direction = (target.position - _OBJ_Weapon.transform.position).normalized;
            var targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetAngle += _weaponRotationOffset;
            
            var currentAngle = _OBJ_Weapon.transform.eulerAngles.z;
            var angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            var newAngle = currentAngle + angleDifference * _autoAimSpeed * Time.deltaTime;
            var currentRotation = _OBJ_Weapon.transform.eulerAngles;
            currentRotation.z = newAngle;
            _OBJ_Weapon.transform.eulerAngles = currentRotation;
        }
        #endregion
    }
}