# Runbook: Configuración de Iluminación URP, Niebla y Ventisca Ciega

---

## 1. Verificación del Render Pipeline (`PC_RPAsset`)

1. Abrir `Assets/Settings/PC_RPAsset.asset`.
2. Confirmar los siguientes ajustes para soportar las luces de la aldea y las guirnaldas navideñas:
   * `HDR`: **Activado** (indispensable para el *Bloom* de la forja y los cristales).
   * `Main Light`: `Cast Shadows = true`, `Shadow Resolution = 2048`.
   * `Additional Lights`: `Per Pixel`, `Per Object Limit = 8`, `Cast Shadows = true` (para el Faro Frontal LED del casco del jugador).

---

## 2. Configuración del `Global Volume` en Escena

1. Seleccionar el objeto `Global Volume` en la escena (perfil `SampleSceneProfile.asset`).
2. Verificar que contenga los siguientes **Volume Overrides**:
   * **Bloom:**
     * `Threshold = 0.9`
     * `Intensity = 0.65`
     * `High Quality Filtering = true`
   * **Vignette (Efecto de Congelamiento):**
     * `Color = #5FA8D3` (Cian escarcha)
     * `Smoothness = 0.4`
     * `Intensity = 0.0` por defecto (modificado dinámicamente por `TemperatureSystem.cs` cuando la temperatura baja de `30 °T`).
   * **Color Adjustments:**
     * `Post Exposure = 0.15`
     * `Contrast = 12`
     * `Saturation = 10`

---

## 3. Configuración de la Niebla Dinámica (`Window > Rendering > Lighting > Environment`)

1. En la pestaña **Environment**, bajo **Other Settings**:
   * Activar la casilla **`Fog`**.
   * `Mode`: **`Exponential Squared`**.
   * `Color`: `#1B4965` (interpolado entre día, noche y ventisca por `DayNightCycleManager.cs`).
   * `Density`: `0.008` (estado base despejado) ➔ sube a `0.065` durante el evento **Ventisca Ciega**.
