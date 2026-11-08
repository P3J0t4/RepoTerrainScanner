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

    /// <summary>
    /// Componente auxiliar anclado al jugador que mantiene sincronizada la posición
    /// del proxy y sus límites para que el recuadro holográfico nativo lo siga en tiempo real.
    /// </summary>
    public class ScannerPlayerProxy : MonoBehaviour
    {
        public PhysGrabObject pgo;
        public Material invMat;

        private void LateUpdate()
        {
            if (pgo != null)
            {
                pgo.centerPoint = transform.position;
            }
        }

        private void OnDestroy()
        {
            if (invMat != null)
            {
                Destroy(invMat);
            }
        }
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
            public PlayerAvatar playerAvatar;
            public ExtractionPoint extractionPoint;
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
                try
                {
                    RepoScannerPlugin.Instance?.Config?.Reload();
                }
                catch { }

                // ====================================================================
                // FASE 1: RECOLECCIÓN EN ÁREA RADIAL (CERO LAG)
                // ====================================================================
                Collider[] colliders = Physics.OverlapSphere(origin, maxDistance);
                int processCount = 0;

                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider col = colliders[i];
                    if (col == null || !col.gameObject.activeInHierarchy) continue;

                    // Los jugadores se detectan y gestionan en Fase 1.5 con seguimiento dinámico
                    if (col.GetComponentInParent<PlayerAvatar>() != null || col.GetComponentInParent<PlayerController>() != null)
                    {
                        continue;
                    }

                    // Los puntos de extracción se detectan y gestionan en Fase 1.6
                    if (col.GetComponentInParent<ExtractionPoint>() != null)
                    {
                        continue;
                    }

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
                        bool enabled = RepoScannerPlugin.ConfigInstance == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableEnemyScan == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableEnemyScan.Value;
                        if (enabled)
                        {
                            targetColor = RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorEnemy != null
                                ? RepoScannerPlugin.ConfigInstance.ScanColorEnemy.Value
                                : Color.red;
                        }
                    } 
                    else if (pgo.GetComponentInParent<ValuableObject>() != null) 
                    {
                        bool enabled = RepoScannerPlugin.ConfigInstance == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableValuableScan == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableValuableScan.Value;
                        if (enabled)
                        {
                            targetColor = RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorValuable != null
                                ? RepoScannerPlugin.ConfigInstance.ScanColorValuable.Value
                                : Color.yellow;
                        }
                    } 
                    else if (pgo.GetComponentInParent<ValuableDiscoverCustom>() != null) 
                    {
                        bool enabled = RepoScannerPlugin.ConfigInstance == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableCosmeticScan == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableCosmeticScan.Value;
                        if (enabled)
                        {
                            targetColor = RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorCosmetic != null
                                ? RepoScannerPlugin.ConfigInstance.ScanColorCosmetic.Value
                                : Color.blue;
                        }
                    } 
                    else if (pgo.GetComponentInParent<ItemAttributes>() != null) 
                    {
                        bool enabled = RepoScannerPlugin.ConfigInstance == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableItemScan == null || 
                                       RepoScannerPlugin.ConfigInstance.EnableItemScan.Value;
                        if (enabled)
                        {
                            targetColor = RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorItem != null
                                ? RepoScannerPlugin.ConfigInstance.ScanColorItem.Value
                                : new Color(0.7f, 0.2f, 1.0f);
                        }
                    } 
                    
                    if (targetColor != Color.clear)
                    {
                        pendingTargets.Add(new ScanTarget { 
                            physGrab = pgo, 
                            distance = dist, 
                            color = targetColor, 
                            position = pgo.transform.position,
                            playerAvatar = null,
                            extractionPoint = null
                        });
                    }

                    processCount++;
                    if (processCount > 40)
                    {
                        processCount = 0;
                        yield return null; 
                    }
                }

                // ====================================================================
                // FASE 1.5: JUGADORES (Cuadro Holográfico Verde de Alta Tecnología)
                // ====================================================================
                bool allowPlayerScan = RepoScannerPlugin.ConfigInstance == null || 
                                       RepoScannerPlugin.ConfigInstance.EnablePlayerScan == null || 
                                       RepoScannerPlugin.ConfigInstance.EnablePlayerScan.Value;

                if (allowPlayerScan)
                {
                    bool allowSelfScan = RepoScannerPlugin.ConfigInstance != null && 
                                         RepoScannerPlugin.ConfigInstance.EnableSelfScan != null && 
                                         RepoScannerPlugin.ConfigInstance.EnableSelfScan.Value;

                    Color playerColor = RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorPlayer != null
                        ? RepoScannerPlugin.ConfigInstance.ScanColorPlayer.Value
                        : Color.green;

                    foreach (var avatar in FindObjectsOfType<PlayerAvatar>())
                    {
                        if (avatar == null || !avatar.gameObject.activeInHierarchy) continue;

                        bool isSelf = IsSelfAvatar(avatar);

                        if (isSelf && !allowSelfScan)
                        {
                            continue;
                        }

                        float dist = Vector3.Distance(origin, avatar.transform.position);
                        if (dist <= maxDistance)
                        {
                            pendingTargets.Add(new ScanTarget { 
                                physGrab = null, 
                                distance = dist, 
                                color = playerColor, 
                                position = avatar.transform.position,
                                playerAvatar = avatar,
                                extractionPoint = null
                            });
                        }
                    }
                }

                // ====================================================================
                // FASE 1.6: PUNTOS DE EXTRACCIÓN (Activos en Cyan, Inactivos en Gris)
                // ====================================================================
                bool allowExtractionScan = RepoScannerPlugin.ConfigInstance == null || 
                                           RepoScannerPlugin.ConfigInstance.EnableExtractionScan == null || 
                                           RepoScannerPlugin.ConfigInstance.EnableExtractionScan.Value;

                if (allowExtractionScan)
                {
                    bool showInactive = RepoScannerPlugin.ConfigInstance == null ||
                                        RepoScannerPlugin.ConfigInstance.ShowInactiveExtractions == null ||
                                        RepoScannerPlugin.ConfigInstance.ShowInactiveExtractions.Value;

                    Color activeColor = (RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorExtraction != null)
                        ? RepoScannerPlugin.ConfigInstance.ScanColorExtraction.Value
                        : new Color(0f, 0.96f, 1f, 1f);

                    Color inactiveColor = (RepoScannerPlugin.ConfigInstance != null && RepoScannerPlugin.ConfigInstance.ScanColorExtractionInactive != null)
                        ? RepoScannerPlugin.ConfigInstance.ScanColorExtractionInactive.Value
                        : new Color(0.5f, 0.5f, 0.5f, 0.8f);

                    foreach (var ep in FindObjectsOfType<ExtractionPoint>())
                    {
                        if (ep == null || !ep.gameObject.activeInHierarchy) continue;

                        bool isInactive = IsExtractionPointInactive(ep);

                        if (isInactive && !showInactive)
                            continue;

                        float dist = Vector3.Distance(origin, ep.transform.position);
                        if (dist <= maxDistance)
                        {
                            Color epColor = isInactive ? inactiveColor : activeColor;

                            pendingTargets.Add(new ScanTarget { 
                                physGrab = null, 
                                distance = dist, 
                                color = epColor, 
                                position = ep.transform.position,
                                playerAvatar = null,
                                extractionPoint = ep
                            });
                        }
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
                    var target = pendingTargets[i];
                    
                    // Seguimiento dinámico en tiempo real para jugadores en movimiento
                    float targetDist = target.distance;
                    if (target.playerAvatar != null)
                    {
                        if (!target.playerAvatar.gameObject.activeInHierarchy)
                        {
                            pendingTargets.RemoveAt(i);
                            continue;
                        }
                        targetDist = Vector3.Distance(origin, target.playerAvatar.transform.position);
                    }

                    if (currentRadius >= targetDist)
                    {
                        if (target.playerAvatar != null)
                        {
                            TriggerPlayerUIBox(target.playerAvatar, target.color, cfg.ScanDuration);
                        }
                        else if (target.extractionPoint != null)
                        {
                            TriggerExtractionUIBox(target.extractionPoint, target.color, cfg.ScanDuration);
                        }
                        else if (target.physGrab != null)
                        {
                            TriggerNativeUIBox(target.color, target.physGrab, cfg.ScanDuration);
                        }
                        else
                        {
                            SpawnMarker(target.position, target.color);
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

        private static readonly System.Reflection.FieldInfo _isCompletedRightAwayField = 
            typeof(ExtractionPoint).GetField("isCompletedRightAway", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        private static bool IsExtractionPointInactive(ExtractionPoint ep)
        {
            if (ep == null) return false;
            try
            {
                if (_isCompletedRightAwayField != null)
                {
                    return (bool)_isCompletedRightAwayField.GetValue(ep);
                }
                return Traverse.Create(ep).Field<bool>("isCompletedRightAway").Value;
            }
            catch
            {
                return false;
            }
        }

        private bool IsSelfAvatar(PlayerAvatar avatar)
        {
            if (avatar == null) return false;

            // 1. Método nativo del juego para obtener el avatar local
            try
            {
                PlayerAvatar localAvatar = SemiFunc.PlayerAvatarLocal();
                if (localAvatar != null && localAvatar == avatar) return true;
            }
            catch { }

            // 2. Instancia singleton del avatar del jugador
            try
            {
                if (PlayerAvatar.instance != null && PlayerAvatar.instance == avatar) return true;
            }
            catch { }

            // 3. Controlador local del jugador
            try
            {
                if (PlayerController.instance != null && PlayerController.instance.playerAvatarScript == avatar) return true;
            }
            catch { }

            // 4. PhotonView perteneciente al cliente local
            try
            {
                if (avatar.photonView != null && avatar.photonView.IsMine) return true;
            }
            catch { }

            // 5. Verificación de jerarquía y cámara
            try
            {
                if (Camera.main != null && Camera.main.transform.IsChildOf(avatar.transform)) return true;
                if (avatar.transform.root == this.transform.root) return true;
            }
            catch { }

            return false;
        }

        private void TriggerPlayerUIBox(PlayerAvatar avatar, Color markerColor, float waveDuration)
        {
            if (avatar == null || !avatar.gameObject.activeInHierarchy) return;

            if (ValuableDiscover.instance != null)
            {
                try
                {
                    // Buscar o crear el proxy exclusivo que sigue al jugador y le da dimensiones reales
                    ScannerPlayerProxy proxy = avatar.GetComponentInChildren<ScannerPlayerProxy>();
                    if (proxy == null)
                    {
                        GameObject proxyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        proxyObj.name = "ScannerPlayerProxy";
                        
                        // Eliminar inmediatamente el collider para cero interferencia física
                        Collider col = proxyObj.GetComponent<Collider>();
                        if (col != null) Destroy(col);

                        // Anclar como hijo del avatar a la altura del torso
                        proxyObj.transform.SetParent(avatar.transform, false);
                        proxyObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                        proxyObj.transform.localScale = new Vector3(0.85f, 1.8f, 0.85f);

                        // Renderizador completamente transparente para que ValuableDiscoverGraphic calcule los límites
                        Renderer rend = proxyObj.GetComponent<Renderer>();
                        Material invMat = new Material(Shader.Find("GUI/Text Shader"));
                        invMat.color = Color.clear;
                        rend.material = invMat;
                        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        rend.receiveShadows = false;

                        PhysGrabObject pgo;
                        try
                        {
                            ScanControllerPatches.shouldSuppressPhysGrabObjectAwake = true;
                            pgo = proxyObj.AddComponent<PhysGrabObject>();
                            pgo.enabled = false;
                            pgo.centerPoint = proxyObj.transform.position;
                        }
                        finally
                        {
                            ScanControllerPatches.shouldSuppressPhysGrabObjectAwake = false;
                        }

                        proxy = proxyObj.AddComponent<ScannerPlayerProxy>();
                        proxy.pgo = pgo;
                        proxy.invMat = invMat;
                    }

                    if (proxy != null && proxy.pgo != null)
                    {
                        // Sincronizar inmediatamente la posición en el mundo
                        proxy.pgo.centerPoint = proxy.transform.position;

                        ValuableDiscoverCustom custom = proxy.GetComponent<ValuableDiscoverCustom>();
                        if (custom == null)
                        {
                            GameObject go = new GameObject("ScannerPlayerCustomDiscover");
                            go.transform.SetParent(proxy.transform);
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

                            ValuableDiscover.instance.New(proxy.pgo, ValuableDiscoverGraphic.State.Custom, custom);
                            return;
                        }
                    }
                }
                catch { }
            }

            // Respaldo de seguridad si ValuableDiscover no estuviera listo
            SpawnMarker(avatar.transform.position, markerColor);
        }

        private void TriggerExtractionUIBox(ExtractionPoint ep, Color markerColor, float waveDuration)
        {
            if (ep == null || !ep.gameObject.activeInHierarchy) return;

            if (ValuableDiscover.instance != null)
            {
                try
                {
                    Transform proxyTransform = ep.transform.Find("ScannerExtractionProxy");
                    PhysGrabObject pgo = null;
                    ValuableDiscoverCustom custom = null;

                    if (proxyTransform == null)
                    {
                        GameObject proxyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        proxyObj.name = "ScannerExtractionProxy";

                        // Eliminar collider inmediatamente para evitar cualquier colisión o interferencia física
                        Collider col = proxyObj.GetComponent<Collider>();
                        if (col != null) Destroy(col);

                        // Anclar centrado a la altura media de la estación de extracción
                        proxyObj.transform.SetParent(ep.transform, false);
                        proxyObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                        proxyObj.transform.localScale = new Vector3(2.6f, 3.0f, 2.6f);

                        // Renderizador completamente transparente para que el HUD calcule los límites 3D
                        Renderer rend = proxyObj.GetComponent<Renderer>();
                        Material invMat = new Material(Shader.Find("GUI/Text Shader"));
                        invMat.color = Color.clear;
                        rend.material = invMat;
                        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        rend.receiveShadows = false;

                        try
                        {
                            ScanControllerPatches.shouldSuppressPhysGrabObjectAwake = true;
                            pgo = proxyObj.AddComponent<PhysGrabObject>();
                            pgo.enabled = false;
                            pgo.centerPoint = proxyObj.transform.position;
                        }
                        finally
                        {
                            ScanControllerPatches.shouldSuppressPhysGrabObjectAwake = false;
                        }

                        GameObject discoverGo = new GameObject("ScannerExtractionCustomDiscover");
                        discoverGo.transform.SetParent(proxyObj.transform, false);
                        discoverGo.transform.localPosition = Vector3.zero;

                        try
                        {
                            ScanControllerPatches.shouldSuppressValuableDiscoverCustomAwake = true;
                            custom = discoverGo.AddComponent<ValuableDiscoverCustom>();
                        }
                        finally
                        {
                            ScanControllerPatches.shouldSuppressValuableDiscoverCustomAwake = false;
                        }
                    }
                    else
                    {
                        pgo = proxyTransform.GetComponent<PhysGrabObject>();
                        custom = proxyTransform.GetComponentInChildren<ValuableDiscoverCustom>();
                        if (pgo != null)
                        {
                            pgo.centerPoint = proxyTransform.position;
                        }
                    }

                    if (pgo != null && custom != null)
                    {
                        custom.colorCorner = markerColor;
                        custom.colorMiddle = new Color(markerColor.r * 0.4f, markerColor.g * 0.4f, markerColor.b * 0.4f, 0.3f);
                        custom.waitTime = Mathf.Max(4.0f, waveDuration);

                        ValuableDiscover.instance.New(pgo, ValuableDiscoverGraphic.State.Custom, custom);
                        return;
                    }
                }
                catch { }
            }

            // Respaldo de seguridad si ValuableDiscover no estuviera listo
            SpawnMarker(ep.transform.position + Vector3.up * 1.5f, markerColor);
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

    [HarmonyPatch]
    public static class ScanControllerPatches
    {
        public static bool shouldSuppressValuableDiscoverCustomAwake = false;
        public static bool shouldSuppressPhysGrabObjectAwake = false;

        [HarmonyPatch(typeof(ValuableDiscoverCustom), "Awake")]
        [HarmonyPrefix]
        public static bool SuppressValuableDiscoverCustomAwake()
        {
            return !shouldSuppressValuableDiscoverCustomAwake;
        }

        [HarmonyPatch(typeof(PhysGrabObject), "Awake")]
        [HarmonyPrefix]
        public static bool SuppressPhysGrabObjectAwake()
        {
            return !shouldSuppressPhysGrabObjectAwake;
        }
    }
}
