using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Other
{
    [RequireComponent(typeof(ActionButton))]
    public class BackToMainMenuButton : MonoBehaviour
    {
        private ActionButton _actionButton;

        private void Awake()
        {
            _actionButton = GetComponent<ActionButton>();
        }

        private void Start()
        {
            _actionButton.OnButtonClick += HandleClick;
        }

        private void HandleClick() => SceneManager.LoadSceneAsync(SceneNames.MAIN_MENU);
    }
}