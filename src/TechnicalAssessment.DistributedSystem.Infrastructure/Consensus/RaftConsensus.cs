using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalAssessment.DistributedSystem.Core.DTOs;
using TechnicalAssessment.DistributedSystem.Core.Entities;
using TechnicalAssessment.DistributedSystem.Core.Interfaces;

namespace TechnicalAssessment.DistributedSystem.Infrastructure.Consensus
{
    /// <summary>
    /// Implementación del algoritmo de consenso Raft
    /// </summary>
    public class RaftConsensus : IConsensusAlgorithm
    {
        // Node identification
        private readonly int _nodeId;
        private readonly IEnumerable<int> _allNodeIds;
        private readonly INetworkSimulator _networkSimulator;
        private readonly ISystemLogger _logger;

        // Raft state
        private NodeState _currentState = NodeState.Follower;
        private int _currentTerm = 0;
        private int? _votedFor = null;
        private int? _currentLeader = null;
        private int _commitIndex = 0;
        private int _lastApplied = 0;
        private int _currentValue = 0;

        // Leader-specific state
        private readonly Dictionary<int, int> _nextIndex = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _matchIndex = new Dictionary<int, int>();

        // Log
        private readonly List<LogEntry> _log = new List<LogEntry>();

        // Timers
        private readonly Random _random = new Random();
        private Timer _electionTimer;
        private Timer _heartbeatTimer;
        private readonly object _stateLock = new object();

        // Election timeout values (milliseconds)
        private const int MinElectionTimeout = 150;
        private const int MaxElectionTimeout = 300;

        // Heartbeat interval (milliseconds)
        private const int HeartbeatInterval = 50;

