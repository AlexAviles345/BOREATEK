# Especificación de Shaders (Shader Graph) y Efectos Visuales (VFX)

---

## 1. Shaders Personalizados en URP Shader Graph

Aunque el `80 %` de los assets utiliza el shader estándar `Universal Render Pipeline/Lit`, el proyecto contempla **3 shaders estilizados clave** en `Assets/_Project/Art/Shaders/`:

### 1.1. `SG_StylizedCrystal_Emissive` (Cristal de Caramelo y Punta del Pico)
* **Objetivo:** Dar vida al **Bosque de Caramelos Gigantes**, a las vetas rojas de Nivel 1 y al **Pico Industrial con Cristal Energético Rojo**.
* **Características:**
  * Nodo `Fresnel Effect` multiplicado por color HDR rojo carmesí (`#E63946` con intensidad `2.5`) conectado al canal `Emission`.
  * Ligera variación sinusoidal en el tiempo (`Time` ➔ `Sine`) para simular un pulso energético interno en las vetas y núcleos.

### 1.2. `SG_TranslucentBlueIce` (La Geoda de Hielo Azul y Cofres Congelados)
* **Objetivo:** Representar las paredes facetadas de **La Geoda de Hielo Azul (Nivel 2)** y el bloque de hielo que sella los **Cofres Congelados**.
* **Características:**
  * Superficie `Opaque` o `Transparent` con falso *Subsurface Scattering* (gradiente de profundidad entre `#1B4965` y `#5FA8D3`).
  * Parámetro expuesto `_MeltProgress` (`0.0` a `1.0`) para que el hielo de los **Cofres Congelados** se disuelva o reduzca suavemente mientras recibe calor de una Baliza Térmica.

### 1.3. `SG_FestiveMagmaFlow` (El Abismo del Núcleo Luminoso — Nivel 3)
* **Objetivo:** Animar la fisura vertical incandescente de **El Abismo del Núcleo Luminoso**.
* **Características:**
  * Desplazamiento UV (`Panner`) vertical con máscara de ruido estilizado (*Voronoi/Simple Noise*) combinando roca oscura (`#1A1A1D`) con ríos de magma dorado/naranja HDR (`#FF5400` y `#FF9F1C`).

---

## 2. Catálogo de Efectos de Partículas (Particle Systems)

| ID | Nombre del Prefab VFX | Sistema que lo Activa | Descripción Visual |
| :--- | :--- | :--- | :--- |
| **VFX-01** | `VFX_SnowStorm_Ambient` | `DayNightCycleManager.cs` | Copos de nieve suaves en exterior; durante la **Ventisca Ciega** aumenta su tasa de emisión (`Rate over Time: 400`) y velocidad horizontal de viento. |
| **VFX-02** | `VFX_HeatBubble_Ring` | `HeatSourceNode.cs` | Anillo sutil de partículas cálidas ascendentes color ámbar que delimita visualmente en el suelo el radio seguro de una Guirnalda o Baliza Térmica. |
| **VFX-03** | `VFX_MiningHit_Sparks` | `HarvestableNode.cs` | Esquirlas geométricas low-poly al golpear un nodo con el pico (astillas blancas en pinos, fragmentos rojos en caramelo, chispas azules en hierro/hielo). |
| **VFX-04** | `VFX_DefectiveToy_Sparks` | `DefectiveToyAI.cs` | Chispas eléctricas intermitentes y humo de vapor oxidado que brotan del cuello y articulaciones del **Soldado de Juguete Defectuoso**. |
| **VFX-05** | `VFX_Firecracker_Explosion` | `FestiveFirecracker.cs` | Explosión festiva con destellos multicolores navideños y onda expansiva al detonar **Petardos Festivos** contra muros de roca o hielo duro. |
