namespace TechnicalAssessment.DistributedSystem.Core.Interfaces
{
    /// <summary>
    /// Interfaz para servicios de registro (logging)
    /// </summary>
    public interface ISystemLogger
    {
        void Log(string message);
    }
}
