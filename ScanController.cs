using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using HarmonyLib;

namespace RepoTerrainScanner
{
    public class ActiveWave
    {
        public Material Mat;
        public Coroutine Routine;
    }

    public class ScanController : MonoBehaviour, IOnEventCallback
    {
        public const byte SCAN_EVENT_CODE = 173;
        public Material scanMaterial;

        private ScanPostProcess _postProcess;
        private bool _wasKeyPressed = false;
        private static Harmony _harmony;
        
        private Dictionary<int, ActiveWave> _activeWaves = new Dictionary<int, ActiveWave>();

        private void Awake()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony("com.repotterrainscanner.hotfix");
                _harmony.PatchAll(typeof(ScanControllerPatches));
            }
        }

        private void OnEnable() { PhotonNetwork.AddCallbackTarget(this); }
        private void OnDisable() { PhotonNetwork.RemoveCallbackTarget(this); }

        private void Start()
        {
            _postProcess = GetComponent<ScanPostProcess>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            Key targetKey = RepoScannerPlugin.ConfigInstance != null ? RepoScannerPlugin.ConfigInstance.ActiveKey.Value : Key.C;
            var keyControl = kb[targetKey];

            if (keyControl != null)
            {
                bool isDown = keyControl.isPressed;
                if (isDown && !_wasKeyPressed)
                {
                    RepoScannerPlugin.Instance.Config.Reload();
                    
                    Vector3 origin = transform.position;
                    ScanSettingsData localSettings = ScanSettingsData.FromLocalConfig();
                    
                    int localPlayerId = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
                    
                    TriggerScan(localPlayerId, origin, localSettings);
                    BroadcastScan(origin, localSettings);
                }
                _wasKeyPressed = isDown;
            }
        }

        private void BroadcastScan(Vector3 origin, ScanSettingsData settings)
        {
            if (!PhotonNetwork.InRoom) return;

            // Paquete limpio con índices exactos (0 al 16)
            object[] content = new object[] {
                origin.x, origin.y, origin.z,
                settings.ScanColorHead.r, settings.ScanColorHead.g, settings.ScanColorHead.b, settings.ScanColorHead.a,
                settings.ScanColor.r, settings.ScanColor.g, settings.ScanColor.b, settings.ScanColor.a,
                settings.ScanLineWidth, settings.ScanLineInterval, settings.HeadScanLineWidth, 
                settings.ScanRange, settings.ScanMaxDistance, settings.ScanDuration
            };

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(SCAN_EVENT_CODE, content, raiseEventOptions, sendOptions);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == SCAN_EVENT_CODE)
            {
                try
                {
                    int senderId = photonEvent.Sender;
                    object[] data = (object[])photonEvent.CustomData;
                    
                    Vector3 remoteOrigin = new Vector3((float)data[0], (float)data[1], (float)data[2]);
                    
                    // Reconstrucción perfecta leyendo los índices correctos del paquete
                    ScanSettingsData remoteSettings = new ScanSettingsData {
                        ScanColorHead = new Color((float)data[3], (float)data[4], (float)data[5], (float)data[6]),
                        ScanColor = new Color((float)data[7], (float)data[8], (float)data[9], (float)data[10]),
                        ScanLineWidth = (float)data[11],
                        ScanLineInterval = (float)data[12],
                        HeadScanLineWidth = (float)data[13],
                        ScanRange = (float)data[14],
                        ScanMaxDistance = (float)data[15],
                        ScanDuration = (float)data[16],
                    };

                    TriggerScan(senderId, remoteOrigin, remoteSettings);
                }
                catch (Exception) { }
            }
        }

        public void TriggerScan(int playerId, Vector3 origin, ScanSettingsData settings)
        {
            if (scanMaterial == null) return;
            if (_postProcess == null) _postProcess = GetComponent<ScanPostProcess>();

            ActiveWave wave;
            
            if (_activeWaves.TryGetValue(playerId, out wave))
            {
                if (wave.Routine != null) StopCoroutine(wave.Routine);
            }
            else
            {
                wave = new ActiveWave();
                wave.Mat = new Material(scanMaterial);
                _activeWaves[playerId] = wave;
                
                if (_postProcess != null && !_postProcess.activeMaterials.Contains(wave.Mat))
                {
                    _postProcess.activeMaterials.Add(wave.Mat);
                }
            }

            wave.Routine = StartCoroutine(AnimateWaveAndDiscover(playerId, wave, origin, settings));
        }

        private struct ScanTarget
        {
            public PhysGrabObject physGrab;
            public float distance;
            public Color color;
            public Vector3 position;
        }

        private static bool ContainsBadWord(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("door") || name.Contains("cabinet") || 
                   name.Contains("drawer") || name.Contains("hinge") || 
                   name.Contains("locker") || name.Contains("closet") ||
                   name.Contains("toilet") || name.Contains("lid") ||
                   name.Contains("vent") || name.Contains("rack") ||
                   name.Contains("chair") || name.Contains("table") ||
                   name.Contains("desk") || name.Contains("trash") ||
                   name.Contains("bin") || name.Contains("shelf") ||
                   name.Contains("glass") || name.Contains("window") ||
                   name.Contains("lamp") || name.Contains("light") ||
                   name.Contains("cart") || name.Contains("vehicle") ||
                   name.Contains("barrel") || name.Contains("box");
        }

        private IEnumerator AnimateWaveAndDiscover(int playerId, ActiveWave wave, Vector3 origin, ScanSettingsData cfg)
        {
            Material mat = wave.Mat;
            float maxDistance = cfg.ScanMaxDistance;
            List<ScanTarget> pendingTargets = new List<ScanTarget>();
            HashSet<int> processedPGOs = new HashSet<int>();

            // --- VERIFICACIÓN DE RED ---
            // Determinamos si el escaneo lo activó este jugador o un compañero online
            int myPlayerId = PhotonNetwork.InRoom ? PhotonNetwork.LocalPlayer.ActorNumber : 0;
            bool isLocalScan = (playerId == myPlayerId);

            // Solo procesamos las físicas y los hologramas si YO activé la onda
            if (isLocalScan)
            {
                // ====================================================================
                // FASE 1: RECOLECCIÓN EN ÁREA RADIAL (CERO LAG)
                // ====================================================================
                Collider[] colliders = Physics.OverlapSphere(origin, maxDistance);
                int processCount = 0;

                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider col = colliders[i];
                    if (col == null || !col.gameObject.activeInHierarchy) continue;

                    PhysGrabObject pgo = col.GetComponentInParent<PhysGrabObject>();
                    if (pgo == null) continue;

                    // Filtro QoL: Ignorar ítems que ya están en el carrito
                    var impactDetector = pgo.GetComponentInChildren<PhysGrabObjectImpactDetector>();
                    if (impactDetector == null) 
                        impactDetector = pgo.GetComponentInParent<PhysGrabObjectImpactDetector>();

                    if (impactDetector != null && impactDetector.inCart)
                    {
                        continue; 
                    }

                    if (!processedPGOs.Add(pgo.gameObject.GetInstanceID())) continue;

                    float dist = Vector3.Distance(origin, pgo.transform.position);
                    Color targetColor = Color.clear;

                    if (pgo.GetComponentInParent<EnemyRigidbody>() != null) 
                    {
                        targetColor = Color.red;
                    } 
                    else if (pgo.GetComponentInParent<PlayerAvatar>() != null || pgo.GetComponentInParent<PlayerController>() != null) 
                    {
                        if (pgo.transform.root != this.transform.root) 
                            targetColor = Color.green;
                    } 
                    else if (pgo.GetComponentInParent<ValuableObject>() != null) 
                    {
                        targetColor = Color.yellow;
                    } 
                    else if (pgo.GetComponentInParent<ValuableDiscoverCustom>() != null) 
                    {
                        targetColor = Color.blue;
                    } 
                    else if (pgo.GetComponentInParent<ItemAttributes>() != null) 
                    {
                        targetColor = new Color(0.7f, 0.2f, 1.0f);
                    } 
                    
                    if (targetColor != Color.clear)
                    {
                        pendingTargets.Add(new ScanTarget { physGrab = pgo, distance = dist, color = targetColor, position = pgo.transform.position });
                    }

                    processCount++;
                    if (processCount > 40)
                    {
                        processCount = 0;
                        yield return null; 
                    }
                }

                // ====================================================================
                // FASE 1.5: JUGADORES VIVOS (Esferas Primitivas Verdes)
                // ====================================================================
                foreach (var avatar in FindObjectsOfType<PlayerAvatar>())
                {
                    if (avatar == null || !avatar.gameObject.activeInHierarchy) continue;

                    if (avatar.transform.root == this.transform.root) 
                        continue;

                    float dist = Vector3.Distance(origin, avatar.transform.position);
                    if (dist <= maxDistance)
                    {
                        pendingTargets.Add(new ScanTarget { physGrab = null, distance = dist, color = Color.green, position = avatar.transform.position });
                    }
                }
            } // <-- FIN DEL IF (isLocalScan)
            // ====================================================================
            // FASE 2: ANIMACIÓN DE ONDA VISUAL (Cero basura gráfica)
            // ====================================================================
            mat.SetVector("scanCenterWS", origin);
            mat.SetColor("scanColorHead", cfg.ScanColorHead);
            mat.SetColor("scanColor", cfg.ScanColor);
            mat.SetFloat("scanLineWidth", cfg.ScanLineWidth);
            mat.SetFloat("scanLineInterval", cfg.ScanLineInterval);
            mat.SetFloat("headScanLineWidth", cfg.HeadScanLineWidth);
            
            // Forzamos los valores del shader a 0 para que no estorben
            mat.SetFloat("scanLineBrightness", 0f);
            mat.SetFloat("outlineWidth", 0f);
            mat.SetFloat("outlineBrightness", 0f);
            mat.SetFloat("outlineStarDistance", 0f);

            float duration = Mathf.Max(0.1f, cfg.ScanDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentRadius = Mathf.Lerp(0f, maxDistance, t);

                for (int i = pendingTargets.Count - 1; i >= 0; i--)
                {
                    if (currentRadius >= pendingTargets[i].distance)
                    {
                        if (pendingTargets[i].physGrab != null)
                        {
                            TriggerNativeUIBox(pendingTargets[i].color, pendingTargets[i].physGrab, cfg.ScanDuration);
                        }
                        else
                        {
                            SpawnMarker(pendingTargets[i].position, pendingTargets[i].color);
                        }

                        pendingTargets.RemoveAt(i);
                    }
                }

                mat.SetFloat("headScanLineDistance", currentRadius);
                float brightnessScale = (t < 0.8f) ? Mathf.Clamp01(elapsed / 0.3f) : Mathf.Clamp01((1f - t) / 0.2f);
                mat.SetFloat("headScanLineBrightness", brightnessScale);
                mat.SetFloat("scanRange", Mathf.Lerp(1f, cfg.ScanRange, Mathf.SmoothStep(0f, 1f, t)));

                yield return null;
            }

            if (_postProcess != null)
                _postProcess.activeMaterials.Remove(mat);
            
            _activeWaves.Remove(playerId);
            Destroy(mat); 
        }

        private void TriggerNativeUIBox(Color markerColor, PhysGrabObject physGrab, float waveDuration)
        {
            if (physGrab != null && ValuableDiscover.instance != null)
            {
                try 
                {
                    ValuableDiscoverCustom custom = physGrab.GetComponent<ValuableDiscoverCustom>();
                    if (custom == null) custom = physGrab.GetComponentInChildren<ValuableDiscoverCustom>();
                    
                    if (custom == null) 
                    {
                        GameObject go = new GameObject("ScannerCustomDiscover");
                        go.transform.SetParent(physGrab.transform);
                        go.transform.localPosition = Vector3.zero;
                        
                        try 
                        {
                            ScanControllerPatches.shouldSuppressValuableDiscoverCustomAwake = true;
                            custom = go.AddComponent<ValuableDiscoverCustom>();
                        } 
                        finally 
                        {
                            ScanControllerPatches.shouldSuppressValuableDiscoverCustomAwake = false;
                        }
                    }

                    if (custom != null)
                    {
                        custom.colorCorner = markerColor;
                        custom.colorMiddle = new Color(markerColor.r * 0.4f, markerColor.g * 0.4f, markerColor.b * 0.4f, 0.3f);
                        custom.waitTime = Mathf.Max(3.0f, waveDuration); 
                        
                        ValuableDiscover.instance.New(physGrab, ValuableDiscoverGraphic.State.Custom, custom);
                    }
                } 
                catch { }
            }
        }

        private void SpawnMarker(Vector3 position, Color markerColor)
        {
            GameObject markerObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObj.transform.position = position;
            Destroy(markerObj.GetComponent<Collider>());

            Renderer rend = markerObj.GetComponent<Renderer>();
            Material xRayMat = new Material(Shader.Find("GUI/Text Shader"));
            xRayMat.color = markerColor;
            rend.material = xRayMat;

            markerObj.AddComponent<ScanMarker>();
        }
    }

    [HarmonyPatch(typeof(ValuableDiscoverCustom), "Awake")]
    public static class ScanControllerPatches
    {
        public static bool shouldSuppressValuableDiscoverCustomAwake = false;

        [HarmonyPrefix]
        public static bool Prefix()
        {
            return !shouldSuppressValuableDiscoverCustomAwake;
        }
    }
}