using TechnicalAssessment.DistributedSystem.Core.DTOs;
using TechnicalAssessment.DistributedSystem.Core.Entities;

namespace TechnicalAssessment.DistributedSystem.Core.Interfaces
{
    /// <summary>
    /// Interfaz para algoritmos de consenso distribuido
    /// </summary>
    public interface IConsensusAlgorithm
    {
        NodeState CurrentState { get; }
        int CurrentValue { get; }
        int CurrentTerm { get; }
        int? CurrentLeader { get; }

        void Start();
        void Stop();
        void ProposeValue(int value);

        bool HandleRequestVote(RequestVoteDto request, int senderId);
        bool HandleVoteResponse(VoteResponseDto response, int senderId);
        bool HandleAppendEntries(AppendEntriesDto request, int senderId);
        bool HandleAppendEntriesResponse(AppendEntriesResponseDto response, int senderId);
        bool HandleProposal(ProposalDto proposal, int senderId);
    }
}
