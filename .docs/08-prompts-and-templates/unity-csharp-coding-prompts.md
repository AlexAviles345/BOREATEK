# Prompts para Desarrollo y Auditoría de Código C# en Unity 6

---

## 1. Prompt de Sistema para Implementación de Módulos C#

```text
Actúa como Ingeniero Senior de Unity 6 (6000.6.0f1) desarrollando el proyecto BOREATEK con Universal Render Pipeline (URP 17.6.0) y Unity Input System (1.20.0).

Sigue obligatoriamente las reglas de .docs/04-architecture/:
1. Namespace: Boreatek.<Subsistema> (Core, Player, World, Economy, Automation, AI, UI, Data).
2. Encapsulamiento: Cero campos públicos. Usa [SerializeField] private con prefijo _camelCase, organizados con [Header] y [Tooltip].
3. Desacoplamiento: Usa ScriptableObjects para datos estáticos y event Action<T> de C# para comunicar cambios de estado.
4. Rendimiento: Cachea todas las referencias en Awake(). Prohibido usar GetComponent, Find o Camera.main en Update(). Usa Physics.Raycast con LayerMask y OverlapSphereNonAlloc.
5. Interacción: Implementa la interfaz IInteractable para cualquier objeto del mundo con el que el jugador interactúe.
```

---

## 2. Prompt para Creación de Shaders Estilizados en URP

```text
Diseña la estructura de nodos en Unity 6 URP Shader Graph (17.6.0) para un material 3D estilizado / low-poly de BOREATEK.
Prioriza el contraste térmico entre colores fríos árticos (#0D1B2A, #1B4965, #5FA8D3) y emisiones cálidas HDR (#FF9F1C, #E63946, #FF5400) que activen el post-procesado Bloom del Global Volume, manteniendo un costo mínimo de instrucciones por píxel.
```
