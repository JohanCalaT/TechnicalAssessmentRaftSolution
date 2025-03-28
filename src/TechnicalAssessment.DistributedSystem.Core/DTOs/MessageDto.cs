using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalAssessment.DistributedSystem.Core.Entities;

namespace TechnicalAssessment.DistributedSystem.Core.DTOs
{
    // Base message class
    public abstract record MessageDto;

    // Request vote message (candidate → followers)
    public record RequestVoteDto(
        int Term,
        int CandidateId,
        int LastLogIndex,
        int LastLogTerm
    ) : MessageDto;

    // Vote response message (follower → candidate)
    public record VoteResponseDto(
        int Term,
        bool VoteGranted
    ) : MessageDto;

    // Append entries message (leader → followers)
    public record AppendEntriesDto(
        int Term,
        int LeaderId,
        int PrevLogIndex,
        int PrevLogTerm,
        List<LogEntry> Entries,
        int LeaderCommit
    ) : MessageDto;

    // Append entries response message (follower → leader)
    public record AppendEntriesResponseDto(
        int Term,
        bool Success,
        int LastLogIndex
    ) : MessageDto;

    // Proposal message (client/node → leader)
    public record ProposalDto(
        int ClientId,
        int Value
    ) : MessageDto;
}
