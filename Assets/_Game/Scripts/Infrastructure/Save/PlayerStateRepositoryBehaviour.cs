using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace IdleMedievalLegends.Infrastructure.Save
{
    /// <summary>
    /// Porta de persistência. Troque a implementação local por um adaptador
    /// PlayFab/Firebase sem alterar o GameManager.
    /// </summary>
    public abstract class PlayerStateRepositoryBehaviour : MonoBehaviour
    {
        public abstract Task<GameSaveData> LoadAsync(CancellationToken cancellationToken);

        public abstract Task SaveAsync(
            GameSaveData saveData,
            CancellationToken cancellationToken);
    }
}
