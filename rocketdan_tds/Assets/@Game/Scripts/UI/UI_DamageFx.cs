// ----- System
using System.Collections;
using System.Collections.Generic;
using TMPro;


// ----- Unity
using UnityEngine;

namespace Game
{
    public class UI_DamageFx : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [SerializeField] private TextMeshPro _TMP_DamageText = null;

        // --------------------------------------------------
        // Methods - Event
        // --------------------------------------------------
        private void Awake()
        {
            _TMP_DamageText.gameObject.SetActive(false);
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        public void Show(int damage)
        {
            StartCoroutine(Co_Show(damage));
        }

        // --------------------------------------------------
        // Methods - Coroutines
        // --------------------------------------------------
        private IEnumerator Co_Show(int damage)
        {
            _TMP_DamageText.gameObject.SetActive(true);
            _TMP_DamageText.text = $"{damage}";
            
            _TMP_DamageText.transform.SetParent(null);
            _TMP_DamageText.transform.position = transform.position;
            
            var startPosition = _TMP_DamageText.transform.position;
            var startColor = _TMP_DamageText.color;
            startColor.a = 1f;
            _TMP_DamageText.color = startColor;
            
            var randomX = Random.Range(-2f, 2f);
            var peakHeight = 1.5f; 
            var targetPosition = startPosition + new Vector3(randomX, 0f, 0f);
            var duration = 1f;
            var elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var currentX = Mathf.Lerp(startPosition.x, targetPosition.x, progress);
                
                var normalizedProgress = progress;
                var currentY = -4f * peakHeight * (normalizedProgress - 0.5f) * (normalizedProgress - 0.5f) + peakHeight;
                
                _TMP_DamageText.transform.position = new Vector3(currentX, startPosition.y + currentY, startPosition.z);
                
                var currentColor = _TMP_DamageText.color;
                if (progress <= 0.5f)
                    currentColor.a = 1f;
                else
                {
                    var fallProgress = (progress - 0.5f) / 0.5f; // 0~1 범위로 정규화
                    currentColor.a = Mathf.Lerp(1f, 0f, fallProgress);
                }
                
                _TMP_DamageText.color = currentColor;
                yield return null;
            }
            
            _TMP_DamageText.transform.SetParent(transform);
            _TMP_DamageText.transform.localPosition = Vector3.zero;

            var finalColor = _TMP_DamageText.color;
            finalColor.a = 0f;
            _TMP_DamageText.color = finalColor;
            _TMP_DamageText.gameObject.SetActive(false);
        }
    }
}