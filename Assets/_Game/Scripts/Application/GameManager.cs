using System;
using System.Threading;
using System.Threading.Tasks;
using IdleMedievalLegends.Config;
using IdleMedievalLegends.Domain.Content;
using IdleMedievalLegends.Domain.Crafting;
using IdleMedievalLegends.Domain.Inventory;
using IdleMedievalLegends.Infrastructure.Save;
using UnityEngine;

namespace IdleMedievalLegends.Application
{
    public enum GameLifecycleState
    {
        None = 0,
        Bootstrapping = 1,
        Ready = 2,
        SavingCache = 3,
        Faulted = 4,
        ShuttingDown = 5
    }

    /// <summary>
    /// Orquestrador de alto nível. Regras de combate, profissões, inventário,
    /// mercado e persistência ficam em serviços próprios.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Infrastructure")]
        [SerializeField]
        private PlayerStateRepositoryBehaviour cachedStateRepository;

        [Header("Balance")]
        [SerializeField]
        private CombatBalanceConfigAsset combatBalanceConfig;

        [SerializeField]
        private CraftingBalanceConfigAsset craftingBalanceConfig;

        [Header("Content")]
        [SerializeField]
        private ContentCatalogAsset contentCatalogAsset;

        private readonly PlayerInventory inventory = new PlayerInventory();
        private readonly PlayerProfessions professions = new PlayerProfessions();
        private readonly SemaphoreSlim saveGate = new SemaphoreSlim(1, 1);

        private CancellationTokenSource lifetimeCancellation;
        private bool isPrimaryInstance;
        private bool hasAuthoritativeInventorySnapshot;
        private bool hasAuthoritativeProfessionSnapshot;
        private string currentPlayerId = string.Empty;

        public event Action<GameLifecycleState> StateChanged;
        public event Action<Exception> InitializationFailed;

        public PlayerInventory Inventory => inventory;
        public PlayerProfessions Professions => professions;
        public GameLifecycleState State { get; private set; } = GameLifecycleState.None;
        public string CurrentPlayerId => currentPlayerId;
        public bool IsReady => State == GameLifecycleState.Ready;
        public PlayerStateRepositoryBehaviour CachedStateRepository => cachedStateRepository;
        public CombatBalanceConfigAsset CombatBalanceConfig => combatBalanceConfig;
        public CraftingBalanceConfigAsset CraftingBalanceConfig => craftingBalanceConfig;
        public ContentCatalogAsset ContentCatalogAsset => contentCatalogAsset;
        public ContentCatalogLookup ContentCatalog { get; private set; }

        public void ConfigureBootstrapDependencies(
            PlayerStateRepositoryBehaviour repository,
            CombatBalanceConfigAsset combatConfig,
            CraftingBalanceConfigAsset craftingConfig,
            ContentCatalogAsset catalogAsset)
        {
            GameBootstrapDependencies.Validate(
                repository,
                combatConfig,
                craftingConfig,
                catalogAsset);
            cachedStateRepository = repository;
            combatBalanceConfig = combatConfig;
            craftingBalanceConfig = craftingConfig;
            contentCatalogAsset = catalogAsset;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            isPrimaryInstance = true;
            lifetimeCancellation = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            if (!isPrimaryInstance)
                return;

            try
            {
                await InitializeAsync(lifetimeCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                SetState(GameLifecycleState.Faulted);
                Debug.LogException(exception, this);
                InitializationFailed?.Invoke(exception);
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            GameBootstrapDependencies.Validate(
                cachedStateRepository,
                combatBalanceConfig,
                craftingBalanceConfig,
                contentCatalogAsset);

            ContentCatalog = contentCatalogAsset.BuildValidatedLookup();

            SetState(GameLifecycleState.Bootstrapping);
            GameSaveData cachedState =
                await cachedStateRepository.LoadAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            cachedState = GameSaveMigration.UpgradeToCurrent(cachedState);
            currentPlayerId = cachedState.PlayerId ?? string.Empty;

            try
            {
                inventory.ApplyServerSnapshot(
                    cachedState.Inventory ?? InventorySnapshotData.CreateEmpty(currentPlayerId));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Cache local de inventário inválido e descartado: {exception.Message}",
                    this);
                inventory.Clear(currentPlayerId);
            }

            try
            {
                professions.ApplyServerSnapshot(
                    cachedState.Professions ?? ProfessionSnapshotData.CreateEmpty(currentPlayerId));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Cache local de profissões inválido e descartado: {exception.Message}",
                    this);
                professions.Clear(currentPlayerId);
            }

            hasAuthoritativeInventorySnapshot = false;
            hasAuthoritativeProfessionSnapshot = false;
            SetState(GameLifecycleState.Ready);
        }

