// ----- System
using System;
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;

namespace Game
{
    public class MainScene : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [SerializeField] private UI_MainScene _uiMainScene = null;
        [SerializeField] private HunterBase _hunter = null;

        // --------------------------------------------------
        // Event
        // --------------------------------------------------
        public Action<int> OnEnergyChanged = null;

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const int MAX_ENERGY = 100;
        private const int ENERGY_PER_SECOND = 2;
        private int _currEnergy = 0;
        private float _time = 0f;

        // --------------------------------------------------
        // Methods - Event
        // --------------------------------------------------
        private void Awake()
        {
            _uiMainScene.SetEnergy(0);
        }

        private void Start()
        {
            OnEnergyChanged += OnUsedItem;
            _uiMainScene.Init(OnEnergyChanged);
        }

        private void Update()
        {
            _time += Time.deltaTime;

            if (_time >= ENERGY_PER_SECOND)
            {
                if (_currEnergy < MAX_ENERGY)
                {
                    _time = 0f;
                    _currEnergy++;
                    _uiMainScene.SetEnergy(_currEnergy);
                }
            }
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        private void OnUsedItem(int itemValue)
        {
            if (_currEnergy >= itemValue)
            {
                _hunter.FireBomb();
                _currEnergy -= itemValue;
                _uiMainScene.SetEnergy(_currEnergy);
            }
        }
    }
}