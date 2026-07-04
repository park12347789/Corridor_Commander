using UnityEngine;

namespace CorridorCommander
{
    [CreateAssetMenu(
        menuName = "Corridor Commander/Shops/Support Truck Catalog",
        fileName = "SupportTruckCatalog")]
    public sealed class SupportTruckShopCatalogSO : ScriptableObject
    {
        [SerializeField] private SupportTruckShopOfferListSO itemOffers;
        [SerializeField] private SupportTruckShopOfferListSO squadOffers;
        [SerializeField] private SupportTruckShopOfferListSO upgradeOffers;

        public SupportTruckShopOfferListSO ItemOffers => itemOffers;
        public SupportTruckShopOfferListSO SquadOffers => squadOffers;
        public SupportTruckShopOfferListSO UpgradeOffers => upgradeOffers;

        public SupportTruckShopOfferListSO GetList(SupportTruckShopCategory category)
        {
            switch (category)
            {
                case SupportTruckShopCategory.Items:
                    return itemOffers;
                case SupportTruckShopCategory.Squad:
                    return squadOffers;
                case SupportTruckShopCategory.Upgrades:
                    return upgradeOffers;
                default:
                    return null;
            }
        }
    }
}