        public void ApplyAuthoritativeInventorySnapshot(
            string playerId,
            InventorySnapshotData authoritativeSnapshot)
        {
            ValidateSnapshotOwner(playerId, authoritativeSnapshot?.PlayerId, "inventário");
            ValidatePartialSnapshotPlayer(
                playerId,
                professions.PlayerId,
                "inventário");

            if (hasAuthoritativeInventorySnapshot &&
                authoritativeSnapshot.ServerRevision < inventory.ServerRevision)
            {
                throw new InvalidOperationException(
                    "Snapshot autoritativo de inventário mais antigo que o estado aplicado.");
            }

            currentPlayerId = playerId;
            inventory.ApplyServerSnapshot(authoritativeSnapshot);
            hasAuthoritativeInventorySnapshot = true;
            _ = PersistLocalCacheSafelyAsync();
        }

        public void ApplyAuthoritativeProfessionSnapshot(
            string playerId,
            ProfessionSnapshotData authoritativeSnapshot)
        {
            ValidateSnapshotOwner(playerId, authoritativeSnapshot?.PlayerId, "profissões");
            ValidatePartialSnapshotPlayer(
                playerId,
                inventory.PlayerId,
                "profissões");

            if (hasAuthoritativeProfessionSnapshot &&
                authoritativeSnapshot.ServerRevision < professions.ServerRevision)
            {
                throw new InvalidOperationException(
                    "Snapshot autoritativo de profissões mais antigo que o estado aplicado.");
            }

            currentPlayerId = playerId;
            professions.ApplyServerSnapshot(authoritativeSnapshot);
            hasAuthoritativeProfessionSnapshot = true;
            _ = PersistLocalCacheSafelyAsync();
        }

        /// <summary>
        /// Preferível no bootstrap: aplica inventário e profissões provenientes da
        /// mesma revisão lógica do backend antes de atualizar o cache.
        /// </summary>
        public void ApplyAuthoritativePlayerState(
            string playerId,
            InventorySnapshotData inventorySnapshot,
            ProfessionSnapshotData professionSnapshot)
        {
            ValidateSnapshotOwner(playerId, inventorySnapshot?.PlayerId, "inventário");
            ValidateSnapshotOwner(playerId, professionSnapshot?.PlayerId, "profissões");

            bool replacesAnotherPlayer =
                !string.IsNullOrWhiteSpace(currentPlayerId) &&
                !string.Equals(currentPlayerId, playerId, StringComparison.Ordinal);

            if (!replacesAnotherPlayer &&
                hasAuthoritativeInventorySnapshot &&
                inventorySnapshot.ServerRevision < inventory.ServerRevision)
            {
                throw new InvalidOperationException(
                    "Snapshot conjunto contém inventário mais antigo que o estado aplicado.");
            }
            if (!replacesAnotherPlayer &&
                hasAuthoritativeProfessionSnapshot &&
                professionSnapshot.ServerRevision < professions.ServerRevision)
            {
                throw new InvalidOperationException(
                    "Snapshot conjunto contém profissões mais antigas que o estado aplicado.");
            }

            // Valida ambos antes de publicar qualquer alteração no estado vivo.
            var inventoryValidator = new PlayerInventory();
            var professionValidator = new PlayerProfessions();
            inventoryValidator.ApplyServerSnapshot(inventorySnapshot);
            professionValidator.ApplyServerSnapshot(professionSnapshot);

            currentPlayerId = playerId;
            inventory.ApplyServerSnapshot(inventorySnapshot);
            professions.ApplyServerSnapshot(professionSnapshot);
            hasAuthoritativeInventorySnapshot = true;
            hasAuthoritativeProfessionSnapshot = true;
            _ = PersistLocalCacheSafelyAsync();
        }

