// ----- System 
using System;
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class HunterBase : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [Header("1. 무기 관련 그룹")]
        [SerializeField] private GameObject _OBJ_Weapon = null;
        [SerializeField] private GameObject _OBJ_ShotArea = null;
        [SerializeField] private Transform _shotStartTrans = null;

        [SerializeField] protected BulletBase[] _bulletSet = new BulletBase[3];
        
        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private EHunterState _currState = EHunterState.Unknown;
        private EHunterState _prevState = EHunterState.Unknown;

        private Coroutine _coState = null;

        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public EHunterState CurrentState => _currState;

        // --------------------------------------------------
        // Method - Events
        // --------------------------------------------------
        // ----- 사용 중
        private void Start()
        {
            if (_OBJ_ShotArea != null)
                _OBJ_ShotArea.SetActive(false);
            
            // 불릿 초기화
            InitializeBullets();
            
            ChangeState(EHunterState.Idle, null);
        }

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

        // --------------------------------------------------
        // Method - Normal
        // --------------------------------------------------

        [Header("클릭 기즈모 설정")]
        [SerializeField] private bool _showClickGizmos = true;
        [SerializeField] private Color _clickGizmoColor = Color.red;
        [SerializeField] private float _clickGizmoRadius = 0.5f;
        [SerializeField] private float _clickGizmoDuration = 2f;

        [Header("선 기즈모 설정")]
        [SerializeField] private bool _showLineGizmos = true;
        [SerializeField] private Color _lineGizmoColor = Color.blue;
        [SerializeField] private float _lineGizmoDuration = 1f;

        [Header("레이캐스트 기즈모 설정")]
        [SerializeField] private bool _showRaycastGizmos = true;
        [SerializeField] private Color _raycastGizmoColor = Color.green;
        [SerializeField] private float _raycastRange = 5f;
        [SerializeField] private float _raycastAngle = 15f; // -15도 ~ +15도

        [Header("자동 조준 설정")]
        [SerializeField] private bool _enableAutoAim = true;
        [SerializeField] private float _autoAimSpeed = 5f; // 회전 속도
        [SerializeField] private LayerMask _enemyLayerMask = -1; // Enemy 레이어 마스크

        [Header("공격 설정")]
        [SerializeField] private float _attackCooldown = 1.0f;
        [SerializeField] private float _bulletSpeed = 5f; // 속도를 낮춤
        [SerializeField] private float _bulletMaxDistance = 10f;
        [SerializeField] private LayerMask _bulletHitLayerMask = -1; // 불릿이 맞을 레이어

        private List<ClickGizmoData> _clickGizmos = new List<ClickGizmoData>();
        private List<LineGizmoData> _lineGizmos = new List<LineGizmoData>();

        // 자동 조준 관련 변수
        private Transform _currentTarget = null;
        private bool _isAutoAiming = false;

        // 공격 관련 변수
        private float _lastAttackTime = 0f;
        private bool _canAttack = true;

        [System.Serializable]
        private class ClickGizmoData
        {
            public Vector3 position;
            public float createTime;
            public float duration;

            public ClickGizmoData(Vector3 pos, float dur)
            {
                position = pos;
                createTime = Time.time;
                duration = dur;
            }

            public bool IsExpired => Time.time - createTime > duration;
        }

        [System.Serializable]
        private class LineGizmoData
        {
            public Vector3 startPosition;
            public Vector3 endPosition;
            public float createTime;
            public float duration;

            public LineGizmoData(Vector3 start, Vector3 end, float dur)
            {
                startPosition = start;
                endPosition = end;
                createTime = Time.time;
                duration = dur;
            }

            public bool IsExpired => Time.time - createTime > duration;
        }

        private void Update()
        {
            HandleClickInput();
            CleanupExpiredGizmos();
            
            // 상태에 따른 자동 조준 처리
            if (_enableAutoAim)
            {
                HandleAutoAim();
            }

            // 공격 처리
            HandleAttack();

            // Idle 상태에서 타겟이 들어오면 AutoAttack으로 변경
            if (_currState == EHunterState.Idle && !Input.GetMouseButton(0))
            {
                if (FindNearestTarget() != null)
                {
                    ChangeState(EHunterState.AutoAttack, null);
                }
            }

            // AutoAttack 상태에서 타겟이 없어지면 Idle로 변경
            if (_currState == EHunterState.AutoAttack && !Input.GetMouseButton(0))
            {
                if (FindNearestTarget() == null)
                {
                    ChangeState(EHunterState.Idle, null);
                }
            }
        }

        private void HandleClickInput()
        {
            // 마우스 클릭 감지
            if (Input.GetMouseButtonDown(0))
            {
                if (_showClickGizmos)
                {
                    CreateClickGizmo();
                }
                
                // ShotArea 활성화
                if (_OBJ_ShotArea != null)
                {
                    _OBJ_ShotArea.SetActive(true);
                }

                // Attack 상태로 변경
                ChangeState(EHunterState.Attack, null);
            }
            
            // 드래그 중일 때 계속 기즈모 생성
            if (Input.GetMouseButton(0))
            {
                if (_showClickGizmos)
                {
                    CreateDragGizmo();
                }
            }
            
            // 마우스 버튼을 떼었을 때
            if (Input.GetMouseButtonUp(0))
            {
                // ShotArea 비활성화
                if (_OBJ_ShotArea != null)
                {
                    _OBJ_ShotArea.SetActive(false);
                }

                // 상태 재평가
                EvaluateState();
            }
        }

        private void CreateClickGizmo()
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f; // 카메라에서 10 유닛 앞
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f; // 2D 게임이므로 Z를 0으로

            // 클릭 기즈모 데이터 생성
            var clickData = new ClickGizmoData(worldPosition, _clickGizmoDuration);
            _clickGizmos.Add(clickData);

            // 선 기즈모 데이터 생성
            if (_showLineGizmos && _shotStartTrans != null)
            {
                var lineData = new LineGizmoData(_shotStartTrans.position, worldPosition, _lineGizmoDuration);
                _lineGizmos.Add(lineData);
            }

            Debug.Log($"클릭 기즈모 생성: {worldPosition}");
        }

        private void CreateDragGizmo()
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f; // 카메라에서 10 유닛 앞
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f; // 2D 게임이므로 Z를 0으로

            // 드래그 기즈모 데이터 생성 (더 짧은 지속시간)
            var dragData = new ClickGizmoData(worldPosition, _clickGizmoDuration * 0.5f);
            _clickGizmos.Add(dragData);

            // 선 기즈모 데이터 생성 (드래그용, 더 짧은 지속시간)
            if (_showLineGizmos && _shotStartTrans != null)
            {
                var lineData = new LineGizmoData(_shotStartTrans.position, worldPosition, _lineGizmoDuration * 0.5f);
                _lineGizmos.Add(lineData);
            }
        }

        private void CleanupExpiredGizmos()
        {
            _clickGizmos.RemoveAll(gizmo => gizmo.IsExpired);
            _lineGizmos.RemoveAll(gizmo => gizmo.IsExpired);
        }

        private void EvaluateState()
        {
            // 마우스가 눌려있으면 Attack 상태 유지
            if (Input.GetMouseButton(0))
            {
                ChangeState(EHunterState.Attack, null);
                return;
            }

            // 타겟이 있는지 확인
            bool hasTarget = FindNearestTarget() != null;

            // 상태 결정
            if (hasTarget)
            {
                ChangeState(EHunterState.AutoAttack, null);
            }
            else
            {
                ChangeState(EHunterState.Idle, null);
            }
        }

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

            _lastAttackTime = Time.time;
            _canAttack = false;

            var fireDirection = GetFireDirection();
            for (int i = 0; i < _bulletSet.Length; i++)
            {
                if (_bulletSet[i] != null)
                {
                    var randomAngle = UnityEngine.Random.Range(-_raycastAngle / 2f, _raycastAngle / 2f);
                    var bulletDirection = GetRotatedDirection(fireDirection, randomAngle);

                    StartCoroutine(FireBullet(_bulletSet[i], bulletDirection));
                }
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

        private void InitializeBullets()
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

        private void OnDrawGizmos()
        {
            if (_showClickGizmos)
            {
                DrawClickGizmos();
            }

            if (_showLineGizmos)
            {
                DrawLineGizmos();
            }

            if (_showRaycastGizmos)
            {
                DrawRaycastGizmos();
            }
        }

        private void DrawClickGizmos()
        {
            foreach (var clickData in _clickGizmos)
            {
                if (clickData.IsExpired)
                    continue;

                // 시간에 따른 투명도 계산
                float elapsed = Time.time - clickData.createTime;
                float alpha = 1f - (elapsed / clickData.duration);
                
                // 색상에 알파값 적용
                Color gizmoColor = _clickGizmoColor;
                gizmoColor.a = alpha;
                Gizmos.color = gizmoColor;

                // 원 그리기
                Gizmos.DrawWireSphere(clickData.position, _clickGizmoRadius);
                
                // 내부 원도 그리기 (더 작은 반지름)
                Gizmos.DrawWireSphere(clickData.position, _clickGizmoRadius * 0.7f);
            }
        }

        private void DrawLineGizmos()
        {
            foreach (var lineData in _lineGizmos)
            {
                if (lineData.IsExpired)
                    continue;

                // 시간에 따른 투명도 계산
                float elapsed = Time.time - lineData.createTime;
                float alpha = 1f - (elapsed / lineData.duration);
                
                // 색상에 알파값 적용
                Color lineColor = _lineGizmoColor;
                lineColor.a = alpha;
                Gizmos.color = lineColor;

                // 선 그리기
                Gizmos.DrawLine(lineData.startPosition, lineData.endPosition);
                
                // 시작점과 끝점에 작은 구체 그리기
                Gizmos.DrawWireSphere(lineData.startPosition, 0.1f);
                Gizmos.DrawWireSphere(lineData.endPosition, 0.1f);
            }
        }

        private void HandleAutoAim()
        {
            // Attack 상태일 때는 마우스 조준
            if (_currState == EHunterState.Attack)
            {
                if (Input.GetMouseButton(0))
                {
                    RotateWeaponToMouse();
                }
                return;
            }

            // AutoAttack 상태일 때만 자동 조준
            if (_currState == EHunterState.AutoAttack)
            {
                // 현재 타겟이 없거나 타겟이 사라졌으면 새로운 타겟 찾기
                if (_currentTarget == null || !IsTargetValid(_currentTarget))
                {
                    _currentTarget = FindNearestTarget();
                }

                // 타겟이 있으면 자동 조준
                if (_currentTarget != null)
                {
                    RotateWeaponToTarget(_currentTarget);
                    _isAutoAiming = true;
                }
                else
                {
                    _isAutoAiming = false;
                }
            }
            else
            {
                _isAutoAiming = false;
                _currentTarget = null;
            }
        }

        private Transform FindNearestTarget()
        {
            // Circle Collider 2D의 반지름 가져오기
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
                return null;

            float detectionRadius = circleCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            Vector3 center = transform.position;

            // 주변의 타겟 찾기 (EnemyLine 레이어들만)
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, detectionRadius);
            
            Transform nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D collider in colliders)
            {
                // 자기 자신은 제외
                if (collider.gameObject == gameObject)
                    continue;

                // EnemyLine 레이어들만 타겟으로 설정
                if (!IsEnemyLineLayer(collider.gameObject))
                    continue;

                float distance = Vector3.Distance(center, collider.transform.position);
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

            // 타겟이 여전히 존재하는지 확인
            if (target.gameObject == null)
                return false;

            // EnemyLine 레이어들만 유효한 타겟
            if (!IsEnemyLineLayer(target.gameObject))
                return false;

            // Circle Collider 2D 범위 내에 있는지 확인
            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
                return false;

            float detectionRadius = circleCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            float distance = Vector3.Distance(transform.position, target.position);
            
            return distance <= detectionRadius;
        }

        private void RotateWeaponToTarget(Transform target)
        {
            if (_OBJ_Weapon == null || target == null)
                return;

            // 무기 위치에서 타겟 위치까지의 방향 계산
            Vector3 direction = (target.position - _OBJ_Weapon.transform.position).normalized;
            
            // 목표 각도 계산
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // 현재 각도
            float currentAngle = _OBJ_Weapon.transform.eulerAngles.z;
            
            // 각도 차이 계산 (가장 짧은 경로로 회전)
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            
            // 부드러운 회전
            float newAngle = currentAngle + angleDifference * _autoAimSpeed * Time.deltaTime;
            
            // 무기의 Z 회전값 설정
            Vector3 currentRotation = _OBJ_Weapon.transform.eulerAngles;
            currentRotation.z = newAngle;
            _OBJ_Weapon.transform.eulerAngles = currentRotation;
        }

        private void RotateWeaponToMouse()
        {
            if (_OBJ_Weapon == null)
                return;

            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f;

            // 무기 위치에서 마우스 위치까지의 방향 계산
            Vector3 direction = (worldPosition - _OBJ_Weapon.transform.position).normalized;
            
            // Z축 회전 각도 계산 (Atan2는 -180도 ~ 180도 반환)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // 무기의 Z 회전값 설정
            Vector3 currentRotation = _OBJ_Weapon.transform.eulerAngles;
            currentRotation.z = angle;
            _OBJ_Weapon.transform.eulerAngles = currentRotation;
        }

        private void DrawRaycastGizmos()
        {
            // 마우스가 눌려있을 때만 레이캐스트 기즈모 표시
            if (!Input.GetMouseButton(0) || _shotStartTrans == null)
                return;

            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 10f;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f;

            // _shotStartTrans에서 마우스 위치까지의 방향 계산
            Vector3 direction = (worldPosition - _shotStartTrans.position).normalized;
            
            // 기본 방향을 기준으로 각도 계산
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // -15도와 +15도 레이캐스트 그리기
            for (int i = 0; i <= 10; i++) // 10개의 선으로 부드럽게 표시
            {
                float angle = Mathf.Lerp(-_raycastAngle, _raycastAngle, i / 10f);
                float totalAngle = baseAngle + angle;
                float angleRad = totalAngle * Mathf.Deg2Rad;
                
                Vector3 rayDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
                Vector3 rayEnd = _shotStartTrans.position + rayDirection * _raycastRange;
                
                // 투명도 계산 (중앙에서 가장 진하고, 끝으로 갈수록 투명)
                float alpha = 1f - Mathf.Abs(i - 5f) / 5f; // 중앙(5)에서 1, 끝에서 0
                Color rayColor = _raycastGizmoColor;
                rayColor.a = alpha * 0.7f; // 전체적으로 약간 투명하게
                Gizmos.color = rayColor;
                
                Gizmos.DrawLine(_shotStartTrans.position, rayEnd);
            }
            
            // 시작점에 구체 그리기
            Gizmos.color = _raycastGizmoColor;
            Gizmos.DrawWireSphere(_shotStartTrans.position, 0.2f);
        }

        [ContextMenu("테스트 클릭 기즈모 생성")]
        private void TestCreateClickGizmo()
        {
            if (_showClickGizmos)
            {
                CreateClickGizmo();
            }
        }
    }
}