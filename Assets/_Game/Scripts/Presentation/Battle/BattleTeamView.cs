using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Combat;
using IdleMedievalLegends.Domain.Content;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleTeamView : MonoBehaviour
    {
        [SerializeField] private BattleSide side;
        [SerializeField] private List<BattleUnitView> unitViews =
            new List<BattleUnitView>();

        public BattleSide Side => side;
        public IReadOnlyList<BattleUnitView> UnitViews => unitViews;
        public bool IsConfigured
        {
            get
            {
                if (!Enum.IsDefined(typeof(BattleSide), side) ||
                    unitViews == null || unitViews.Count != 3)
                {
                    return false;
                }

                var slots = new HashSet<int>();
                for (int i = 0; i < unitViews.Count; i++)
                {
                    if (unitViews[i] == null || !unitViews[i].IsConfigured ||
                        !slots.Add(unitViews[i].Slot))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public void Configure(BattleSide teamSide, IEnumerable<BattleUnitView> views)
        {
            side = teamSide;
            unitViews = views == null
                ? throw new ArgumentNullException(nameof(views))
                : new List<BattleUnitView>(views);
        }

        public void Bind(BattleTeam team, ContentCatalogLookup catalog)
        {
            if (team == null) throw new ArgumentNullException(nameof(team));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (team.Side != side)
                throw new InvalidOperationException("Equipe atribuída ao lado visual incorreto.");

            for (int i = 0; i < unitViews.Count; i++)
                unitViews[i].gameObject.SetActive(false);

            for (int i = 0; i < team.Units.Count; i++)
            {
                BattleUnit unit = team.Units[i];
                BattleUnitView view = FindSlot(unit.Slot);
                HeroDefinition definition = catalog.GetHero(unit.DefinitionId);
                string displayName = side == BattleSide.Defender
                    ? $"{definition.DisplayName} [Debug]"
                    : definition.DisplayName;
                view.Bind(unit, displayName);
            }
        }

        public BattleUnitView FindUnit(string unitId)
        {
            for (int i = 0; i < unitViews.Count; i++)
            {
                if (unitViews[i].gameObject.activeSelf &&
                    string.Equals(unitViews[i].UnitId, unitId, StringComparison.Ordinal))
                {
                    return unitViews[i];
                }
            }

            return null;
        }

        public void ApplyFinal(IReadOnlyList<CombatSnapshot> snapshots)
        {
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            for (int i = 0; i < snapshots.Count; i++)
            {
                CombatSnapshot snapshot = snapshots[i];
                if (snapshot.Team != side)
                    continue;

                BattleUnitView view = FindUnit(snapshot.UnitId);
                if (view == null)
                    throw new InvalidOperationException(
                        $"Snapshot final sem view: {snapshot.UnitId}.");
                view.ResetToHome();
                view.SetHealth(snapshot.CurrentHealth);
            }
        }

        public void ResetAllToHome()
        {
            for (int i = 0; i < unitViews.Count; i++)
            {
                if (unitViews[i].gameObject.activeSelf)
                    unitViews[i].ResetToHome();
            }
        }

        private BattleUnitView FindSlot(int requestedSlot)
        {
            for (int i = 0; i < unitViews.Count; i++)
            {
                if (unitViews[i] != null && unitViews[i].Slot == requestedSlot)
                    return unitViews[i];
            }

            throw new InvalidOperationException($"Não existe view para o slot {requestedSlot}.");
        }
    }
}
