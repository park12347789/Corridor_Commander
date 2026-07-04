using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CorridorCommander.PlayerControl;

namespace CorridorCommander
{
    [DisallowMultipleComponent]
    public sealed class SupportTruckShop : MonoBehaviour
    {
        [SerializeField] private SupportTruckShopCatalogSO catalog;
        [SerializeField] private GameObject squadMemberPrefab;
        [SerializeField] private Transform defaultFollowTarget;
        [SerializeField] private Transform hireSpawnPoint;
        [SerializeField] private int squadMemberCost = 100;
        [SerializeField] private int maxSquadMembers = 4;
        [SerializeField] private bool autoFindPlayerByTag = true;
        [SerializeField] private bool spawnAsSceneRoot = true;
        [SerializeField] private float hireSpawnBehindDistance = 2.2f;
        [SerializeField] private float hireSpawnLateralSpacing = 1.1f;
        [SerializeField] private float hireSpawnRowSpacing = 1.2f;
        [SerializeField] private float hireSpawnNavMeshSampleDistance = 2.5f;

        private int hiredSquadCount;

        public SupportTruckShopCatalogSO Catalog => catalog;
        public int SquadMemberCost => squadMemberCost;
        public int HiredSquadCount => hiredSquadCount;
        public int MaxSquadMembers => maxSquadMembers;

        public void ConfigureCatalog(SupportTruckShopCatalogSO configuredCatalog)
        {
            if (configuredCatalog != null)
            {
                catalog = configuredCatalog;
            }
        }

        public SupportTruckShopOfferListSO GetOfferList(SupportTruckShopCategory category)
        {
            return catalog != null ? catalog.GetList(category) : null;
        }

        public bool TryPurchaseOffer(
            SupportTruckShopOfferEntry offer,
            int availableCurrency,
            out int remainingCurrency,
            out GameObject spawnedObject,
            out string statusMessage)
        {
            remainingCurrency = availableCurrency;
            spawnedObject = null;

            if (offer == null)
            {
                statusMessage = "No offer selected";
                return false;
            }

            if (availableCurrency < offer.Cost)
            {
                statusMessage = "Not enough currency";
                return false;
            }

            switch (offer.Action)
            {
                case SupportTruckShopOfferAction.HireSquadMember:
                    spawnedObject = HireSquadMember(offer.SquadMemberPrefab);
                    if (spawnedObject == null)
                    {
                        statusMessage = "Squad hire failed";
                        return false;
                    }

                    ApplySquadRosterIcon(spawnedObject, offer.Icon);
                    remainingCurrency -= offer.Cost;
                    statusMessage = "Hired: " + offer.DisplayName;
                    return true;

                case SupportTruckShopOfferAction.GrantItem:
                    if (!TryGrantItem(offer, 1, out string itemGrantStatus))
                    {
                        statusMessage = itemGrantStatus;
                        Debug.Log(statusMessage, this);
                        return false;
                    }

                    remainingCurrency -= offer.Cost;
                    statusMessage = itemGrantStatus;
                    Debug.Log(statusMessage, this);
                    return true;

                case SupportTruckShopOfferAction.BuyUpgrade:
                    if (offer.UnlockKey == SupportTruckShopUnlockKey.None)
                    {
                        statusMessage = "No unlock data: " + offer.DisplayName;
                        Debug.Log(statusMessage, this);
                        return false;
                    }

                    if (!SupportTruckShopGlobalUnlocks.TryUnlock(offer.UnlockKey))
                    {
                        statusMessage = "Already unlocked: " + offer.DisplayName;
                        Debug.Log(statusMessage, this);
                        return false;
                    }

                    remainingCurrency -= offer.Cost;
                    statusMessage = "Unlocked: " + offer.DisplayName;
                    Debug.Log(statusMessage, this);
                    return true;

                default:
                    statusMessage = "No action: " + offer.DisplayName;
                    Debug.Log(statusMessage, this);
                    return false;
            }
        }

        public bool CanHireSquadMember(int availableCurrency)
        {
            return squadMemberPrefab != null
                && hiredSquadCount < maxSquadMembers
                && availableCurrency >= squadMemberCost;
        }

        public GameObject HireSquadMember()
        {
            return HireSquadMember(squadMemberPrefab);
        }

        public GameObject HireSquadMember(GameObject prefabOverride)
        {
            Transform target = ResolveFollowTarget();
            GameObject prefab = prefabOverride != null ? prefabOverride : squadMemberPrefab;
            if (target == null || prefab == null || hiredSquadCount >= maxSquadMembers)
            {
                return null;
            }

            GameObject spawnedObject = CreateSquadMember(target, prefab);
            if (spawnedObject == null)
            {
                return null;
            }

            if (TryResolveSquadRosterIcon(prefab, out Sprite rosterIcon))
            {
                ApplySquadRosterIcon(spawnedObject, rosterIcon);
            }
            else
            {
                Debug.LogError(
                    "[SupportTruckShop] Squad roster icon is missing for prefab: " + prefab.name,
                    this);
            }

            return spawnedObject;
        }

