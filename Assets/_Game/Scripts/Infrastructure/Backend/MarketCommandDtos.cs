using System;
using UnityEngine;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    [Serializable]
    public sealed class CreateMarketListingCommand
    {
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private string itemInstanceId = string.Empty;
        [SerializeField] private long priceGems;

        public string RequestId => requestId;
        public string ItemInstanceId => itemInstanceId;
        public long PriceGems => priceGems;

        public CreateMarketListingCommand(
            string requestId,
            string itemInstanceId,
            long priceGems)
        {
            this.requestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            this.itemInstanceId = itemInstanceId ?? throw new ArgumentNullException(nameof(itemInstanceId));
            this.priceGems = priceGems;
        }
    }

    [Serializable]
    public sealed class BuyMarketListingCommand
    {
        [SerializeField] private string requestId = string.Empty;
        [SerializeField] private string listingId = string.Empty;

        public string RequestId => requestId;
        public string ListingId => listingId;

        public BuyMarketListingCommand(string requestId, string listingId)
        {
            this.requestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            this.listingId = listingId ?? throw new ArgumentNullException(nameof(listingId));
        }
    }
}
