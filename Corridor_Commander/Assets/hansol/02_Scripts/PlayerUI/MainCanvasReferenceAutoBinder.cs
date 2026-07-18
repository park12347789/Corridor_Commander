using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CorridorCommander.PlayerUI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MainCanvasReferenceAutoBinder : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private bool logResults = true;

        [ContextMenu("Auto Bind UI References")]
        public void AutoBindUiReferences()
        {
            int changedCount = 0;
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(includeInactive);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];

                if (behaviour == null || behaviour == this)
                {
                    continue;
                }

                changedCount += TryBindBehaviour(behaviour);
            }

            if (changedCount > 0)
            {
                EditorUtility.SetDirty(gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
            }

            if (logResults)
            {
                Debug.Log($"[MainCanvasReferenceAutoBinder] Auto bind completed. Changed fields: {changedCount}", this);
            }
        }

        private int TryBindBehaviour(MonoBehaviour behaviour)
        {
            SerializedObject serializedObject = new SerializedObject(behaviour);
            SerializedProperty iterator = serializedObject.GetIterator();
            int changedCount = 0;

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.objectReferenceValue != null)
                {
                    continue;
                }

                UnityEngine.Object resolvedObject = ResolveReference(behaviour, iterator.name, iterator.type);
                if (resolvedObject == null)
                {
                    continue;
                }

                iterator.objectReferenceValue = resolvedObject;
                changedCount++;

                if (logResults)
                {
                    Debug.Log(
                        $"[MainCanvasReferenceAutoBinder] Bound {behaviour.GetType().Name}.{iterator.name} -> {resolvedObject.name}",
                        behaviour);
                }
            }

            if (changedCount > 0)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviour);
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
            }

            return changedCount;
        }

        private UnityEngine.Object ResolveReference(
            MonoBehaviour owner,
            string fieldName,
            string serializedType)
        {
            Type fieldType = ResolveSerializedObjectType(serializedType);
            if (fieldType == null)
            {
                return null;
            }

            string ownerTypeName = owner.GetType().Name;
            Transform ownerRoot = owner.transform;

            if (fieldType == typeof(GameObject))
            {
                Transform target = ResolveGameObjectField(ownerTypeName, ownerRoot, fieldName);
                return target != null ? target.gameObject : null;
            }

            if (fieldType == typeof(Transform) || fieldType == typeof(RectTransform))
            {
                return ResolveTransformField(ownerTypeName, ownerRoot, fieldName);
            }

            if (fieldType == typeof(Text))
            {
                Transform target = ResolveTextTransform(ownerTypeName, ownerRoot, fieldName);
                return target != null ? target.GetComponent<Text>() : null;
            }

            if (fieldType == typeof(TMP_Text))
            {
                Transform target = ResolveTmpTextTransform(ownerTypeName, ownerRoot, fieldName);
                return target != null ? target.GetComponent<TMP_Text>() : null;
            }

            if (fieldType == typeof(Image))
            {
                Transform target = ResolveImageTransform(ownerTypeName, ownerRoot, fieldName);
                return target != null ? target.GetComponent<Image>() : null;
            }

            if (fieldType == typeof(Button))
            {
                Transform target = ResolveButtonTransform(ownerTypeName, ownerRoot, fieldName);
                return target != null ? target.GetComponent<Button>() : null;
            }

            return null;
        }

        private Transform ResolveGameObjectField(
            string ownerTypeName,
            Transform ownerRoot,
            string fieldName)
        {
            string normalizedName = Normalize(fieldName);

            if (normalizedName.Contains("panelroot"))
            {
                return FindFirstNamed(
                    ownerRoot,
                    ownerRoot.name.Replace("Presenter", "Panel"),
                    "Panel",
                    "MenuPanel",
                    "SupportTruckShopPanel",
                    "PlayerCommandRadialPanel",
                    "TreasureRewardPanel",
                    "InstalledObjectPanel",
                    "PlacementBuildMenuPanel");
            }

            if (normalizedName.Contains("promptroot"))
            {
                return FindFirstNamed(ownerRoot, "InteractionPromptRoot", "SupportTruckShopPrompt", "PromptRoot");
            }

            if (normalizedName.Contains("statinfopanel"))
            {
                return FindFirstNamed(ownerRoot, "StatUpgradePanel", "LeftStatusPanel");
            }

            if (normalizedName.Contains("orderselection"))
            {
                return FindFirstNameContains(ownerRoot, "Order");
            }

            if (normalizedName.Contains("skillselection"))
            {
                return FindFirstNameContains(ownerRoot, "Skill");
            }

            if (normalizedName.Contains("itemselection"))
            {
                return FindFirstNameContains(ownerRoot, "Item");
            }

            if (normalizedName.Contains("popupframe"))
            {
                return FindFirstNamed(ownerRoot, "PopupFrame", "Popup", "Panel");
            }

            if (normalizedName.Contains("closebutton"))
            {
                return FindFirstNamed(ownerRoot, "CloseButton", "ExitButton");
            }

            return null;
        }

        private Transform ResolveTransformField(
            string ownerTypeName,
            Transform ownerRoot,
            string fieldName)
        {
            string normalizedName = Normalize(fieldName);

            if (normalizedName.Contains("panelroot"))
            {
                return ResolveGameObjectField(ownerTypeName, ownerRoot, fieldName);
            }

            if (normalizedName.Contains("runtime") || normalizedName.Contains("content"))
            {
                return FindFirstNamed(ownerRoot, "RuntimeListContent", "LayoutReference_RuntimeListContent", "Content");
            }

            return null;
        }

        private Transform ResolveTextTransform(
            string ownerTypeName,
            Transform ownerRoot,
            string fieldName)
        {
            string normalizedName = Normalize(fieldName);

            if (normalizedName.Contains("title"))
            {
                return FindTextByNames(ownerRoot, "TitleText", "Title", "Text_Name", "Text_name");
            }

            if (normalizedName.Contains("currency") || normalizedName.Contains("money"))
            {
                return FindTextByNames(ownerRoot, "CurrencyText", "MoneyText", "prices_text", "Text_Num");
            }

            if (normalizedName.Contains("description") || normalizedName.Contains("detail"))
            {
                return FindTextByNames(ownerRoot, "DescriptionText", "DetailText", "explanation");
            }

            if (normalizedName.Contains("hint"))
            {
                return FindTextByNames(ownerRoot, "HintText", "explanation");
            }

            if (normalizedName.Contains("status"))
            {
                return FindTextByNames(ownerRoot, "StatusText", "status");
            }

            if (normalizedName.Contains("prompt"))
            {
                return FindTextByNames(ownerRoot, "PromptText", "Text");
            }

            if (normalizedName.Contains("health"))
            {
                return FindTextByNames(ownerRoot, "HealthText", "HealthUpgradeLevelText", "Text_Num");
            }

            if (normalizedName.Contains("stamina"))
            {
                return FindTextByNames(ownerRoot, "StaminaText", "StaminaUpgradeLevelText", "Text_Num");
            }

            if (normalizedName.Contains("level") || normalizedName.Contains("progress"))
            {
                return FindTextByNames(ownerRoot, "LevelProgressText", "LevelText", "Text_Num");
            }

            return null;
        }

        private Transform ResolveTmpTextTransform(
            string ownerTypeName,
            Transform ownerRoot,
            string fieldName)
        {
            Transform exactLegacyCandidate = ResolveTextTransform(ownerTypeName, ownerRoot, fieldName);
            if (exactLegacyCandidate != null && exactLegacyCandidate.GetComponent<TMP_Text>() != null)
            {
                return exactLegacyCandidate;
            }

            string normalizedName = Normalize(fieldName);

            if (normalizedName.Contains("title"))
            {
                return FindTmpTextByNames(ownerRoot, "TitleText", "Title", "Text_Name", "Text_name", "Text (TMP)");
            }

            if (normalizedName.Contains("currency") || normalizedName.Contains("money"))
            {
                return FindTmpTextByNames(ownerRoot, "CurrencyText", "MoneyText", "prices_text", "Text_Num", "Text (TMP)");
            }

            if (normalizedName.Contains("hint") || normalizedName.Contains("description") || normalizedName.Contains("detail"))
            {
                return FindTmpTextByNames(ownerRoot, "HintText", "DescriptionText", "DetailText", "explanation", "Text (TMP)");
            }

            if (normalizedName.Contains("status"))
            {
                return FindTmpTextByNames(ownerRoot, "StatusText", "status", "Text (TMP)");
            }

            return FindTmpTextByNames(ownerRoot, "Text (TMP)", "Text_Name", "Text_name", "Text_Num");
        }

        private Transform ResolveImageTransform(
            string ownerTypeName,
            Transform ownerRoot,
            string fieldName)
        {
            string normalizedName = Normalize(fieldName);

            if (normalizedName.Contains("fill"))
            {
                return FindImageByNames(ownerRoot, "HealthFill", "StaminaFill", "Fill");
            }

            if (normalizedName.Contains("icon"))
            {
                return FindImageByNames(ownerRoot, "ItemIcon", "Icon", "CurrencyIcon", "CostIcon");
            }

            return FindImageByNames(ownerRoot, ownerRoot.name, "Bg", "InnerBg", "Image");
        }

        private Transform ResolveButtonTransform(
            string ownerTypeName,
            Transform ownerRoot,
            string fieldName)
        {
            string normalizedName = Normalize(fieldName);

            if (normalizedName.Contains("close") || normalizedName.Contains("exit"))
            {
                return FindButtonByNames(ownerRoot, "CloseButton", "ExitButton");
            }

            if (normalizedName.Contains("turret"))
            {
                return FindButtonByNames(ownerRoot, "Turret", "TurretButton");
            }

            if (normalizedName.Contains("barricade"))
            {
                return FindButtonByNames(ownerRoot, "Barricade", "BarricadeButton");
            }

            if (normalizedName.Contains("mortar"))
            {
                return FindButtonByNames(ownerRoot, "Mortar", "MortarButton");
            }

            return null;
        }

        private Type ResolveSerializedObjectType(string serializedType)
        {
            if (serializedType.Contains("GameObject"))
            {
                return typeof(GameObject);
            }

            if (serializedType.Contains("RectTransform"))
            {
                return typeof(RectTransform);
            }

            if (serializedType.Contains("Transform"))
            {
                return typeof(Transform);
            }

            if (serializedType.Contains("UnityEngine.UI.Text"))
            {
                return typeof(Text);
            }

            if (serializedType.Contains("TMPro.TMP_Text"))
            {
                return typeof(TMP_Text);
            }

            if (serializedType.Contains("UnityEngine.UI.Image"))
            {
                return typeof(Image);
            }

            if (serializedType.Contains("UnityEngine.UI.Button"))
            {
                return typeof(Button);
            }

            return null;
        }

        private Transform FindTextByNames(Transform root, params string[] names)
        {
            return FindComponentByNames<Text>(root, names);
        }

        private Transform FindTmpTextByNames(Transform root, params string[] names)
        {
            return FindComponentByNames<TMP_Text>(root, names);
        }

        private Transform FindImageByNames(Transform root, params string[] names)
        {
            return FindComponentByNames<Image>(root, names);
        }

        private Transform FindButtonByNames(Transform root, params string[] names)
        {
            return FindComponentByNames<Button>(root, names);
        }

        private Transform FindComponentByNames<T>(Transform root, params string[] names)
            where T : Component
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform found = FindFirstNamed(root, names[i]);
                if (found != null && found.GetComponent<T>() != null)
                {
                    return found;
                }
            }

            T[] components = root.GetComponentsInChildren<T>(includeInactive);
            return components.Length > 0 ? components[0].transform : null;
        }

        private Transform FindFirstNamed(Transform root, params string[] names)
        {
            if (root == null || names == null)
            {
                return null;
            }

            for (int i = 0; i < names.Length; i++)
            {
                Transform found = FindChildRecursive(root, names[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private Transform FindFirstNameContains(Transform root, string namePart)
        {
            if (root == null || string.IsNullOrWhiteSpace(namePart))
            {
                return null;
            }

            string normalizedPart = Normalize(namePart);
            Transform[] children = root.GetComponentsInChildren<Transform>(includeInactive);

            for (int i = 0; i < children.Length; i++)
            {
                if (Normalize(children[i].name).Contains(normalizedPart))
                {
                    return children[i];
                }
            }

            return null;
        }

        private Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("_", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty)
                    .ToLowerInvariant();
        }
#else
        public void AutoBindUiReferences()
        {
        }
#endif
    }
}

/*
Unity setup outline:
1. Add MainCanvasReferenceAutoBinder to the root MainCanvas while repairing UI references.
2. In the component context menu, run Auto Bind UI References.
3. Review the Console logs and manually check any fields that remain empty.
4. Remove this component after the UI prefab or scene has been repaired.
*/
