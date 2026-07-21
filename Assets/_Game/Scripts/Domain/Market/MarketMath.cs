using System;

namespace IdleMedievalLegends.Domain.Market
{
    public readonly struct MarketSettlement
    {
        public long GrossPrice { get; }
        public long FeeBurned { get; }
        public long SellerNet { get; }

        public MarketSettlement(long grossPrice, long feeBurned, long sellerNet)
        {
            GrossPrice = grossPrice;
            FeeBurned = feeBurned;
            SellerNet = sellerNet;
        }
    }

    public static class MarketMath
    {
        public const int BasisPointDenominator = 10_000;
        public const int DefaultMarketFeeBasisPoints = 1_000; // 10%
        public const long RecommendedMinimumListingPrice = 10;

        /// <summary>
        /// Calcula a taxa com inteiros e arredondamento para cima. O servidor deve
        /// repetir o cálculo; o resultado do cliente serve apenas para preview.
        /// </summary>
        public static MarketSettlement CalculateSettlement(
            long grossPrice,
            int feeBasisPoints = DefaultMarketFeeBasisPoints)
        {
            if (grossPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(grossPrice));
            if (feeBasisPoints < 0 || feeBasisPoints > BasisPointDenominator)
                throw new ArgumentOutOfRangeException(nameof(feeBasisPoints));

            long wholeUnits = grossPrice / BasisPointDenominator;
            long remainder = grossPrice % BasisPointDenominator;

            long feeFromWholeUnits = checked(wholeUnits * feeBasisPoints);
            long remainderNumerator = checked(remainder * feeBasisPoints);
            long feeFromRemainder =
                (remainderNumerator + BasisPointDenominator - 1) /
                BasisPointDenominator;

            long fee = checked(feeFromWholeUnits + feeFromRemainder);
            long sellerNet = checked(grossPrice - fee);

            return new MarketSettlement(grossPrice, fee, sellerNet);
        }
    }
}
