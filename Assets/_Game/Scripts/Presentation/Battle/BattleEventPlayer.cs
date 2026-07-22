using System;
using System.Collections;
using IdleMedievalLegends.Domain.Combat;
using UnityEngine;

namespace IdleMedievalLegends.Presentation.Battle
{
    public sealed class BattleEventPlayer : MonoBehaviour
    {
        [SerializeField] private BattlePresentationConfig presentationConfig;
        [SerializeField] private BattleTeamView playerTeamView;
        [SerializeField] private BattleTeamView enemyTeamView;
        [SerializeField] private BattleHudView hudView;

        private BattlePresenter presenter;
        private Coroutine playbackCoroutine;
        private BattleUnitView currentAttacker;
        private bool cancelled;

        public bool HasActivePlayback => playbackCoroutine != null;
        public BattlePresentationConfig PresentationConfig => presentationConfig;
        public BattleTeamView PlayerTeamView => playerTeamView;
        public BattleTeamView EnemyTeamView => enemyTeamView;
        public BattleHudView HudView => hudView;
        public bool IsConfigured => presentationConfig != null &&
            playerTeamView != null && enemyTeamView != null && hudView != null;

        public void Configure(
            BattlePresentationConfig config,
            BattleTeamView playerTeam,
            BattleTeamView enemyTeam,
            BattleHudView hud)
        {
            presentationConfig = config != null
                ? config
                : throw new ArgumentNullException(nameof(config));
            playerTeamView = playerTeam != null
                ? playerTeam
                : throw new ArgumentNullException(nameof(playerTeam));
            enemyTeamView = enemyTeam != null
                ? enemyTeam
                : throw new ArgumentNullException(nameof(enemyTeam));
            hudView = hud != null ? hud : throw new ArgumentNullException(nameof(hud));
        }

        public void Begin(BattlePresenter battlePresenter)
        {
            if (battlePresenter == null)
                throw new ArgumentNullException(nameof(battlePresenter));
            if (battlePresenter.Result == null)
                throw new InvalidOperationException("Presenter sem resultado de batalha.");
            ValidateReferences();
            CancelPlayback();

            presenter = battlePresenter;
            presenter.Play();
            cancelled = false;
            playbackCoroutine = StartCoroutine(PlayEvents());
        }

        public void Skip()
        {
            if (presenter == null || !presenter.BeginSkip())
                return;

            CancelPlayback();
            ApplyFinalState();
        }

        public void CancelPlayback()
        {
            cancelled = true;
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }
            // Unity pode destruir a view antes do player durante unload da cena.
            // O operador ?. não respeita o null customizado de UnityEngine.Object.
            if (currentAttacker != null)
                currentAttacker.ResetToHome();
            currentAttacker = null;
        }

