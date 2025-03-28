using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TechnicalAssessment.DistributedSystem.Core.DTOs;
using TechnicalAssessment.DistributedSystem.Core.Interfaces;

namespace TechnicalAssessment.DistributedSystem.Infrastructure.Simulation
{
    public class NetworkSimulator : INetworkSimulator
    {
        private readonly ISystemLogger _logger;
        private readonly Random _random = new Random();
        private readonly ConcurrentDictionary<int, INode> _nodes = new ConcurrentDictionary<int, INode>();
        private readonly ConcurrentDictionary<int, HashSet<int>> _partitions = new ConcurrentDictionary<int, HashSet<int>>();
        private readonly ConcurrentDictionary<(int, int), int> _latencies = new ConcurrentDictionary<(int, int), int>();
        private readonly ConcurrentQueue<(MessageDto, int, int, DateTime)> _messageQueue = new ConcurrentQueue<(MessageDto, int, int, DateTime)>();

        private readonly int _minLatencyMs;
        private readonly int _maxLatencyMs;
        private readonly double _messageLossRate;

        private CancellationTokenSource _cancellationTokenSource;
        private Task _messageProcessingTask;

        public NetworkSimulator(ISystemLogger logger, int minLatencyMs = 5, int maxLatencyMs = 50, double messageLossRate = 0.05)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _minLatencyMs = minLatencyMs;
            _maxLatencyMs = maxLatencyMs;
            _messageLossRate = messageLossRate;

            // Start message processing task
            _cancellationTokenSource = new CancellationTokenSource();
            _messageProcessingTask = Task.Run(() => ProcessMessagesAsync(_cancellationTokenSource.Token));
        }

        public void RegisterNode(INode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            _nodes.TryAdd(node.Id, node);
            _partitions.TryAdd(node.Id, new HashSet<int>());
            _logger.Log($"Node {node.Id} registered in the network");
        }

        public bool SendMessage(MessageDto message, int sourceId, int destinationId)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (!_nodes.ContainsKey(sourceId) || !_nodes.ContainsKey(destinationId))
            {
                _logger.Log($"Cannot send message from {sourceId} to {destinationId}: One or both nodes not registered");
                return false;
            }

            // Check for network partitions
            if (IsPartitioned(sourceId, destinationId))
            {
                _logger.Log($"Cannot send message from {sourceId} to {destinationId}: Network partition exists");
                return false;
            }

            // Simulate message loss
            if (_random.NextDouble() < _messageLossRate)
            {
                _logger.Log($"Message lost in transit from {sourceId} to {destinationId}: {message.GetType().Name}");
                return false;
            }

            // Calculate latency for this message
            int latency = GetOrCreateLatency(sourceId, destinationId);
            DateTime deliveryTime = DateTime.UtcNow.AddMilliseconds(latency);

            // Enqueue message with delivery time
            _messageQueue.Enqueue((message, sourceId, destinationId, deliveryTime));
            _logger.Log($"Message queued from {sourceId} to {destinationId}: {message.GetType().Name} with {latency}ms latency");

            return true;
        }

        public void SimulatePartition(int nodeId, IEnumerable<int> partitionedNodes)
        {
            if (!_nodes.ContainsKey(nodeId))
            {
                _logger.Log($"Cannot create partition: Node {nodeId} not registered");
                return;
            }

            var partitionSet = new HashSet<int>(partitionedNodes.Where(id => _nodes.ContainsKey(id)));
            _partitions[nodeId] = partitionSet;

            foreach (var partitionedId in partitionSet)
            {
                if (_partitions.TryGetValue(partitionedId, out var reversePartition))
                {
                    reversePartition.Add(nodeId);
                }
            }

            _logger.Log($"Node {nodeId} partitioned from nodes: {string.Join(", ", partitionSet)}");
        }

        public void HealPartition(int nodeId)
        {
            if (!_nodes.ContainsKey(nodeId))
            {
                _logger.Log($"Cannot heal partition: Node {nodeId} not registered");
                return;
            }

            if (_partitions.TryGetValue(nodeId, out var partitionSet))
            {
                // Clear all partitions for this node
                foreach (var partitionedId in partitionSet)
                {
                    if (_partitions.TryGetValue(partitionedId, out var reversePartition))
                    {
                        reversePartition.Remove(nodeId);
                    }
                }

                partitionSet.Clear();
                _logger.Log($"Partitions healed for node {nodeId}");
            }
        }

        public void Shutdown()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _messageProcessingTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException)
            {
                // Task was canceled, expected
            }
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _messageProcessingTask = null;
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool processed = false;

                if (_messageQueue.TryPeek(out var messageInfo))
                {
                    var (message, sourceId, destinationId, deliveryTime) = messageInfo;

                    if (DateTime.UtcNow >= deliveryTime)
                    {
                        // Time to deliver this message
                        if (_messageQueue.TryDequeue(out _)) // Remove from queue
                        {
                            processed = true;

                            if (_nodes.TryGetValue(destinationId, out var destinationNode))
                            {
                                try
                                {
                                    // Check again for partitions at delivery time
                                    if (!IsPartitioned(sourceId, destinationId))
                                    {
                                        await Task.Run(() => destinationNode.ReceiveMessage(message, sourceId));
                                        _logger.Log($"Message delivered from {sourceId} to {destinationId}: {message.GetType().Name}");
                                    }
                                    else
                                    {
                                        _logger.Log($"Message dropped due to partition from {sourceId} to {destinationId}: {message.GetType().Name}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Log($"Error delivering message from {sourceId} to {destinationId}: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // If no message was processed, wait a little before checking again
                if (!processed)
                {
                    await Task.Delay(5, cancellationToken);
                }
            }
        }

        private bool IsPartitioned(int sourceId, int destinationId)
        {
            return _partitions.TryGetValue(sourceId, out var partitionSet) &&
                   partitionSet.Contains(destinationId);
        }

        private int GetOrCreateLatency(int sourceId, int destinationId)
        {
            var key = (sourceId, destinationId);

            if (!_latencies.TryGetValue(key, out int latency))
            {
                // Create a stable latency for this pair of nodes
                latency = _random.Next(_minLatencyMs, _maxLatencyMs + 1);
                _latencies[key] = latency;
            }

            // Add some jitter (±20% of base latency)
            int jitter = (int)(latency * 0.2);
            return Math.Max(1, latency - jitter + _random.Next(jitter * 2 + 1));
        }
    }
}
