using System;
using IdleMedievalLegends.Domain.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private int slot;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private HealthBarView healthBar;
        [SerializeField] private Text nameLabel;

        private Vector3 homePosition;
        private Color baseColor = Color.white;

        public string UnitId { get; private set; } = string.Empty;
        public int Slot => slot;
        public long CurrentHealth { get; private set; }
        public long MaximumHealth { get; private set; }
        public bool IsDefeated { get; private set; }
        public Vector3 HomePosition => homePosition;
        public bool IsConfigured => modelRoot != null && bodyRenderer != null &&
            healthBar != null && healthBar.IsConfigured;

        public void Configure(
            int viewSlot,
            Transform visualRoot,
            Renderer renderer,
            GameObject indicator,
            HealthBarView bar,
            Text label)
        {
            slot = viewSlot;
            modelRoot = visualRoot != null
                ? visualRoot
                : throw new ArgumentNullException(nameof(visualRoot));
            bodyRenderer = renderer != null
                ? renderer
                : throw new ArgumentNullException(nameof(renderer));
            selectionIndicator = indicator;
            healthBar = bar != null ? bar : throw new ArgumentNullException(nameof(bar));
            nameLabel = label;
        }

        public void Bind(BattleUnit unit, string displayName)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (unit.Slot != slot)
                throw new InvalidOperationException("Unidade atribuída ao slot visual incorreto.");

            UnitId = unit.UnitId;
            MaximumHealth = unit.MaximumHealth;
            CurrentHealth = unit.CurrentHealth;
            IsDefeated = false;
            homePosition = transform.position;
            if (bodyRenderer != null)
            {
                bodyRenderer.gameObject.SetActive(true);
                baseColor = bodyRenderer.sharedMaterial != null
                    ? bodyRenderer.sharedMaterial.color
                    : Color.white;
                bodyRenderer.material.color = baseColor;
            }
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(true);
                healthBar.SetHealth(CurrentHealth, MaximumHealth);
            }
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(true);
                nameLabel.text = displayName ?? unit.UnitId;
            }

            SetSelected(false);
            gameObject.SetActive(true);
        }

        public void SetHealth(long currentHealth)
        {
            CurrentHealth = Math.Max(0, Math.Min(currentHealth, MaximumHealth));
            healthBar?.SetHealth(CurrentHealth, MaximumHealth);
            if (CurrentHealth == 0)
                SetDefeated();
        }

        public void SetSelected(bool selected)
        {
            if (selectionIndicator != null)
                selectionIndicator.SetActive(selected && !IsDefeated);
            if (bodyRenderer != null && !IsDefeated)
                bodyRenderer.material.color = selected ? baseColor * 1.45f : baseColor;
        }

        public void FlashDamage(bool critical)
        {
            if (bodyRenderer == null || IsDefeated)
                return;
            bodyRenderer.material.color = critical
                ? new Color(1f, 0.2f, 0.05f)
                : new Color(1f, 0.45f, 0.35f);
        }

        public void RestoreColor()
        {
            if (bodyRenderer != null && !IsDefeated)
                bodyRenderer.material.color = baseColor;
        }

        public void SetDefeated()
        {
            IsDefeated = true;
            CurrentHealth = 0;
            healthBar?.SetHealth(0, MaximumHealth);
            SetSelected(false);
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = Color.gray;
                bodyRenderer.transform.localScale = new Vector3(1f, 0.2f, 1f);
            }
        }

        public void ResetToHome()
        {
            transform.position = homePosition;
            SetSelected(false);
            RestoreColor();
        }

        public void SetWorldPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
