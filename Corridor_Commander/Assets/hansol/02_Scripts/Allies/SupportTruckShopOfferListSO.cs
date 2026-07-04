using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Shops/Support Truck Offer List",
        fileName = "SupportTruckOfferList")]
    public sealed class SupportTruckShopOfferListSO : ScriptableObject
    {
        [SerializeField] private SupportTruckShopCategory category;
        [SerializeField] private string displayName = "Offer List";
        [SerializeField] private List<SupportTruckShopOfferEntry> offers = new List<SupportTruckShopOfferEntry>();

        public SupportTruckShopCategory Category => category;
        public string DisplayName => displayName;
        public IReadOnlyList<SupportTruckShopOfferEntry> Offers => offers;

        public SupportTruckShopOfferEntry GetOffer(int index)
        {
            if (index < 0 || index >= offers.Count)
            {
                return null;
            }

            return offers[index];
        }
    }
}
