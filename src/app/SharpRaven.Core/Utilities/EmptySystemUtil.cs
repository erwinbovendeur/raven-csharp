using System.Collections.Generic;

namespace SharpRaven.Utilities
{
    class EmptySystemUtil : ISystemUtil
    {
        public EmptySystemUtil()
        {
            Logger = "root";
        }

        public bool CanCompress
        {
            get { return false; }
        }

        public string Logger { get; set; }

        public string MachineName
        {
            get { return string.Empty; }
        }

        public string UserName
        {
            get { return string.Empty; }
        }

        public IDictionary<string, string> GetModules()
        {
            return null;
        }
    }
}
