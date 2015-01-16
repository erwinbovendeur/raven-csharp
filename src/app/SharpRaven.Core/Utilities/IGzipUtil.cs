using System.IO;

namespace SharpRaven.Utilities
{
    public interface IGzipUtil
    {
        void Write(string json, Stream stream);
    }
}
