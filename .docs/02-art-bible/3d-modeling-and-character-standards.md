# Estándares de Modelado 3D, Escala y Personajes

---

## 1. Reglas de Proporción y Anatomía Estilizada

Siguiendo el diseño aprobado en [`concept-art/personajes/concepto_duende_minero.png`](concept-art/personajes/concepto_duende_minero.png):

* **Escala en Motor:** `1 unidad de Unity = 1 metro real`.
* **Altura del Protagonista (Duende Ingeniero):** `1.20 m` (incluyendo el casco minero).
  * Proporción cabeza-cuerpo: **1:3.5** (cabeza grande expresiva para que el casco con faro frontal, las orejas puntiagudas y las expresiones faciales sean claramente legibles tanto en tercera persona como en retratos de UI).
  * Silueta de equipamiento claramente segmentada:
    1. **Cabeza:** Casco metálico verde oliva con faro circular de latón frontal y orejas laterales sobresalientes.
    2. **Torso:** Abrigo térmico acolchado con patrón de rombos grandes, cuello y puños de borrego grueso, y bolsillos frontales de cuero.
    3. **Espalda:** Tanque-mochila cilíndrico de latón con **2 manómetros laterales** de agujas grandes y válvulas superiores.
    4. **Extremidades:** Guantes tejidos gruesos y botas de nieve robustas con suela dentada.
    5. **Herramienta:** Pico industrial de acero con **dos puntas de Cristal de Caramelo rojo** emisivo.
* **Altura del Soldado de Juguete Defectuoso:** `1.85 m` (imponente frente al duende de `1.20 m`, acentuando la amenaza física de los antiguos autómatas de tamaño humano).

---

## 2. Presupuesto Poligonal (Polycount Budget) y Texturas

Para mantener `60+ FPS` constantes en PC (y compatibilidad futura con el perfil URP Mobile) aún con decenas de nodos, guirnaldas y cintas transportadoras en pantalla:

| Categoría de Modelo 3D | Triángulos Máximos (LOD0) | Resolución de Textura | Formato de Exportación |
| :--- | :---: | :---: | :--- |
| **Protagonista (Duende Ingeniero + Equipo)** | `8,000 - 12,000 tris` | `2048 x 2048` (Albedo, Normal, Mask/Emission) | `.FBX` (Humanoid o Generic Rig) |
| **Soldado de Juguete Defectuoso / Gólem** | `5,000 - 9,000 tris` | `1024 x 1024` o `2048 x 2048` | `.FBX` |
| **Duende Mecánico Extractor (Autómata Aliado)** | `2,500 - 4,500 tris` | `1024 x 1024` | `.FBX` |
| **Árboles (Pinos Cristalizados / Caramelos Gigantes)** | `800 - 2,500 tris` | `1024 x 1024` / Atlas compartido | `.FBX` |
| **Vetas Minerales y Cofres Congelados** | `400 - 1,500 tris` | `1024 x 1024` | `.FBX` |
| **Tramos de Túnel de Ventilación y Cintas** | `600 - 2,000 tris` | Trim Sheet `2048 x 2048` | `.FBX` (Modular cada `2m` o `4m`) |
| **Guirnaldas, Balizas y Consumibles** | `150 - 600 tris` | Atlas `1024 x 1024` | `.FBX` |

---

## 3. Checklist Técnico antes de Exportar de Blender / Maya a Unity

1. **Orientación de Ejes:** `+Z` hacia adelante (*Forward*), `+Y` hacia arriba (*Up*).
2. **Transformaciones Aplicadas:** `Scale = (1, 1, 1)`, `Rotation = (0, 0, 0)` (Aplicar *Freeze/Apply All Transforms* antes de exportar).
3. **Ubicación del Pivote:**
   * Personajes, enemigos, árboles, cofres y edificios: **en la base central a nivel del suelo `(0, 0, 0)`**.
   * Herramientas equipables (Pico, Termo, Bastón de Menta): **en el punto exacto de agarre de la mano derecha**.
   * Módulos de túneles y cintas transportadoras: **en el extremo de conexión inicial** para facilitar el *Grid Snapping* en Unity.
4. **Nomenclatura de Mallas y Materiales:**
   * Prefijos obligatorios: `SM_` (Static Mesh), `SK_` (Skeletal Mesh), `M_` (Material), `T_` (Texture). Ejemplo: `SK_ElfEngineer`, `SM_CandyTree_01`, `M_CrystalRed_Emissive`.