        private bool TryResolveSquadRosterIcon(GameObject prefab, out Sprite icon)
        {
            icon = null;

            SupportTruckShopOfferListSO squadOffers = catalog != null ? catalog.SquadOffers : null;
            if (squadOffers == null || squadOffers.Offers == null)
            {
                return false;
            }

            IReadOnlyList<SupportTruckShopOfferEntry> offers = squadOffers.Offers;
            for (int i = 0; i < offers.Count; i++)
            {
                SupportTruckShopOfferEntry offer = offers[i];
                if (offer == null || offer.Action != SupportTruckShopOfferAction.HireSquadMember)
                {
                    continue;
                }

                GameObject offerPrefab = offer.SquadMemberPrefab != null ? offer.SquadMemberPrefab : squadMemberPrefab;
                if (offerPrefab != prefab || offer.Icon == null)
                {
                    continue;
                }

                icon = offer.Icon;
                return true;
            }

            return false;
        }

        private static void ApplySquadRosterIcon(GameObject spawnedObject, Sprite icon)
        {
            AlliedSquadMemberFollower follower = spawnedObject != null
                ? spawnedObject.GetComponent<AlliedSquadMemberFollower>()
                : null;

            if (follower == null && spawnedObject != null)
            {
                follower = spawnedObject.GetComponentInChildren<AlliedSquadMemberFollower>(true);
            }

            if (follower == null)
            {
                Debug.LogError("[SupportTruckShop] Hired squad member has no AlliedSquadMemberFollower.", spawnedObject);
                return;
            }

            if (icon == null)
            {
                Debug.LogError("[SupportTruckShop] Hired squad member roster icon is missing.", spawnedObject);
                return;
            }

            follower.SetRosterIcon(icon);
        }

        private bool TryGrantItem(SupportTruckShopOfferEntry offer, int amount, out string statusMessage)
        {
            statusMessage = string.Empty;
            SupportTruckShopItemGrant itemGrant = offer != null ? offer.ItemGrant : SupportTruckShopItemGrant.None;

            if (itemGrant == SupportTruckShopItemGrant.TemporaryGun && offer.WeaponDefinition != null)
            {
                ISupportTruckWeaponReceiver weaponReceiver = ResolveWeaponReceiver();
                if (weaponReceiver == null)
                {
                    statusMessage = "No weapon receiver found";
                    return false;
                }

                return weaponReceiver.TryReceiveSupportTruckWeapon(
                    offer.WeaponDefinition,
                    offer.FillWeaponMagazine,
                    out statusMessage);
            }

            if (offer != null && offer.ItemDefinition != null)
            {
                ISupportTruckPlayerItemReceiver playerItemReceiver = ResolvePlayerItemReceiver();
                if (playerItemReceiver == null)
                {
                    statusMessage = "No player item receiver found";
                    return false;
                }

                int grantAmount = Mathf.Max(1, offer.ItemAmount) * Mathf.Max(1, amount);
                return playerItemReceiver.TryReceiveSupportTruckPlayerItem(
                    offer.ItemDefinition,
                    grantAmount,
                    out statusMessage);
            }

            if (itemGrant == SupportTruckShopItemGrant.None)
            {
                statusMessage = "No item data";
                return false;
            }

            ISupportTruckItemReceiver receiver = ResolveItemReceiver();
            if (receiver == null)
            {
                statusMessage = "No item receiver found";
                return false;
            }

            return receiver.TryReceiveSupportTruckItem(itemGrant, Mathf.Max(1, amount), out statusMessage);
        }

        private ISupportTruckPlayerItemReceiver ResolvePlayerItemReceiver()
        {
            ISupportTruckPlayerItemReceiver receiver = FindPlayerItemReceiverInHierarchy(ResolveFollowTarget());
            if (receiver != null)
            {
                return receiver;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ISupportTruckPlayerItemReceiver foundReceiver)
                {
                    return foundReceiver;
                }
            }

            return null;
        }

        private static ISupportTruckPlayerItemReceiver FindPlayerItemReceiverInHierarchy(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            MonoBehaviour[] parentBehaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < parentBehaviours.Length; i++)
            {
                if (parentBehaviours[i] is ISupportTruckPlayerItemReceiver receiver)
                {
                    return receiver;
                }
            }

