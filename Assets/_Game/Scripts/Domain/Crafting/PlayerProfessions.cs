using System;
using System.Collections.Generic;
using IdleMedievalLegends.Domain.Common;

namespace IdleMedievalLegends.Domain.Crafting
{
    /// <summary>
    /// Read model do cliente. Progressão, XP, Foco, receitas e jobs só são
    /// alterados por snapshots/resultados autoritativos do backend.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfessions
    {
        private ProfessionSnapshotData snapshot = ProfessionSnapshotData.CreateEmpty();

        [NonSerialized]
        private Dictionary<CraftingProfession, ProfessionProgressData> progressIndex =
            new Dictionary<CraftingProfession, ProfessionProgressData>();

        [NonSerialized]
        private HashSet<string> recipeIndex = new HashSet<string>(StringComparer.Ordinal);

        public event Action Changed;

        public string PlayerId => snapshot.PlayerId;
        public long ServerRevision => snapshot.ServerRevision;
        public CraftingProfession PrimaryProfession => snapshot.PrimaryProfession;
        public int FocusAvailable => snapshot.FocusAvailable;
        public int FocusCap => snapshot.FocusCap;
        public IReadOnlyList<ProfessionProgressData> Progress => snapshot.Professions;
        public IReadOnlyList<CraftingJobData> ActiveJobs => snapshot.ActiveJobs;

        public void ApplyServerSnapshot(ProfessionSnapshotData newSnapshot)
        {
            if (newSnapshot == null) throw new ArgumentNullException(nameof(newSnapshot));
            EnsureRuntimeState();

            if (newSnapshot.SchemaVersion != ProfessionSnapshotData.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Schema de profissões não suportado: {newSnapshot.SchemaVersion}.");
            }

            if (newSnapshot.PrimaryProfession != CraftingProfession.None &&
                !newSnapshot.PrimaryProfession.IsCraftingProfession())
            {
                throw new InvalidOperationException("Profissão primária inválida.");
            }
            if (newSnapshot.FocusAvailable < 0 ||
                newSnapshot.FocusCap <= 0 ||
                newSnapshot.FocusAvailable > newSnapshot.FocusCap)
            {
                throw new InvalidOperationException("Estado de Foco artesanal inválido.");
            }

            var rebuiltProgress = new Dictionary<CraftingProfession, ProfessionProgressData>();
            for (int i = 0; i < newSnapshot.Professions.Count; i++)
            {
                ProfessionProgressData progress = newSnapshot.Professions[i];
                if (progress == null)
                    throw new InvalidOperationException($"Profissão nula no índice {i}.");

                progress.Validate();
                if (rebuiltProgress.ContainsKey(progress.Profession))
                    throw new InvalidOperationException($"Profissão duplicada: {progress.Profession}.");

                rebuiltProgress.Add(progress.Profession, progress);
            }

            if (rebuiltProgress.Count != 5)
            {
                throw new InvalidOperationException(
                    "O snapshot deve conter as cinco profissões exatamente uma vez.");
            }
            if (newSnapshot.PrimaryProfession != CraftingProfession.None &&
                !rebuiltProgress.ContainsKey(newSnapshot.PrimaryProfession))
            {
                throw new InvalidOperationException(
                    "A profissão primária não existe no progresso recebido.");
            }

            var rebuiltRecipes = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < newSnapshot.RecipeUnlocks.Count; i++)
            {
                RecipeUnlockData unlock = newSnapshot.RecipeUnlocks[i];
                if (unlock == null || string.IsNullOrWhiteSpace(unlock.RecipeId))
                    throw new InvalidOperationException($"Recipe unlock inválido no índice {i}.");
                if (!rebuiltRecipes.Add(unlock.RecipeId))
                    throw new InvalidOperationException($"Receita duplicada: {unlock.RecipeId}.");
            }

            var jobIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < newSnapshot.ActiveJobs.Count; i++)
            {
                CraftingJobData job = newSnapshot.ActiveJobs[i];
                if (job == null)
                    throw new InvalidOperationException($"Job nulo no índice {i}.");
                job.Validate();
                if (!jobIds.Add(job.JobId))
                    throw new InvalidOperationException($"Job duplicado: {job.JobId}.");
            }

            snapshot = newSnapshot;
            progressIndex = rebuiltProgress;
            recipeIndex = rebuiltRecipes;
            Changed?.Invoke();
        }

        public bool TryGetProgress(
            CraftingProfession profession,
            out ProfessionProgressData progress)
        {
            EnsureRuntimeState();
            return progressIndex.TryGetValue(profession, out progress);
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            EnsureRuntimeState();
            return !string.IsNullOrWhiteSpace(recipeId) && recipeIndex.Contains(recipeId);
        }

        public ProfessionSnapshotData CaptureSnapshotForCache()
        {
            return snapshot;
        }

        public void Clear(string playerId = "")
        {
            ApplyServerSnapshot(ProfessionSnapshotData.CreateEmpty(playerId));
        }

        private void EnsureRuntimeState()
        {
            if (snapshot == null)
                snapshot = ProfessionSnapshotData.CreateEmpty();

            if (progressIndex != null && recipeIndex != null)
                return;

            progressIndex = new Dictionary<CraftingProfession, ProfessionProgressData>();
            recipeIndex = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < snapshot.Professions.Count; i++)
            {
                ProfessionProgressData progress = snapshot.Professions[i];
                if (progress != null)
                    progressIndex[progress.Profession] = progress;
            }

            for (int i = 0; i < snapshot.RecipeUnlocks.Count; i++)
            {
                RecipeUnlockData unlock = snapshot.RecipeUnlocks[i];
                if (unlock != null && !string.IsNullOrWhiteSpace(unlock.RecipeId))
                    recipeIndex.Add(unlock.RecipeId);
            }
        }
    }
}
