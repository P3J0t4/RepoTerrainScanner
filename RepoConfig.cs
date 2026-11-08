using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RepoTerrainScanner
{
    public class RepoConfig
    {
        public ConfigEntry<Key> ActiveKey;
        public ConfigEntry<bool> EnableSelfScan;
        public ConfigEntry<Color> ScanColorHead, ScanColor;
        public ConfigEntry<float> ScanLineWidth, ScanLineInterval, HeadScanLineWidth;
        public ConfigEntry<float> ScanRange, ScanMaxDistance, ScanDuration;

        public RepoConfig(ConfigFile cfg)
        {
            ActiveKey = cfg.Bind("General", "ActiveKey", Key.C, "Tecla para activar");
            EnableSelfScan = cfg.Bind("Experimental", "EnableSelfScan", true, "Permite que el escaner te detecte a ti mismo (utilidad temporal de prueba para mods de 3ra persona, se removera en futuras versiones).");
            ScanColorHead = cfg.Bind("Style", "ScanColorHead", new Color(1f, 0.83f, 0.41f, 1f));
            ScanColor = cfg.Bind("Style", "ScanColor", new Color(0.38f, 0.73f, 0.87f, 1f));
            ScanLineWidth = cfg.Bind("Style", "ScanLineWidth", 1.0f);
            ScanLineInterval = cfg.Bind("Style", "ScanLineInterval", 1.0f);
            HeadScanLineWidth = cfg.Bind("Style", "HeadScanLineWidth", 1.0f);
            ScanRange = cfg.Bind("Style", "ScanRange", 5.0f);
            ScanMaxDistance = cfg.Bind("Style", "ScanMaxDistance", 30f);
            ScanDuration = cfg.Bind("Style", "ScanDuration", 5f);
        }
    }

    public struct ScanSettingsData
    {
        public Color ScanColorHead;
        public Color ScanColor;
        public float ScanLineWidth, ScanLineInterval, HeadScanLineWidth;
        public float ScanRange, ScanMaxDistance, ScanDuration;

        public static ScanSettingsData FromLocalConfig()
        {
            var cfg = RepoScannerPlugin.ConfigInstance;
            return new ScanSettingsData
            {
                ScanColorHead = cfg.ScanColorHead.Value,
                ScanColor = cfg.ScanColor.Value,
                ScanLineWidth = cfg.ScanLineWidth.Value,
                ScanLineInterval = cfg.ScanLineInterval.Value,
                HeadScanLineWidth = cfg.HeadScanLineWidth.Value,
                ScanRange = cfg.ScanRange.Value,
                ScanMaxDistance = cfg.ScanMaxDistance.Value,
                ScanDuration = cfg.ScanDuration.Value
            };
        }
    }
}
