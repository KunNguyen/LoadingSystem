namespace Jis.LoadingSystems
{
    /// <summary>
    /// Các bước trong loading pipeline. Có thể mở rộng theo nhu cầu project.
    /// </summary>
    public enum LoadingStep
    {
        Boot,
        InitSDK,
        FetchRemoteConfig,
        CheckAuth,
        LoadLocalData,
        LoadCloudData,
        MergeData,
        EnterGame
    }
}
