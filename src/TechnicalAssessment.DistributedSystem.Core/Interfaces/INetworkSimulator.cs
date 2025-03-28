using TechnicalAssessment.DistributedSystem.Core.DTOs;

namespace TechnicalAssessment.DistributedSystem.Core.Interfaces
{
    public interface INetworkSimulator
    {
        void RegisterNode(INode node);
        bool SendMessage(MessageDto message, int sourceId, int destinationId);
        void SimulatePartition(int nodeId, IEnumerable<int> partitionedNodes);
        void HealPartition(int nodeId);
        void Shutdown();
    }
}
