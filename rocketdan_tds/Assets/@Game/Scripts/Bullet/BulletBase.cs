// ----- System
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class BulletBase : MonoBehaviour
    {
        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private float _power = 50f;
        
        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public float Power => _power;

        // --------------------------------------------------
        // Method - Events
        // --------------------------------------------------
        private void OnTriggerEnter2D(Collider2D other)
        {
            var targetLayer = other.gameObject.layer;
            var isEnemyLineLayer = targetLayer == LayerMask.NameToLayer("EnemyLine_0") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_1") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_2");

            if (isEnemyLineLayer)
            {
                var enemy = other.gameObject.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.Hit(_power);
                    gameObject.SetActive(false);
                }
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var targetLayer = other.gameObject.layer;
            var isEnemyLineLayer = targetLayer == LayerMask.NameToLayer("EnemyLine_0") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_1") ||
                                  targetLayer == LayerMask.NameToLayer("EnemyLine_2");

            if (isEnemyLineLayer)
            {
                var enemy = other.gameObject.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.Hit(_power);
                    gameObject.SetActive(false);
                }
            }
        }

        // --------------------------------------------------
        // Method - Normals
        // --------------------------------------------------
        public void SetPower(float power)
        {
            _power = power;
        }
    }
}