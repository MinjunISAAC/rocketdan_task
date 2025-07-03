// ----- System
using System;
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class BombBase : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [SerializeField] private GameObject _model = null;
        [SerializeField] private ParticleSystem _explosionEffect = null;

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const float EXPLOSION_DELAY = 3f;
        private const float EXPLOSION_EFFECT_DELAY = 1.5f;
        private const float MAX_DAMAGE = 100f;
        private const float MIN_DAMAGE = 25f;

        private List<GameObject> _targetsInRange = new List<GameObject>();
        private bool _isExploded = false;
        private float _damage = 0f;

        // --------------------------------------------------
        // Methods - Events
        // --------------------------------------------------
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isExploded) 
                return;

            if (IsEnemyLineLayer(collision.gameObject))
            {
                if (!_targetsInRange.Contains(collision.gameObject))
                    _targetsInRange.Add(collision.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (_isExploded) 
                return;

            if (_targetsInRange.Contains(collision.gameObject))
                _targetsInRange.Remove(collision.gameObject);
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        public void Fire(Action doneCallBack = null)
        {
            _damage = UnityEngine.Random.Range(MIN_DAMAGE, MAX_DAMAGE);

            if (_isExploded) 
                return;

            StartCoroutine(Co_ExplodeAfterDelay(doneCallBack));
        }

        private bool IsEnemyLineLayer(GameObject targetObject)
        {
            var targetLayer = targetObject.layer;
            var isEnemyLineLayer = targetLayer == LayerMask.NameToLayer("EnemyLine_0") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_1") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_2");
            
            return isEnemyLineLayer;
        }

        // --------------------------------------------------
        // Methods - Coroutines
        // --------------------------------------------------
        private IEnumerator Co_ExplodeAfterDelay(Action doneCallBack)
        {
            yield return new WaitForSeconds(EXPLOSION_DELAY);
            
            _isExploded = true;

            foreach (var target in _targetsInRange)
            {
                if (target != null)
                {
                    var enemy = target.GetComponent<EnemyBase>();
                    if (enemy != null)
                        enemy.Hit((int)_damage);
                }
            }

            _model.SetActive(false);
            _explosionEffect.Play();
            _explosionEffect.transform.SetParent(null);
            yield return new WaitForSeconds(EXPLOSION_EFFECT_DELAY);
            
            Destroy(_explosionEffect.gameObject);
            Destroy(gameObject);
            doneCallBack?.Invoke();
        }
    }
}