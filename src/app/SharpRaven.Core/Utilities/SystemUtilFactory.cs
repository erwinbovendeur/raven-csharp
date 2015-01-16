namespace SharpRaven.Utilities
{
    public static class SystemUtilFactory
    {
        private static ISystemUtil _instance;

        public static ISystemUtil Instance
        {
            get { return _instance ?? (_instance = new EmptySystemUtil()); }
            set { _instance = value; }
        }
    }
}
