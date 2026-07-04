using System.Reflection;
using UnityEngine;

namespace CorridorCommander.PlayerControl
{
    [DisallowMultipleComponent]
    public sealed class SquadShopCapacityBridge : MonoBehaviour
    {
        private const string HiredCountFieldName = "hiredSquadCount";

        [SerializeField] private PlayerSquadRoster roster;
        [SerializeField, Min(0.1f)] private float fallbackSyncInterval = 0.5f;
        [SerializeField] private bool logSynchronization;

        private static readonly FieldInfo HiredCountField = typeof(SupportTruckShop).GetField(
            HiredCountFieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        private float nextSyncTime;

        private void Awake()
        {
            ResolveRoster();
        }

        private void OnEnable()
        {
            ResolveRoster();
            Subscribe();
            SynchronizeShops();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSyncTime)
            {
                return;
            }

            nextSyncTime = Time.unscaledTime + Mathf.Max(0.1f, fallbackSyncInterval);
            ResolveRoster();
            Subscribe();
            SynchronizeShops();
        }

        private void Subscribe()
        {
            if (roster == null)
            {
                return;
            }

            roster.RosterChanged -= HandleRosterChanged;
            roster.RosterChanged += HandleRosterChanged;
        }

        private void Unsubscribe()
        {
            if (roster != null)
            {
                roster.RosterChanged -= HandleRosterChanged;
            }
        }

        private void HandleRosterChanged()
        {
            SynchronizeShops();
        }

        private void SynchronizeShops()
        {
            if (roster == null || HiredCountField == null)
            {
                return;
            }

            SupportTruckShop[] shops = FindObjectsByType<SupportTruckShop>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < shops.Length; i++)
            {
                SupportTruckShop shop = shops[i];
                if (shop == null || shop.HiredSquadCount == roster.MemberCount)
                {
                    continue;
                }

                HiredCountField.SetValue(shop, roster.MemberCount);

                if (logSynchronization)
                {
                    Debug.Log(
                        $"[SquadShopCapacityBridge] Synchronized {shop.name}: {roster.MemberCount}/{PlayerSquadRoster.MaxMemberCount}",
                        shop);
                }
            }
        }

        private void ResolveRoster()
        {
            if (roster == null)
            {
                roster = GetComponent<PlayerSquadRoster>();
            }

            if (roster == null)
            {
                roster = PlayerSquadRoster.Instance;
            }
        }
    }
}

/*
Unity setup outline:
1. Add SquadShopCapacityBridge beside PlayerSquadRoster on PlayerSetup 1.
2. Assign the Roster, or leave it empty for automatic binding.
3. The bridge keeps the existing SupportTruckShop hire counter equal to the living roster count.
4. This allows a dead member's F-slot to be purchased again without modifying hansol C# files.
*/
