using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TechnicalAssessment.DistributedSystem.Core.Interfaces;
using TechnicalAssessment.DistributedSystem.Core.Models;

namespace TechnicalAssessment.DistributedSystem.Infrastructure.Configuration
{
    /// <summary>
    /// Service for loading node configuration from different sources
    /// </summary>
    public class NodeConfigurationService : INodeConfigurationService
    {
        private readonly ISystemLogger _logger;

        public NodeConfigurationService(ISystemLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads node configuration from a JSON file
        /// </summary>
        public async Task<NodeConfiguration> LoadFromJsonAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Log($"Configuration file not found: {filePath}");
                    return CreateDefaultConfiguration();
                }

                string jsonContent = await File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<NodeConfiguration>(jsonContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (config == null || config.NodeIds.Count == 0)
                {
                    _logger.Log("Invalid configuration or no nodes specified in the configuration file");
                    return CreateDefaultConfiguration();
                }

                _logger.Log($"Loaded configuration with {config.NodeIds.Count} nodes from {filePath}");
                return config;
            }
            catch (Exception ex)
            {
                _logger.Log($"Error loading configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Collects node configuration from console input
        /// </summary>
        public NodeConfiguration CollectFromConsole()
        {
            var config = new NodeConfiguration();

            Console.WriteLine("\nEnter node configuration:");

            // Collect number of nodes
            int nodeCount = GetIntInput("Enter number of nodes (3-7 recommended): ", 1, 10);

            // Generate node IDs
            for (int i = 1; i <= nodeCount; i++)
            {
                config.NodeIds.Add(i);
            }

            // Collect network parameters
            Console.WriteLine("\nNetwork simulation parameters (press Enter for defaults):");

            // Min latency
            string minLatencyInput = GetStringInput("Minimum latency in ms (default: 5): ");
            if (!string.IsNullOrWhiteSpace(minLatencyInput) && int.TryParse(minLatencyInput, out int minLatency))
            {
                config.MinLatencyMs = Math.Max(1, minLatency);
            }

            // Max latency
            string maxLatencyInput = GetStringInput("Maximum latency in ms (default: 50): ");
            if (!string.IsNullOrWhiteSpace(maxLatencyInput) && int.TryParse(maxLatencyInput, out int maxLatency))
            {
                config.MaxLatencyMs = Math.Max(config.MinLatencyMs + 1, maxLatency);
            }

            // Message loss rate
            string lossRateInput = GetStringInput("Message loss rate (0.0-1.0, default: 0.05): ");
            if (!string.IsNullOrWhiteSpace(lossRateInput) &&
                double.TryParse(lossRateInput, out double lossRate))
            {
                config.MessageLossRate = Math.Clamp(lossRate, 0.0, 1.0);
            }

            _logger.Log($"Configured system with {nodeCount} nodes");
            return config;
        }

        /// <summary>
        /// Creates a default configuration with 3 nodes
        /// </summary>
        private NodeConfiguration CreateDefaultConfiguration()
        {
            var config = new NodeConfiguration
            {
                NodeIds = new List<int> { 1, 2, 3 },
                MinLatencyMs = 5,
                MaxLatencyMs = 50,
                MessageLossRate = 0.05
            };

            _logger.Log("Using default configuration with 3 nodes");
            return config;
        }

        /// <summary>
        /// Gets an integer input from the console with validation
        /// </summary>
        private int GetIntInput(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
                {
                    return value;
                }
                Console.WriteLine($"Please enter a valid number between {min} and {max}");
            }
        }

        /// <summary>
        /// Gets a string input from the console
        /// </summary>
        private string GetStringInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
