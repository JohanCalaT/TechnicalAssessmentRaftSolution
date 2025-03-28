using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnicalAssessment.DistributedSystem.Core.Entities
{
    /// <summary>
    /// Representa una entrada en el registro (log) del algoritmo Raft
    /// </summary>
    public class LogEntry
    {
        public LogEntry(int term, int value)
        {
            Term = term;
            Value = value;
        }

        // El término en el que se creó esta entrada
        public int Term { get; }

        // El valor de estado propuesto
        public int Value { get; }

        public override string ToString()
        {
            return $"LogEntry(Term={Term}, Value={Value})";
        }
    }
}
