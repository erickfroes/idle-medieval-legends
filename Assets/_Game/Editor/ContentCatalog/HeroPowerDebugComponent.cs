using System;
using System.Globalization;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Heroes;
using UnityEngine;

namespace IdleMedievalLegends.Editor.ContentCatalog
{
    /// <summary>
    /// Adaptador opcional de Editor para inspecionar o cálculo puro de Poder.
    /// Não participa do runtime nem é necessário para calcular atributos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroPowerDebugComponent : MonoBehaviour
    {
        [Header("Fontes")]
        [SerializeField] private ContentCatalogAsset contentCatalog;
        [SerializeField] private CombatBalanceConfigAsset combatBalance;

        [Header("Herói simulado")]
        [SerializeField] private string definitionId = "hero_paladin_001";
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField, Min(0)] private int ascensionLevel;

        [Header("Bônus de equipamento simulados")]
        [SerializeField] private long flatHealth;
        [SerializeField] private double percentHealth;
        [SerializeField] private long flatAttack;
        [SerializeField] private double percentAttack;
        [SerializeField] private long flatDefense;
        [SerializeField] private double percentDefense;
        [SerializeField] private double flatSpeed;
        [SerializeField] private double percentSpeed;

        [Header("Resultado somente leitura")]
        [SerializeField, TextArea(12, 18)] private string preview =
            "Use o menu de contexto Refresh Hero Power Preview.";

        [ContextMenu("Refresh Hero Power Preview")]
        public void RefreshPreview()
        {
            try
            {
                if (contentCatalog == null)
                    throw new InvalidOperationException("ContentCatalogAsset não atribuído.");
                if (combatBalance == null)
                    throw new InvalidOperationException("CombatBalanceConfigAsset não atribuído.");

                combatBalance.EnsureInitialized();
                CombatBalanceTuning tuning = combatBalance.Tuning;
                HeroDefinition definition = contentCatalog
                    .BuildValidatedLookup()
                    .GetHero(definitionId);
                HeroInstance hero = HeroInstance.Restore(
                    "editor_power_preview",
                    definition.DefinitionId,
                    string.Empty,
                    level,
                    0,
                    rarity,
                    ascensionLevel,
                    0,
                    null,
                    true,
                    0,
                    0,
                    null,
                    0,
                    tuning);
                var equipmentModifiers = new HeroStatModifiers(
                    flatHealth,
                    percentHealth,
                    flatAttack,
                    percentAttack,
                    flatDefense,
                    percentDefense,
                    flatSpeed,
                    percentSpeed);
                HeroPowerBreakdown result = HeroPowerCalculator.CalculateBreakdown(
                    hero,
                    definition,
                    equipmentModifiers,
                    HeroStatModifiers.None,
                    tuning);

                preview = Format(result);
            }
            catch (Exception exception)
            {
                preview = $"ERRO: {exception.Message}";
            }
        }

        private static string Format(HeroPowerBreakdown value)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return
                $"Base: HP {value.BaseStats.MaxHealth}, ATK {value.BaseStats.Attack}, " +
                $"DEF {value.BaseStats.Defense}, SPD {value.BaseStats.Speed.ToString("0.##", culture)}\n" +
                $"Multiplicadores: nível {value.LevelMultiplier.ToString("0.####", culture)}, " +
                $"raridade {value.RarityMultiplier.ToString("0.####", culture)}, " +
                $"ascensão {value.AscensionMultiplier.ToString("0.####", culture)}\n" +
                $"Bônus: HP +{value.CombinedModifiers.FlatHealth} / " +
                $"{value.CombinedModifiers.PercentHealth.ToString("P2", culture)}, " +
                $"ATK +{value.CombinedModifiers.FlatAttack} / " +
                $"{value.CombinedModifiers.PercentAttack.ToString("P2", culture)}, " +
                $"DEF +{value.CombinedModifiers.FlatDefense} / " +
                $"{value.CombinedModifiers.PercentDefense.ToString("P2", culture)}, " +
                $"SPD +{value.CombinedModifiers.FlatSpeed.ToString("0.##", culture)} / " +
                $"{value.CombinedModifiers.PercentSpeed.ToString("P2", culture)}\n" +
                $"Final: HP {value.FinalStats.MaxHealth}, ATK {value.FinalStats.Attack}, " +
                $"DEF {value.FinalStats.Defense}, SPD {value.FinalStats.Speed.ToString("0.##", culture)}\n" +
                $"Redução de dano: {value.DamageReduction.ToString("P2", culture)}\n" +
                $"Vida efetiva: {value.EffectiveHealth.ToString("0.##", culture)}\n" +
                $"Fator de velocidade: {value.SpeedFactor.ToString("0.####", culture)}\n" +
                $"Índice ofensivo: {value.OffenseIndex.ToString("0.##", culture)}\n" +
                $"Poder: {value.HeroPower.Value}";
        }
    }
}
