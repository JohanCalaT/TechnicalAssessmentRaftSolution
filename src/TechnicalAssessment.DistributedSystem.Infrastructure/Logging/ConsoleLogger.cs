using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnicalAssessment.DistributedSystem.Core.Interfaces;

namespace TechnicalAssessment.DistributedSystem.Infrastructure.Logging
{
    /// <summary>
    /// Implementación de ILogger que escribe en la consola y mantiene un buffer para recuperar logs
    /// </summary>
    public class ConsoleLogger : ISystemLogger
    {
        private readonly bool _verbose;
        private readonly object _lock = new object();

        public ConsoleLogger(bool verbose = true)
        {
            _verbose = verbose;
        }

        public void Log(string message)
        {
            if (!_verbose)
                return;

            lock (_lock)
            {
                Console.WriteLine(message);
            }
        }
    }




}
