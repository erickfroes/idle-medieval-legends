using System;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Text valueLabel;

        public float NormalizedValue { get; private set; }
        public bool IsConfigured => fillImage != null;

        public void Configure(Image fill, Text label)
        {
            fillImage = fill != null ? fill : throw new ArgumentNullException(nameof(fill));
            valueLabel = label;
        }

        public void SetHealth(long currentHealth, long maximumHealth)
        {
            long displayedMaximum = Math.Max(0, maximumHealth);
            long displayedCurrent = Math.Max(0, Math.Min(currentHealth, displayedMaximum));
            NormalizedValue = BattlePresentationMath.NormalizeHealth(
                displayedCurrent,
                displayedMaximum);

            if (fillImage != null)
                fillImage.fillAmount = NormalizedValue;
            if (valueLabel != null)
                valueLabel.text = $"{displayedCurrent}/{displayedMaximum}";
        }
    }
}
