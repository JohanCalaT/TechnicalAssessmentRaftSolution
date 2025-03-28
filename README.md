<div align="center">
🔄 Distributed System with Raft Consensus
Una simulación robusta de un sistema distribuido implementando el algoritmo de consenso Raft

<img src="https://user-images.githubusercontent.com/25181517/121405625-8e73b580-c95d-11eb-9377-eb36858dc4b6.png" alt=".NET Core" width="80"/> </div>
📋 Tabla de Contenidos
✨ Características
🚀 Cómo Ejecutar
🧪 Escenario de Prueba
🏗️ Arquitectura
⚙️ Implementación de Raft
🔄 Simulador de Red
📈 Posibles Mejoras
👤 Autor
📜 Licencia
✨ Características
<table> <tr> <td> <ul> <li>🤝 Algoritmo de consenso Raft completo</li> <li>🌐 Simulación de red con latencia</li> <li>❌ Simulación de pérdida de mensajes</li> <li>🧩 Particiones de red</li> <li>🔄 Replicación de log</li> </ul> </td> <td> <ul> <li>👑 Elección de líder robusta</li> <li>🛡️ Propiedades de safety</li> <li>📊 Logs detallados</li> <li>🧪 Suite completa de pruebas</li> <li>📱 Interfaz de consola interactiva</li> </ul> </td> </tr> </table>
🚀 Cómo Ejecutar
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
🧪 Escenario de Prueba
La simulación ejecuta automáticamente el siguiente escenario:

<div align="center">
Paso	Descripción	Resultado Esperado
1️⃣	Creación de 3 nodos	Clúster formado
2️⃣	Elección de líder	Un nodo se convierte en líder
3️⃣	Nodo 1 propone valor 1	Valor replicado en todos los nodos
4️⃣	Nodo 2 propone valor 2	Valor replicado en todos los nodos
5️⃣	Partición: Nodo 3 ↔️ Nodo 1	Comunicación interrumpida
6️⃣	Nodo 2 propone valor 3	Valor replicado parcialmente
7️⃣	Sanación de partición	Comunicación restaurada
8️⃣	Verificación	Todos los nodos con valor 3
</div>
Durante la ejecución, el programa permite avanzar paso a paso presionando ENTER, facilitando el seguimiento del flujo.

🏗️ Arquitectura
El proyecto sigue los principios de Clean Architecture:
Esta estructura garantiza:

🔄 Bajo acoplamiento
🛡️ Alta cohesión
🔌 Inyección de dependencias
🧪 Facilidad de pruebas
⚙️ Implementación de Raft
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
🔄 Simulador de Red
El NetworkSimulator proporciona:

🌐 Enrutamiento de mensajes entre nodos
⏱️ Latencia configurable
❌ Pérdida de mensajes probabilística
🧩 Creación y sanación de particiones
📨 Entrega ordenada por tiempo
📈 Posibles Mejoras
💾 Persistencia para recuperación tras fallos completos
📊 Métricas de rendimiento en tiempo real
🖥️ Interfaz web para visualización gráfica
🔄 Transferencia de liderazgo controlada
📦 Compactación de logs grandes
📸 Snapshots para nodos rezagados
🔌 Compatibilidad con clústeres dinámicos
👤 Autor
<div align="center"> <img src="https://avatars.githubusercontent.com/u/24512708?v=4" width="100px;" alt="Profile picture" style="border-radius: 50%;"/> <br> <strong>Johan Eduardo Cala Torra</strong> <br> Ingeniero de Sistemas en Informática <br> Desarrollador Full Stack <br> <br> <a href="https://github.com/JohanCalaT"> <img src="https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub"/> </a> </div>
📜 Licencia
Este proyecto está licenciado bajo la Licencia MIT.

<div align="center"> <p> <em>Desarrollado como parte de un ejercicio técnico para demostrar conocimientos en sistemas distribuidos y algoritmos de consenso</em> </p> </div>
