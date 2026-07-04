using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class BuildableObject : MonoBehaviour
    {
        [SerializeField] private BuildableDefinitionSO definition;

        private readonly List<IBuildableRole> roles = new List<IBuildableRole>();
        private bool isInitialized;
        private BuildContext installContext;

        public BuildableDefinitionSO Definition => definition;
        public BuildContext InstallContext => installContext;
        public bool IsInitialized => isInitialized;

        public void Initialize(BuildContext context)
        {
            if (isInitialized)
            {
                return;
            }

            installContext = context;
            if (definition == null)
            {
                definition = context.Definition;
            }

            CacheRoles();
            for (int i = 0; i < roles.Count; i++)
            {
                roles[i].Initialize(this, context);
            }

            isInitialized = true;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < roles.Count; i++)
            {
                roles[i].Dispose();
            }
        }

        private void CacheRoles()
        {
            roles.Clear();
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IBuildableRole role)
                {
                    roles.Add(role);
                }
            }
        }
    }
}
