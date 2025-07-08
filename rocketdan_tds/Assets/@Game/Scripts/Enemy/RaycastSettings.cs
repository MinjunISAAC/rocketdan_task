// ----- Unity
using UnityEngine;

namespace Game
{
    [System.Serializable]
    public class RaycastSettings
    {
        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        [Header("레이캐스트 설정")]
        [SerializeField] public float distance = 1f;
        [SerializeField] public float heightOffset = 0f;
        [SerializeField] public float widthOffset = 0f;
        [SerializeField] public float angle = 0f;
        
        [Header("디버그 설정")]
        [SerializeField] public bool showDebug = true;
        [SerializeField] public Color debugColor = Color.green;
        [SerializeField] public Color hitColor = Color.red;

        // --------------------------------------------------
        // Constructor
        // --------------------------------------------------
        public RaycastSettings(float distance = 1f, float heightOffset = 0f, float widthOffset = 0f, float angle = 0f)
        {
            this.distance = distance;
            this.heightOffset = heightOffset;
            this.widthOffset = widthOffset;
            this.angle = angle;
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        public Vector3 GetOrigin(Vector3 basePosition)
        {
            return basePosition + Vector3.up * heightOffset + Vector3.right * widthOffset;
        }

        public Vector2 GetDirection(Vector2 baseDirection)
        {
            return Quaternion.Euler(0, 0, angle) * baseDirection;
        }

        public RaycastHit2D PerformRaycast(Vector3 basePosition, Vector2 baseDirection, GameObject ignoreObject = null)
        {
            Vector3 origin = GetOrigin(basePosition);
            Vector2 direction = GetDirection(baseDirection);
            
            // 레이캐스트 실행
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);

            // 지정된 오브젝트를 제외한 첫 번째 히트 찾기
            RaycastHit2D result = new RaycastHit2D();
            foreach (RaycastHit2D hit in hits)
            {
                if (ignoreObject == null || hit.collider.gameObject != ignoreObject)
                {
                    result = hit;
                    break;
                }
            }

            DrawRay(origin, direction, result.collider != null);

            return result;
        }

        public void DrawRay(Vector3 origin, Vector2 direction, bool hasHit)
        {
#if UNITY_EDITOR
            if (!showDebug) return;

            Color rayColor = hasHit ? hitColor : debugColor;
            Debug.DrawRay(origin, direction * distance, rayColor);
#endif
        }
    }
} 