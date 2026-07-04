using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using CorridorCommander.PlayerControl;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class PlacementPreviewController : MonoBehaviour
    {
        [SerializeField] private float rotationStepDegrees = 90f;
        [SerializeField, Range(0.05f, 1f)] private float previewAlpha = 0.45f;
        [SerializeField] private GameObject instructionRoot;
        [SerializeField] private Text instructionText;
        [SerializeField] private TMP_Text instructionTmpText;

        private PlacementPoint placementPoint;
        private BuildableKind kind;
        private BuildableDefinitionSO definition;
        private GameObject builder;
        private GameObject previewObject;
        private int startedFrame;
        private int lastTickFrame = -1;

        public bool IsActive => previewObject != null;

        private void Awake()
        {
            ResolveInstructionUi();
            SetInstructionActive(false);
        }

        private void OnDestroy()
        {
            DestroyPreviewObject();
            SetInstructionActive(false);
            UiInputCoordinator.EndContextIfActive(this);
        }

        public bool IsPreviewing(PlacementPoint point)
        {
            return IsActive && placementPoint == point;
        }

        public bool Begin(PlacementPoint point, BuildableKind buildKind, GameObject buildOwner)
        {
            if (point == null || !point.TryGetPrefab(buildKind, out GameObject prefab))
            {
                return false;
            }

            Cancel();
            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.PlacementPreview))
            {
                return false;
            }

            placementPoint = point;
            kind = buildKind;
            builder = buildOwner;
            startedFrame = Time.frameCount;

            Transform anchor = point.BuildAnchor;
            previewObject = Instantiate(prefab, anchor.position, anchor.rotation);
            previewObject.name = $"{buildKind}_PlacementPreview";
            ConfigurePreviewObject(previewObject);
            point.AlignPreviewObject(previewObject);
            DisablePreviewGameplay(previewObject);
            ApplyPreviewVisuals(previewObject);
            RefreshInstructionUi();
            return true;
        }

        public bool Begin(PlacementPoint point, BuildableDefinitionSO buildableDefinition, GameObject buildOwner)
        {
            if (point == null || buildableDefinition == null || buildableDefinition.Prefab == null)
            {
                return false;
            }

            Cancel();
            if (!UiInputCoordinator.Instance.TryBeginContext(this, UiInputContext.PlacementPreview))
            {
                return false;
            }

            placementPoint = point;
            kind = buildableDefinition.Kind;
            definition = buildableDefinition;
            builder = buildOwner;
            startedFrame = Time.frameCount;

            Transform anchor = point.BuildAnchor;
            previewObject = Instantiate(buildableDefinition.Prefab, anchor.position, anchor.rotation);
            previewObject.name = $"{ResolveBuildableName(buildableDefinition)}_PlacementPreview";
            ConfigurePreviewObject(previewObject);
            point.AlignPreviewObject(previewObject);
            DisablePreviewGameplay(previewObject);
            ApplyPreviewVisuals(previewObject);
            RefreshInstructionUi();
            return true;
        }

        public void Tick()
        {
            if (!IsActive)
            {
                return;
            }

            if (lastTickFrame == Time.frameCount)
            {
                return;
            }

            lastTickFrame = Time.frameCount;
            Mouse mouse = Mouse.current;

            if (KeyboardInputMessenger.WasReloadPressed())
            {
                RotatePreview(rotationStepDegrees);
            }

            if (mouse != null)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.001f)
                {
                    RotatePreview(Mathf.Sign(scrollY) * rotationStepDegrees);
                }
            }

            bool canConfirmThisFrame = Time.frameCount != startedFrame;
            if (canConfirmThisFrame
                && (KeyboardInputMessenger.WasInteractPressed()
                    || (mouse != null && mouse.rightButton.wasPressedThisFrame)))
            {
                if (!UiInputCoordinator.Instance.TryConsumeContextInput(this))
                {
                    return;
                }

                Confirm();
                return;
            }

            if (KeyboardInputMessenger.WasCancelPressed()
                && UiInputCoordinator.Instance.TryConsumeCancel(this))
            {
                Cancel();
            }
        }

        public void Confirm()
        {
            if (!IsActive || placementPoint == null)
            {
                Cancel();
                return;
            }

            Quaternion rotation = previewObject.transform.rotation;
            PlacementPoint targetPoint = placementPoint;
            BuildableKind targetKind = kind;
            BuildableDefinitionSO targetDefinition = definition;
            GameObject targetBuilder = builder;

            if (targetDefinition != null && !CanPayBuildCost(targetDefinition, targetBuilder))
            {
                return;
            }

            Cancel();
            if (targetDefinition != null)
            {
                targetPoint.Build(targetDefinition, targetBuilder, rotation);
                return;
            }

            targetPoint.Build(targetKind, targetBuilder, rotation);
        }

        private bool CanPayBuildCost(BuildableDefinitionSO buildableDefinition, GameObject buildOwner)
        {
            int price = buildableDefinition != null ? Mathf.Max(0, buildableDefinition.Price) : 0;
            if (price <= 0)
            {
                return true;
            }

            if (placementPoint == null || !placementPoint.CanBuild(buildableDefinition))
            {
                return false;
            }

            PlayerCurrencyWallet wallet = ResolveCurrencyWallet(buildOwner);
            if (wallet == null)
            {
                Debug.LogWarning("[PlacementPreviewController] PlayerCurrencyWallet is not connected for build cost.", this);
                return false;
            }

            if (!wallet.TrySpendMoney(price))
            {
                Debug.Log($"[PlacementPreviewController] Not enough money to build {buildableDefinition.DisplayName}. Need {price}, Current {wallet.CurrentMoney}.", this);
                return false;
            }

            Debug.Log($"[PlacementPreviewController] Build cost paid: {buildableDefinition.DisplayName} -{price}.", this);
            return true;
        }

        private static PlayerCurrencyWallet ResolveCurrencyWallet(GameObject buildOwner)
        {
            if (buildOwner == null)
            {
                return null;
            }

            PlayerCurrencyWallet wallet = buildOwner.GetComponentInParent<PlayerCurrencyWallet>();
            if (wallet != null)
            {
                return wallet;
            }

            return buildOwner.GetComponentInChildren<PlayerCurrencyWallet>(true);
        }

        private void RotatePreview(float yawDegrees)
        {
            if (previewObject == null)
            {
                return;
            }

            previewObject.transform.Rotate(Vector3.up, yawDegrees, Space.World);
            placementPoint?.AlignPreviewObject(previewObject);
        }

        public void Cancel()
        {
            DestroyPreviewObject();
            placementPoint = null;
            definition = null;
            builder = null;
            startedFrame = -1;
            lastTickFrame = -1;
            SetInstructionActive(false);
            UiInputCoordinator.Instance.EndContext(this);
        }

        private static void ConfigurePreviewObject(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            root.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }

        private void DestroyPreviewObject()
        {
            if (previewObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(previewObject);
            }
            else
            {
                DestroyImmediate(previewObject);
            }

            previewObject = null;
        }

        private void RefreshInstructionUi()
        {
            ResolveInstructionUi();
            string instruction = ResolveInstructionText();

            if (instructionText != null)
            {
                instructionText.text = instruction;
                instructionText.raycastTarget = false;
            }

            if (instructionTmpText != null)
            {
                instructionTmpText.text = instruction;
                instructionTmpText.raycastTarget = false;
            }

            SetInstructionActive(true);
        }

        private string ResolveInstructionText()
        {
            string buildableName = definition != null
                ? ResolveBuildableName(definition)
                : (kind switch
                {
                    BuildableKind.Barricade => "\uBC29\uBCBD",
                    BuildableKind.Turret => "\uD3EC\uD0D1",
                    BuildableKind.Mortar => "\uBC15\uACA9\uD3EC",
                    _ => "\uAE30\uBB3C"
                });

            return $"{buildableName} 배치 미리보기\nR / 휠: 회전    E / 우클릭: 설치    Esc: 취소";
        }

        private static string ResolveBuildableName(BuildableDefinitionSO buildableDefinition)
        {
            if (buildableDefinition == null)
            {
                return "\uAE30\uBB3C";
            }

            if (!string.IsNullOrWhiteSpace(buildableDefinition.DisplayName))
            {
                return buildableDefinition.DisplayName;
            }

            return !string.IsNullOrWhiteSpace(buildableDefinition.BuildableId)
                ? buildableDefinition.BuildableId
                : buildableDefinition.Kind.ToString();
        }

        private void ResolveInstructionUi()
        {
            if (instructionRoot == null)
            {
                Debug.LogWarning("[PlacementPreviewController] Instruction Root is not assigned.", this);
            }

            if (instructionRoot != null && instructionTmpText == null)
            {
                instructionTmpText = instructionRoot.GetComponentInChildren<TMP_Text>(true);
            }

            if (instructionRoot != null && instructionText == null)
            {
                instructionText = instructionRoot.GetComponentInChildren<Text>(true);
            }

            if (instructionText == null && instructionTmpText == null)
            {
                Debug.LogWarning("[PlacementPreviewController] Instruction Text is not assigned.", this);
            }
        }

        private void SetInstructionActive(bool active)
        {
            if (instructionRoot != null)
            {
                instructionRoot.SetActive(active);
            }
        }

        private void ApplyPreviewVisuals(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Material[] materials = targetRenderer.materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    ConfigurePreviewMaterial(materials[j]);
                }

                targetRenderer.materials = materials;
            }
        }

        private void ConfigurePreviewMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = previewAlpha;
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                color.a = previewAlpha;
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void DisablePreviewGameplay(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            NavMeshObstacle[] obstacles = root.GetComponentsInChildren<NavMeshObstacle>(true);
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacles[i].enabled = false;
            }

            Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }
        }
    }
}
