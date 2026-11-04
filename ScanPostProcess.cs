using UnityEngine;
using System.Collections.Generic;

namespace RepoTerrainScanner
{
    [RequireComponent(typeof(Camera))]
    public class ScanPostProcess : MonoBehaviour
    {
        public Material scanMaterial; // Se mantiene por compatibilidad con el inicializador
        
        // Aquí se almacenan todas las ondas que están ocurriendo al mismo tiempo
        public List<Material> activeMaterials = new List<Material>();
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _cam.depthTextureMode |= DepthTextureMode.Depth;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // Si no hay ondas activas, renderizamos la pantalla normal y ahorramos recursos
            if (activeMaterials.Count == 0 || _cam == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, false);
            Matrix4x4 inverseViewProj = (gpuProj * _cam.worldToCameraMatrix).inverse;

            // Creamos dos texturas temporales para hacer el efecto "Ping-Pong" de capas
            RenderTexture rt1 = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);
            RenderTexture rt2 = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);

            Graphics.Blit(src, rt1);
            bool ping = true;

            // Dibujamos cada onda activa, una sobre otra
            for (int i = 0; i < activeMaterials.Count; i++)
            {
                Material mat = activeMaterials[i];
                if (mat == null) continue;

                mat.SetMatrix("_InverseViewProj", inverseViewProj);

                if (ping)
                    Graphics.Blit(rt1, rt2, mat);
                else
                    Graphics.Blit(rt2, rt1, mat);
                
                ping = !ping;
            }

            // Enviamos el resultado final con todas las ondas mezcladas a la pantalla
            Graphics.Blit(ping ? rt1 : rt2, dest);
            RenderTexture.ReleaseTemporary(rt1);
            RenderTexture.ReleaseTemporary(rt2);
        }
    }
}