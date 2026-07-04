using System;
using System.Collections;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MapExpansionDoorOpener : MonoBehaviour
    {
        [SerializeField] private GameObject closedDoorRoot;
        [SerializeField] private GameObject openedDoorRoot;
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string openTriggerName = "Open";
        [SerializeField] private GameObject passageBlocker;
        [SerializeField] private float openCompletionDelay = 1f;
        [SerializeField] private bool keepOpenedDoorRootHiddenUntilOpenComplete = true;
        [SerializeField] private bool openOnAwake;
        [SerializeField] private AudioClip openAudioClip;
        [SerializeField, Range(0f, 1f)] private float openAudioVolume = 0.85f;

        public bool IsOpen { get; private set; }
        public bool IsOpening { get; private set; }
        public event Action<MapExpansionDoorOpener> Opened;

        private Coroutine openRoutine;

        private void Awake()
        {
            IsOpen = openOnAwake;
            ApplyInitialDoorState();
        }

        private void OnEnable()
        {
            ApplyInitialDoorState();
        }

        private void LateUpdate()
        {
            if (IsOpening && keepOpenedDoorRootHiddenUntilOpenComplete && openedDoorRoot != null && openedDoorRoot.activeSelf)
            {
                openedDoorRoot.SetActive(false);
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            StartOpening();
            Opened?.Invoke(this);
        }

        public void CompleteOpenAnimation()
        {
            if (!IsOpen)
            {
                return;
            }

            ApplyOpenedState();
        }

        private void ApplyInitialDoorState()
        {
            if (IsOpen)
            {
                ApplyOpenedState();
                return;
            }

            ApplyClosedState();
        }

        private void ApplyClosedState()
        {
            IsOpening = false;

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            SetActiveIfAssigned(closedDoorRoot, true);
            SetActiveIfAssigned(openedDoorRoot, false);
            SetActiveIfAssigned(passageBlocker, true);
        }

        private void StartOpening()
        {
            IsOpening = true;
            SetActiveIfAssigned(closedDoorRoot, true);
            SetActiveIfAssigned(passageBlocker, true);

            if (keepOpenedDoorRootHiddenUntilOpenComplete)
            {
                SetActiveIfAssigned(openedDoorRoot, false);
            }
            else
            {
                SetActiveIfAssigned(openedDoorRoot, true);
            }

            PlayOpenAnimation();
            PlayOpenAudio();

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(CompleteOpenAfterDelay());
        }

        private IEnumerator CompleteOpenAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, openCompletionDelay));
            openRoutine = null;
            ApplyOpenedState();
        }

        private void ApplyOpenedState()
        {
            IsOpening = false;

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            SetActiveIfAssigned(closedDoorRoot, false);
            SetActiveIfAssigned(openedDoorRoot, true);
            SetActiveIfAssigned(passageBlocker, false);
        }

        private void PlayOpenAnimation()
        {
            if (doorAnimator == null)
            {
                Debug.LogError("[MapExpansionDoorOpener] Door Animator is not assigned.", this);
                return;
            }

            if (doorAnimator.runtimeAnimatorController == null)
            {
                Debug.LogError("[MapExpansionDoorOpener] Door AnimatorController is not assigned.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(openTriggerName))
            {
                Debug.LogError("[MapExpansionDoorOpener] Open trigger name is empty.", this);
                return;
            }

            doorAnimator.ResetTrigger(openTriggerName);
            doorAnimator.SetTrigger(openTriggerName);
        }

        private void PlayOpenAudio()
        {
            if (openAudioClip == null)
            {
                return;
            }

            RuntimeFeedbackUtility.PlayClip(openAudioClip, transform.position, openAudioVolume, "MapExpansionDoorOpenSfx");
        }

        private static void SetActiveIfAssigned(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
