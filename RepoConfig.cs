using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RepoTerrainScanner
{
    public class RepoConfig
    {
        // Controles y opciones de activación (General)
        public ConfigEntry<Key> ActiveKey;
        public ConfigEntry<float> ScanCooldown;
        public ConfigEntry<bool> EnableEnemyScan;
        public ConfigEntry<bool> EnableValuableScan;
        public ConfigEntry<bool> EnableCosmeticScan;
        public ConfigEntry<bool> EnableItemScan;
        public ConfigEntry<bool> EnablePlayerScan;
        public ConfigEntry<bool> EnableExtractionScan;
        public ConfigEntry<bool> ShowInactiveExtractions;
        public ConfigEntry<bool> EnableScanSound;
        public ConfigEntry<bool> EnableRemotePlayerSound;

        // Opciones experimentales
        public ConfigEntry<bool> EnableSelfScan;

        // Colores y sonido (Style)
        public ConfigEntry<float> ScanSoundVolume;
        public ConfigEntry<Color> ScanColorEnemy;
        public ConfigEntry<Color> ScanColorValuable;
        public ConfigEntry<Color> ScanColorCosmetic;
        public ConfigEntry<Color> ScanColorItem;
        public ConfigEntry<Color> ScanColorPlayer;
        public ConfigEntry<Color> ScanColorExtraction;
        public ConfigEntry<Color> ScanColorExtractionInactive;

        // Parámetros de la onda expansiva visual (Style)
        public ConfigEntry<Color> ScanColorHead, ScanColor;
        public ConfigEntry<float> ScanLineWidth, ScanLineInterval, HeadScanLineWidth;
        public ConfigEntry<float> ScanRange, ScanMaxDistance, ScanDuration;

        public RepoConfig(ConfigFile cfg)
        {
            // ==========================================
            // SECCIÓN: General
            // ==========================================
            ActiveKey = cfg.Bind("General", "ActiveKey", Key.C, 
                "[EN] Key to activate the scanner. / [ES] Tecla para activar el escaner.");

            ScanCooldown = cfg.Bind("General", "ScanCooldown", 6.0f, 
                "[EN] Cooldown time in seconds between scanner activations to prevent spam. / [ES] Tiempo de enfriamiento (cooldown) en segundos entre activaciones del escaner para evitar spam.");

            EnableEnemyScan = cfg.Bind("General", "EnableEnemyScan", true, 
                "[EN] Scan and highlight enemies in Red. / [ES] Escanea y resalta enemigos en Rojo.");

            EnableValuableScan = cfg.Bind("General", "EnableValuableScan", true, 
                "[EN] Scan and highlight scrap and valuable objects in Yellow. / [ES] Escanea y resalta chatarra y objetos de valor en Amarillo.");

            EnableCosmeticScan = cfg.Bind("General", "EnableCosmeticScan", true, 
                "[EN] Scan and highlight cosmetic items in Blue. / [ES] Escanea y resalta objetos cosmeticos en Azul.");

            EnableItemScan = cfg.Bind("General", "EnableItemScan", true, 
                "[EN] Scan and highlight tools, weapons and items in Purple. / [ES] Escanea y resalta herramientas, armas e items en Morado.");

            EnablePlayerScan = cfg.Bind("General", "EnablePlayerScan", true, 
                "[EN] Scan and highlight teammates and players in Green. / [ES] Escanea y resalta companeros de equipo y jugadores en Verde.");

            EnableExtractionScan = cfg.Bind("General", "EnableExtractionScan", true, 
                "[EN] Scan and highlight extraction points on the map. / [ES] Escanea y resalta los puntos de extraccion en el mapa.");

            ShowInactiveExtractions = cfg.Bind("General", "ShowInactiveExtractions", true, 
                "[EN] Show inactive or already completed extraction points. If false, only active points appear. / [ES] Muestra puntos de extraccion inactivos o completados. Si es false, solo se muestran los activos.");

            EnableScanSound = cfg.Bind("General", "EnableScanSound", true, 
                "[EN] Play sonar sound effect when scanning. / [ES] Reproduce el efecto de sonido sonar al escanear.");

            EnableRemotePlayerSound = cfg.Bind("General", "EnableRemotePlayerSound", false, 
                "[EN] Hear the sonar sound when other players scan in multiplayer. / [ES] Escuchar el sonido del sonar cuando otros jugadores escanean en multijugador.");

            // ==========================================
            // SECCIÓN: Experimental
            // ==========================================
            EnableSelfScan = cfg.Bind("Experimental", "EnableSelfScan", true, 
                "[EN] Allow the scanner to detect yourself (temporary testing option for 3rd person mods, will be removed in future updates). / [ES] Permite que el escaner te detecte a ti mismo (utilidad temporal de prueba para mods de 3ra persona, se removera en futuras versiones).");

            // ==========================================
            // SECCIÓN: Style (Colores, Audio y Onda Expansiva)
            // ==========================================
            ScanSoundVolume = cfg.Bind("Style", "ScanSoundVolume", 0.8f, 
                "[EN] Volume of the scan sound effect (0.0 to 1.0). / [ES] Volumen del efecto de sonido del escaner (0.0 a 1.0).");

            ScanColorEnemy = cfg.Bind("Style", "ScanColorEnemy", Color.red, 
                "[EN] Holographic color for enemies. / [ES] Color holografico para enemigos.");

            ScanColorValuable = cfg.Bind("Style", "ScanColorValuable", Color.yellow, 
                "[EN] Holographic color for scrap and valuables. / [ES] Color holografico para chatarra y objetos de valor.");

            ScanColorCosmetic = cfg.Bind("Style", "ScanColorCosmetic", Color.blue, 
                "[EN] Holographic color for cosmetic items. / [ES] Color holografico para objetos cosmeticos.");

            ScanColorItem = cfg.Bind("Style", "ScanColorItem", new Color(0.7f, 0.2f, 1.0f), 
                "[EN] Holographic color for tools, weapons and usable items. / [ES] Color holografico para herramientas, armas e items usables.");

            ScanColorPlayer = cfg.Bind("Style", "ScanColorPlayer", Color.green, 
                "[EN] Holographic color for players and allies. / [ES] Color holografico para jugadores y aliados.");

            ScanColorExtraction = cfg.Bind("Style", "ScanColorExtraction", new Color(0f, 0.96f, 1f, 1f), 
                "[EN] Holographic color for active/ready extraction points. / [ES] Color holografico para puntos de extraccion activos o en espera.");

            ScanColorExtractionInactive = cfg.Bind("Style", "ScanColorExtractionInactive", new Color(0.5f, 0.5f, 0.5f, 0.8f), 
                "[EN] Holographic color for inactive or completed extraction points. / [ES] Color holografico para puntos de extraccion inactivos o completados.");

            ScanColorHead = cfg.Bind("Style", "ScanColorHead", new Color(1f, 0.83f, 0.41f, 1f), 
                "[EN] Color of the leading edge of the expanding wave. / [ES] Color del borde delantero de la onda expansiva.");

            ScanColor = cfg.Bind("Style", "ScanColor", new Color(0.38f, 0.73f, 0.87f, 1f), 
                "[EN] Color of the body/tail of the expanding wave. / [ES] Color del cuerpo/cola de la onda expansiva.");

            ScanLineWidth = cfg.Bind("Style", "ScanLineWidth", 1.0f, 
                "[EN] Visual thickness of the scanner lines. / [ES] Grosor visual de las lineas del escaner.");

            ScanLineInterval = cfg.Bind("Style", "ScanLineInterval", 1.0f, 
                "[EN] Spacing and frequency between scan line subdivisions. / [ES] Espaciado y frecuencia entre subdivisiones de la linea.");

            HeadScanLineWidth = cfg.Bind("Style", "HeadScanLineWidth", 1.0f, 
                "[EN] Thickness of the leading front beam. / [ES] Grosor del haz principal delantero.");

            ScanRange = cfg.Bind("Style", "ScanRange", 5.0f, 
                "[EN] Visual depth field of the scan wave. / [ES] Profundidad del campo visual de la onda.");

            ScanMaxDistance = cfg.Bind("Style", "ScanMaxDistance", 30f, 
                "[EN] Maximum radial distance the wave travels (in meters). / [ES] Distancia maxima radial que recorrera la onda (en metros).");

            ScanDuration = cfg.Bind("Style", "ScanDuration", 5f, 
                "[EN] Total duration in seconds for the scan wave to complete. / [ES] Tiempo total en segundos que tarda la onda en completar su recorrido.");
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
