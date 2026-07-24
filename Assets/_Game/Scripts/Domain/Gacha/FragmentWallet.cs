#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Heroes;
using UnityEngine;

namespace IdleMedievalLegends.Domain.Gacha
{
    [Serializable]
    public sealed class HeroFragmentBalance
    {
        [SerializeField] private string heroDefinitionId = string.Empty;
        [SerializeField] private long quantity;

        public HeroFragmentBalance()
        {
        }

        public HeroFragmentBalance(string heroDefinitionId, long quantity)
        {
            this.heroDefinitionId = heroDefinitionId ?? string.Empty;
            this.quantity = quantity;
            Validate();
        }

        public string HeroDefinitionId => heroDefinitionId;
        public long Quantity => quantity;

        internal void SetQuantity(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            quantity = value;
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(heroDefinitionId) || quantity < 0)
                throw new InvalidOperationException("Saldo de fragmentos inválido.");
        }
    }

    /// <summary>
    /// Carteira serializável do simulador. O Dictionary é apenas índice reconstruído
    /// em memória e nunca é entregue ao JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class FragmentWallet : ISerializationCallbackReceiver
    {
        [SerializeField] private List<HeroFragmentBalance> balances =
            new List<HeroFragmentBalance>();
        [SerializeField] private long revision;

        [NonSerialized] private Dictionary<string, HeroFragmentBalance> index;

        public FragmentWallet()
        {
        }

        public IReadOnlyList<HeroFragmentBalance> Balances => balances.AsReadOnly();
        public long Revision => revision;

        public long GetBalance(string heroDefinitionId)
        {
            EnsureIndex();
            if (string.IsNullOrWhiteSpace(heroDefinitionId))
                throw new ArgumentException(
                    "heroDefinitionId é obrigatório.",
                    nameof(heroDefinitionId));
            return index.TryGetValue(heroDefinitionId, out HeroFragmentBalance value)
                ? value.Quantity
                : 0;
        }

        public void Credit(string heroDefinitionId, long quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(quantity),
                    "Crédito de fragmentos deve ser positivo.");
            EnsureIndex();
            if (string.IsNullOrWhiteSpace(heroDefinitionId))
                throw new ArgumentException(
                    "heroDefinitionId é obrigatório.",
                    nameof(heroDefinitionId));

            if (!index.TryGetValue(heroDefinitionId, out HeroFragmentBalance value))
            {
                value = new HeroFragmentBalance(heroDefinitionId, quantity);
                balances.Add(value);
                index.Add(heroDefinitionId, value);
            }
            else
            {
                value.SetQuantity(checked(value.Quantity + quantity));
            }
            revision = checked(revision + 1);
        }

        public void CreditRewards(IReadOnlyList<GachaReward> rewards)
        {
            if (rewards == null) throw new ArgumentNullException(nameof(rewards));
            var additions = new Dictionary<string, long>(StringComparer.Ordinal);
            for (int i = 0; i < rewards.Count; i++)
            {
                GachaReward reward = rewards[i];
                if (reward == null ||
                    reward.RewardType != GachaRewardType.HeroFragments)
                {
                    continue;
                }
                long current = additions.TryGetValue(
                    reward.HeroDefinitionId,
                    out long value) ? value : 0;
                additions[reward.HeroDefinitionId] =
                    checked(current + reward.Quantity);
            }

            EnsureIndex();
            foreach (KeyValuePair<string, long> addition in additions)
            {
                checked
                {
                    _ = GetBalance(addition.Key) + addition.Value;
                }
            }
            foreach (KeyValuePair<string, long> addition in additions)
                Credit(addition.Key, addition.Value);
        }

        internal void Debit(string heroDefinitionId, long quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity));
            EnsureIndex();
            if (!index.TryGetValue(heroDefinitionId, out HeroFragmentBalance value) ||
                value.Quantity < quantity)
            {
                throw new InvalidOperationException("Fragmentos insuficientes.");
            }
            value.SetQuantity(value.Quantity - quantity);
            revision = checked(revision + 1);
        }

        public void Validate()
        {
            if (balances == null || revision < 0)
                throw new InvalidOperationException("FragmentWallet inválida.");
            index = new Dictionary<string, HeroFragmentBalance>(StringComparer.Ordinal);
            for (int i = 0; i < balances.Count; i++)
            {
                HeroFragmentBalance balance = balances[i];
                if (balance == null)
                    throw new InvalidOperationException("Saldo de fragmentos nulo.");
                balance.Validate();
                if (!index.TryAdd(balance.HeroDefinitionId, balance))
                    throw new InvalidOperationException(
                        "Saldo duplicado para " + balance.HeroDefinitionId + ".");
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            index = null;
        }

        private void EnsureIndex()
        {
            if (index == null)
                Validate();
        }
    }

    public static class BannerEligibilityRules
    {
        public static bool CanUnlock(
            FragmentWallet wallet,
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            ValidateIdentity(wallet, hero);
            hero.Validate(tuning);
            if (hero.Unlocked)
                return false;
            long cost = HeroProgressionRules.GetUnlockFragmentCost(hero.Rarity, tuning);
            return checked(wallet.GetBalance(hero.DefinitionId) + hero.OwnedFragments) >= cost;
        }

        public static HeroInstance Unlock(
            FragmentWallet wallet,
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            ValidateIdentity(wallet, hero);
            hero.Validate(tuning);
            if (hero.Unlocked)
                throw new InvalidOperationException("Herói já está desbloqueado.");

            long cost = HeroProgressionRules.GetUnlockFragmentCost(hero.Rarity, tuning);
            long requiredFromWallet = Math.Max(0, cost - hero.OwnedFragments);
            if (wallet.GetBalance(hero.DefinitionId) < requiredFromWallet)
                throw new InvalidOperationException("Fragmentos insuficientes para desbloqueio.");

            HeroInstance funded = requiredFromWallet == 0
                ? hero
                : HeroProgressionRules.AddFragments(hero, requiredFromWallet, tuning);
            HeroInstance unlocked = HeroProgressionRules.Unlock(funded, tuning);
            if (requiredFromWallet > 0)
                wallet.Debit(hero.DefinitionId, requiredFromWallet);
            return unlocked;
        }

        public static bool CanAscend(
            FragmentWallet wallet,
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            ValidateIdentity(wallet, hero);
            hero.Validate(tuning);
            if (!hero.Unlocked || hero.AscensionLevel >= tuning.maxAscensionLevel)
                return false;
            long cost = HeroProgressionRules.GetAscensionFragmentCost(
                hero.AscensionLevel,
                tuning);
            return checked(wallet.GetBalance(hero.DefinitionId) + hero.OwnedFragments) >= cost;
        }

        public static HeroInstance Ascend(
            FragmentWallet wallet,
            HeroInstance hero,
            CombatBalanceTuning tuning)
        {
            if (!CanAscend(wallet, hero, tuning))
                throw new InvalidOperationException("Requisitos de ascensão não atendidos.");
            long cost = HeroProgressionRules.GetAscensionFragmentCost(
                hero.AscensionLevel,
                tuning);
            long requiredFromWallet = Math.Max(0, cost - hero.OwnedFragments);
            HeroInstance funded = requiredFromWallet == 0
                ? hero
                : HeroProgressionRules.AddFragments(hero, requiredFromWallet, tuning);
            HeroInstance ascended = HeroProgressionRules.Ascend(funded, tuning);
            if (requiredFromWallet > 0)
                wallet.Debit(hero.DefinitionId, requiredFromWallet);
            return ascended;
        }

        private static void ValidateIdentity(
            FragmentWallet wallet,
            HeroInstance hero)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (string.IsNullOrWhiteSpace(hero.DefinitionId))
                throw new InvalidOperationException("Herói sem definitionId.");
        }
    }
}
#endif
