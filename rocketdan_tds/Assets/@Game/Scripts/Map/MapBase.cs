// ----- System
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class MapBase : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [Header("1. 배경 그룹")]
        [SerializeField] private SpriteRenderer[] _floorSpriteRenderers = new SpriteRenderer[3];
        [SerializeField] private SpriteRenderer[] _backgroundLayer0_SpriteRenderers = new SpriteRenderer[3];
        [SerializeField] private SpriteRenderer[] _backgroundLayer1_SpriteRenderers = new SpriteRenderer[3];
        [SerializeField] private SpriteRenderer[] _backgroundLayer2_SpriteRenderers = new SpriteRenderer[3];

        [Space(1.5f)]
        [Header("2. 바닥 이동 옵션")]
        [SerializeField] private float _floorMoveSpeed = 5f;
        [SerializeField] private float _resetPositionX = -20f; // 이 X 위치를 지나면 오른쪽으로 재배치

        [Space(1.5f)]
        [Header("3. 배경 이동 옵션")]
        [SerializeField] private float[] _backgroundMoveSpeeds = new float[3] { 2f, 3f, 4f }; // 각 배경 레이어별 속도

        // --------------------------------------------------
        // Methods - Event
        // --------------------------------------------------
        private void Start()
        {
            SetupFloorPositions();
            SetupBackgroundPositions();
        }

        private void Update()
        {
            MoveFloors();
            CheckAndRepositionFloors();
            MoveBackgrounds();
            CheckAndRepositionBackgrounds();
        }
        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        private void SetupFloorPositions()
        {
            var currentPosition = _floorSpriteRenderers[0].transform.position;
            
            for (int i = 1; i < _floorSpriteRenderers.Length && i < 3; i++)
            {
                var prevRenderer = _floorSpriteRenderers[i - 1];
                var prevWidth = prevRenderer.bounds.size.x;
                
                currentPosition.x += prevWidth;
                _floorSpriteRenderers[i].transform.position = currentPosition;
            }
        }

        private void MoveFloors()
        {
            for (int i = 0; i < _floorSpriteRenderers.Length; i++)
            {
                if (_floorSpriteRenderers[i] != null)
                {
                    Vector3 position = _floorSpriteRenderers[i].transform.position;
                    position.x -= _floorMoveSpeed * Time.deltaTime;
                    _floorSpriteRenderers[i].transform.position = position;
                }
            }
        }

        private void SetupBackgroundPositions()
        {
            // 배경 레이어 0 설정
            SetupBackgroundLayer(_backgroundLayer0_SpriteRenderers);
            // 배경 레이어 1 설정
            SetupBackgroundLayer(_backgroundLayer1_SpriteRenderers);
            // 배경 레이어 2 설정
            SetupBackgroundLayer(_backgroundLayer2_SpriteRenderers);
        }

        private void SetupBackgroundLayer(SpriteRenderer[] backgroundLayer)
        {
            if (backgroundLayer == null || backgroundLayer.Length == 0 || backgroundLayer[0] == null)
                return;

            var currentPosition = backgroundLayer[0].transform.position;
            
            for (int i = 1; i < backgroundLayer.Length && i < 3; i++)
            {
                if (backgroundLayer[i] != null && backgroundLayer[i - 1] != null)
                {
                    var prevRenderer = backgroundLayer[i - 1];
                    var prevWidth = prevRenderer.bounds.size.x;
                    
                    currentPosition.x += prevWidth;
                    backgroundLayer[i].transform.position = currentPosition;
                }
            }
        }

        private void MoveBackgrounds()
        {
            // 각 배경 레이어를 다른 속도로 이동
            MoveBackgroundLayer(_backgroundLayer0_SpriteRenderers, 0);
            MoveBackgroundLayer(_backgroundLayer1_SpriteRenderers, 1);
            MoveBackgroundLayer(_backgroundLayer2_SpriteRenderers, 2);
        }

        private void MoveBackgroundLayer(SpriteRenderer[] backgroundLayer, int layerIndex)
        {
            if (backgroundLayer == null)
                return;

            // 속도 배열의 범위를 벗어나지 않도록 확인
            float moveSpeed = (layerIndex < _backgroundMoveSpeeds.Length) ? _backgroundMoveSpeeds[layerIndex] : _backgroundMoveSpeeds[0];

            for (int i = 0; i < backgroundLayer.Length; i++)
            {
                if (backgroundLayer[i] != null)
                {
                    Vector3 position = backgroundLayer[i].transform.position;
                    position.x -= moveSpeed * Time.deltaTime;
                    backgroundLayer[i].transform.position = position;
                }
            }
        }

        private void CheckAndRepositionBackgrounds()
        {
            // 각 배경 레이어별로 재배치 확인
            CheckAndRepositionBackgroundLayer(_backgroundLayer0_SpriteRenderers);
            CheckAndRepositionBackgroundLayer(_backgroundLayer1_SpriteRenderers);
            CheckAndRepositionBackgroundLayer(_backgroundLayer2_SpriteRenderers);
        }

        private void CheckAndRepositionBackgroundLayer(SpriteRenderer[] backgroundLayer)
        {
            if (backgroundLayer == null)
                return;

            // 가장 왼쪽으로 나간 배경을 찾아서 오른쪽 끝으로 재배치
            for (int i = 0; i < backgroundLayer.Length; i++)
            {
                if (backgroundLayer[i] != null)
                {
                    // 배경이 재배치 기준점을 넘어갔는지 확인 (배경의 오른쪽 끝 기준)
                    float rightEdge = backgroundLayer[i].transform.position.x + (backgroundLayer[i].bounds.size.x * 0.5f);
                    
                    if (rightEdge < _resetPositionX)
                    {
                        // 가장 오른쪽에 있는 배경을 찾기
                        SpriteRenderer rightmostBackground = FindRightmostBackgroundInLayer(backgroundLayer);
                        
                        if (rightmostBackground != null)
                        {
                            // 가장 오른쪽 배경의 오른쪽 끝에 딱 붙여서 재배치
                            Vector3 newPosition = rightmostBackground.transform.position;
                            newPosition.x += rightmostBackground.bounds.size.x;
                            backgroundLayer[i].transform.position = newPosition;
                        }
                    }
                }
            }
        }

        private void CheckAndRepositionFloors()
        {
            // 가장 왼쪽으로 나간 바닥을 찾아서 오른쪽 끝으로 재배치
            for (int i = 0; i < _floorSpriteRenderers.Length; i++)
            {
                if (_floorSpriteRenderers[i] != null)
                {
                    // 바닥이 재배치 기준점을 넘어갔는지 확인 (바닥의 오른쪽 끝 기준)
                    float rightEdge = _floorSpriteRenderers[i].transform.position.x + (_floorSpriteRenderers[i].bounds.size.x * 0.5f);
                    
                    if (rightEdge < _resetPositionX)
                    {
                        // 가장 오른쪽에 있는 바닥을 찾기
                        SpriteRenderer rightmostFloor = FindRightmostFloor();
                        
                        if (rightmostFloor != null)
                        {
                            // 가장 오른쪽 바닥의 오른쪽 끝에 딱 붙여서 재배치
                            Vector3 newPosition = rightmostFloor.transform.position;
                            newPosition.x += rightmostFloor.bounds.size.x;
                            _floorSpriteRenderers[i].transform.position = newPosition;
                        }
                    }
                }
            }
        }

        private SpriteRenderer FindRightmostFloor()
        {
            SpriteRenderer rightmost = null;
            float maxX = float.MinValue;

            for (int i = 0; i < _floorSpriteRenderers.Length; i++)
            {
                if (_floorSpriteRenderers[i] != null)
                {
                    float currentX = _floorSpriteRenderers[i].transform.position.x;
                    if (currentX > maxX)
                    {
                        maxX = currentX;
                        rightmost = _floorSpriteRenderers[i];
                    }
                }
            }

            return rightmost;
        }

        private SpriteRenderer FindRightmostBackgroundInLayer(SpriteRenderer[] backgroundLayer)
        {
            SpriteRenderer rightmost = null;
            float maxX = float.MinValue;

            for (int i = 0; i < backgroundLayer.Length; i++)
            {
                if (backgroundLayer[i] != null)
                {
                    float currentX = backgroundLayer[i].transform.position.x;
                    if (currentX > maxX)
                    {
                        maxX = currentX;
                        rightmost = backgroundLayer[i];
                    }
                }
            }

            return rightmost;
        }
    }
}