using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems.Editor
{
    /// <summary>
    /// Setup template JIS Loading System trong 1 bước: tạo scenes + scripts + Build Settings.
    /// Menu: Tools > JIS Loading System > Setup Template
    /// Hoặc: Right-click trong Project > Create > JIS Loading System > Setup Template
    /// </summary>
    public static class LoadingSystemSetup
    {
        private const string MenuPath = "Tools/JIS Loading System/Setup Template";
        private const string CreateMenuPath = "Assets/Create/JIS Loading System/Setup Template";
        private const string QuickMenuPath = "Tools/JIS Loading System/Setup Template (1-click)";
        private const string DefaultFolder = "Assets/JISLoadingSystem";

        [MenuItem(MenuPath, false, 100)]
        [MenuItem(CreateMenuPath, false, 100)]
        public static void Setup()
        {
            var folder = GetOrChooseFolder();
            if (string.IsNullOrEmpty(folder)) return;
            RunSetup(folder);
        }

        [MenuItem(QuickMenuPath, false, 101)]
        public static void SetupQuick()
        {
            RunSetup(DefaultFolder);
        }

        private static void RunSetup(string folder)
        {
            try
            {
                EditorUtility.DisplayProgressBar("JIS Loading System", "Đang tạo template...", 0f);

                CreateFolderStructure(folder);
                CreateScripts(folder);
                AssetDatabase.Refresh();
                var scenePaths = CreateScenes(folder);
                AddScenesToBuildSettings(scenePaths);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();
                var result = EditorUtility.DisplayDialog("Setup hoàn tất",
                    $"Đã tạo template tại:\n{folder}\n\n" +
                    "Scenes: BootstrapScene, InitSdkScene, ControllerScene\n" +
                    "Scripts (ví dụ): InitSdkSceneController_Example, ControllerSceneController_Example\n\n" +
                    "BootstrapScene đã có SceneFlowManager + StubLoadingUI + BootstrapController.\n" +
                    "Mở BootstrapScene và Play để test.",
                    "Mở thư mục", "Đóng");

                if (result)
                    EditorUtility.RevealInFinder(Path.Combine(Directory.GetCurrentDirectory(), folder));
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", ex.Message + "\n\n" + ex.StackTrace, "Đóng");
            }
        }

        private static string GetOrChooseFolder()
        {
            var activePath = "Assets";
            if (Selection.activeObject != null)
            {
                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path))
                    activePath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            }

            var folder = EditorUtility.SaveFolderPanel("Chọn thư mục tạo template", activePath, "JISLoadingSystem");
            if (string.IsNullOrEmpty(folder)) return null;

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var projectPath = Path.GetFullPath(projectRoot + Path.DirectorySeparatorChar);
            var folderFull = Path.GetFullPath(folder);
            if (!folderFull.StartsWith(projectPath))
            {
                EditorUtility.DisplayDialog("Lỗi", "Chọn thư mục bên trong project (Assets/...)", "Đóng");
                return null;
            }

            return folderFull.Substring(projectPath.Length).Replace(Path.DirectorySeparatorChar, '/');
        }

        private static void CreateFolderStructure(string baseFolder)
        {
            Directory.CreateDirectory(Path.Combine(baseFolder, "Scenes"));
            Directory.CreateDirectory(Path.Combine(baseFolder, "Scripts"));
        }

        private static string[] CreateScenes(string baseFolder)
        {
            var scenesPath = baseFolder + "/Scenes";
            EditorUtility.DisplayProgressBar("JIS Loading System", "Tạo BootstrapScene...", 0.2f);

            // BootstrapScene: SceneFlowManager + StubLoadingUI (đã link)
            var bootstrapPath = scenesPath + "/BootstrapScene.unity";
            var bootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var flowGo = new GameObject("SceneFlowManager");
            var flow = flowGo.AddComponent<SceneFlowManager>();

            var loadingGo = new GameObject("LoadingUI");
            var loadingUI = loadingGo.AddComponent<StubLoadingUI>();

            // Gán loadingUI vào flow qua SerializedObject
            var so = new SerializedObject(flow);
            so.FindProperty("loadingUIRaw").objectReferenceValue = loadingUI;
            so.FindProperty("initSdkScene").stringValue = "InitSdkScene";
            so.FindProperty("controllerScene").stringValue = "ControllerScene";
            so.ApplyModifiedPropertiesWithoutUndo();

            // BootstrapController gọi StartGame
            flowGo.AddComponent<BootstrapController>();

            EditorSceneManager.SaveScene(bootScene, bootstrapPath);

            EditorUtility.DisplayProgressBar("JIS Loading System", "Tạo InitSdkScene...", 0.4f);
            var initPath = scenesPath + "/InitSdkScene.unity";
            var initScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("InitSdkSceneController").AddComponent<DefaultInitSdkSceneController>();
            EditorSceneManager.SaveScene(initScene, initPath);

            EditorUtility.DisplayProgressBar("JIS Loading System", "Tạo ControllerScene...", 0.55f);
            var ctrlPath = scenesPath + "/ControllerScene.unity";
            var ctrlScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ControllerSceneController").AddComponent<DefaultControllerSceneController>();
            EditorSceneManager.SaveScene(ctrlScene, ctrlPath);

            EditorUtility.DisplayProgressBar("JIS Loading System", "Tạo GameplayScene...", 0.65f);
            var gameplayPath = scenesPath + "/GameplayScene.unity";
            var gameplayScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(gameplayScene, gameplayPath);

            return new[] { bootstrapPath, initPath, ctrlPath, gameplayPath };
        }

        private static void CreateScripts(string baseFolder)
        {
            var scriptsPath = baseFolder + "/Scripts";
            EditorUtility.DisplayProgressBar("JIS Loading System", "Tạo script templates...", 0.8f);

            WriteScript(scriptsPath, "InitSdkSceneController_Example.cs", GetInitSdkControllerTemplate());
            WriteScript(scriptsPath, "ControllerSceneController_Example.cs", GetControllerSceneTemplate());
        }

        private static void WriteScript(string dir, string filename, string content)
        {
            var path = Path.Combine(dir, filename);
            File.WriteAllText(path, content);
        }

        private static string GetInitSdkControllerTemplate() => @"using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Jis.LoadingSystems
{
    /// <summary>Khởi tạo SDK (Ads, IAP, Firebase...) khi InitSdkScene load.</summary>
    public class InitSdkSceneController : MonoBehaviour, ISceneLifecycle
    {
        public async UniTask OnSceneLoaded(object payload)
        {
            // TODO: Init SDK của bạn
            // await AdsManager.InitAsync();
            // await FirebaseManager.InitAsync();
            await UniTask.CompletedTask;
        }
    }
}
";

        private static string GetControllerSceneTemplate() => @"using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jis.LoadingSystems
{
    /// <summary>Điều phối scene sau boot. Load GameplayScene hoặc scene khác.</summary>
    public class ControllerSceneController : MonoBehaviour, ISceneLifecycle
    {
        public async UniTask OnSceneLoaded(object payload)
        {
            if (payload is ScenePayload p && p.FromLogin)
            {
                // Có thể hiện popup chào mừng
            }

            // Load scene chính (đổi tên theo project)
            await SceneFlowManager.Instance.LoadSceneByName(
                ""GameplayScene"",
                null,
                LoadSceneMode.Additive);
        }
    }
}
";

        private static void AddScenesToBuildSettings(string[] scenePaths)
        {
            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var toAdd = new System.Collections.Generic.List<string>();
            foreach (var path in scenePaths)
            {
                if (!existing.Exists(s => s.path == path))
                    toAdd.Add(path);
            }
            for (var i = toAdd.Count - 1; i >= 0; i--)
            {
                existing.Insert(0, new EditorBuildSettingsScene(toAdd[i], true));
            }
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }
}
