# Runbook: Cómo Integrar un Nuevo Modelo 3D y Nodo Recolectable

Sigue esta guía paso a paso cada vez que se incorpore un nuevo recurso (ej. *Árbol de Caramelo Gigante*, *Veta de Hierro Polar* o *Cofre Congelado*) al proyecto.

---

## Paso 1: Importación del Modelo 3D Estilizado (`.fbx`)
1. Guardar el archivo `.fbx` en `Assets/_Project/Art/Meshes/Environment/`.
2. En el Inspector de importación (*Model Import Settings*):
   * `Scale Factor`: `1` (`1 unidad = 1 metro`).
   * `Mesh Compression`: `Low` o `Medium`.
   * `Read/Write`: Desactivado (a menos que requiera deformación por código).
   * En la pestaña *Materials*, elegir `Location: Use External Materials (Legacy)` o remapear a los materiales de `Assets/_Project/Art/Materials/`.

## Paso 2: Configuración del Material URP Estilizado
1. Crear un Material en `Assets/_Project/Art/Materials/` con prefijo `M_` (ej. `M_CandyCrystal_Red`).
2. Seleccionar el shader `Universal Render Pipeline/Lit` (o `SG_StylizedCrystal_Emissive`).
3. Asignar la textura base y, si es un cristal energético o foco festivo, marcar **Emission** con color HDR (`#E63946`, intensidad `2.0`).

## Paso 3: Creación de los Datos (`ScriptableObject`)
1. En `Assets/_Project/Data/Resources/`, clic derecho `Create > Boreatek > Harvestable Node Config`.
2. Configurar:
   * `Required Pickaxe Tier`: `1`, `2` o `3`.
   * `Max Hit Points`: Golpes necesarios para extraerlo.
   * `Requires Heat To Melt`: `true` únicamente si es un **Cofre Congelado**.
   * `Gingerbread Coin Value`: Valor al depositarse en la Tolva Central.

## Paso 4: Creación del Prefab Jugable
1. Arrastrar la malla a la escena, asignarle el Layer `Interactable`.
2. Añadir un `CapsuleCollider` o `BoxCollider` ajustado a su silueta.
3. Añadir el script `HarvestableNode.cs` y conectar el `ScriptableObject` del Paso 3.
4. Arrastrar el objeto a `Assets/_Project/Prefabs/Gameplay/` para convertirlo en Prefab.
