using TechnicalAssessment.DistributedSystem.Core.DTOs;
using TechnicalAssessment.DistributedSystem.Core.Entities;

namespace TechnicalAssessment.DistributedSystem.Core.Interfaces
{
    /// <summary>
    /// Representa un nodo en el sistema distribuido
    /// </summary>
    public interface INode
    {
        int Id { get; }
        NodeState State { get; }
        int CurrentValue { get; }
        int CurrentTerm { get; }
        int? CurrentLeader { get; }

        void Start();
        void Stop();
        void AddNeighbor(int nodeId);
        void RemoveNeighbor(int nodeId);
        void ProposeState(int value);
        void SimulatePartition(IEnumerable<int> partitionedNodes);
        void HealPartition();
        string RetrieveLog();
        void ReceiveMessage(MessageDto message, int senderId);
    }
}
