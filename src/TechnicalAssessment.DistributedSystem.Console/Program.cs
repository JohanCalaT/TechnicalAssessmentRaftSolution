using TechnicalAssessment.DistributedSystem.Core.Interfaces;
using TechnicalAssessment.DistributedSystem.Core.Models;
using TechnicalAssessment.DistributedSystem.Infrastructure.Configuration;
using TechnicalAssessment.DistributedSystem.Infrastructure.Consensus;
using TechnicalAssessment.DistributedSystem.Infrastructure.Logging;
using TechnicalAssessment.DistributedSystem.Infrastructure.Simulation;

namespace TechnicalAssessment.DistributedSystem.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool detailedLogs = args.Contains("--logs");

            ShowHeader("SIMULACIÓN DE ALGORITMO RAFT");
            System.Console.WriteLine("Esta simulación demostrará el funcionamiento del algoritmo Raft");
            System.Console.WriteLine("para alcanzar consenso en un sistema distribuido, incluso");
            System.Console.WriteLine("cuando hay particiones de red.");
            System.Console.WriteLine();

            // Crear logger (solo muestra mensajes detallados si se solicita)
            ISystemLogger logger = new ConsoleLogger(verbose: detailedLogs);

            // Crear servicio de configuración
            var configService = new NodeConfigurationService(logger);

            // Solicitar el modo de configuración
            NodeConfiguration config = await GetNodeConfigurationAsync(configService);

            // Crear simulador de red
            INetworkSimulator networkSimulator = new NetworkSimulator(
                logger,
                config.MinLatencyMs,
                config.MaxLatencyMs,
                config.MessageLossRate);

            ShowStep("CREANDO NODOS");
            System.Console.WriteLine($"Se crearán {config.NodeIds.Count} nodos que formarán el clúster.");

            // Lista para almacenar los nodos
            var nodes = new List<INode>();

            // Crear los nodos
            foreach (var nodeId in config.NodeIds)
            {
                // Crear el algoritmo de consenso para este nodo
                var consensusAlgorithm = new RaftConsensus(
                    nodeId,
                    config.NodeIds,
                    networkSimulator,
                    logger
                );

                // Crear el nodo
                var node = new Node(
                    nodeId,
                    consensusAlgorithm,
                    networkSimulator,
                    logger
                );

                nodes.Add(node);
                System.Console.WriteLine($"Nodo {nodeId} creado.");
                await Task.Delay(500); // Pausa para hacer la salida más legible
            }

            System.Console.WriteLine("\nPresiona ENTER para iniciar la simulación...");
            System.Console.ReadLine();

            ShowStep("CONECTANDO NODOS");
            System.Console.WriteLine("Configurando red totalmente conectada entre los nodos.");

            // Configurar vecinos (red totalmente conectada)
            foreach (var node in nodes)
            {
                foreach (var otherNode in nodes.Where(n => n.Id != node.Id))
                {
                    node.AddNeighbor(otherNode.Id);
                    System.Console.WriteLine($"Nodo {node.Id} conectado a Nodo {otherNode.Id}");
                    await Task.Delay(300); // Pausa para hacer la salida más legible
                }
            }

            ShowStep("INICIANDO NODOS");
            System.Console.WriteLine("Iniciando todos los nodos del clúster.");

            // Iniciar los nodos
            foreach (var node in nodes)
            {
                node.Start();
                System.Console.WriteLine($"Nodo {node.Id} iniciado.");
                await Task.Delay(500); // Pausa para hacer la salida más legible
            }

            // Esperar a que se elija un líder
            System.Console.WriteLine("\nEsperando a que se elija un líder...");
            await Task.Delay(3000);

            // Mostrar el líder elegido
            var leader = nodes.FirstOrDefault(n => n.State == Core.Entities.NodeState.Leader);
            if (leader != null)
            {
                System.Console.WriteLine($"\n✅ LÍDER ELEGIDO: Nodo {leader.Id} es el líder en el término {leader.CurrentTerm}");
            }
            else
            {
                System.Console.WriteLine("\n❌ ERROR: No se ha elegido un líder. Algo está mal en la implementación.");
                return;
            }

            // Esperar a que el usuario continúe
            System.Console.WriteLine("\nPresiona ENTER para continuar con el escenario de prueba...");
            System.Console.ReadLine();

            // Escenario de prueba
            await RunTestScenario(nodes);

            // Opcionalmente, mostrar logs detallados
            if (detailedLogs)
            {
                ShowHeader("LOGS DETALLADOS");
                foreach (var node in nodes)
                {
                    System.Console.WriteLine($"\nLogs del Nodo {node.Id}:\n");
                    System.Console.WriteLine(node.RetrieveLog());
                    System.Console.WriteLine("\nPresiona ENTER para continuar...");
                    System.Console.ReadLine();
                }
            }

            // Detener los nodos
            ShowStep("FINALIZANDO SIMULACIÓN");
            System.Console.WriteLine("Deteniendo todos los nodos...");

            foreach (var node in nodes)
            {
                node.Stop();
                System.Console.WriteLine($"Nodo {node.Id} detenido.");
                await Task.Delay(300);
            }

            // Detener el simulador de red
            System.Console.WriteLine("Deteniendo simulador de red...");
            networkSimulator.Shutdown();

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("\n==================================================");
            System.Console.WriteLine("           SIMULACIÓN COMPLETADA                  ");
            System.Console.WriteLine("==================================================");
            System.Console.ResetColor();
        }

        /// <summary>
        /// Obtiene la configuración de nodos desde JSON o consola según elección del usuario
        /// </summary>
        private static async Task<NodeConfiguration> GetNodeConfigurationAsync(INodeConfigurationService configService)
        {
            System.Console.WriteLine("¿Cómo deseas configurar los nodos?");
            System.Console.WriteLine("1. Cargar desde archivo JSON");
            System.Console.WriteLine("2. Configurar manualmente");
            System.Console.WriteLine("3. Usar configuración predeterminada (3 nodos)");

            int option = 0;
            while (option < 1 || option > 3)
            {
                System.Console.Write("Selecciona una opción (1-3): ");
                if (!int.TryParse(System.Console.ReadLine(), out option) || option < 1 || option > 3)
                {
                    System.Console.WriteLine("Opción inválida. Por favor, selecciona 1, 2 o 3.");
                }
            }

            if (option == 1)
            {
                string defaultPath = "nodeconfig.json";
                System.Console.Write($"Ingresa la ruta del archivo JSON (Enter para usar '{defaultPath}'): ");
                string path = System.Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(path))
                {
                    path = defaultPath;
                }

                return await configService.LoadFromJsonAsync(path);
            }
            else if (option == 2)
            {
                return configService.CollectFromConsole();
            }
            else
            {
                // Opción 3: Configuración predeterminada
                return new NodeConfiguration
                {
                    NodeIds = new List<int> { 1, 2, 3 },
                    MinLatencyMs = 5,
                    MaxLatencyMs = 50,
                    MessageLossRate = 0.05
                };
            }
        }

        /// <summary>
        /// Ejecuta el escenario de prueba con los nodos dados
        /// </summary>
        private static async Task RunTestScenario(List<INode> nodes)
        {
            ShowHeader("ESCENARIO DE PRUEBA");

            // 1. Nodo 1 propone estado 1
            ShowStep("PASO 1: Nodo 1 propone estado 1");
            nodes[0].ProposeState(1);
            await Task.Delay(2000);
            MostrarEstadoActual(nodes);
            System.Console.WriteLine("\nPresiona ENTER para continuar...");
            System.Console.ReadLine();

            // 2. Nodo 2 propone estado 2
            ShowStep("PASO 2: Nodo 2 propone estado 2");
            nodes[1].ProposeState(2);
            await Task.Delay(2000);
            MostrarEstadoActual(nodes);
            System.Console.WriteLine("\nPresiona ENTER para continuar...");
            System.Console.ReadLine();

            // 3. Simular partición
            ShowStep("PASO 3: Simulando partición de red");
            int lastNodeIndex = nodes.Count - 1;
            System.Console.WriteLine($"El Nodo {nodes[lastNodeIndex].Id} no podrá comunicarse con el Nodo {nodes[0].Id}.");
            nodes[lastNodeIndex].SimulatePartition(new[] { nodes[0].Id });
            await Task.Delay(2000);
            MostrarEstadoActual(nodes);
            System.Console.WriteLine("\nPresiona ENTER para continuar...");
            System.Console.ReadLine();

            // 4. Nodo 2 propone estado 3
            ShowStep("PASO 4: Nodo 2 propone estado 3");
            System.Console.WriteLine("Este valor se propagará a pesar de la partición.");
            nodes[1].ProposeState(3);
            await Task.Delay(2000);
            MostrarEstadoActual(nodes);
            System.Console.WriteLine("\nPresiona ENTER para continuar...");
            System.Console.ReadLine();

            // 5. Sanar la partición
            ShowStep("PASO 5: Sanando la partición");
            System.Console.WriteLine("Se restablecerá la comunicación entre todos los nodos.");
            nodes[lastNodeIndex].HealPartition();

            // Esperar a que todos los nodos alcancen consenso
            System.Console.WriteLine("\nEsperando a que todos los nodos alcancen consenso...");
            await Task.Delay(3000);
            MostrarEstadoActual(nodes);

            // Verificar que todos tengan el mismo valor
            var allSameValue = nodes.All(n => n.CurrentValue == nodes[0].CurrentValue);
            if (allSameValue && nodes[0].CurrentValue == 3)
            {
                System.Console.WriteLine("\n✅ ÉXITO: Todos los nodos han alcanzado consenso con el valor 3.");
            }
            else
            {
                System.Console.WriteLine("\n❌ ERROR: No se ha alcanzado consenso correctamente.");
            }
        }

        static void ShowHeader(string title)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("==================================================");
            System.Console.WriteLine($"           {title}");
            System.Console.WriteLine("==================================================");
            System.Console.ResetColor();
            System.Console.WriteLine();
        }

        static void ShowStep(string step)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($">> {step}");
            System.Console.WriteLine("--------------------------------------------------");
            System.Console.ResetColor();
        }

        static void MostrarEstadoActual(List<INode> nodes)
        {
            System.Console.WriteLine("\nEstado actual de los nodos:");
            System.Console.WriteLine("--------------------------------------------------");
            foreach (var node in nodes)
            {
                string leaderInfo = node.CurrentLeader.HasValue ? $"Líder: Nodo {node.CurrentLeader}" : "Sin líder";
                string roleInfo = node.State == Core.Entities.NodeState.Leader ? "[LÍDER]" :
                                  node.State == Core.Entities.NodeState.Candidate ? "[CANDIDATO]" : "[SEGUIDOR]";

                System.Console.WriteLine($"Nodo {node.Id} {roleInfo}: Valor = {node.CurrentValue}, Término = {node.CurrentTerm}, {leaderInfo}");
            }
        }
    }

}
