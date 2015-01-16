namespace SharpRaven
{
    public static class RavenClientFactory
    {
        public static Dsn Dsn { get; set; }

        public static IRavenClient Client { get; set; }
    }
}
