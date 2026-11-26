# ADR-001: Adopción de Unity 6 con URP y Estilo 3D Estilizado / *Low-Poly*

* **Fecha:** 2026-11-24
* **Estado:** Aceptado

---

## 1. Contexto
El proyecto **BOREATEK** requiere representar escenarios árticos abiertos con auroras boreales, niebla dinámica de ventisca y cavernas profundas iluminadas por docenas de luces puntuales (guirnaldas navideñas, balizas térmicas, cristales emisivos y magma), manteniendo un rendimiento fluido (`60+ FPS`) y un tiempo de producción viable para una Mini Game Jam.

## 2. Decisión
1. Utilizar **Unity 6 (`6000.6.0f1`)** con **Universal Render Pipeline (URP `17.6.0`)**.
2. Establecer como dirección de arte obligatoria un **estilo 3D animado, estilizado / *low-poly* texturizado** (proporciones caricaturescas/*chibi* como el concepto `concepto_duende_minero.png`), utilizando las ilustraciones conceptuales realistas únicamente como referencia de atmósfera, composición e iluminación.

## 3. Consecuencias
* **Positivas:**
  * URP permite manejar múltiples luces puntuales (guirnaldas y balizas) y efectos de *Bloom* / *Fog* con bajo costo de GPU.
  * El modelado *low-poly* estilizado acelera drásticamente la creación de assets 3D y unifica la estética del juego bajo una identidad propia 100 % original.
* **Restricciones:**
  * Las luces puntuales de las guirnaldas deben desactivar la proyección de sombras individuales (`No Shadows`), reservando sombras en tiempo real para la `Directional Light`, el Faro Frontal del Casco y la Forja Central.
