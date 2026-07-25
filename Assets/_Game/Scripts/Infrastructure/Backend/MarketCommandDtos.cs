using System;

namespace IdleMedievalLegends.Infrastructure.Backend
{
    [Serializable]
    public sealed class CreateMarketListingRequest
    {
        public string itemInstanceId = string.Empty;
        public long priceGems;

        public CreateMarketListingRequest()
        {
        }

        public CreateMarketListingRequest(
            string itemInstanceId,
            long priceGems)
        {
            this.itemInstanceId = itemInstanceId ?? throw new ArgumentNullException(nameof(itemInstanceId));
            this.priceGems = priceGems;
        }
    }

    [Serializable]
    public sealed class EmptyMarketListingRequest
    {
    }
}
