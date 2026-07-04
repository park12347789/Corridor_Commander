using CorridorCommander;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.Editor
{
    public static class BuildableLifecycleFeedbackBinder
    {
        private const string SafeEffectsRoot = "Assets/90_ThirdParty/SpecialSkillsEffectsPack/AllEffects/EffectsSet_1(NotScriptBased)/Effects";
        private const string ScriptEffectsRoot = "Assets/90_ThirdParty/SpecialSkillsEffectsPack/AllEffects/EffectsSet_2(ScriptBased)/Effects";
        private const string InstallVfxPath = "Assets/hansol/03_Prefabs/VFX/BuildableInstallMagicCircleRise.prefab";
        private const string UpgradeVfxPath = "Assets/hansol/03_Prefabs/VFX/BuildableUpgradePulse_Quick.prefab";
        private const string RepairVfxPath = "Assets/hansol/03_Prefabs/VFX/BuildableRepairPulse_Quick.prefab";
        private const string DismantleVfxPath = "Assets/hansol/03_Prefabs/VFX/BuildableDismantleBurst_Quick.prefab";
        private const string TurretFireVfxPath = "Assets/90_ThirdParty/Eric VFX Studio/Game VFX - Stylized Beams/Prefabs/Built-In/FX_Hit_Orange.prefab";
        private const string TurretImpactVfxPath = "Assets/90_ThirdParty/Eric VFX Studio/Game VFX - Stylized Beams/Prefabs/Built-In/FX_Hit_Orange.prefab";
        private const string SawTrapAttackVfxPath = ScriptEffectsRoot + "/Effect_25_CriticalSlash/Effect_25_SpiralWheels.prefab";
        private const string MortarBasicMuzzleVfxPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/MortarMuzzle_SmokeBurst_Basic.prefab";
        private const string MortarRapidMuzzleVfxPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/MortarMuzzle_SmokeBurst_Rapid.prefab";
        private const string MortarHeavyMuzzleVfxPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/MortarMuzzle_SmokeBurst_Heavy.prefab";
        private const string MortarProjectileVfxPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/MortarShell_SmokeTrail_Follow.prefab";
        private const string MortarBasicProjectilePath = "Assets/hansol/03_Prefabs/TEMP_MortarProjectile_BasicShell.prefab";
        private const string MortarRapidProjectilePath = "Assets/hansol/03_Prefabs/TEMP_MortarProjectile_SlowBlueShell.prefab";
        private const string MortarHeavyProjectilePath = "Assets/hansol/03_Prefabs/TEMP_MortarProjectile_HeavyShell.prefab";
        private const string MortarBasicImpactVfxPath = "Assets/CartoonVFX9X/StylizedExplosionEffectVol2 URP/Prefabs/Explosion_9.prefab";
        private const string MortarRapidImpactVfxPath = "Assets/CartoonVFX9X/StylizedExplosionEffectVol2 URP/Prefabs/Explosion_6.prefab";
        private const string MortarHeavyImpactVfxPath = "Assets/CartoonVFX9X/StylizedExplosionEffectVol2 URP/Prefabs/Explosion_9.prefab";
        private const string TurretFirePointPrefabPath = "Assets/hansol/03_Prefabs/Turret_FirePointSocket.prefab";

        private const string InstallAudioPath = "Assets/hansol/08_Audio/SFX/SFX_Buildable_Install_ClickRise.wav";
        private const string UpgradeAudioPath = "Assets/hansol/08_Audio/SFX/SFX_Buildable_Upgrade_Pulse.wav";
        private const string RepairAudioPath = "Assets/hansol/08_Audio/SFX/SFX_Buildable_Repair_Chime.wav";
        private const string DismantleAudioPath = "Assets/hansol/08_Audio/SFX/SFX_Buildable_Dismantle_Snap.wav";

        private static readonly string[] PrefabPaths =
        {
            "Assets/hansol/03_Prefabs/Turret_Basic.prefab",
            "Assets/hansol/03_Prefabs/Turret_Rapid.prefab",
            "Assets/hansol/03_Prefabs/Turret_LongRange.prefab",
            "Assets/hansol/03_Prefabs/TEMP_Mortar_Basic.prefab",
            "Assets/hansol/03_Prefabs/TEMP_Mortar_Rapid.prefab",
            "Assets/hansol/03_Prefabs/TEMP_Mortar_Heavy.prefab",
            "Assets/hansol/03_Prefabs/SawTrap_Turret_Yellow.prefab",
            "Assets/hansol/03_Prefabs/Barricade_Basic.prefab",
            "Assets/hansol/03_Prefabs/Barricade_Level2.prefab"
        };

        private static readonly string[] MortarRolePaths =
        {
            "Assets/hansol/09_Settings/Skills/Role_Mortar.asset",
            "Assets/hansol/09_Settings/Skills/Role_Mortar_Rapid.asset",
            "Assets/hansol/09_Settings/Skills/Role_Mortar_Heavy.asset"
        };

        [MenuItem("Corridor Commander/Bind Buildable Lifecycle Feedback")]
        public static void Bind()
        {
            GameObject installVfx = LoadVfx(InstallVfxPath);
            GameObject upgradeVfx = LoadVfx(UpgradeVfxPath);
            GameObject repairVfx = LoadVfx(RepairVfxPath);
            GameObject dismantleVfx = LoadVfx(DismantleVfxPath);
            GameObject sawTrapAttackVfx = LoadVfx(SawTrapAttackVfxPath);
            GameObject mortarBasicMuzzleVfx = LoadVfx(MortarBasicMuzzleVfxPath);
            GameObject mortarRapidMuzzleVfx = LoadVfx(MortarRapidMuzzleVfxPath);
            GameObject mortarHeavyMuzzleVfx = LoadVfx(MortarHeavyMuzzleVfxPath);
            GameObject mortarProjectileVfx = LoadVfx(MortarProjectileVfxPath);
            MortarProjectile mortarBasicProjectile = LoadProjectile(MortarBasicProjectilePath);
            MortarProjectile mortarRapidProjectile = LoadProjectile(MortarRapidProjectilePath);
            MortarProjectile mortarHeavyProjectile = LoadProjectile(MortarHeavyProjectilePath);
            GameObject mortarBasicImpactVfx = LoadVfx(MortarBasicImpactVfxPath);
            GameObject mortarRapidImpactVfx = LoadVfx(MortarRapidImpactVfxPath);
            GameObject mortarHeavyImpactVfx = LoadVfx(MortarHeavyImpactVfxPath);
            GameObject turretFireVfx = LoadVfx(TurretFireVfxPath);
            ParticleSystem turretImpactVfx = LoadParticleVfx(TurretImpactVfxPath);

            AudioClip installAudio = LoadAudio(InstallAudioPath);
            AudioClip upgradeAudio = LoadAudio(UpgradeAudioPath);
            AudioClip repairAudio = LoadAudio(RepairAudioPath);
            AudioClip dismantleAudio = LoadAudio(DismantleAudioPath);

            if (installVfx == null || upgradeVfx == null || repairVfx == null || dismantleVfx == null
                || sawTrapAttackVfx == null || mortarBasicMuzzleVfx == null || mortarRapidMuzzleVfx == null || mortarHeavyMuzzleVfx == null
                || mortarProjectileVfx == null || mortarBasicProjectile == null || mortarRapidProjectile == null || mortarHeavyProjectile == null
                || mortarBasicImpactVfx == null || mortarRapidImpactVfx == null || mortarHeavyImpactVfx == null
                || turretFireVfx == null || turretImpactVfx == null
                || installAudio == null || upgradeAudio == null || repairAudio == null || dismantleAudio == null)
            {
                Debug.LogError("[BuildableLifecycleFeedbackBinder] Missing lifecycle feedback asset. Binding aborted.");
                return;
            }

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                BindPrefab(PrefabPaths[i], installVfx, upgradeVfx, repairVfx, dismantleVfx, sawTrapAttackVfx, installAudio, upgradeAudio, repairAudio, dismantleAudio);
            }

            BindTurretFirePoint(turretFireVfx, turretImpactVfx);
            BindMortarRoles(
                mortarBasicMuzzleVfx,
                mortarRapidMuzzleVfx,
                mortarHeavyMuzzleVfx,
                mortarProjectileVfx,
                mortarBasicProjectile,
                mortarRapidProjectile,
                mortarHeavyProjectile,
                mortarBasicImpactVfx,
                mortarRapidImpactVfx,
                mortarHeavyImpactVfx);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BuildableLifecycleFeedbackBinder] Buildable lifecycle feedback bound.");
        }

        private static void BindPrefab(
            string prefabPath,
            GameObject installVfx,
            GameObject upgradeVfx,
            GameObject repairVfx,
            GameObject dismantleVfx,
            GameObject sawTrapAttackVfx,
            AudioClip installAudio,
            AudioClip upgradeAudio,
            AudioClip repairAudio,
            AudioClip dismantleAudio)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BindInstallFeedback(root, installVfx, installAudio);
                BindLifecycleFeedback(root, upgradeVfx, repairVfx, dismantleVfx, upgradeAudio, repairAudio, dismantleAudio);
                BindSawTrapFeedback(root, sawTrapAttackVfx, dismantleAudio);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BindInstallFeedback(GameObject root, GameObject installVfx, AudioClip installAudio)
        {
            TurretInstallable turretInstallable = root.GetComponent<TurretInstallable>();
            if (turretInstallable != null)
            {
                SerializedObject serializedInstallable = new SerializedObject(turretInstallable);
                serializedInstallable.FindProperty("installVfxPrefab").objectReferenceValue = installVfx;
                serializedInstallable.FindProperty("installAudioClip").objectReferenceValue = installAudio;
                serializedInstallable.FindProperty("installAudioVolume").floatValue = 0.85f;
                serializedInstallable.FindProperty("installFeedbackLifetime").floatValue = 1.45f;
                serializedInstallable.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            BarricadeInstallable barricadeInstallable = root.GetComponent<BarricadeInstallable>();
            if (barricadeInstallable != null)
            {
                SerializedObject serializedBarricade = new SerializedObject(barricadeInstallable);
                serializedBarricade.FindProperty("installVfxPrefab").objectReferenceValue = installVfx;
                serializedBarricade.FindProperty("installAudioClip").objectReferenceValue = installAudio;
                serializedBarricade.FindProperty("installAudioVolume").floatValue = 0.85f;
                serializedBarricade.FindProperty("installFeedbackLifetime").floatValue = 1.45f;
                serializedBarricade.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            BuildableInstallable buildableInstallable = root.GetComponent<BuildableInstallable>();
            if (buildableInstallable == null)
            {
                return;
            }

            SerializedObject serializedBuildable = new SerializedObject(buildableInstallable);
            serializedBuildable.FindProperty("installVfxPrefab").objectReferenceValue = installVfx;
            serializedBuildable.FindProperty("installAudioClip").objectReferenceValue = installAudio;
            serializedBuildable.FindProperty("installAudioVolume").floatValue = 0.85f;
            serializedBuildable.FindProperty("installFeedbackLifetime").floatValue = 1.45f;
            serializedBuildable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindLifecycleFeedback(
            GameObject root,
            GameObject upgradeVfx,
            GameObject repairVfx,
            GameObject dismantleVfx,
            AudioClip upgradeAudio,
            AudioClip repairAudio,
            AudioClip dismantleAudio)
        {
            BuildableLifecycleFeedback feedback = root.GetComponent<BuildableLifecycleFeedback>();
            if (feedback == null)
            {
                feedback = root.AddComponent<BuildableLifecycleFeedback>();
            }

            SerializedObject serializedFeedback = new SerializedObject(feedback);
            serializedFeedback.FindProperty("upgradeVfxPrefab").objectReferenceValue = upgradeVfx;
            serializedFeedback.FindProperty("upgradeAudioClip").objectReferenceValue = upgradeAudio;
            serializedFeedback.FindProperty("upgradeAudioVolume").floatValue = 0.85f;
            serializedFeedback.FindProperty("upgradeFeedbackLifetime").floatValue = 0.95f;
            serializedFeedback.FindProperty("repairVfxPrefab").objectReferenceValue = repairVfx;
            serializedFeedback.FindProperty("repairAudioClip").objectReferenceValue = repairAudio;
            serializedFeedback.FindProperty("repairAudioVolume").floatValue = 0.8f;
            serializedFeedback.FindProperty("repairFeedbackLifetime").floatValue = 0.9f;
            serializedFeedback.FindProperty("dismantleVfxPrefab").objectReferenceValue = dismantleVfx;
            serializedFeedback.FindProperty("dismantleAudioClip").objectReferenceValue = dismantleAudio;
            serializedFeedback.FindProperty("dismantleAudioVolume").floatValue = 0.9f;
            serializedFeedback.FindProperty("dismantleFeedbackLifetime").floatValue = 0.75f;
            serializedFeedback.ApplyModifiedPropertiesWithoutUndo();

            BindLifecycleReference(root.GetComponent<TurretServiceController>(), feedback);
            BindLifecycleReference(root.GetComponent<BarricadeInstalledActionProvider>(), feedback);
            BindLifecycleReference(root.GetComponent<MortarInstalledActionProvider>(), feedback);
            BindLifecycleReference(root.GetComponent<SawTrapServiceController>(), feedback);
        }

        private static void BindSawTrapFeedback(GameObject root, GameObject attackVfx, AudioClip hitAudio)
        {
            SawTrapTurretController sawTrap = root.GetComponent<SawTrapTurretController>();
            if (sawTrap == null)
            {
                return;
            }

            SerializedObject serializedSawTrap = new SerializedObject(sawTrap);
            serializedSawTrap.FindProperty("attackVfxPrefab").objectReferenceValue = attackVfx;
            SerializedProperty hitAudioClips = serializedSawTrap.FindProperty("hitAudioClips");
            hitAudioClips.arraySize = 1;
            hitAudioClips.GetArrayElementAtIndex(0).objectReferenceValue = hitAudio;
            serializedSawTrap.FindProperty("hitAudioVolume").floatValue = 0.7f;
            serializedSawTrap.FindProperty("attackVfxLifetime").floatValue = 1.25f;
            serializedSawTrap.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindTurretFirePoint(GameObject fireVfxPrefab, ParticleSystem impactVfx)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TurretFirePointPrefabPath);
            try
            {
                ProjectileFirePoint firePoint = root.GetComponentInChildren<ProjectileFirePoint>(true);
                if (firePoint == null)
                {
                    Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing ProjectileFirePoint: {TurretFirePointPrefabPath}");
                    return;
                }

                ParticleSystem fireVfx = EnsureChildParticleVfx(root.transform, fireVfxPrefab, "GunStyle_TurretFireVfx", 0.75f);
                if (fireVfx == null)
                {
                    Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing child fire VFX ParticleSystem: {TurretFirePointPrefabPath}");
                    return;
                }

                SerializedObject serializedFirePoint = new SerializedObject(firePoint);
                serializedFirePoint.FindProperty("fireEffect").objectReferenceValue = fireVfx;
                serializedFirePoint.FindProperty("fireVfxPrefab").objectReferenceValue = fireVfxPrefab;
                serializedFirePoint.FindProperty("fireVfxScale").floatValue = 0.72f;
                serializedFirePoint.FindProperty("fireVfxLifetime").floatValue = 0.16f;
                serializedFirePoint.FindProperty("impactEffectPrefab").objectReferenceValue = impactVfx;
                serializedFirePoint.FindProperty("impactEffectScale").floatValue = 0.72f;
                serializedFirePoint.FindProperty("impactEffectLifetime").floatValue = 0.16f;
                serializedFirePoint.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, TurretFirePointPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ParticleSystem EnsureChildParticleVfx(Transform parent, GameObject sourcePrefab, string childName, float localScale)
        {
            if (parent == null || sourcePrefab == null)
            {
                return null;
            }

            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject, true);
            }

            GameObject child = PrefabUtility.InstantiatePrefab(sourcePrefab, parent) as GameObject;
            if (child == null)
            {
                Debug.LogError($"[BuildableLifecycleFeedbackBinder] Failed to instantiate VFX prefab: {AssetDatabase.GetAssetPath(sourcePrefab)}");
                return null;
            }

            child.name = childName;
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one * localScale;
            return child.GetComponentInChildren<ParticleSystem>(true);
        }

        private static void BindMortarRoles(
            GameObject mortarBasicMuzzleVfx,
            GameObject mortarRapidMuzzleVfx,
            GameObject mortarHeavyMuzzleVfx,
            GameObject mortarProjectileVfx,
            MortarProjectile mortarBasicProjectile,
            MortarProjectile mortarRapidProjectile,
            MortarProjectile mortarHeavyProjectile,
            GameObject mortarBasicImpactVfx,
            GameObject mortarRapidImpactVfx,
            GameObject mortarHeavyImpactVfx)
        {
            for (int i = 0; i < MortarRolePaths.Length; i++)
            {
                MortarSkillRoleDefinitionSO role = AssetDatabase.LoadAssetAtPath<MortarSkillRoleDefinitionSO>(MortarRolePaths[i]);
                if (role == null)
                {
                    Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing mortar role: {MortarRolePaths[i]}");
                    continue;
                }

                bool rapid = MortarRolePaths[i].Contains("Rapid");
                bool heavy = MortarRolePaths[i].Contains("Heavy");
                SerializedObject serializedRole = new SerializedObject(role);
                serializedRole.FindProperty("muzzleVfxPrefab").objectReferenceValue = heavy
                    ? mortarHeavyMuzzleVfx
                    : rapid
                        ? mortarRapidMuzzleVfx
                        : mortarBasicMuzzleVfx;
                serializedRole.FindProperty("muzzleVfxScale").floatValue = 1f;
                serializedRole.FindProperty("projectilePrefab").objectReferenceValue = heavy
                    ? mortarHeavyProjectile
                    : rapid
                        ? mortarRapidProjectile
                        : mortarBasicProjectile;
                serializedRole.FindProperty("projectileVfxPrefab").objectReferenceValue = mortarProjectileVfx;
                serializedRole.FindProperty("impactVfxPrefab").objectReferenceValue = heavy
                    ? mortarHeavyImpactVfx
                    : rapid
                        ? mortarRapidImpactVfx
                        : mortarBasicImpactVfx;
                serializedRole.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(role);
            }
        }

        private static GameObject LoadVfx(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing VFX prefab: {path}");
            }

            return prefab;
        }

        private static MortarProjectile LoadProjectile(string path)
        {
            MortarProjectile prefab = AssetDatabase.LoadAssetAtPath<MortarProjectile>(path);
            if (prefab == null)
            {
                Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing mortar projectile prefab: {path}");
            }

            return prefab;
        }

        private static ParticleSystem LoadParticleVfx(string path)
        {
            GameObject prefab = LoadVfx(path);
            if (prefab == null)
            {
                return null;
            }

            ParticleSystem particleSystem = prefab.GetComponentInChildren<ParticleSystem>(true);
            if (particleSystem == null)
            {
                Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing ParticleSystem in VFX prefab: {path}");
            }

            return particleSystem;
        }

        private static AudioClip LoadAudio(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogError($"[BuildableLifecycleFeedbackBinder] Missing audio clip: {path}");
            }

            return clip;
        }

        private static void BindLifecycleReference(Object target, BuildableLifecycleFeedback feedback)
        {
            if (target == null || feedback == null)
            {
                return;
            }

            SerializedObject serializedTarget = new SerializedObject(target);
            SerializedProperty lifecycleProperty = serializedTarget.FindProperty("lifecycleFeedback");
            if (lifecycleProperty == null)
            {
                Debug.LogError($"[BuildableLifecycleFeedbackBinder] lifecycleFeedback field not found on {target.GetType().Name}.");
                return;
            }

            lifecycleProperty.objectReferenceValue = feedback;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
