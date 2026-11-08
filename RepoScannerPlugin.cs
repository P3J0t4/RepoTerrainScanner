using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RepoTerrainScanner
{
    [BepInPlugin("com.mod.repoterrainscanner", "Repo Terrain Scanner", "3.1.1")]
    public class RepoScannerPlugin : BaseUnityPlugin
    {
        public static RepoScannerPlugin Instance { get; private set; }
        public static RepoConfig ConfigInstance { get; private set; }
        internal static ManualLogSource ModLogger;

        public Material ScanMat { get; private set; }
        public AudioClip ScanAudioClip { get; private set; }

        private void Awake()
        {
            Instance = this;
            ModLogger = Logger;
            ModLogger.LogInfo("=== [RepoTerrainScanner] Iniciando (Sistema Modular v3.1.1) ===");

            ConfigInstance = new RepoConfig(Config);
            LoadBundles();

            SceneManager.sceneLoaded += (scene, mode) => EnsureCameraHook();
        }

        private void LoadBundles()
        {
            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // 1. Cargar scanwave.bundle (Material / Shader)
            string wavePath = Path.Combine(pluginDir, "scanwave.bundle");
            if (File.Exists(wavePath))
            {
                AssetBundle waveBundle = AssetBundle.LoadFromFile(wavePath);
                if (waveBundle != null && waveBundle.LoadAllAssets<Material>().Length > 0)
                {
                    ScanMat = waveBundle.LoadAllAssets<Material>()[0];
                    ModLogger.LogInfo("[RepoTerrainScanner] scanwave.bundle cargado con exito.");
                }
            }
            else
            {
                ModLogger.LogError($"[RepoTerrainScanner] No se encontro scanwave.bundle en: {wavePath}");
            }

            // 2. Cargar scansound.bundle (AudioClip darkSonar)
            string soundPath = Path.Combine(pluginDir, "scansound.bundle");
            if (File.Exists(soundPath))
            {
                AssetBundle soundBundle = AssetBundle.LoadFromFile(soundPath);
                if (soundBundle != null)
                {
                    AudioClip clip = soundBundle.LoadAsset<AudioClip>("darkSonar");
                    if (clip == null)
                    {
                        var allClips = soundBundle.LoadAllAssets<AudioClip>();
                        if (allClips.Length > 0) clip = allClips[0];
                    }

                    if (clip != null)
                    {
                        ScanAudioClip = clip;
                        ModLogger.LogInfo($"[RepoTerrainScanner] Sonido del escaner ({clip.name}) cargado con exito.");
                    }
                }
            }
            else
            {
                ModLogger.LogWarning($"[RepoTerrainScanner] No se encontro scansound.bundle en: {soundPath}");
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
