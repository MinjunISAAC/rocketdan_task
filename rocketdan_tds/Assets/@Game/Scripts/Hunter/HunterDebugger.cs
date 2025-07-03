#if UNITY_EDITOR
// ----- System
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class HunterDebugger : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [SerializeField] private HunterBase _hunterBase = null;
        [SerializeField] private Transform _shotStartTrans = null;

        // --------------------------------------------------
        // Gizmo Settings
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

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private List<ClickGizmoData> _clickGizmos = new List<ClickGizmoData>();
        private List<LineGizmoData> _lineGizmos = new List<LineGizmoData>();

        // --------------------------------------------------
        // Methods - Events
        // --------------------------------------------------
        private void Update()
        {
            CleanupExpiredGizmos();
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        public void CreateClickGizmo()
        {
            var mousePosition = Input.mousePosition;
            mousePosition.z = 10f;
            var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f;

            var clickData = new ClickGizmoData(worldPosition, _clickGizmoDuration);
            _clickGizmos.Add(clickData);

            if (_showLineGizmos && _shotStartTrans != null)
            {
                var lineData = new LineGizmoData(_shotStartTrans.position, worldPosition, _lineGizmoDuration);
                _lineGizmos.Add(lineData);
            }
        }

        public void CreateDragGizmo()
        {
            var mousePosition = Input.mousePosition;
            mousePosition.z = 10f;
            var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f;

            var dragData = new ClickGizmoData(worldPosition, _clickGizmoDuration * 0.5f);
            _clickGizmos.Add(dragData);

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

        // --------------------------------------------------
        // Methods - Gizmos
        // --------------------------------------------------
        private void OnDrawGizmos()
        {
            if (_showClickGizmos)
                DrawClickGizmos();

            if (_showLineGizmos)
                DrawLineGizmos();

            if (_showRaycastGizmos)
                DrawRaycastGizmos();
        }

        private void DrawClickGizmos()
        {
            foreach (var clickData in _clickGizmos)
            {
                if (clickData.IsExpired)
                    continue;

                var elapsed = Time.time - clickData.createTime;
                var alpha = 1f - (elapsed / clickData.duration);
                
                Color gizmoColor = _clickGizmoColor;
                gizmoColor.a = alpha;

                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(clickData.position, _clickGizmoRadius);
                Gizmos.DrawWireSphere(clickData.position, _clickGizmoRadius * 0.7f);
            }
        }

        private void DrawLineGizmos()
        {
            foreach (var lineData in _lineGizmos)
            {
                if (lineData.IsExpired)
                    continue;

                var elapsed = Time.time - lineData.createTime;
                var alpha = 1f - (elapsed / lineData.duration);
                
                Color lineColor = _lineGizmoColor;
                lineColor.a = alpha;
                Gizmos.color = lineColor;

                Gizmos.DrawLine(lineData.startPosition, lineData.endPosition);
                Gizmos.DrawWireSphere(lineData.startPosition, 0.1f);
                Gizmos.DrawWireSphere(lineData.endPosition, 0.1f);
            }
        }

        private void DrawRaycastGizmos()
        {
            if (!Input.GetMouseButton(0) || _shotStartTrans == null)
                return;

            var mousePosition = Input.mousePosition;
            mousePosition.z = 10f;
            var worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            worldPosition.z = 0f;

            var direction = (worldPosition - _shotStartTrans.position).normalized;
            
            var baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            for (int i = 0; i <= 10; i++)
            {
                var angle = Mathf.Lerp(-_raycastAngle, _raycastAngle, i / 10f);
                var totalAngle = baseAngle + angle;
                var angleRad = totalAngle * Mathf.Deg2Rad;
                var rayDirection = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f);
                var rayEnd = _shotStartTrans.position + rayDirection * _raycastRange;
                
                var alpha = 1f - Mathf.Abs(i - 5f) / 5f;
                Color rayColor = _raycastGizmoColor;
                rayColor.a = alpha * 0.7f;
                Gizmos.color = rayColor;
                
                Gizmos.DrawLine(_shotStartTrans.position, rayEnd);
            }
            
            Gizmos.color = _raycastGizmoColor;
            Gizmos.DrawWireSphere(_shotStartTrans.position, 0.2f);
        }

        // --------------------------------------------------
        // Data Classes
        // --------------------------------------------------
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

        // --------------------------------------------------
        // Context Menu
        // --------------------------------------------------
        [ContextMenu("테스트 클릭 기즈모 생성")]
        private void TestCreateClickGizmo()
        {
            if (_showClickGizmos)
                CreateClickGizmo();
        }
    }
}
#endif 