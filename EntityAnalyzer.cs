using UnityEngine;

namespace RepoTerrainScanner
{
    public static class EntityAnalyzer
    {
        public static Color Analyze(Collider hit, out Transform logicalTarget)
        {
            // Agrupamos por el Rigidbody (el cuerpo físico real) para no detectar piezas sueltas
            logicalTarget = hit.attachedRigidbody != null ? hit.attachedRigidbody.transform : hit.transform;

            // --- 1. ENEMIGOS Y JUGADORES (Clases Maestras) ---
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                logicalTarget = enemyHealth.transform;
                return Color.red;
            }

            var playerCtrl = hit.GetComponentInParent<PlayerController>();
            if (playerCtrl != null)
            {
                logicalTarget = playerCtrl.transform;
                return Color.green;
            }

            // --- 2. BÚSQUEDA SEGURA DE SCRIPTS (Para el Multijugador) ---
            // Buscamos solo en los scripts, NUNCA en el nombre del objeto (adiós a las escaleras "PlayerClip")
            Component[] parents = hit.GetComponentsInParent<Component>(false);
            foreach (var comp in parents)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;

                // Si es el script de un amigo en línea
                if (typeName == "NetworkPlayer" || typeName == "PlayerAvatar" || typeName == "PlayerHealth")
                {
                    logicalTarget = comp.transform;
                    return Color.green;
                }

                // Si es un monstruo raro (arañas, ciempiés, etc. que terminan en "Enemy")
                if (typeName == "EnemyAI" || typeName == "EnemyRigidbody" || typeName.EndsWith("Enemy"))
                {
                    logicalTarget = comp.transform;
                    return Color.red;
                }
            }


            // --- 3. ITEMS Y VALUABLES (Amarillo) ---
            // Si pasamos los filtros de arriba, estamos 100% seguros de que NO es un ser vivo.
            
            var valObj = hit.GetComponentInParent<ValuableObject>();
            if (valObj != null)
            {
                logicalTarget = valObj.transform;
                return Color.yellow;
            }

            var valCustom = hit.GetComponentInParent<ValuableDiscoverCustom>();
            if (valCustom != null)
            {
                logicalTarget = valCustom.transform;
                return Color.yellow;
            }

            var physObj = hit.GetComponentInParent<PhysGrabObject>();
            if (physObj != null)
            {
                logicalTarget = physObj.transform;
                
                // Filtro para que los muebles no brillen
                string hitName = hit.gameObject.name.ToLower();
                string scriptName = physObj.gameObject.name.ToLower();
                string parentName = physObj.transform.parent != null ? physObj.transform.parent.name.ToLower() : "";

                if (ContainsBadWord(hitName) || ContainsBadWord(scriptName) || ContainsBadWord(parentName))
                {
                    return Color.clear;
                }
                
                return Color.yellow;
            }

            // Si no es un monstruo, ni un jugador, ni un item... se ignora (paredes, pisos, etc.)
            return Color.clear;
        }

        private static bool ContainsBadWord(string name)
        {
            return name.Contains("door") || 
                   name.Contains("cabinet") || 
                   name.Contains("drawer") || 
                   name.Contains("hinge") || 
                   name.Contains("locker") ||
                   name.Contains("closet");
        }
    }
}