# Reglas de Rendimiento, Física y Memoria en Unity 6

---

## 1. Caché de Componentes y Búsquedas

* **Prohibido en `Update()` / `FixedUpdate()` / `LateUpdate()`:**
  * ❌ `GetComponent<T>()`
  * ❌ `GameObject.Find()` o `FindObjectOfType<T>()`
  * ❌ `Camera.main` (en su lugar, guardar `private Camera _mainCamera;` en `Awake()`).
  * ❌ Concatenación de strings con `+` para actualizar textos de UI cada frame (solo actualizar el texto de UI cuando el valor entero cambie mediante evento).

---

## 2. Física 3D y Detección de Burbujas de Calor

1. **Matriz de Colisión por Capas (`Layers`):**
   Definir capas específicas en `ProjectSettings/TagManager.asset`:
   * `Layer 6: Player`
   * `Layer 7: Interactable` (Vetas, Pinos, Cofres, Tolva)
   * `Layer 8: HeatZone` (Triggers de Guirnaldas, Balizas y Aldea)
   * `Layer 9: Enemy` (Soldados de Plomo y Gólems)
2. **Consultas Físicas sin Basura en Memoria (`Zero GC Allocation`):**
   * Al usar `Physics.Raycast` desde la cámara del jugador para detectar vetas minerales o cofres, pasar siempre la máscara `_interactableLayerMask` y una distancia máxima (`_interactionRange = 3.5f`).
   * Si una Baliza Térmica busca cofres congelados alrededor, usar `Physics.OverlapSphereNonAlloc(transform.position, _radius, _hitBuffer, _interactableLayerMask)` en lugar de `Physics.OverlapSphere`.

---

## 3. Optimización de URP y Object Pooling

* **Object Pooling para Partículas y Drops:** Los fragmentos de mineral al picar y las chispas de los enemigos deben reutilizarse mediante `UnityEngine.Pool.ObjectPool<T>` (nativo de Unity) en lugar de llamar a `Instantiate` y `Destroy` repetidamente.
* **Static Batching:** Todos los objetos inmóviles de la Aldea Industrial, rocas de las cuevas y pinos decorativos no talables deben marcarse como `Batching Static` en la escena.
