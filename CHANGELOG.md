# Changelog / Registro de Cambios

All notable changes to the **Repo Terrain Scanner** mod will be documented in this file.  
Todos los cambios notables del mod **Repo Terrain Scanner** se documentarán en este archivo.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).  
El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto sigue [Versionado Semántico](https://semver.org/lang/es/).

---

## English

### [3.1.0] - 2026-11-08

#### Added
* **Tactical Sonar Audio:** Integrated custom sonar sound effect (`scansound.bundle` / `darkSonar.wav`). Plays crisp, centered 2D audio for the local player and 3D positional audio across the facility for remote teammates in multiplayer. Includes volume and toggle controls in the configuration.
* **Configurable Cooldown:** Added `ScanCooldown` (Default: `6.0` seconds) to prevent spamming the scanner and audio. Can be adjusted freely to match user preference.
* **Multiplayer Sonar Audio Filter:** Added `EnableRemotePlayerSound` (Default: `false`) allowing players to decide whether they want to hear teammates' sonar pings in 3D or only their own scan audio.
* **Extraction Point Scanning:** The scanner now detects extraction terminals (`ExtractionPoint`) generated across the facility and projects tactical holographic frames around them.
  * Active/Ready extraction points are displayed in **Electric Cyan** (`#00F6FF`).
  * Inactive/Completed extraction points (`isCompletedRightAway`) are displayed in **Dim Gray** (`#808080`).
* **Independent Category Toggles:** Added individual toggles in the config file to enable or disable scanning for each specific entity type (`EnableEnemyScan`, `EnableValuableScan`, `EnableCosmeticScan`, `EnableItemScan`, `EnablePlayerScan`, `EnableExtractionScan`, `ShowInactiveExtractions`). All are enabled (`true`) by default.
* **Custom Category Colors:** Every entity type now has its own configurable holographic color in the `[Style]` section (`ScanColorEnemy`, `ScanColorValuable`, `ScanColorCosmetic`, `ScanColorItem`, `ScanColorPlayer`, `ScanColorExtraction`, `ScanColorExtractionInactive`).
* **Bilingual Configuration:** The entire BepInEx `.cfg` file now provides full bilingual descriptions in English and Spanish (`[EN] ... / [ES] ...`).
* **Real-Time Config Reloading:** Configuration changes made in `com.mod.repoterrainscanner.cfg` are reloaded on each scan press, allowing settings to update on the fly without restarting the game.

#### Fixed
* **Accurate Local Player Identification:** Fixed an issue where the local player was identified using mismatched transform roots; now utilizes native `SemiFunc.PlayerAvatarLocal()`, `PlayerAvatar.instance`, and `PhotonView.IsMine` for reliable self-scan toggling.

---

### [3.0.5] - 2026-11-07

#### Added
* **Dynamic Holographic Player/Ally Frames:** Living players and teammates are now tracked with native, high-tech discovery holographic boxes (`ValuableDiscoverCustom`) that scale and follow their movement dynamically in real time.
* **Self-Scan Configuration (Testing Option):** Added a new configuration option `EnableSelfScan` (Default: `true`) under the `[Experimental]` section in the config file. This allows players to toggle self-detection when testing with third-person perspective mods.  
  *> **Note:** This option is strictly temporary for testing purposes and will be removed in a future update.*

#### Fixed
* **Map Origin Spawning Bug:** Resolved an issue where player discovery markers were incorrectly placed at the world origin `(0, 0, 0)` (map entrance door) due to inactive ragdoll components.

---

### [3.0.4] - 2026-11-05

#### Added
* **Cart Filtering (QoL):** Items placed inside extraction carts (`PhysGrabObjectImpactDetector.inCart`) are now ignored by the wave detection. This eliminates yellow clutter and marker overlap once loot has been secured.
* **Client-Side Discovery Isolation:** Discovery holographic frames (`ValuableDiscoverCustom`) and player markers are now rendered strictly on the client that fired the scan.

#### Changed
* **Multiplayer Performance Optimization:** Remote clients in the room still see and render the expanding wave shader across the level, but skip the `Physics.OverlapSphere` physics query completely. This saves CPU time and prevents HUD clutter on teammates' screens.

---

### [3.0.3] - 2026-11-04

#### Changed
* Minor internal stability adjustments to component discovery initialization.
* Refined discovery marker binding on dynamic physics bodies.

---

### [3.0.2] - 2026-11-04

#### Fixed
* Corrected repository URL in `manifest.json` pointing directly to the public GitHub repository.
* Resolved Thunderstore package rejection regarding open-source licensing.

#### Added
* Explicit credits and attributions in `README.md` for Kistras' *REPO-Scanner* (MPL-2.0) and the original *TerrainScanner* post-processing shader (GPL).
* Included shader source files (`HLSL`/`ShaderLab`) in the GitHub repository.

---

### [3.0.1] - 2026-11-04

#### Added
* Embedded official `LICENSE` file (MPL-2.0) directly into the package root.
* Cleaned Git repository index to exclude build output folders (`bin/`, `obj/`).

---

### [3.0.0] - 2026-11-03

#### Added
* Initial modular release of **Repo Terrain Scanner** for *R.E.P.O.*
* Integration with the game's native UI holographic discovery system (`ValuableDiscover`).
* Dynamic expanding post-processing wave shader.
* Full multiplayer synchronization using custom Photon PUN RPC network events.
* Color classification hierarchy: Enemies (Red), Teammates (Green), Valuables/Scrap (Yellow), Cosmetics (Blue), Tools/Weapons (Purple).
* Runtime configuration file via BepInEx for custom colors, scan radius, wave duration, and keybindings.

---

## Español

### [3.1.0] - 2026-11-08

#### Añadido
* **Efecto de Sonido Sonar Táctico:** Se integró un sonido de sonar personalizado (`scansound.bundle` / `darkSonar.wav`). Se reproduce en audio 2D directo y claro para el jugador local y en audio 3D posicional para los compañeros en multijugador. Incluye control de volumen e interruptor en la configuración.
* **Tiempo de Enfriamiento (Cooldown) Configurable:** Se agregó la opción `ScanCooldown` (Por defecto: `6.0` segundos) para evitar el spam del escaneo y la repetición molesta del audio. Se puede ajustar libremente a cualquier valor.
* **Filtro de Sonar en Multijugador:** Se agregó la opción `EnableRemotePlayerSound` (Por defecto: `false`) que permite elegir si deseas escuchar el sonar de los demás jugadores en 3D o solo el tuyo propio.
* **Escaneo de Puntos de Extracción:** El escáner ahora detecta las terminales de extracción (`ExtractionPoint`) generadas por el mapa y proyecta recuadros holográficos tácticos a su alrededor.
  * Los puntos activos/disponibles se muestran en **Cyan Eléctrico** (`#00F6FF`).
  * Los puntos inactivos/completados (`isCompletedRightAway`) se muestran en **Gris Apagado** (`#808080`).
* **Interruptores de Categorías Independientes:** Se agregaron opciones en el archivo de configuración para activar o desactivar individualmente el escaneo de cada tipo de entidad (`EnableEnemyScan`, `EnableValuableScan`, `EnableCosmeticScan`, `EnableItemScan`, `EnablePlayerScan`, `EnableExtractionScan`, `ShowInactiveExtractions`). Todos vienen activados (`true`) por defecto.
* **Colores Holográficos Personalizables:** Cada categoría ahora cuenta con su propio ajuste de color en la sección `[Style]` (`ScanColorEnemy`, `ScanColorValuable`, `ScanColorCosmetic`, `ScanColorItem`, `ScanColorPlayer`, `ScanColorExtraction`, `ScanColorExtractionInactive`).
* **Configuración Bilingüe:** Todo el archivo de configuración `.cfg` ahora incluye descripciones claras tanto en inglés como en español (`[EN] ... / [ES] ...`).
* **Recarga de Configuración en Tiempo Real:** Las modificaciones en `com.mod.repoterrainscanner.cfg` se recargan al presionar el escáner, aplicando cambios al vuelo sin reiniciar la partida.

#### Corregido
* **Identificación Precisa del Jugador Local:** Se solucionó el problema por el cual el jugador local no era reconocido debido a raíces de transform separadas; ahora utiliza las funciones nativas `SemiFunc.PlayerAvatarLocal()`, `PlayerAvatar.instance` y `PhotonView.IsMine` para un control fiable del auto-escaneo.

---

### [3.0.5] - 2026-11-07

#### Añadido
* **Recuadros Holográficos Dinámicos en Jugadores/Aliados:** Los compañeros vivos y el propio jugador ahora se resaltan con los recuadros holográficos nativos de alta tecnología (`ValuableDiscoverCustom`), los cuales escalan y siguen su cuerpo en tiempo real a donde vayan.
* **Configuración de Auto-Escaneo (Opción de Prueba):** Se agregó la opción de configuración `EnableSelfScan` (Por defecto: `true`) bajo la sección `[Experimental]` del archivo de configuración. Permite activar o desactivar que el escáner te detecte a ti mismo para probarlo con mods de tercera persona.  
  *> **Aviso:** Esta opción es estrictamente temporal con fines de prueba y se quitará en una próxima versión.*

#### Corregido
* **Error de Marcadores en (0,0,0):** Se solucionó el problema por el cual los cuadros de jugadores aparecían flotando estáticos en la puerta de inicio del mapa `(0, 0, 0)` a causa de componentes de ragdoll inactivos.

---

### [3.0.4] - 2026-11-05

#### Añadido
* **Filtro de Carrito (QoL):** Los objetos guardados dentro de los carritos de extracción (`PhysGrabObjectImpactDetector.inCart`) ahora son ignorados por el escáner. Esto elimina por completo el manchón amarillo y la saturación visual al acumular botín.
* **Aislamiento de Hologramas en Local:** Los recuadros holográficos nativos (`ValuableDiscoverCustom`) y los marcadores de jugadores solo se activan en la pantalla de quien dispara el escáner.

#### Modificado
* **Optimización en Red (Multijugador):** Los compañeros de equipo en la sala siguen viendo el shader de la onda expandirse en tiempo real por el mapa, pero su juego se salta el cálculo de físicas (`Physics.OverlapSphere`). Esto ahorra uso de CPU y mantiene su pantalla libre de marcadores no deseados.

---

### [3.0.3] - 2026-11-04

#### Modificado
* Ajustes internos menores en la inicialización de componentes de descubrimiento.
* Mayor estabilidad al generar marcadores temporales en objetos físicos dinámicos.

---

### [3.0.2] - 2026-11-04

#### Corregido
* Se corrigió la URL del repositorio en `manifest.json` apuntando al repositorio público de GitHub.
* Se resolvió el rechazo de publicación en Thunderstore por cumplimiento de licencias libres.

#### Añadido
* Créditos explícitos en `README.md` hacia *REPO-Scanner* de Kistras (MPL-2.0) y el shader original *TerrainScanner* (GPL).
* Se incluyó el código fuente del shader (`HLSL`/`ShaderLab`) en el repositorio público.

---

### [3.0.1] - 2026-11-04

#### Añadido
* Se incluyó el archivo oficial `LICENSE` (MPL-2.0) en la raíz del paquete `.zip`.
* Limpieza del índice de Git para excluir carpetas de compilación (`bin/`, `obj/`).

---

### [3.0.0] - 2026-11-03

#### Añadido
* Lanzamiento inicial modular de **Repo Terrain Scanner** para *R.E.P.O.*
* Integración directa con los cuadros holográficos nativos de la interfaz (`ValuableDiscover`).
* Shader de post-procesamiento con onda expansiva dinámica.
* Sincronización en red multijugador mediante eventos RPC de Photon PUN.
* Sistema de colores táctico: Enemigos (Rojo), Aliados (Verde), Botín/Chatarra (Amarillo), Cosméticos (Azul), Herramientas/Armas (Morado).
* Archivo de configuración BepInEx en tiempo de ejecución para colores, distancia, duración y tecla de activación.
