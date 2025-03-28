using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalAssessment.DistributedSystem.Core.Entities
{
    /// <summary>
    /// Enumeración de los posibles estados de un nodo en el algoritmo Raft
    /// </summary>
    public enum NodeState
    {
        /// <summary>
        /// Nodo que recibe y procesa solicitudes del líder
        /// </summary>
        Follower,

        /// <summary>
        /// Nodo que está solicitando votos para convertirse en líder
        /// </summary>
        Candidate,

        /// <summary>
        /// Nodo que coordina todas las operaciones en el sistema
        /// </summary>
        Leader
    }
}
