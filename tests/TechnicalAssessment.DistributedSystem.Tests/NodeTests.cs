using TechnicalAssessment.DistributedSystem.Core.Entities;
using TechnicalAssessment.DistributedSystem.Core.Interfaces;
using TechnicalAssessment.DistributedSystem.Infrastructure.Consensus;
using TechnicalAssessment.DistributedSystem.Infrastructure.Logging;
using TechnicalAssessment.DistributedSystem.Infrastructure.Simulation;

namespace TechnicalAssessment.DistributedSystem.Tests
{
    public class NodeTests
    {
        [Fact]
        public async Task SingleNode_ShouldBecomeLeader()
        {
            // Arrange
            var logger = new ConsoleLogger(verbose: false);
            var networkSimulator = new NetworkSimulator(logger);

            int nodeId = 1;
            int[] nodeIds = { nodeId };

            var consensusAlgorithm = new RaftConsensus(nodeId, nodeIds, networkSimulator, logger);
            var node = new Node(nodeId, consensusAlgorithm, networkSimulator, logger);

            // Act
            node.Start();

            // Wait for leader election
            await Task.Delay(500);

            // Assert
            Assert.Equal(NodeState.Leader, node.State);
            Assert.Equal(nodeId, node.CurrentLeader);

            // Cleanup
            node.Stop();
            networkSimulator.Shutdown();
        }

        [Fact]
        public async Task ThreeNodes_ShouldElectLeaderAndReplicateState()
        {
            // Arrange
            var logger = new ConsoleLogger(verbose: false);
            var networkSimulator = new NetworkSimulator(logger, minLatencyMs: 5, maxLatencyMs: 15);

            int[] nodeIds = { 1, 2, 3 };
            var nodes = new List<INode>();

            // Create nodes
            foreach (var nodeId in nodeIds)
            {
                var consensusAlgorithm = new RaftConsensus(nodeId, nodeIds, networkSimulator, logger);
                var node = new Node(nodeId, consensusAlgorithm, networkSimulator, logger);
                nodes.Add(node);
            }

            // Connect nodes
            foreach (var node in nodes)
            {
                foreach (var otherNode in nodes.Where(n => n.Id != node.Id))
                {
                    node.AddNeighbor(otherNode.Id);
                }
            }

            // Act
            // Start all nodes
            foreach (var node in nodes)
            {
                node.Start();
            }

            // Wait for leader election
            await Task.Delay(2000);

            // Find the leader
            var leader = nodes.FirstOrDefault(n => n.State == NodeState.Leader);

            // Assert
            Assert.NotNull(leader);

            // Propose a value from the leader
            const int testValue = 42;
            leader.ProposeState(testValue);

            // Wait for replication
            await Task.Delay(1000);

            // Check that all nodes have the same value
            foreach (var node in nodes)
            {
                Assert.Equal(testValue, node.CurrentValue);
            }

            // Cleanup
            foreach (var node in nodes)
            {
                node.Stop();
            }
            networkSimulator.Shutdown();
        }

        [Fact]
        public async Task LeaderFailure_ShouldElectNewLeader()
        {
            // Arrange
            var logger = new ConsoleLogger(verbose: false);
            var networkSimulator = new NetworkSimulator(logger, minLatencyMs: 5, maxLatencyMs: 15);

            int[] nodeIds = { 1, 2, 3 };
            var nodes = new List<INode>();

            // Create nodes
            foreach (var nodeId in nodeIds)
            {
                var consensusAlgorithm = new RaftConsensus(nodeId, nodeIds, networkSimulator, logger);
                var node = new Node(nodeId, consensusAlgorithm, networkSimulator, logger);
                nodes.Add(node);
            }

            // Connect nodes
            foreach (var node in nodes)
            {
                foreach (var otherNode in nodes.Where(n => n.Id != node.Id))
                {
                    node.AddNeighbor(otherNode.Id);
                }
            }

            // Start all nodes
            foreach (var node in nodes)
            {
                node.Start();
            }

            // Wait for initial leader election
            await Task.Delay(2000);

            // Find the first leader
            var firstLeader = nodes.FirstOrDefault(n => n.State == NodeState.Leader);
            Assert.NotNull(firstLeader);

            // Act
            // Partition the leader from all other nodes (simulating leader failure)
            var partitionedNodes = nodes.Where(n => n.Id != firstLeader.Id).Select(n => n.Id);
            firstLeader.SimulatePartition(partitionedNodes);

            // Wait for new leader election
            await Task.Delay(2000);

            // Assert
            // Find the new leader among the remaining nodes
            var remainingNodes = nodes.Where(n => n.Id != firstLeader.Id);
            var newLeader = remainingNodes.FirstOrDefault(n => n.State == NodeState.Leader);

            Assert.NotNull(newLeader);
            Assert.NotEqual(firstLeader.Id, newLeader.Id);

            // Cleanup
            foreach (var node in nodes)
            {
                node.Stop();
            }
            networkSimulator.Shutdown();
        }

