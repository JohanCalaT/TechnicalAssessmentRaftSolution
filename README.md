# 🔄 Distributed System with Raft Consensus

<div align="center">
<p><em>A robust simulation of a distributed system implementing the Raft consensus algorithm</em></p>

<img src="https://user-images.githubusercontent.com/25181517/121405625-8e73b580-c95d-11eb-9377-eb36858dc4b6.png" alt=".NET Core" width="80"/>
</div>

## 📋 Table of Contents

- [✨ Features](#-features)
- [🚀 How to Run](#-how-to-run)
- [🧪 Test Scenario](#-test-scenario)
- [🏗️ Architecture](#️-architecture)
- [⚙️ Raft Implementation](#️-raft-implementation)
- [🔄 Network Simulator](#-network-simulator)
- [🎛️ Dynamic Node Configuration](#️-dynamic-node-configuration)
- [📈 Possible Improvements](#-possible-improvements)
- [👤 Author](#-author)
- [📜 License](#-license)

## ✨ Features

<table>
  <tr>
    <td>
      <ul>
        <li>🤝 Complete Raft consensus algorithm</li>
        <li>🌐 Network simulation with latency</li>
        <li>❌ Message loss simulation</li>
        <li>🧩 Network partitions</li>
        <li>🔄 Log replication</li>
      </ul>
    </td>
    <td>
      <ul>
        <li>👑 Robust leader election</li>
        <li>🛡️ Safety properties</li>
        <li>📊 Detailed logs</li>
        <li>🧪 Complete test suite</li>
        <li>📱 Interactive console interface</li>
        <li>🎛️ Dynamic node configuration</li>
      </ul>
    </td>
  </tr>
</table>

## 🚀 How to Run

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher

### Step 1: Clone the Repository

```bash
git clone https://github.com/JohanCalaT/TechnicalAssessmentRaftSolution.git
cd TechnicalAssessmentRaftSolution
```

### Step 2: Build the Project

```bash
dotnet build
```

### Step 3: Run the Simulation

**Standard Mode:**

```bash
dotnet run --project src/TechnicalAssessment.DistributedSystem.Console
```

**With Detailed Logs:**

```bash
dotnet run --project src/TechnicalAssessment.DistributedSystem.Console -- --logs
```

### Step 4: Run the Tests

```bash
dotnet test
```

## 🧪 Test Scenario

The simulation automatically executes the following scenario:

| Step | Description | Expected Result |
|:----:|-------------|-------------------|
| 1️⃣ | Creation of nodes | Cluster formed |
| 2️⃣ | Leader election | One node becomes leader |
| 3️⃣ | Node 1 proposes value 1 | Value replicated across all nodes |
| 4️⃣ | Node 2 proposes value 2 | Value replicated across all nodes |
| 5️⃣ | Partition: Node 3 ↔️ Node 1 | Communication interrupted |
| 6️⃣ | Node 2 proposes value 3 | Value partially replicated |
| 7️⃣ | Partition healing | Communication restored |
| 8️⃣ | Verification | All nodes with value 3 |

During execution, the program allows advancing step by step by pressing ENTER, facilitating the flow tracking.

## 🏗️ Architecture

The project follows **Clean Architecture** principles:

```
TechnicalAssessmentRaftSolution/
├── src/
│   ├── TechnicalAssessment.DistributedSystem.Core/             # Domain core
│   │   ├── Entities/                                           # Entities 
│   │   │   ├── NodeState.cs                                    # State enum
│   │   │   └── LogEntry.cs                                     # Log entry
│   │   ├── Interfaces/                                         
│   │   │   ├── INode.cs                                        # Node interface
│   │   │   ├── IConsensusAlgorithm.cs                          # Consensus interface
│   │   │   ├── INetworkSimulator.cs                            # Network interface
│   │   │   ├── ISystemLogger.cs                                # Logger interface
│   │   │   └── INodeConfigurationService.cs                    # Config service interface
│   │   ├── Models/                                             
│   │   │   └── NodeConfiguration.cs                            # Configuration model
│   │   └── DTOs/                                               
│   │       └── MessageDto.cs                                   # Message DTOs
│   ├── TechnicalAssessment.DistributedSystem.Infrastructure/   
│   │   ├── Consensus/                                         
│   │   │   ├── Node.cs                                         # Node implementation
│   │   │   └── RaftConsensus.cs                                # Raft implementation
│   │   ├── Logging/                                           
│   │   │   └── ConsoleLogger.cs                                # Console logger
│   │   ├── Simulation/                                        
│   │   │   └── NetworkSimulator.cs                             # Network simulator
│   │   └── Configuration/                                      
│   │       └── NodeConfigurationService.cs                     # Configuration service
│   └── TechnicalAssessment.DistributedSystem.Console/         
│       ├── Program.cs                                          # Entry point
│       └── nodeconfig.json                                     # Config file
└── tests/
    └── TechnicalAssessment.DistributedSystem.Tests/           
        └── NodeTests.cs                                        # Unit tests
```

This structure ensures:
- 🔄 Low coupling
- 🛡️ High cohesion
- 🔌 Dependency injection
- 🧪 Testability

## ⚙️ Raft Implementation

The implementation faithfully follows the [original Raft paper](https://raft.github.io/raft.pdf):

### 👑 Leader Election

- ⏱️ Random timeouts (150-300ms)
- 🗳️ Voting based on log currency
- 🔄 Timer resets on communications

### 📝 Log Replication

- 💓 Periodic heartbeats (50ms)
- 📊 Batch replication to followers
- ✅ Majority-based commit

### 🛡️ Safety

- 🔒 Term-based commit restriction
- 🔄 Log conflict resolution
- 🛠️ Recovery after partitions

## 🔄 Network Simulator

The `NetworkSimulator` provides:

- 🌐 Message routing between nodes
- ⏱️ Configurable latency
- ❌ Probabilistic message loss
- 🧩 Creation and healing of partitions
- 📨 Time-ordered message delivery

## 🎛️ Dynamic Node Configuration

The new functionality allows configuring nodes in three different ways:

### 📄 JSON Configuration

- Loads configuration from an external JSON file
- Supports custom definition of nodes and network parameters
- Example format:
  ```json
  {
    "nodeIds": [1, 2, 3, 4, 5],
    "minLatencyMs": 10,
    "maxLatencyMs": 100,
    "messageLossRate": 0.02
  }
  ```

### 💻 Console Configuration

- Allows interactive system configuration
- The user can define:
  - Number of nodes (recommended between 3-7)
  - Minimum and maximum latency for network simulation
  - Message loss rate

### ⚡ Default Configuration

- Quick option to use a predefined 3-node configuration
- Ideal for demonstrations and quick tests

This implementation follows SOLID principles and Clean Architecture, maintaining separation of concerns and dependency inversion.

## 📈 Possible Improvements

- 💾 Persistence for recovery after complete failures
- 📊 Real-time performance metrics
- 🖥️ Web interface for graphical visualization
- 🔄 Controlled leadership transfer
- 📦 Large log compaction
- 📸 Snapshots for lagging nodes
- 🔌 Dynamic cluster size compatibility

## 👤 Author

<div align="center">
  <img src="https://avatars.githubusercontent.com/u/47458063?v=4" width="100px" style="border-radius: 50%;">
  <br><br>
  <strong>Johan Eduardo Cala Torra</strong>
  <br>
  Systems Engineer in Computer Science
  <br>
  Full Stack Developer
  <br><br>
  <a href="https://github.com/JohanCalaT">
    <img src="https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub"/>
  </a>
</div>

## 📜 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">
  <p>
    <em>Developed as part of a technical exercise to demonstrate knowledge in distributed systems and consensus algorithms</em>
  </p>
</div>