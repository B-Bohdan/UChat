namespace uchat.Models.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when a failure occurs while configuring a connection.
    /// </summary>
    public class ConfiguringConnectionException : Exception
    {
        public ConfiguringConnectionException() { }

        public ConfiguringConnectionException(string message) : base(message) { }
    }
}
