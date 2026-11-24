# ADR-003: Uso del Nuevo Input System (`InputSystem_Actions.inputactions`)

* **Fecha:** 2026-11-24
* **Estado:** Aceptado

---

## 1. Contexto
El proyecto ya incluye el paquete `com.unity.inputsystem` (`1.20.0`) y el activo preconfigurado `Assets/InputSystem_Actions.inputactions` con los mapas `Player` (`Move`, `Look`, `Attack`, `Interact`, `Crouch`, `Jump`, `Sprint`, `Previous`, `Next`) y `UI`.

## 2. Decisión
Prohibir el uso de la API heredada `UnityEngine.Input.GetKey / GetAxis` y centralizar toda la lectura de controles del jugador y de la tienda a través del mapa de acciones `InputSystem_Actions`.

## 3. Consecuencias
* **Positivas:**
  * Soporte nativo automático para **Teclado/Ratón** y **Gamepad** sin escribir lógica duplicada.
  * La acción `Interact` ya cuenta con interacción tipo `Hold` configurada, ideal para recoger chatarra o activar mecanismos.
