// ----- System
using System.Collections;
using System.Collections.Generic;

// ----- Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Game
{
    public class UI_MainScene : MonoBehaviour
    {
        // --------------------------------------------------
        // Components
        // --------------------------------------------------
        [Space(1.5f)]
        [Header("1. 에너지 그룹")]
        [SerializeField] private Slider _sliderEnergy = null;
        [SerializeField] private TextMeshProUGUI _TMP_Energy = null;

        [Space(1.5f)]
        [Header("2. 공격 그룹")]
        [SerializeField] private Button _BTN_Bomb = null;

        // --------------------------------------------------
        // Variables
        // --------------------------------------------------
        private const int BOMB_ENERGY = 2;

        private Action<int> _onUsedItem = null;

        // --------------------------------------------------
        // Methods - Event
        // --------------------------------------------------
        private void Awake()
        {
            _sliderEnergy.value = 0f;
            _TMP_Energy.text = "0";
        }

        // --------------------------------------------------
        // Methods - Normal
        // --------------------------------------------------
        public void Init(Action<int> onEnergyChanged)
        {
            _onUsedItem = onEnergyChanged;
            _BTN_Bomb.onClick.AddListener(OnClickBombButton);
        }

        public void SetEnergy(int energy)
        {
            _sliderEnergy.value = energy;
            _TMP_Energy.text = energy.ToString();
        }

        #region [Button Event Group]
        private void OnClickBombButton()
        {
            _onUsedItem?.Invoke(BOMB_ENERGY);
        }
        #endregion
    }
}