        [Fact]
        public async Task NetworkPartition_ShouldMaintainConsistency()
        {
            // Arrange
            var logger = new ConsoleLogger(verbose: false);
            var networkSimulator = new NetworkSimulator(logger, minLatencyMs: 5, maxLatencyMs: 15);

            int[] nodeIds = { 1, 2, 3 };
            var nodes = new List<INode>();

            // Create nodes
            foreach (var nodeId in nodeIds)
            {
                var consensusAlgorithm = new RaftConsensus(nodeId, nodeIds, networkSimulator, logger);
                var node = new Node(nodeId, consensusAlgorithm, networkSimulator, logger);
                nodes.Add(node);
            }

            // Connect nodes
            foreach (var node in nodes)
            {
                foreach (var otherNode in nodes.Where(n => n.Id != node.Id))
                {
                    node.AddNeighbor(otherNode.Id);
                }
            }

            // Start all nodes
            foreach (var node in nodes)
            {
                node.Start();
            }

            // Wait for leader election
            await Task.Delay(2000);

            // Act
            // Create partition: node 1 and 2 in one group, node 3 in another
            nodes[0].SimulatePartition(new[] { 3 });
            nodes[1].SimulatePartition(new[] { 3 });
            nodes[2].SimulatePartition(new[] { 1, 2 });

            // Propose different values from different partitions
            nodes[0].ProposeState(100);
            nodes[2].ProposeState(200);

            // Wait for processing
            await Task.Delay(1000);

            // Heal the partition
            foreach (var node in nodes)
            {
                node.HealPartition();
            }

            // Wait for nodes to sync
            await Task.Delay(2000);

            // Assert
            // All nodes should have the same value after partition is healed
            var expectedValue = nodes[0].CurrentValue;
            foreach (var node in nodes)
            {
                Assert.Equal(expectedValue, node.CurrentValue);
            }

            // Cleanup
            foreach (var node in nodes)
            {
                node.Stop();
            }
            networkSimulator.Shutdown();
        }

        [Fact]
        public async Task FullScenarioTest()
        {
            // Arrange
            var logger = new ConsoleLogger(verbose: false);
            var networkSimulator = new NetworkSimulator(logger, minLatencyMs: 5, maxLatencyMs: 20);

            int[] nodeIds = { 1, 2, 3 };
            var nodes = new List<INode>();

            // Create nodes
            foreach (var nodeId in nodeIds)
            {
                var consensusAlgorithm = new RaftConsensus(nodeId, nodeIds, networkSimulator, logger);
                var node = new Node(nodeId, consensusAlgorithm, networkSimulator, logger);
                nodes.Add(node);
            }

            // Connect nodes (fully connected network)
            foreach (var node in nodes)
            {
                foreach (var otherNode in nodes.Where(n => n.Id != node.Id))
                {
                    node.AddNeighbor(otherNode.Id);
                }
            }

            // Start all nodes
            foreach (var node in nodes)
            {
                node.Start();
            }

            // Wait for leader election
            await Task.Delay(2000);

            // Act - Run the scenario

            // 1. Nodo 1 propone estado 1
            nodes[0].ProposeState(1);
            await Task.Delay(500);

            // 2. Nodo 2 propone estado 2
            nodes[1].ProposeState(2);
            await Task.Delay(500);

            // 3. Simular partición donde Nodo 3 no puede comunicarse con Nodo 1
            nodes[2].SimulatePartition(new[] { 1 });
            await Task.Delay(500);

            // 4. Nodo 2 propone estado 3
            nodes[1].ProposeState(3);
            await Task.Delay(500);

            // 5. Sanar la partición
            nodes[2].HealPartition();

            // Wait for sync
            await Task.Delay(2000);

            // Assert
            // All nodes should have the value 3 at the end
            foreach (var node in nodes)
            {
                Assert.Equal(3, node.CurrentValue);
            }

            // Cleanup
            foreach (var node in nodes)
            {
                node.Stop();
            }
            networkSimulator.Shutdown();
        }
    }
}
