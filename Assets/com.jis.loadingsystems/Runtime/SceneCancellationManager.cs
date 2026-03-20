using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>
    /// Quản lý cancellation token theo scene. Hủy task khi chuyển scene.
    /// </summary>
    public class SceneCancellationManager
    {
        private readonly Dictionary<string, CancellationTokenSource> _tokens = new();
        private string _currentScene;

        public void RegisterSceneToken(string sceneName, bool isSingleMode = true)
        {
            if (isSingleMode) ClearAll();
            if (!_tokens.ContainsKey(sceneName))
                _tokens[sceneName] = new CancellationTokenSource();
            _currentScene = sceneName;
        }

        public CancellationToken GetSceneToken(string sceneName)
        {
            if (!_tokens.TryGetValue(sceneName, out var cts))
            {
                RegisterSceneToken(sceneName);
                return _tokens[sceneName].Token;
            }
            return cts.Token;
        }

        public CancellationToken GetCurrentSceneToken()
        {
            if (string.IsNullOrEmpty(_currentScene)) return default;
            return GetSceneToken(_currentScene);
        }

        public void CancelSceneTokens(string sceneName)
        {
            if (_tokens.TryGetValue(sceneName, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _tokens.Remove(sceneName);
            }
        }

        public void ClearAll()
        {
            foreach (var cts in _tokens.Values)
            {
                cts?.Cancel();
                cts?.Dispose();
            }
            _tokens.Clear();
            _currentScene = null;
        }

        public string GetCurrentActiveScene() => _currentScene;
    }
}
