using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RepoTerrainScanner
{
    [BepInPlugin("com.mod.repoterrainscanner", "Repo Terrain Scanner", "3.0.3")]
    public class RepoScannerPlugin : BaseUnityPlugin
    {
        public static RepoScannerPlugin Instance { get; private set; }
        public static RepoConfig ConfigInstance { get; private set; }
        internal static ManualLogSource ModLogger;

        public Material ScanMat { get; private set; }

        private void Awake()
        {
            Instance = this;
            ModLogger = Logger;
            ModLogger.LogInfo("=== [RepoTerrainScanner] Iniciando (Sistema Modular v3.0.0) ===");

            ConfigInstance = new RepoConfig(Config);
            LoadBundle();

            SceneManager.sceneLoaded += (scene, mode) => EnsureCameraHook();
        }

        private void LoadBundle()
        {
            string bundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "scanwave.bundle");
            if (!File.Exists(bundlePath))
            {
                ModLogger.LogError($"[RepoTerrainScanner] No se encontro scanwave.bundle en: {bundlePath}");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle != null && bundle.LoadAllAssets<Material>().Length > 0)
            {
                ScanMat = bundle.LoadAllAssets<Material>()[0];
            }
        }

        private void Update()
        {
            EnsureCameraHook();
        }

        public void EnsureCameraHook()
        {
            if (ScanMat == null) return;

            Camera targetCam = Camera.main ?? FindObjectOfType<Camera>();
            if (targetCam == null) return;

            if (targetCam.gameObject.GetComponent<ScanController>() == null)
            {
                Material runtimeMat = new Material(ScanMat);
                var post = targetCam.gameObject.GetComponent<ScanPostProcess>() ?? targetCam.gameObject.AddComponent<ScanPostProcess>();
                post.scanMaterial = runtimeMat;
                var controller = targetCam.gameObject.AddComponent<ScanController>();
                controller.scanMaterial = runtimeMat;
            }
        }
    }
}