using System.Collections;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class MuzzleRecoilFeedback : MonoBehaviour
    {
        [SerializeField] private Transform recoilTarget;
        [SerializeField] private Vector3 recoilOffset = new Vector3(0f, 0f, -0.12f);
        [SerializeField, Min(0.01f)] private float recoilDuration = 0.035f;
        [SerializeField, Min(0.01f)] private float returnDuration = 0.11f;

        private Coroutine recoilRoutine;
        private Vector3 restLocalPosition;
        private bool initialized;

        private void Reset()
        {
            recoilTarget = transform;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            if (recoilRoutine != null)
            {
                StopCoroutine(recoilRoutine);
                recoilRoutine = null;
            }

            if (initialized && recoilTarget != null)
            {
                recoilTarget.localPosition = restLocalPosition;
            }
        }

        public void Play()
        {
            Initialize();

            if (recoilTarget == null || !isActiveAndEnabled)
            {
                return;
            }

            if (recoilRoutine != null)
            {
                StopCoroutine(recoilRoutine);
            }

            recoilTarget.localPosition = restLocalPosition;
            recoilRoutine = StartCoroutine(PlayRoutine());
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (recoilTarget == null)
            {
                recoilTarget = transform;
            }

            if (recoilTarget != null)
            {
                restLocalPosition = recoilTarget.localPosition;
            }

            initialized = true;
        }

        private IEnumerator PlayRoutine()
        {
            Vector3 recoilLocalPosition = restLocalPosition + recoilOffset;

            yield return MoveBetween(restLocalPosition, recoilLocalPosition, recoilDuration);
            yield return MoveBetween(recoilLocalPosition, restLocalPosition, returnDuration);

            recoilRoutine = null;
        }

        private IEnumerator MoveBetween(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                recoilTarget.localPosition = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            recoilTarget.localPosition = to;
        }
    }
}