        public RaftConsensus(
            int nodeId,
            IEnumerable<int> allNodeIds,
            INetworkSimulator networkSimulator,
            ISystemLogger logger)
        {
            _nodeId = nodeId;
            _allNodeIds = allNodeIds ?? throw new ArgumentNullException(nameof(allNodeIds));
            _networkSimulator = networkSimulator ?? throw new ArgumentNullException(nameof(networkSimulator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize for leader
            foreach (var id in _allNodeIds.Where(id => id != _nodeId))
            {
                _nextIndex[id] = 1;
                _matchIndex[id] = 0;
            }

            // Log entry at index 0 (not used, for simplicity)
            _log.Add(new LogEntry(0, 0));

            // Initialize election timer
            ResetElectionTimer();
        }

        public NodeState CurrentState => _currentState;

        public int CurrentValue => _currentValue;

        public int CurrentTerm => _currentTerm;

        public int? CurrentLeader => _currentLeader;

        public void Start()
        {
            _logger.Log($"Node {_nodeId}: Starting Raft consensus algorithm");
            ResetElectionTimer();
        }

        public void Stop()
        {
            _electionTimer?.Dispose();
            _heartbeatTimer?.Dispose();
            _logger.Log($"Node {_nodeId}: Stopping Raft consensus algorithm");
        }

        public void ProposeValue(int value)
        {
            lock (_stateLock)
            {
                if (_currentState == NodeState.Leader)
                {
                    _logger.Log($"Node {_nodeId}: Leader proposing value {value}");
                    AppendEntryToLog(value);
                }
                else if (_currentLeader.HasValue)
                {
                    _logger.Log($"Node {_nodeId}: Forwarding proposal {value} to leader {_currentLeader}");
                    var proposal = new ProposalDto(_nodeId, value);
                    _networkSimulator.SendMessage(proposal, _nodeId, _currentLeader.Value);
                }
                else
                {
                    _logger.Log($"Node {_nodeId}: No leader known, cannot propose value {value}");
                }
            }
        }

        public bool HandleProposal(ProposalDto proposal, int senderId)
        {
            lock (_stateLock)
            {
                if (_currentState != NodeState.Leader)
                {
                    _logger.Log($"Node {_nodeId}: Received proposal but not leader, ignoring");
                    return false;
                }

                _logger.Log($"Node {_nodeId}: Leader received proposal value {proposal.Value} from node {senderId}");
                AppendEntryToLog(proposal.Value);
                return true;
            }
        }

        public bool HandleRequestVote(RequestVoteDto request, int senderId)
        {
            lock (_stateLock)
            {
                _logger.Log($"Node {_nodeId}: Received vote request from {senderId} for term {request.Term}");

                if (request.Term < _currentTerm)
                {
                    _logger.Log($"Node {_nodeId}: Rejecting vote for {senderId}, term {request.Term} < current term {_currentTerm}");
                    return false;
                }

                if (request.Term > _currentTerm)
                {
                    _logger.Log($"Node {_nodeId}: Higher term detected, stepping down");
                    StepDown(request.Term);
                }

                // Check if candidate's log is at least as up-to-date as ours
                bool logOk = request.LastLogTerm > LastLogTerm() ||
                             (request.LastLogTerm == LastLogTerm() && request.LastLogIndex >= LastLogIndex());

                // Vote if we haven't voted and the candidate's log is sufficient
                if ((_votedFor == null || _votedFor == senderId) && logOk)
                {
                    _votedFor = senderId;
                    _logger.Log($"Node {_nodeId}: Voting for {senderId} in term {_currentTerm}");

                    // Reset election timer when granting vote
                    ResetElectionTimer();

                    return true;
                }

                _logger.Log($"Node {_nodeId}: Rejecting vote for {senderId} in term {_currentTerm}");
                return false;
            }
        }

        public bool HandleVoteResponse(VoteResponseDto response, int senderId)
        {
            lock (_stateLock)
            {
                _logger.Log($"Node {_nodeId}: Received vote response from {senderId} for term {response.Term}: {response.VoteGranted}");

                if (_currentState != NodeState.Candidate)
                {
                    _logger.Log($"Node {_nodeId}: No longer a candidate, ignoring vote from {senderId}");
                    return false;
                }

                if (response.Term > _currentTerm)
                {
                    _logger.Log($"Node {_nodeId}: Higher term in vote response, stepping down");
                    StepDown(response.Term);
                    return false;
                }

                if (response.Term == _currentTerm && response.VoteGranted)
                {
                    _votesReceived.Add(senderId);

                    // Check if we have a majority
                    int majorityCutoff = (_allNodeIds.Count() / 2) + 1;
                    if (_votesReceived.Count >= majorityCutoff)
                    {
                        _logger.Log($"Node {_nodeId}: Won election for term {_currentTerm} with {_votesReceived.Count} votes");
                        BecomeLeader();
                    }

                    return true;
                }

                return false;
            }
        }

        public bool HandleAppendEntries(AppendEntriesDto request, int senderId)
        {
            lock (_stateLock)
            {
                _logger.Log($"Node {_nodeId}: Received AppendEntries from {senderId} for term {request.Term}");

                if (request.Term < _currentTerm)
                {
                    _logger.Log($"Node {_nodeId}: Rejecting AppendEntries, term {request.Term} < current term {_currentTerm}");
                    return false;
                }

                // If the leader's term is at least as large as our term
                if (request.Term >= _currentTerm)
                {
                    // If higher term, update our term
                    if (request.Term > _currentTerm)
                    {
                        StepDown(request.Term);
                    }

                    // Recognize sender as leader
                    _currentLeader = senderId;

                    // Reset election timer due to valid AppendEntries
                    ResetElectionTimer();

                    // Handle log consistency check
                    if (request.PrevLogIndex > 0)
                    {
                        // We need to have the previous log entry
                        if (request.PrevLogIndex > LastLogIndex())
                        {
                            _logger.Log($"Node {_nodeId}: Rejecting AppendEntries, missing previous log entry at {request.PrevLogIndex}");
                            return false;
                        }

                        // The previous log entry must match the leader's term
                        if (_log[request.PrevLogIndex].Term != request.PrevLogTerm)
                        {
                            _logger.Log($"Node {_nodeId}: Rejecting AppendEntries, term mismatch at previous log entry");

                            // Delete the conflicting entry and all that follow
                            while (_log.Count > request.PrevLogIndex)
                            {
                                _log.RemoveAt(_log.Count - 1);
                            }

                            return false;
                        }
                    }

                    // Handle new entries
                    if (request.Entries != null && request.Entries.Any())
                    {
                        // Remove existing entries that conflict with new ones
                        int entryIndex = request.PrevLogIndex + 1;
                        foreach (var entry in request.Entries)
                        {
                            if (entryIndex <= LastLogIndex())
                            {
                                // Check for term mismatch
                                if (_log[entryIndex].Term != entry.Term)
                                {
                                    // Delete this and all that follow
                                    while (_log.Count > entryIndex)
                                    {
                                        _log.RemoveAt(_log.Count - 1);
                                    }
                                }
                            }

                            // If we're still at this point, append the entry if not already present
                            if (entryIndex > LastLogIndex())
                            {
                                _log.Add(entry);
                                _logger.Log($"Node {_nodeId}: Appended entry at index {entryIndex} with term {entry.Term} and value {entry.Value}");
                            }

                            entryIndex++;
                        }
                    }

                    // Update commit index if leader has a higher commit index
                    if (request.LeaderCommit > _commitIndex)
                    {
                        int newCommitIndex = Math.Min(request.LeaderCommit, LastLogIndex());
                        _commitIndex = newCommitIndex;
                        ApplyCommittedEntries();
                    }

                    return true;
                }

                return false;
            }
        }

        public bool HandleAppendEntriesResponse(AppendEntriesResponseDto response, int senderId)
        {
            lock (_stateLock)
            {
                _logger.Log($"Node {_nodeId}: Received AppendEntries response from {senderId} for term {response.Term}: {response.Success}");

                if (_currentState != NodeState.Leader)
                {
                    _logger.Log($"Node {_nodeId}: No longer leader, ignoring AppendEntries response");
                    return false;
                }

                if (response.Term > _currentTerm)
                {
                    _logger.Log($"Node {_nodeId}: Higher term in AppendEntries response, stepping down");
                    StepDown(response.Term);
                    return false;
                }

                if (response.Term == _currentTerm)
                {
                    if (response.Success)
                    {
                        // Update nextIndex and matchIndex for this follower
                        _nextIndex[senderId] = response.LastLogIndex + 1;
                        _matchIndex[senderId] = response.LastLogIndex;

                        // Try to commit entries
                        TryCommitEntries();

                        return true;
                    }
                    else
                    {
                        // If append failed because of log inconsistency
                        if (_nextIndex[senderId] > 1)
                        {
                            // Decrement nextIndex and retry
                            _nextIndex[senderId] = Math.Max(1, _nextIndex[senderId] - 1);
                            _logger.Log($"Node {_nodeId}: Decremented nextIndex for {senderId} to {_nextIndex[senderId]}");

                            // Send updated AppendEntries right away
                            SendAppendEntries(senderId);
                        }
                    }
                }

                return false;
            }
        }

        // Helper methods

        private void StepDown(int newTerm)
        {
            _logger.Log($"Node {_nodeId}: Stepping down to follower, term {_currentTerm} -> {newTerm}");

            _currentTerm = newTerm;
            _currentState = NodeState.Follower;
            _votedFor = null;
            _currentLeader = null;

            // Stop heartbeat timer if it's running
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;

            // Reset election timer
            ResetElectionTimer();

            // Clear candidate state
            _votesReceived.Clear();
        }

        // Election timer expired, start a new election
        private void StartElection()
        {
            lock (_stateLock)
            {
                _currentTerm++;
                _currentState = NodeState.Candidate;
                _votedFor = _nodeId; // Vote for self
                _currentLeader = null;

                _logger.Log($"Node {_nodeId}: Starting election for term {_currentTerm}");

                // Reset votes received for this election
                _votesReceived = new HashSet<int> { _nodeId }; // Include self-vote

                // Send RequestVote to all other nodes
                foreach (var nodeId in _allNodeIds.Where(id => id != _nodeId))
                {
                    var requestVote = new RequestVoteDto(
                        _currentTerm,
                        _nodeId,
                        LastLogIndex(),
                        LastLogTerm()
                    );

                    _networkSimulator.SendMessage(requestVote, _nodeId, nodeId);
                }

                // Reset election timer
                ResetElectionTimer();

                // If we're the only node, become leader immediately
                if (_allNodeIds.Count() == 1)
                {
                    BecomeLeader();
                }
            }
        }

        private HashSet<int> _votesReceived = new HashSet<int>();

        private void BecomeLeader()
        {
            _currentState = NodeState.Leader;
            _currentLeader = _nodeId;
            _logger.Log($"Node {_nodeId}: Became leader for term {_currentTerm}");

            // Initialize leader state
            foreach (var nodeId in _allNodeIds.Where(id => id != _nodeId))
            {
                _nextIndex[nodeId] = LastLogIndex() + 1;
                _matchIndex[nodeId] = 0;
            }

            // Stop election timer
            _electionTimer?.Dispose();

            // Start sending heartbeats
            _heartbeatTimer = new Timer(_ => SendHeartbeats(), null, 0, HeartbeatInterval);
        }

        private void SendHeartbeats()
        {
            try
            {
                lock (_stateLock)
                {
                    if (_currentState != NodeState.Leader)
                    {
                        return;
                    }

                    _logger.Log($"Node {_nodeId}: Sending heartbeats for term {_currentTerm}");

                    foreach (var nodeId in _allNodeIds.Where(id => id != _nodeId))
                    {
                        SendAppendEntries(nodeId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Node {_nodeId}: Error sending heartbeats: {ex.Message}");
            }
        }

        private void SendAppendEntries(int destinationId)
        {
            int prevLogIndex = _nextIndex[destinationId] - 1;
            int prevLogTerm = prevLogIndex > 0 && prevLogIndex < _log.Count
                ? _log[prevLogIndex].Term
                : 0;

            // Get entries to send
            var entries = _log
                .Skip(_nextIndex[destinationId])
                .Take(100) // Limit batch size
                .ToList();

            var appendEntries = new AppendEntriesDto(
                _currentTerm,
                _nodeId,
                prevLogIndex,
                prevLogTerm,
                entries,
                _commitIndex
            );

            _networkSimulator.SendMessage(appendEntries, _nodeId, destinationId);
        }

        private void ResetElectionTimer()
        {
            // Dispose existing timer
            _electionTimer?.Dispose();

            // Random timeout between MinElectionTimeout and MaxElectionTimeout
            int timeout = _random.Next(MinElectionTimeout, MaxElectionTimeout);

            // Create new timer
            _electionTimer = new Timer(_ =>
            {
                try
                {
                    _logger.Log($"Node {_nodeId}: Election timeout, starting election");
                    StartElection();
                }
                catch (Exception ex)
                {
                    _logger.Log($"Node {_nodeId}: Error during election timeout: {ex.Message}");
                }
            }, null, timeout, Timeout.Infinite);
        }

        private int LastLogIndex()
        {
            return _log.Count - 1;
        }

        private int LastLogTerm()
        {
            return _log.Count > 0 ? _log[LastLogIndex()].Term : 0;
        }

        private void AppendEntryToLog(int value)
        {
            // Create a new log entry with the current term and value
            var entry = new LogEntry(_currentTerm, value);
            _log.Add(entry);

            _logger.Log($"Node {_nodeId}: Appended entry at index {LastLogIndex()} with term {_currentTerm} and value {value}");

            // For single-node clusters, commit immediately
            if (_allNodeIds.Count() == 1)
            {
                _commitIndex = LastLogIndex();
                ApplyCommittedEntries();
            }
        }

        private void TryCommitEntries()
        {
            // Only the leader can commit entries
            if (_currentState != NodeState.Leader)
            {
                return;
            }

            // For each index from commitIndex+1 to lastLogIndex
            for (int idx = _commitIndex + 1; idx <= LastLogIndex(); idx++)
            {
                // Skip entries from previous terms (Raft safety property)
                if (_log[idx].Term != _currentTerm)
                {
                    continue;
                }

                // Count nodes that have this log entry
                int replicationCount = 1; // Leader has it

                foreach (var nodeId in _allNodeIds.Where(id => id != _nodeId))
                {
                    if (_matchIndex.TryGetValue(nodeId, out int matchIndex) && matchIndex >= idx)
                    {
                        replicationCount++;
                    }
                }

                // If a majority have it, commit up to this index
                if (replicationCount >= (_allNodeIds.Count() / 2) + 1)
                {
                    _commitIndex = idx;
                    _logger.Log($"Node {_nodeId}: Updated commit index to {_commitIndex}");
                }
                else
                {
                    // If we can't commit this index, we can't commit any higher indices
                    break;
                }
            }

            // Apply any newly committed entries
            ApplyCommittedEntries();
        }

        private void ApplyCommittedEntries()
        {
            // Apply all committed but unapplied entries
            for (int idx = _lastApplied + 1; idx <= _commitIndex; idx++)
            {
                // Apply the log entry
                var entry = _log[idx];
                _currentValue = entry.Value;
                _lastApplied = idx;

                _logger.Log($"Node {_nodeId}: Applied entry at index {idx}: value {entry.Value}");
            }
        }
    }
}
