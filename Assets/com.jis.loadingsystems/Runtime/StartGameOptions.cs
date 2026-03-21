using System;
using Cysharp.Threading.Tasks;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Tùy chọn cho StartGame. Giúp gọi API rõ ràng hơn khi có nhiều tham số.
    /// </summary>
    public struct StartGameOptions
    {
        public bool Reload;
        public bool FromLogin;
        public Func<UniTask> OnLoadLocalDataAsync;
        public Func<UniTask> OnSyncCloudDataAsync;

        public static StartGameOptions Default => new()
        {
            Reload = false,
            FromLogin = false
        };

        public static StartGameOptions FromLogin() => new() { FromLogin = true };
        public static StartGameOptions ReloadGame() => new() { Reload = true };
    }
}
