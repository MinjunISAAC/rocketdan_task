// ----- System
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class EnemySpawner : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [SerializeField] private EnemyBase _enemyPrefab = null;
        [SerializeField] private Transform _spawnPoint = null;
        [SerializeField] private Transform _enemyParent = null;

        // --------------------------------------------------
        // Properties
        // --------------------------------------------------
        public Transform SpawnPoint => _spawnPoint;

        // --------------------------------------------------
        // Methods - Events
        // --------------------------------------------------
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SpawnEnemy(0);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                SpawnEnemy(1);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                SpawnEnemy(2);
        }

        // --------------------------------------------------
        // Methods - Normals
        // --------------------------------------------------
        public void SpawnEnemy(int spawnLayer)
        {
            var enemy = Instantiate(_enemyPrefab, _spawnPoint.position, Quaternion.identity);
            enemy.Spawn(EEnemyType.Zombie_0, spawnLayer, _spawnPoint, _enemyParent);
        }
    }
}