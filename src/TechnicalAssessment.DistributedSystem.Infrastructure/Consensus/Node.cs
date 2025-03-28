using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalAssessment.DistributedSystem.Core.DTOs;
using TechnicalAssessment.DistributedSystem.Core.Entities;
using TechnicalAssessment.DistributedSystem.Core.Interfaces;
using TechnicalAssessment.DistributedSystem.Infrastructure.Logging;

namespace TechnicalAssessment.DistributedSystem.Infrastructure.Consensus
{
    /// <summary>
    /// Implementación de un nodo en el sistema distribuido usando el algoritmo Raft
    /// </summary>
    public class Node : INode
    {
        private readonly IConsensusAlgorithm _consensusAlgorithm;
        private readonly INetworkSimulator _networkSimulator;
        private readonly ISystemLogger _logger;
        private readonly List<string> _logMessages = new List<string>();
        private readonly object _logLock = new object();
        private readonly HashSet<int> _neighbors = new HashSet<int>();

        public Node(
            int id,
            IConsensusAlgorithm consensusAlgorithm,
            INetworkSimulator networkSimulator,
            ISystemLogger logger)
        {
            Id = id;
            _consensusAlgorithm = consensusAlgorithm ?? throw new ArgumentNullException(nameof(consensusAlgorithm));
            _networkSimulator = networkSimulator ?? throw new ArgumentNullException(nameof(networkSimulator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Register with network simulator
            _networkSimulator.RegisterNode(this);
        }

        public int Id { get; }

        public NodeState State => _consensusAlgorithm.CurrentState;

        public int CurrentValue => _consensusAlgorithm.CurrentValue;

        public int CurrentTerm => _consensusAlgorithm.CurrentTerm;

        public int? CurrentLeader => _consensusAlgorithm.CurrentLeader;

        public void Start()
        {
            Log($"Node {Id} starting");
            _consensusAlgorithm.Start();
        }

        public void Stop()
        {
            Log($"Node {Id} stopping");
            _consensusAlgorithm.Stop();
        }

        public void AddNeighbor(int nodeId)
        {
            if (nodeId == Id)
                return;

            _neighbors.Add(nodeId);
            Log($"Added neighbor: Node {nodeId}");
        }

        public void RemoveNeighbor(int nodeId)
        {
            if (_neighbors.Remove(nodeId))
            {
                Log($"Removed neighbor: Node {nodeId}");
            }
        }

        public void ProposeState(int value)
        {
            Log($"Proposing state: {value}");
            _consensusAlgorithm.ProposeValue(value);
        }

        public void SimulatePartition(IEnumerable<int> partitionedNodes)
        {
            var partitionedList = partitionedNodes.ToList();
            Log($"Simulating partition from nodes: {string.Join(", ", partitionedList)}");
            _networkSimulator.SimulatePartition(Id, partitionedList);
        }

        public void HealPartition()
        {
            Log("Healing all partitions");
            _networkSimulator.HealPartition(Id);
        }

        public string RetrieveLog()
        {
            lock (_logLock)
            {
                return string.Join(Environment.NewLine, _logMessages);
            }
        }

        public void ReceiveMessage(MessageDto message, int senderId)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            Log($"Received {message.GetType().Name} message from Node {senderId}");

            bool success = false;
            bool shouldRespond = false;
            MessageDto response = null;

            try
            {
                switch (message)
                {
                    case RequestVoteDto requestVote:
                        success = _consensusAlgorithm.HandleRequestVote(requestVote, senderId);
                        shouldRespond = true;
                        response = new VoteResponseDto(_consensusAlgorithm.CurrentTerm, success);
                        break;

                    case VoteResponseDto voteResponse:
                        _consensusAlgorithm.HandleVoteResponse(voteResponse, senderId);
                        break;

                    case AppendEntriesDto appendEntries:
                        success = _consensusAlgorithm.HandleAppendEntries(appendEntries, senderId);
                        shouldRespond = true;
                        response = new AppendEntriesResponseDto(
                            _consensusAlgorithm.CurrentTerm,
                            success,
                            success ? appendEntries.PrevLogIndex + (appendEntries.Entries?.Count ?? 0) : 0
                        );
                        break;

                    case AppendEntriesResponseDto appendEntriesResponse:
                        _consensusAlgorithm.HandleAppendEntriesResponse(appendEntriesResponse, senderId);
                        break;

                    case ProposalDto proposal:
                        _consensusAlgorithm.HandleProposal(proposal, senderId);
                        break;

                    default:
                        Log($"Unknown message type: {message.GetType().Name}");
                        break;
                }

                if (shouldRespond && response != null)
                {
                    _networkSimulator.SendMessage(response, Id, senderId);
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling message: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [Node {Id}] [Term {CurrentTerm}] [State {State}] {message}";

            lock (_logLock)
            {
                _logMessages.Add(formattedMessage);
            }

            _logger.Log(formattedMessage);
        }
    }
}
