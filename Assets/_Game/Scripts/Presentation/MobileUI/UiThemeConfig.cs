using System;
using System.Collections.Generic;
using System.Globalization;
using IdleMedievalLegends.Domain.Common;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.MobileUI
{
    [CreateAssetMenu(
        menuName = "Idle Medieval Legends/UI/Mobile Theme",
        fileName = "UiThemeConfig")]
    public sealed class UiThemeConfig : ScriptableObject
    {
        [Header("Typography")]
        [SerializeField] private Font primaryFont;

        [Header("Colors")]
        [SerializeField] private Color background = new Color(0.055f, 0.065f, 0.085f, 1f);
        [SerializeField] private Color surface = new Color(0.10f, 0.12f, 0.16f, 1f);
        [SerializeField] private Color elevatedSurface = new Color(0.15f, 0.17f, 0.22f, 1f);
        [SerializeField] private Color primary = new Color(0.78f, 0.50f, 0.18f, 1f);
        [SerializeField] private Color secondary = new Color(0.24f, 0.31f, 0.40f, 1f);
        [SerializeField] private Color textPrimary = new Color(0.96f, 0.94f, 0.88f, 1f);
        [SerializeField] private Color textSecondary = new Color(0.72f, 0.75f, 0.80f, 1f);
        [SerializeField] private Color success = new Color(0.25f, 0.72f, 0.42f, 1f);
        [SerializeField] private Color warning = new Color(0.95f, 0.68f, 0.20f, 1f);
        [SerializeField] private Color error = new Color(0.88f, 0.27f, 0.27f, 1f);
        [SerializeField] private Color locked = new Color(0.38f, 0.40f, 0.46f, 1f);
        [SerializeField] private Color overlay = new Color(0f, 0f, 0f, 0.76f);

        [Header("Rarity Colors")]
        [SerializeField] private Color common = new Color(0.68f, 0.68f, 0.68f, 1f);
        [SerializeField] private Color uncommon = new Color(0.32f, 0.76f, 0.38f, 1f);
        [SerializeField] private Color rare = new Color(0.25f, 0.48f, 0.92f, 1f);
        [SerializeField] private Color epic = new Color(0.66f, 0.31f, 0.86f, 1f);
        [SerializeField] private Color legendary = new Color(0.95f, 0.55f, 0.12f, 1f);
        [SerializeField] private Color mythic = new Color(0.91f, 0.20f, 0.40f, 1f);

        [Header("Metrics")]
        [SerializeField, Min(1f)] private float spacingUnit = 8f;
        [SerializeField, Min(0f)] private float cornerRadius = 12f;
        [SerializeField, Min(0.5f)] private float compactScale = 0.9f;
        [SerializeField, Min(1f)] private float expandedScale = 1.15f;
        [SerializeField, Min(40f)] private float minimumTouchSize = 48f;
        [SerializeField, Min(12)] private int bodyFontSize = 18;
        [SerializeField, Min(14)] private int titleFontSize = 28;

        public Font PrimaryFont => primaryFont;
        public Color Background => background;
        public Color Surface => surface;
        public Color ElevatedSurface => elevatedSurface;
        public Color Primary => primary;
        public Color Secondary => secondary;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color Success => success;
        public Color Warning => warning;
        public Color Error => error;
        public Color Locked => locked;
        public Color Overlay => overlay;
        public float SpacingUnit => spacingUnit;
        public float CornerRadius => cornerRadius;
        public float CompactScale => compactScale;
        public float ExpandedScale => expandedScale;
        public float MinimumTouchSize => minimumTouchSize;
        public int BodyFontSize => bodyFontSize;
        public int TitleFontSize => titleFontSize;

        public Color GetRarityColor(GameRarity rarity)
        {
            switch (rarity)
            {
                case GameRarity.Common: return common;
                case GameRarity.Uncommon: return uncommon;
                case GameRarity.Rare: return rare;
                case GameRarity.Epic: return epic;
                case GameRarity.Legendary: return legendary;
                case GameRarity.Mythic: return mythic;
                default: throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        public void Validate()
        {
            if (spacingUnit <= 0f)
                throw new InvalidOperationException("SpacingUnit deve ser positivo.");
            if (minimumTouchSize < 40f)
                throw new InvalidOperationException("MinimumTouchSize deve ter ao menos 40 unidades.");
            if (bodyFontSize < 12 || titleFontSize < bodyFontSize)
                throw new InvalidOperationException("Escala tipográfica inválida.");
            if (compactScale <= 0f || expandedScale < 1f)
                throw new InvalidOperationException("Escalas responsivas inválidas.");
        }
    }

    public static class CurrencyFormatter
    {
        public static string Format(long amount)
        {
            long absolute = amount == long.MinValue ? long.MaxValue : Math.Abs(amount);
            if (absolute < 1_000)
                return amount.ToString("N0", CultureInfo.InvariantCulture);
            if (absolute < 1_000_000)
                return FormatCompact(amount, 1_000, "K");
            if (absolute < 1_000_000_000)
                return FormatCompact(amount, 1_000_000, "M");
            if (absolute < 1_000_000_000_000)
                return FormatCompact(amount, 1_000_000_000, "B");
            return FormatCompact(amount, 1_000_000_000_000, "T");
        }

        private static string FormatCompact(long amount, long divisor, string suffix)
        {
            decimal value = decimal.Divide(amount, divisor);
            string format = decimal.Remainder(Math.Abs(value), 1m) == 0m ? "0" : "0.#";
            return value.ToString(format, CultureInfo.InvariantCulture) + suffix;
        }
    }

    public interface IUiPreferenceStore
    {
        int GetInt(string key, int defaultValue);
        float GetFloat(string key, float defaultValue);
        string GetString(string key, string defaultValue);
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void SetString(string key, string value);
        void Save();
    }

    public sealed class PlayerPrefsUiPreferenceStore : IUiPreferenceStore
    {
        private const string Prefix = "iml.ui.";

        public int GetInt(string key, int defaultValue) =>
            PlayerPrefs.GetInt(Prefix + key, defaultValue);

        public float GetFloat(string key, float defaultValue) =>
            PlayerPrefs.GetFloat(Prefix + key, defaultValue);

        public string GetString(string key, string defaultValue) =>
            PlayerPrefs.GetString(Prefix + key, defaultValue);

        public void SetInt(string key, int value) => PlayerPrefs.SetInt(Prefix + key, value);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(Prefix + key, value);
        public void SetString(string key, string value) => PlayerPrefs.SetString(Prefix + key, value);
        public void Save() => PlayerPrefs.Save();
    }

    [Serializable]
    public sealed class UiAccessibilitySettings
    {
        public bool MusicEnabled = true;
        public bool EffectsEnabled = true;
        public bool VibrationEnabled = true;
        public bool ReduceMotion;
        public bool HighContrast;
        public float TextScale = 1f;
        public int BattleSpeed = 1;
        public string Language = "system";
    }

    public sealed class UiPreferenceService
    {
        private readonly IUiPreferenceStore store;

        public UiPreferenceService(IUiPreferenceStore preferenceStore)
        {
            store = preferenceStore ?? throw new ArgumentNullException(nameof(preferenceStore));
        }

        public UiAccessibilitySettings Load()
        {
            return new UiAccessibilitySettings
            {
                MusicEnabled = store.GetInt("music", 1) != 0,
                EffectsEnabled = store.GetInt("effects", 1) != 0,
                VibrationEnabled = store.GetInt("vibration", 1) != 0,
                ReduceMotion = store.GetInt("reduce_motion", 0) != 0,
                HighContrast = store.GetInt("high_contrast", 0) != 0,
                TextScale = Mathf.Clamp(store.GetFloat("text_scale", 1f), 0.8f, 1.5f),
                BattleSpeed = Mathf.Clamp(store.GetInt("battle_speed", 1), 1, 3),
                Language = store.GetString("language", "system")
            };
        }

        public void Save(UiAccessibilitySettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            store.SetInt("music", settings.MusicEnabled ? 1 : 0);
            store.SetInt("effects", settings.EffectsEnabled ? 1 : 0);
            store.SetInt("vibration", settings.VibrationEnabled ? 1 : 0);
            store.SetInt("reduce_motion", settings.ReduceMotion ? 1 : 0);
            store.SetInt("high_contrast", settings.HighContrast ? 1 : 0);
            store.SetFloat("text_scale", Mathf.Clamp(settings.TextScale, 0.8f, 1.5f));
            store.SetInt("battle_speed", Mathf.Clamp(settings.BattleSpeed, 1, 3));
            store.SetString("language", settings.Language ?? "system");
            store.Save();
        }
    }

    public interface IUiTextService
    {
        string Get(string key);
    }

    public sealed class PortugueseUiTextService : IUiTextService
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>
        {
            { "route.home", "Início" },
            { "route.battle", "Batalha" },
            { "route.heroes", "Heróis" },
            { "route.inventory", "Inventário" },
            { "route.crafting", "Crafting" },
            { "route.dungeons", "Masmorras" },
            { "route.gacha", "Invocações" },
            { "route.market", "Mercado" },
            { "route.profile", "Perfil" },
            { "route.settings", "Ajustes" },
            { "route.campaign", "Campanha" },
            { "route.more", "Mais" },
            { "common.loading", "Carregando…" },
            { "common.back", "Voltar" },
            { "common.cancel", "Cancelar" },
            { "common.confirm", "Confirmar" },
            { "common.coming_soon", "Em breve" },
            { "common.on", "Ligado" },
            { "common.off", "Desligado" },
            { "currency.gold", "Ouro" },
            { "currency.energy", "Energia" },
            { "home.heroes_empty", "A coleção de heróis será conectada a snapshots autoritativos em uma etapa futura." },
            { "home.profile_empty", "Perfil e conta ainda não estão conectados." },
            { "home.quick_battle", "Ir para Batalha" },
            { "home.quick_rewards", "Recompensas" },
            { "home.load_error", "Não foi possível carregar o resumo." },
            { "home.team_power", "PODER DA EQUIPE" },
            { "home.current_stage", "ESTÁGIO ATUAL" },
            { "home.gold", "OURO" },
            { "home.energy", "ENERGIA" },
            { "home.offline_progress", "PROGRESSO OFFLINE" },
            { "home.crafting_active", "CRAFTING EM ANDAMENTO" },
            { "home.stage_value", "Estágio {0}" },
            { "settings.music", "Música" },
            { "settings.effects", "Efeitos de UI" },
            { "settings.vibration", "Vibração" },
            { "settings.reduce_motion", "Reduzir movimento" },
            { "settings.high_contrast", "Alto contraste" },
            { "settings.text_scale", "Escala do texto" },
            { "settings.battle_speed", "Velocidade da batalha" },
            { "settings.language", "Idioma" },
            { "settings.account", "Conta" },
            { "settings.language_placeholder", "Sistema (placeholder)" },
            { "settings.account_placeholder", "Não conectada" },
            { "settings.saved", "Preferência salva." },
            { "error.invalid_route", "Destino de navegação inválido." },
            { "error.scene_unavailable", "Cena indisponível: {0}." },
            { "error.scene_load", "Não foi possível abrir {0}." },
            { "market.locked", "O Mercado exige conexão e backend autoritativo. O recurso ainda não está disponível." },
            { "gacha.unavailable", "A bancada de invocações existe apenas no Editor. Nenhuma moeda real está disponível." }
        };

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;
            return values.TryGetValue(key, out string value) ? value : $"[{key}]";
        }
    }
}
