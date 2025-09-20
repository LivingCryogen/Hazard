namespace AzProxy.DataTransform
{
    public class PartialFailureException(string message, IEnumerable<string> failures) : Exception (message)
    {
        public IEnumerable<string> Failures { get; } = failures;
    }
}