        private IEnumerator PlayEvents()
        {
            BattleResult result = presenter.Result;
            for (int i = 0; i < result.Events.Count && !cancelled; i++)
            {
                CombatEvent combatEvent = result.Events[i];
                switch (combatEvent.EventType)
                {
                    case CombatEventType.BattleStarted:
                        hudView.SetStatus("Batalha iniciada");
                        yield return WaitForPresentation(presentationConfig.BattleStartDuration);
                        break;

                    case CombatEventType.UnitSelected:
                        currentAttacker = FindUnit(combatEvent.SourceUnitId);
                        currentAttacker?.SetSelected(true);
                        hudView.SetStatus($"Turno {combatEvent.Turn}");
                        yield return WaitForPresentation(presentationConfig.SelectionDuration);
                        break;

                    case CombatEventType.BasicAttackStarted:
                    {
                        BattleUnitView source = RequireUnit(combatEvent.SourceUnitId);
                        BattleUnitView target = RequireUnit(combatEvent.TargetUnitId);
                        yield return MoveToTarget(source, target);
                        break;
                    }

                    case CombatEventType.AttackMissed:
                        hudView.SetStatus("Errou!");
                        yield return WaitForPresentation(presentationConfig.ImpactDuration);
                        break;

                    case CombatEventType.CriticalHit:
                        hudView.SetStatus("CRÍTICO!");
                        break;

                    case CombatEventType.DamageDealt:
                    {
                        BattleUnitView target = RequireUnit(combatEvent.TargetUnitId);
                        target.SetHealth(combatEvent.TargetHealthAfter);
                        target.FlashDamage(combatEvent.Critical);
                        hudView.SetStatus(
                            combatEvent.Critical
                                ? $"CRÍTICO • {combatEvent.Value}"
                                : $"-{combatEvent.Value}");
                        yield return WaitForPresentation(presentationConfig.ImpactDuration);
                        target.RestoreColor();
                        break;
                    }

                    case CombatEventType.UnitDefeated:
                        RequireUnit(combatEvent.TargetUnitId).SetDefeated();
                        hudView.SetStatus("Unidade derrotada");
                        break;

                    case CombatEventType.TurnEnded:
                        if (currentAttacker != null)
                        {
                            yield return MoveTo(
                                currentAttacker,
                                currentAttacker.HomePosition,
                                presentationConfig.ReturnDuration);
                            currentAttacker.ResetToHome();
                            currentAttacker = null;
                        }
                        break;

                    case CombatEventType.BattleEnded:
                        yield return WaitForPresentation(presentationConfig.BattleEndDuration);
                        ApplyFinalState();
                        yield break;
                }
            }

            if (!cancelled && presenter.State != BattlePresentationState.Completed)
                ApplyFinalState();
        }

        private IEnumerator MoveToTarget(BattleUnitView source, BattleUnitView target)
        {
            Vector3 direction = source.transform.position - target.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.left;
            direction.Normalize();
            Vector3 destination = target.transform.position +
                direction * presentationConfig.AttackStopDistance;
            destination.y = source.transform.position.y;
            yield return MoveTo(source, destination, presentationConfig.ApproachDuration);
        }

        private IEnumerator MoveTo(
            BattleUnitView unit,
            Vector3 destination,
            float duration)
        {
            Vector3 origin = unit.transform.position;
            if (duration <= 0f)
            {
                unit.SetWorldPosition(destination);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && !cancelled)
            {
                if (presenter.State == BattlePresentationState.Paused)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime * presenter.SpeedController.Speed;
                unit.SetWorldPosition(Vector3.Lerp(origin, destination, elapsed / duration));
                yield return null;
            }

            if (!cancelled)
                unit.SetWorldPosition(destination);
        }

        private IEnumerator WaitForPresentation(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && !cancelled)
            {
                if (presenter.State != BattlePresentationState.Paused)
                    elapsed += Time.unscaledDeltaTime * presenter.SpeedController.Speed;
                yield return null;
            }
        }

        private void ApplyFinalState()
        {
            if (presenter == null || presenter.Result == null)
                return;

            playerTeamView.ResetAllToHome();
            enemyTeamView.ResetAllToHome();
            playerTeamView.ApplyFinal(presenter.Result.FinalSnapshots);
            enemyTeamView.ApplyFinal(presenter.Result.FinalSnapshots);
            presenter.Complete();
            hudView.ShowResult(presenter.Result);
            playbackCoroutine = null;
            currentAttacker = null;
        }

        private BattleUnitView FindUnit(string unitId)
        {
            return playerTeamView.FindUnit(unitId) ?? enemyTeamView.FindUnit(unitId);
        }

        private BattleUnitView RequireUnit(string unitId)
        {
            return FindUnit(unitId) ?? throw new InvalidOperationException(
                $"Não existe view para a unidade {unitId}.");
        }

        private void ValidateReferences()
        {
            if (presentationConfig == null || playerTeamView == null ||
                enemyTeamView == null || hudView == null)
            {
                throw new InvalidOperationException(
                    "BattleEventPlayer possui referências de composição ausentes.");
            }

            presentationConfig.EnsureValid();
        }

        private void OnDisable()
        {
            CancelPlayback();
        }

        private void OnDestroy()
        {
            CancelPlayback();
        }
    }
}
