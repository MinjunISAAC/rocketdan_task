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
        [Header("1. 스폰 그룹")]
        [SerializeField] private Transform _spawnPoint = null;
        [SerializeField] private Transform _enemyParent = null;

        [Space(1.5f)]
        [Header("2. 스폰 데이터 그룹")]
        [SerializeField] private List<EnemyBase> _enemyBaseList = new List<EnemyBase>();
        [SerializeField] private float _spawnMaxInterval = 2f;
        
        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private bool _isSpawning = false;

        // --------------------------------------------------
        // Methods - Events
        // --------------------------------------------------
        private void Start()
        {
            StartAutoSpawn();
        }
        
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_isSpawning)
                    StopAutoSpawn();
                else
                    StartAutoSpawn();
            }
        }

        // --------------------------------------------------
        // Methods - Normals
        // --------------------------------------------------
        public void StartAutoSpawn()
        {
            if (!_isSpawning)
            {
                _isSpawning = true;
                StartCoroutine(Co_AutoSpawn());
            }
        }
        
        public void StopAutoSpawn()
        {
            _isSpawning = false;
        }
        
        public void SpawnEnemy()
        {
            var enemyBase = _enemyBaseList[Random.Range(0, _enemyBaseList.Count)];
            var randomSpawnLayer = Random.Range(0, 3);

            var enemy = Instantiate(enemyBase, _spawnPoint.position, Quaternion.identity);
            enemy.Spawn(EEnemyType.Zombie_0, 100, randomSpawnLayer, _spawnPoint, _enemyParent);
        }
        
        public void SpawnEnemy(int layer)
        {
            var enemyBase = _enemyBaseList[Random.Range(0, _enemyBaseList.Count)];
            var enemy = Instantiate(enemyBase, _spawnPoint.position, Quaternion.identity);
            enemy.Spawn(EEnemyType.Zombie_0, 100, layer, _spawnPoint, _enemyParent);
        }
        
        // --------------------------------------------------
        // Methods - Coroutines
        // --------------------------------------------------
        private IEnumerator Co_AutoSpawn()
        {
            while (_isSpawning)
            {
                SpawnEnemy();
                
                var delay = Random.Range(1f, 2f);
                yield return new WaitForSeconds((int)delay);
            }
        }
    }
}