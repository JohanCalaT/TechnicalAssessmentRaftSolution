using TechnicalAssessment.DistributedSystem.Core.Interfaces;
using TechnicalAssessment.DistributedSystem.Infrastructure.Consensus;
using TechnicalAssessment.DistributedSystem.Infrastructure.Logging;
using TechnicalAssessment.DistributedSystem.Infrastructure.Simulation;

namespace TechnicalAssessment.DistributedSystem.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool detailedLogs = args.Length > 0 && args[0] == "--logs";

            ShowHeader("SIMULACIÓN DE ALGORITMO RAFT");
            System.Console.WriteLine("Esta simulación demostrará el funcionamiento del algoritmo Raft");
            System.Console.WriteLine("para alcanzar consenso en un sistema distribuido, incluso");
            System.Console.WriteLine("cuando hay particiones de red.");
            System.Console.WriteLine();
            System.Console.WriteLine("Presiona ENTER para iniciar la simulación...");
            System.Console.ReadLine();

            // Crear logger (solo muestra mensajes detallados si se solicita)
            ISystemLogger logger = new ConsoleLogger(verbose: detailedLogs);

            // Crear simulador de red
            INetworkSimulator networkSimulator = new NetworkSimulator(logger);

            ShowStep("CREANDO NODOS");
            System.Console.WriteLine("Se crearán 3 nodos que formarán el clúster.");

            // IDs de los nodos
            int[] nodeIds = { 1, 2, 3 };

            // Lista para almacenar los nodos
            var nodes = new List<INode>();

            // Crear los nodos
            foreach (var nodeId in nodeIds)
            {
                // Crear el algoritmo de consenso para este nodo
                var consensusAlgorithm = new RaftConsensus(
                    nodeId,
                    nodeIds,
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

            // 3. Simular partición donde Nodo 3 no puede comunicarse con Nodo 1
            ShowStep("PASO 3: Simulando partición de red");
            System.Console.WriteLine("El Nodo 3 no podrá comunicarse con el Nodo 1.");
            nodes[2].SimulatePartition(new[] { 1 });
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
            nodes[2].HealPartition();

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
