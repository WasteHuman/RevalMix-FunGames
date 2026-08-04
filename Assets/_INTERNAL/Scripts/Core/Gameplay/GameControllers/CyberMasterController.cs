using Core.Common;
using Core.Data.Cyber21;
using System.Collections.Generic;
using UI.CyberMaster;
using UnityEngine;

namespace Core.Gameplay.GameControllers
{
    public class CyberMasterController : GameController
    {
        [Header("Cards Setup")]
        [SerializeField] private List<CardData> _deckCards;

        [Space(5), Header("View Setup")]
        [SerializeField] private CyberMasterView _view;

        private List<CardData> _playerHand;

        private bool _isGameActive;

        private int _currentScore;
        private int _currentBet;
        private int _baseReward;

        public override void Enter()
        {
        }

        public override void Initialize()
        {
        }

        public override void Exit()
        {
        }
    }
}