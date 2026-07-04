using UnityEngine;
using UnityEngine.UI;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class SpecimenExtractionInteraction : MonoBehaviour, IInteractionPromptSource
    {
        private const string PromptMessage = "E  목표 확인";

        [SerializeField] private ExtractionObjectiveController objectiveController;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 2.1f, 0f);
        [SerializeField] private int promptPriority = 20;
        [SerializeField] private Collider[] interactionColliders;
        [SerializeField] private Light[] glowLights;
        [SerializeField] private Renderer[] emissionRenderers;
        [SerializeField] private Material emissionMaterial;
        [Header("UI")]
        [SerializeField] private GameObject objectiveMarkerRoot;
        [SerializeField] private Text objectiveMarkerText;
        [SerializeField] private string objectiveMarkerMessage = "목표";

        private GameObject currentPlayer;
        private bool isPromptVisible;

        public bool IsPromptVisible => isPromptVisible;
        public string PromptText => PromptMessage;
        public Vector3 PromptWorldPosition => transform.position + promptWorldOffset;
        public float PromptDistanceSqr => currentPlayer != null
            ? GetInteractionDistanceSqr(currentPlayer.transform.position)
            : float.MaxValue;
        public int PromptPriority => promptPriority;

        private void OnEnable()
        {
            ResolveInteractionColliders();
            InteractionPromptPresenter.Register(this);
            SetGlowActive(true);
        }

        private void OnDisable()
        {
            InteractionPromptPresenter.Unregister(this);
            isPromptVisible = false;
            SetObjectiveMarkerVisible(false);
        }

        private void Awake()
        {
            ResolveInteractionColliders();
            SetGlowActive(true);
            SetObjectiveMarkerVisible(false);
        }

        private void Update()
        {
            currentPlayer = FindClosestPlayer();
            bool canInteract = CanInteract();
            isPromptVisible = canInteract;
            SetObjectiveMarkerVisible(canInteract);

            if (!canInteract)
            {
                return;
            }

            if (KeyboardInputMessenger.WasInteractPressed()
                && InteractionPromptPresenter.IsBestVisibleSource(this)
                && UiInputCoordinator.Instance.TryConsumeInteract(this))
            {
                objectiveController.BeginEscapeSequence(this);
                isPromptVisible = false;
                SetObjectiveMarkerVisible(false);
            }
        }

        private GameObject FindClosestPlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
            GameObject closestPlayer = null;
            float closestDistance = interactionRange * interactionRange;
            for (int i = 0; i < players.Length; i++)
            {
                GameObject player = players[i];
                if (player == null)
                {
                    continue;
                }

                float distance = GetInteractionDistanceSqr(player.transform.position);
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }

        private bool CanInteract()
        {
            return currentPlayer != null
                && objectiveController != null
                && !objectiveController.EscapeStarted
                && UiInputCoordinator.Instance.CanUseWorldInteraction(this);
        }

        private float GetInteractionDistanceSqr(Vector3 worldPosition)
        {
            Vector3 closestPoint = GetClosestInteractionPoint(worldPosition);
            return Vector3.SqrMagnitude(worldPosition - closestPoint);
        }

        private Vector3 GetClosestInteractionPoint(Vector3 worldPosition)
        {
            ResolveInteractionColliders();
            bool hasClosestPoint = false;
            Vector3 closestPoint = transform.position;
            float closestDistanceSqr = float.MaxValue;

            if (interactionColliders != null)
            {
                for (int i = 0; i < interactionColliders.Length; i++)
                {
                    Collider target = interactionColliders[i];
                    if (target != null && target.enabled && target.gameObject.activeInHierarchy)
                    {
                        Vector3 candidate = target.ClosestPoint(worldPosition);
                        float distanceSqr = Vector3.SqrMagnitude(worldPosition - candidate);
                        if (distanceSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            closestPoint = candidate;
                            hasClosestPoint = true;
                        }
                    }
                }
            }

            if (hasClosestPoint)
            {
                return closestPoint;
            }

            Renderer[] renderers = emissionRenderers != null && emissionRenderers.Length > 0
                ? emissionRenderers
                : GetComponentsInChildren<Renderer>(true);
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer target = renderers[i];
                    if (target != null && target.enabled && target.gameObject.activeInHierarchy)
                    {
                        Vector3 candidate = target.bounds.ClosestPoint(worldPosition);
                        float distanceSqr = Vector3.SqrMagnitude(worldPosition - candidate);
                        if (distanceSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            closestPoint = candidate;
                            hasClosestPoint = true;
                        }
                    }
                }
            }

            return hasClosestPoint ? closestPoint : transform.position;
        }

        private void ResolveInteractionColliders()
        {
            if (HasInteractionCollider())
            {
                return;
            }

            interactionColliders = GetComponentsInChildren<Collider>(true);
        }

        private bool HasInteractionCollider()
        {
            if (interactionColliders == null)
            {
                return false;
            }

            for (int i = 0; i < interactionColliders.Length; i++)
            {
                if (interactionColliders[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetGlowActive(bool active)
        {
            if (glowLights != null)
            {
                for (int i = 0; i < glowLights.Length; i++)
                {
                    if (glowLights[i] != null)
                    {
                        glowLights[i].enabled = active;
                    }
                }
            }

            if (active && emissionMaterial != null && emissionRenderers != null)
            {
                for (int i = 0; i < emissionRenderers.Length; i++)
                {
                    Renderer target = emissionRenderers[i];
                    if (target != null)
                    {
                        target.sharedMaterial = emissionMaterial;
                    }
                }
            }
        }

        private void SetObjectiveMarkerVisible(bool visible)
        {
            if (objectiveMarkerText != null)
            {
                objectiveMarkerText.text = objectiveMarkerMessage;
            }

            if (objectiveMarkerRoot != null)
            {
                objectiveMarkerRoot.SetActive(visible);
            }
        }
    }
}
