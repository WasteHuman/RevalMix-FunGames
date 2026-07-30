using Core.Services;
using TMPro;
using UnityEngine;

namespace UI.Player
{
    public class PlayerInfoView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;

        private void Awake()
        {
            _nameLabel.text = GameServices.SaveService.PlayerData.Name;
        }

        private void Start()
        {
            
        }
    }
}