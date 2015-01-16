using System.Diagnostics;

namespace SharpRaven.Data
{
    /// <summary>
    /// A response object representing the sentry response
    /// </summary>
    [DebuggerDisplay("Response = {Id}")]
    public class SentryResponse
    {
        /// <summary>
        /// The ID of the response
        /// </summary>
        public string Id { get; set; }
    }
}
