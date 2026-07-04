using UnityEngine;
using UnityEngine.InputSystem;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class MapExpansionDoorInteraction : MonoBehaviour, IInteractionPromptSource
    {
        private const string PromptMessage = "E  Open Door";

        [SerializeField] private MapExpansionDoorOpener doorOpener;
        [SerializeField] private GameObject interactionPromptRoot;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 1.15f, 0f);

        private bool playerInRange;
        private Transform currentPlayer;
        private bool isPromptVisible;

        public bool IsPromptVisible => isPromptVisible;
        public string PromptText => PromptMessage;
        public Vector3 PromptWorldPosition => transform.position + promptWorldOffset;
        public float PromptDistanceSqr => currentPlayer != null
            ? Vector3.SqrMagnitude(currentPlayer.position - transform.position)
            : float.MaxValue;
        public int PromptPriority => 0;

        private void OnEnable()
        {
            InteractionPromptPresenter.Register(this);
        }

        private void OnDisable()
        {
            InteractionPromptPresenter.Unregister(this);
            isPromptVisible = false;
        }

        private void Awake()
        {
            Collider doorCollider = GetComponent<Collider>();
            doorCollider.isTrigger = true;

            if (doorOpener == null)
            {
                doorOpener = GetComponentInParent<MapExpansionDoorOpener>();
            }

            SetPromptActive(false);
        }

        private void Update()
        {
            bool canInteract = doorOpener != null
                && !doorOpener.IsOpen
                && playerInRange
                && UiInputCoordinator.Instance.CanUseWorldInteraction(this);

            isPromptVisible = canInteract;
            SetPromptActive(false);
            if (!canInteract)
            {
                return;
            }

            if (KeyboardInputMessenger.WasInteractPressed()
                && InteractionPromptPresenter.IsBestVisibleSource(this)
                && UiInputCoordinator.Instance.TryConsumeInteract(this))
            {
                doorOpener.Open();
                isPromptVisible = false;
                SetPromptActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInRange = true;
            currentPlayer = other.transform;
            SetPromptActive(false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            playerInRange = false;
            currentPlayer = null;
            isPromptVisible = false;
            SetPromptActive(false);
        }

        private bool IsPlayer(Collider other)
        {
            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return true;
            }

            Transform current = other.transform;
            while (current != null)
            {
                if (current.CompareTag(playerTag))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void SetPromptActive(bool active)
        {
            if (interactionPromptRoot != null)
            {
                interactionPromptRoot.SetActive(active);
            }
        }
    }
}
