# Checklist de QA, Rendimiento y Entrega Final (Release)

---

## 1. Pruebas Funcionales del Bucle Jugable
- [ ] **Prueba de Supervivencia Térmica:** Salir de la Aldea Industrial reduce la temperatura; entrar en el radio de una Guirnalda Luminosa o Baliza Térmica regenera el calor; llegar a `0 °T` durante `5s` ejecuta el rescate a la aldea descontando el `30 %` de recursos no asegurados.
- [ ] **Prueba de Progresión de Herramientas:** Un nodo de *Hierro Polar / Hielo Azul (Nivel 2)* rechaza el Pico Nivel 1 y solo permite su extracción tras comprar el Pico Nivel 2 en la tienda.
- [ ] **Prueba de Cofre Congelado:** Un cofre en *El Cementerio de Trineos* no puede abrirse hasta colocar una Baliza Térmica a su lado durante `15 segundos`.
- [ ] **Prueba de Ventisca Ciega:** Al activarse la ventisca, la niebla URP reduce la visibilidad y el drenaje térmico se multiplica por `2.5x`, mientras que el *Termo de Chocolate Caliente* frena el enfriamiento durante `60 segundos`.
- [ ] **Prueba de La Gran Nochebuena:** Al activarse el evento final, todos los nodos entregan el doble (`2x`) de materiales.

## 2. Verificación de Rendimiento y Estándares Técnicos
- [ ] **Framerate:** `60+ FPS` estables en `1920x1080` con `PC_RPAsset` activo.
- [ ] **Zero GC Spikes:** Sin llamadas a `GetComponent`, `Find` o `Instantiate` descontrolado en `Update()`.
- [ ] **Luces URP:** Las luces puntuales de las guirnaldas tienen `Shadows: No Shadows` para no saturar el atlas de sombras de URP.
- [ ] **Consola Limpia:** `0` errores de compilación y `0` excepciones en tiempo de ejecución durante una sesión completa de ciclo día/noche (20 minutos).
