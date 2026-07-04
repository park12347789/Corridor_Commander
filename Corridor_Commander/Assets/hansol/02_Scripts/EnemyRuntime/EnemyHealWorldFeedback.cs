using TMPro;
using UnityEngine;

namespace CorridorCommander.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyHealWorldFeedback : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text textComponent;
        [SerializeField] private string textContent = "++";
        [SerializeField] private bool showHealingAmount;
        [SerializeField] private Color textColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField, Min(0.1f)] private float fontSize = 4f;

        [Header("Motion")]
        [SerializeField, Min(0.05f)] private float lifetime = 0.8f;
        [SerializeField, Min(0f)] private float riseDistance = 1.1f;

        private Vector3 startPosition;
        private float startTime;
        private float healingAmount;

        public void Initialize(float amount)
        {
            healingAmount = Mathf.Max(0f, amount);
            RefreshText();
        }

        private void Awake()
        {
            ResolveTextComponent();
            RefreshText();
        }

        private void OnEnable()
        {
            startPosition = transform.position;
            startTime = Time.time;
        }

        private void Update()
        {
            float progress = Mathf.Clamp01((Time.time - startTime) / lifetime);
            transform.position = startPosition + Vector3.up * (riseDistance * progress);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - mainCamera.transform.position,
                    Vector3.up);
            }

            if (textComponent != null)
            {
                Color color = textColor;
                color.a *= 1f - progress;
                textComponent.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void ResolveTextComponent()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TMP_Text>();
            }

            if (textComponent == null)
            {
                TextMeshPro textMesh = gameObject.AddComponent<TextMeshPro>();
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.fontSize = fontSize;
                textMesh.color = textColor;
                textMesh.sortingOrder = 50;
                textComponent = textMesh;
            }
        }

        private void RefreshText()
        {
            if (textComponent == null)
            {
                return;
            }

            textComponent.text = showHealingAmount
                ? $"+{Mathf.CeilToInt(healingAmount)}"
                : textContent;
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
        }
    }
}

/*
Unity setup:
1. Add this component to a small world feedback prefab.
2. Edit Text Content, color, lifetime, and rise distance in the Inspector.
3. EnemyHealFeedbackPresenter creates it above the healed zombie.
*/
