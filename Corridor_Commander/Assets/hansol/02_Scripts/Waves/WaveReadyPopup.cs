using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class WaveReadyPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private DotweenUiPanelTransition panelTransition;
        [SerializeField] private Text messageText;
        [SerializeField] private TMP_Text messageTmpText;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button cancelButton;

        private WaveDirector director;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            HideImmediate();

            if (readyButton != null)
            {
                readyButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Cancel);
            }
        }

        private void OnDestroy()
        {
            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
            }
        }

        public void Bind(WaveDirector owner)
        {
            director = owner;
        }

        public void Show(string message)
        {
            SetText(message);

            if (panelTransition != null)
            {
                panelTransition.Show();
            }
            else if (root != null)
            {
                root.SetActive(true);
            }

            PopupDimOverlayController.RequestShow(this, root != null ? root.transform : transform);
            SelectReadyButton();
        }

        public void Hide()
        {
            if (panelTransition != null)
            {
                panelTransition.Hide();
            }
            else if (root != null)
            {
                root.SetActive(false);
            }

            PopupDimOverlayController.Release(this);
        }

        private void HideImmediate()
        {
            if (panelTransition != null)
            {
                panelTransition.HideImmediate();
            }
            else if (root != null)
            {
                root.SetActive(false);
            }

            PopupDimOverlayController.Release(this);
        }

        private void Confirm()
        {
            director?.ConfirmReady();
        }

        private void Cancel()
        {
            director?.CancelReady();
        }

        private void SelectReadyButton()
        {
            if (readyButton == null || !readyButton.gameObject.activeInHierarchy || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(readyButton.gameObject);
        }

        private void SetText(string message)
        {
            if (messageTmpText != null)
            {
                messageTmpText.text = message;
                return;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }
        }
    }
}
