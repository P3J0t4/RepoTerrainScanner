# Repo Terrain Scanner

Un escáner táctico de terreno y entidades para *R.E.P.O.* Diseñado para integrarse de forma fluida con la estética nativa del juego, este mod proyecta una onda visual y revela objetivos a través del sistema holográfico oficial (cuadros de descubrimiento) a medida que la onda hace contacto físico con ellos.

Totalmente sincronizado en multijugador y optimizado para ofrecer un rendimiento suave y sin congelamientos.

---

## Características Principales

* **Detección Dinámica por Onda:** La onda visual viaja desde la posición del jugador. Los objetivos no se marcan de golpe: se revelan progresivamente cuando el pulso expansivo los alcanza físicamente.
* **Integración con la UI Nativa:** Sustituye los marcadores toscos por los cuadros de descubrimiento tácticos del propio motor del juego (`ValuableDiscover`), que ahora encuadran y siguen a los jugadores en tiempo real.
* **Rendimiento Cero-Lag:** Utiliza consultas de física en radio local (`Physics.OverlapSphere`) combinadas con procesamiento distribuido por fotogramas (*time-slicing*). Despídete de los tirones o bajadas de FPS al activar la tecla.
* **Filtro Inteligente de Entornos y Carrito:** Reconoce la estructura interna del mapa para evitar marcar puertas o elementos estáticos, e ignora automáticamente los objetos ya depositados en el carrito de extracción.

---

## Código de Colores Táctico

Los objetos detectados se clasifican y resaltan con un código visual intuitivo:

| Color | Categoría | Descripción |
| :--- | :--- | :--- |
| 🔴 **Rojo** | **Enemigos** | Monstruos vivos y amenazas detectadas mediante su cuerpo físico (`EnemyRigidbody`). |
| 🟢 **Verde** | **Aliados / Jugadores** | Compañeros de equipo y jugador local (recuadros holográficos dinámicos que siguen el cuerpo en tiempo real). |
| 🟡 **Amarillo** | **Valuables / Botín** | Chatarra y objetos de valor listos para ser transportados y vendidos (`ValuableObject`). |
| 🔵 **Azul** | **Cosméticos** | Cajas y artículos cosméticos especiales encontrados en la expedición. |
| 🟣 **Morado** | **Herramientas / Armas** | Ítems de supervivencia, armamento y drones caídos en el mapa (`ItemAttributes`). |

---

## Sincronización Online (Multijugador)

* **Sincronización Photon (RPC):** Cuando activas el escáner, los parámetros de origen y estilo de tu onda se transmiten a los demás jugadores de la sala mediante eventos de red.
* **Visibilidad Cruzada:** Tus aliados verán tu pulso visual propagarse por el mapa con la configuración exacta de color y alcance que lleves en tu cliente.

---

## Personalización (Configuración)

Al ejecutar el mod por primera vez, se generará automáticamente el archivo de configuración en `BepInEx/config/com.mod.repoterrainscanner.cfg`.

Puedes modificar los parámetros directamente o mediante un gestor de configuración en tiempo de ejecución:

| Parámetro | Valor por defecto | Descripción |
| :--- | :--- | :--- |
| `ActiveKey` | `C` | Tecla asignada para detonar la onda de escaneo. |
| `EnableEnemyScan` | `true` | Escanea y resalta enemigos (Rojo). |
| `EnableValuableScan` | `true` | Escanea y resalta chatarra y objetos de valor (Amarillo). |
| `EnableCosmeticScan` | `true` | Escanea y resalta objetos cosméticos (Azul). |
| `EnableItemScan` | `true` | Escanea y resalta herramientas, armas e ítems (Morado). |
| `EnablePlayerScan` | `true` | Escanea y resalta compañeros de equipo y jugadores (Verde). |
| `EnableExtractionScan` | `true` | Escanea y resalta puntos de extracción en el mapa (Cyan). |
| `ShowInactiveExtractions` | `true` | Muestra puntos de extracción inactivos o ya completados (Gris). Si es `false`, solo se muestran los activos. |
| `EnableScanSound` | `true` | Activa o desactiva la reproducción del efecto de sonido sonar al escanear. |
| `ScanSoundVolume` | `0.8` | Volumen del efecto de sonido del escáner (de 0.0 a 1.0). |
| `EnableSelfScan` | `true` | *(Experimental / Prueba)* Permite que el escáner te detecte a ti mismo para probar con mods de 3ra persona (se removerá en futuras versiones). |
| `ScanColorEnemy` | `#FF0000` | Color del recuadro holográfico para enemigos. |
| `ScanColorValuable` | `#FFFF00` | Color del recuadro holográfico para chatarra y valiosos. |
| `ScanColorCosmetic` | `#0000FF` | Color del recuadro holográfico para cosméticos. |
| `ScanColorItem` | `#B233FF` | Color del recuadro holográfico para herramientas, armas e ítems. |
| `ScanColorPlayer` | `#00FF00` | Color del recuadro holográfico para jugadores y aliados. |
| `ScanColorExtraction` | `#00F6FF` | Color del recuadro holográfico para puntos de extracción activos. |
| `ScanColorExtractionInactive` | `#808080` | Color del recuadro holográfico para puntos de extracción inactivos/completados. |
| `ScanColorHead` | `#FFD469` | Color del borde frontal de la onda expansiva. |
| `ScanColor` | `#61BADE` | Color del cuerpo/cola de la onda expansiva. |
| `ScanLineWidth` | `1.0` | Grosor visual de la línea del escáner. |
| `ScanLineInterval` | `1.0` | Frecuencia de las subdivisiones de la línea. |
| `HeadScanLineWidth` | `1.0` | Grosor del haz principal delantero. |
| `ScanRange` | `5.0` | Profundidad del campo visual de la onda. |
| `ScanMaxDistance` | `30.0` | Distancia máxima radial que recorrerá el escáner (en metros). |
| `ScanDuration` | `5.0` | Tiempo total (en segundos) que tarda la onda en completar su recorrido. |

---

## Instalación

### Con Mod Manager (Recomendado)
1. Instala el mod directamente desde la aplicación de **Thunderstore** o **r2modman**.
2. Todas las dependencias (`BepInExPack`) se resolverán de forma automática.

### Manual
1. Asegúrate de tener instalado **BepInEx 5.4.21** o superior.
2. Descarga el `.zip` de la última versión del mod.
3. Extrae el contenido y coloca los archivos `RepoTerrainScanner.dll`, `scanwave.bundle` y `scansound.bundle` dentro de tu carpeta `REPO/BepInEx/plugins/`.
4. Inicia el juego.

---

## Créditos y Agradecimientos

* **[TerrainScanner (PEAK)](https://github.com/LIghtJUNction/PeakMods):** Por la lógica del shader de post-procesado, la estética de la onda expansiva y los materiales base del asset bundle portados a *R.E.P.O.* (bajo licencia GPL).
* **[Valuables Scanner](https://github.com/Kistras/REPO-Scanner) (por Kistras):** Bajo licencia [MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/), por la estructura de integración con los cuadros holográficos nativos (`ValuableDiscover`) y el parche de estabilidad de componentes.
