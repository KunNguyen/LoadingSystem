namespace Jis.LoadingSystems
{
    /// <summary>
    /// Context truyền qua loading pipeline. Mở rộng theo nhu cầu project.
    /// </summary>
    public class LoadingContext
    {
        public bool IsLoggedIn;
        public bool IsReload;
        public bool CloudDataAvailable;
        public bool FromLogin { get; set; }
    }
}
