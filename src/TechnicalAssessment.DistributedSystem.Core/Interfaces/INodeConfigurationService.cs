using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalAssessment.DistributedSystem.Core.Models;

namespace TechnicalAssessment.DistributedSystem.Core.Interfaces
{
    /// <summary>
    /// Interface for node configuration service
    /// </summary>
    public interface INodeConfigurationService
    {
        /// <summary>
        /// Loads node configuration from a JSON file
        /// </summary>
        /// <param name="filePath">Path to the JSON configuration file</param>
        /// <returns>NodeConfiguration object</returns>
        Task<NodeConfiguration> LoadFromJsonAsync(string filePath);

        /// <summary>
        /// Collects node configuration from console input
        /// </summary>
        /// <returns>NodeConfiguration object</returns>
        NodeConfiguration CollectFromConsole();
    }
}