            MonoBehaviour[] childBehaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < childBehaviours.Length; i++)
            {
                if (childBehaviours[i] is ISupportTruckPlayerItemReceiver receiver)
                {
                    return receiver;
                }
            }

            return null;
        }

        private ISupportTruckWeaponReceiver ResolveWeaponReceiver()
        {
            ISupportTruckWeaponReceiver receiver = FindWeaponReceiverInHierarchy(ResolveFollowTarget());
            if (receiver != null)
            {
                return receiver;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ISupportTruckWeaponReceiver foundReceiver)
                {
                    return foundReceiver;
                }
            }

            return null;
        }

        private static ISupportTruckWeaponReceiver FindWeaponReceiverInHierarchy(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            MonoBehaviour[] parentBehaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < parentBehaviours.Length; i++)
            {
                if (parentBehaviours[i] is ISupportTruckWeaponReceiver receiver)
                {
                    return receiver;
                }
            }

            MonoBehaviour[] childBehaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < childBehaviours.Length; i++)
            {
                if (childBehaviours[i] is ISupportTruckWeaponReceiver receiver)
                {
                    return receiver;
                }
            }

            return null;
        }

        private ISupportTruckItemReceiver ResolveItemReceiver()
        {
            ISupportTruckItemReceiver receiver = FindReceiverInHierarchy(ResolveFollowTarget());
            if (receiver != null)
            {
                return receiver;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ISupportTruckItemReceiver foundReceiver)
                {
                    return foundReceiver;
                }
            }

            return null;
        }

        private static ISupportTruckItemReceiver FindReceiverInHierarchy(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            MonoBehaviour[] parentBehaviours = target.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < parentBehaviours.Length; i++)
            {
                if (parentBehaviours[i] is ISupportTruckItemReceiver receiver)
                {
                    return receiver;
                }
            }

            MonoBehaviour[] childBehaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < childBehaviours.Length; i++)
            {
                if (childBehaviours[i] is ISupportTruckItemReceiver receiver)
                {
                    return receiver;
                }
            }

            return null;
        }

        public bool TryHireSquadMember(int availableCurrency, out int remainingCurrency, out GameObject hiredMember)
        {
            remainingCurrency = availableCurrency;
            hiredMember = null;

            if (!CanHireSquadMember(availableCurrency))
            {
                return false;
            }

            hiredMember = HireSquadMember();
            if (hiredMember == null)
            {
                return false;
            }

            remainingCurrency -= squadMemberCost;
            return true;
        }

        public void SetDefaultFollowTarget(Transform target)
        {
            defaultFollowTarget = target;
        }

        private GameObject CreateSquadMember(Transform target, GameObject prefab)
        {
            Vector3 spawnPosition = ResolveHireSpawnPosition(target);
            Quaternion spawnRotation = ResolveHireSpawnRotation(target);
            GameObject member = Instantiate(prefab, spawnPosition, spawnRotation);
            member.name = "TEMP_HiredAlliedDummy_" + (hiredSquadCount + 1).ToString("00");

            if (!spawnAsSceneRoot)
            {
                member.transform.SetParent(transform.parent, true);
            }

            AlliedSquadMemberFollower follower = member.GetComponent<AlliedSquadMemberFollower>();
            if (follower == null)
            {
                follower = member.AddComponent<AlliedSquadMemberFollower>();
            }

            follower.Configure(target, hiredSquadCount);
            RegisterHiredMember(follower);
            hiredSquadCount++;
            return member;
        }

        private void RegisterHiredMember(AlliedSquadMemberFollower follower)
        {
            if (follower == null)
            {
                Debug.LogError("[SupportTruckShop] Hired squad member has no AlliedSquadMemberFollower.", this);
                return;
            }

            PlayerSquadRoster roster = PlayerSquadRoster.Instance;
            if (roster == null)
            {
                Debug.LogError("[SupportTruckShop] PlayerSquadRoster is not available for hired squad member registration.", this);
                return;
            }

            if (!roster.TryRegisterMember(follower))
            {
                Debug.LogError("[SupportTruckShop] PlayerSquadRoster rejected hired squad member registration.", follower);
            }
        }

        private Vector3 ResolveHireSpawnPosition(Transform target)
        {
            Vector3 desiredPosition = target.position + GetHireFormationOffset(target, hiredSquadCount);
            return TrySampleNavMesh(desiredPosition, out Vector3 navMeshPosition)
                ? navMeshPosition
                : desiredPosition;
        }

        private Vector3 GetHireFormationOffset(Transform target, int formationIndex)
        {
            Vector3 forward = target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = target.right;
            right.y = 0f;
            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            int row = Mathf.Max(0, formationIndex) / 3;
            int column = Mathf.Max(0, formationIndex) % 3;
            float lateralOffset = (column - 1) * hireSpawnLateralSpacing;
            float backOffset = hireSpawnBehindDistance + row * hireSpawnRowSpacing;

            return right.normalized * lateralOffset - forward.normalized * backOffset;
        }

        private bool TrySampleNavMesh(Vector3 position, out Vector3 sampledPosition)
        {
            sampledPosition = position;
            float sampleDistance = Mathf.Max(0.1f, hireSpawnNavMeshSampleDistance);
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
            {
                return false;
            }

            sampledPosition = hit.position;
            return true;
        }

        private static Quaternion ResolveHireSpawnRotation(Transform target)
        {
            Vector3 forward = target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return target.rotation;
            }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private Transform ResolveFollowTarget()
        {
            if (defaultFollowTarget != null || !autoFindPlayerByTag)
            {
                return defaultFollowTarget;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                defaultFollowTarget = player.transform;
            }

            return defaultFollowTarget;
        }
    }
}
