using System;
using UnityEngine;

namespace CorridorCommander.Audio
{
    [CreateAssetMenu(
        fileName = "SceneBgmTable",
        menuName = "Corridor Commander/Audio/Scene BGM Table")]
    public sealed class SceneBgmTableSO : ScriptableObject
    {
        [Serializable]
        public sealed class SceneBgmEntry
        {
            [SerializeField] private string sceneName;
            [SerializeField] private BgmDefinitionSO bgm;

            public string SceneName => sceneName;
            public BgmDefinitionSO Bgm => bgm;
        }

        [Header("Fallback")]
        [SerializeField] private BgmDefinitionSO fallbackBgm;

        [Header("Scene Entries")]
        [SerializeField] private SceneBgmEntry[] entries = Array.Empty<SceneBgmEntry>();

        public BgmDefinitionSO FallbackBgm => fallbackBgm;

        public BgmDefinitionSO GetBgmForScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || entries == null)
            {
                return fallbackBgm;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                SceneBgmEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.SceneName))
                {
                    continue;
                }

                if (string.Equals(entry.SceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Bgm != null ? entry.Bgm : fallbackBgm;
                }
            }

            return fallbackBgm;
        }
    }
}

/*
Unity setup outline:
1. Create one Scene BGM Table asset.
2. Add expected scene names such as MainMenu, Tutorial, MainScene, and GameOver.
3. After the merge, replace placeholder scene names with the exact Unity scene asset names.
4. Assign the table to a BgmPlayer in the first loaded scene.
*/
