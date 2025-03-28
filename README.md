Distributed System with Raft Consensus
Show Image

Show Image

Show Image

Show Image

Una simulación robusta de un sistema distribuido implementando el algoritmo de consenso Raft

Table of Contents
Características
Cómo Ejecutar
Escenario de Prueba
Arquitectura
Implementación de Raft
Simulador de Red
Posibles Mejoras
Autor
Licencia
Características
Algoritmo	Simulación	Pruebas
Show Image
Show Image
Show Image
🤝 Algoritmo de consenso Raft completo
🌐 Simulación de red con latencia
❌ Simulación de pérdida de mensajes
🧩 Particiones de red
🔄 Replicación de log
👑 Elección de líder robusta
🛡️ Propiedades de safety
📊 Logs detallados
🧪 Suite completa de pruebas
📱 Interfaz de consola interactiva
Cómo Ejecutar
Requisitos Previos
.NET 8.0 SDK o superior
Paso 1: Clonar el Repositorio
bash
git clone https://github.com/JohanCalaT/TechnicalAssessmentRaftSolution.git
cd TechnicalAssessmentRaftSolution
Paso 2: Compilar el Proyecto
bash
dotnet build
Paso 3: Ejecutar la Simulación
Modo Estándar:

bash
dotnet run --project src/TechnicalAssessment.DistributedSystem.Console
Con Logs Detallados:

bash
dotnet run --project src/TechnicalAssessment.DistributedSystem.Console -- --logs
Paso 4: Ejecutar las Pruebas
bash
dotnet test
Escenario de Prueba
La simulación ejecuta automáticamente el siguiente escenario:

Paso	Descripción	Resultado Esperado
1️⃣	Creación de 3 nodos	Clúster formado
2️⃣	Elección de líder	Un nodo se convierte en líder
3️⃣	Nodo 1 propone valor 1	Valor replicado en todos los nodos
4️⃣	Nodo 2 propone valor 2	Valor replicado en todos los nodos
5️⃣	Partición: Nodo 3 ↔️ Nodo 1	Comunicación interrumpida
6️⃣	Nodo 2 propone valor 3	Valor replicado parcialmente
7️⃣	Sanación de partición	Comunicación restaurada
8️⃣	Verificación	Todos los nodos con valor 3
Durante la ejecución, el programa permite avanzar paso a paso presionando ENTER, facilitando el seguimiento del flujo.

Arquitectura
El proyecto sigue los principios de Clean Architecture:

TechnicalAssessmentRaftSolution/
├── src/
│   ├── TechnicalAssessment.DistributedSystem.Core/             # Núcleo de dominio
│   │   ├── Entities/                                           # Entidades 
│   │   │   ├── NodeState.cs                                    # Enum de estados
│   │   │   └── LogEntry.cs                                     # Entrada de log
│   │   ├── Interfaces/                                         
│   │   │   ├── INode.cs                                        # Interfaz de nodo
│   │   │   ├── IConsensusAlgorithm.cs                          # Interfaz de consenso
│   │   │   ├── INetworkSimulator.cs                            # Interfaz de red
│   │   │   └── ISystemLogger.cs                                # Interfaz de logger
│   │   └── DTOs/                                               
│   │       └── MessageDto.cs                                   # DTOs de mensajes
│   ├── TechnicalAssessment.DistributedSystem.Infrastructure/   
│   │   ├── Consensus/                                         
│   │   │   ├── Node.cs                                         # Implementación de nodo
│   │   │   └── RaftConsensus.cs                                # Implementación de Raft
│   │   ├── Logging/                                           
│   │   │   └── ConsoleLogger.cs                                # Logger de consola
│   │   ├── Simulation/                                        
│   │   │   └── NetworkSimulator.cs                             # Simulador de red
│   └── TechnicalAssessment.DistributedSystem.Console/         
│       └── Program.cs                                          # Punto de entrada
└── tests/
    └── TechnicalAssessment.DistributedSystem.Tests/           
        └── NodeTests.cs                                        # Pruebas unitarias
Esta estructura garantiza:

🔄 Bajo acoplamiento
🛡️ Alta cohesión
🔌 Inyección de dependencias
🧪 Facilidad de pruebas
Implementación de Raft
La implementación sigue fielmente el paper original de Raft:

👑 Elección de Líder
⏱️ Timeouts aleatorios (150-300ms)
🗳️ Votación basada en actualidad del log
🔄 Reset de timers en comunicaciones
📝 Replicación de Log
💓 Heartbeats periódicos (50ms)
📊 Replicación en lote a seguidores
✅ Commit basado en mayoría
🛡️ Safety
🔒 Restricción de commit por término
🔄 Resolución de conflictos en logs
🛠️ Recuperación tras particiones
Simulador de Red
El NetworkSimulator proporciona:

🌐 Enrutamiento de mensajes entre nodos
⏱️ Latencia configurable
❌ Pérdida de mensajes probabilística
🧩 Creación y sanación de particiones
📨 Entrega ordenada por tiempo
Posibles Mejoras
💾 Persistencia para recuperación tras fallos completos
📊 Métricas de rendimiento en tiempo real
🖥️ Interfaz web para visualización gráfica
🔄 Transferencia de liderazgo controlada
📦 Compactación de logs grandes
📸 Snapshots para nodos rezagados
🔌 Compatibilidad con clústeres dinámicos
Autor
Show Image

Johan Eduardo Cala Torra, GitHub.

Ingeniero de Sistemas en Informática

Desarrollador Full Stack

Licencia
Distributed System with Raft Consensus está disponible bajo la licencia MIT. Ver el archivo LICENSE para más información.

<p align="center"> <em>Desarrollado como parte de un ejercicio técnico para demostrar conocimientos en sistemas distribuidos y algoritmos de consenso</em> </p> <!-- Links --> <!-- Badges --> <!-- Images -->