        public async Task PersistLocalCacheAsync(CancellationToken cancellationToken)
        {
            if (cachedStateRepository == null)
                return;

            await saveGate.WaitAsync(cancellationToken);
            GameLifecycleState previousState = State;

            try
            {
                if (previousState != GameLifecycleState.Faulted &&
                    previousState != GameLifecycleState.ShuttingDown)
                {
                    SetState(GameLifecycleState.SavingCache);
                }

                var saveData = new GameSaveData(
                    currentPlayerId,
                    inventory.CaptureSnapshotForCache(),
                    professions.CaptureSnapshotForCache());

                await cachedStateRepository.SaveAsync(saveData, cancellationToken);
            }
            finally
            {
                saveGate.Release();

                if (State == GameLifecycleState.SavingCache)
                    SetState(GameLifecycleState.Ready);
            }
        }

        private async Task PersistLocalCacheSafelyAsync()
        {
            try
            {
                if (lifetimeCancellation == null || lifetimeCancellation.IsCancellationRequested)
                    return;

                await PersistLocalCacheAsync(lifetimeCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Falha ao salvar cache local: {exception.Message}", this);
            }
        }

        private static void ValidateSnapshotOwner(
            string playerId,
            string snapshotPlayerId,
            string snapshotName)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("playerId é obrigatório.", nameof(playerId));
            if (snapshotPlayerId == null)
                throw new ArgumentNullException(snapshotName + "Snapshot");
            if (string.IsNullOrWhiteSpace(snapshotPlayerId))
            {
                throw new InvalidOperationException(
                    $"Snapshot de {snapshotName} deve declarar playerId.");
            }
            if (!string.Equals(playerId, snapshotPlayerId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Snapshot de {snapshotName} pertence a outro jogador.");
            }
        }

        private void ValidatePartialSnapshotPlayer(
            string playerId,
            string retainedSnapshotPlayerId,
            string snapshotName)
        {
            bool changesCurrentPlayer =
                !string.IsNullOrWhiteSpace(currentPlayerId) &&
                !string.Equals(currentPlayerId, playerId, StringComparison.Ordinal);
            bool conflictsWithRetainedState =
                !string.IsNullOrWhiteSpace(retainedSnapshotPlayerId) &&
                !string.Equals(retainedSnapshotPlayerId, playerId, StringComparison.Ordinal);

            if (changesCurrentPlayer || conflictsWithRetainedState)
            {
                throw new InvalidOperationException(
                    $"Snapshot parcial de {snapshotName} não pode trocar o jogador atual. " +
                    "Use ApplyAuthoritativePlayerState para substituir todo o estado.");
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && isPrimaryInstance && State == GameLifecycleState.Ready)
                _ = PersistLocalCacheSafelyAsync();
        }

        private void OnDestroy()
        {
            if (!isPrimaryInstance)
                return;

            SetState(GameLifecycleState.ShuttingDown);
            lifetimeCancellation?.Cancel();
            lifetimeCancellation?.Dispose();

            if (Instance == this)
                Instance = null;
        }

        private void SetState(GameLifecycleState newState)
        {
            if (State == newState)
                return;

            State = newState;
            StateChanged?.Invoke(newState);
        }
    }
}
