using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalAssessment.DistributedSystem.Core.Models
{
    /// <summary>
    /// Configuration model for nodes in the distributed system
    /// </summary>
    public class NodeConfiguration
    {
        /// <summary>
        /// List of node IDs in the system
        /// </summary>
        public List<int> NodeIds { get; set; } = new List<int>();

        /// <summary>
        /// Minimum latency in milliseconds for network simulation
        /// </summary>
        public int MinLatencyMs { get; set; } = 5;

        /// <summary>
        /// Maximum latency in milliseconds for network simulation
        /// </summary>
        public int MaxLatencyMs { get; set; } = 50;

        /// <summary>
        /// Probability of message loss in network simulation (0.0 to 1.0)
        /// </summary>
        public double MessageLossRate { get; set; } = 0.05;
    }
}
