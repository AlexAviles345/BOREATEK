# Mecánicas de Supervivencia Térmica y Peligros Ambientales

---

## 1. El Medidor de Temperatura Corporal

El **Duende Ingeniero** posee un rango de temperatura corporal de `0 °T` a `100 °T` (representado tanto en la interfaz como en el manómetro superior de su **Tanque-Mochila Presurizado de Latón**).

### 1.1. Ecuación de Balance Térmico por Segundo

$$
\Delta T = -\left( D_{\text{bioma}} \times M_{\text{ciclo/clima}} \times (1 - R_{\text{abrigo}}) \right) + G_{\text{burbuja}}
$$

### 1.2. Tabla de Parámetros Térmicos

| Variable | Parámetro | Valor | Condición |
| :--- | :--- | :---: | :--- |
| $D_{\text{bioma}}$ | Aldea Industrial de Santa | `0.0 °T/s` | Zona segura base (regenera `+15.0 °T/s`). |
| $D_{\text{bioma}}$ | Tundra Inicial / Túneles de Ventilación | `1.0 °T/s` | Frío exterior básico. |
| $D_{\text{bioma}}$ | Bosque de Caramelos / Cuevas Nivel 1 | `1.5 °T/s` | Frío moderado. |
| $D_{\text{bioma}}$ | Cementerio de Trineos / Geoda Hielo Azul (N2) | `2.5 °T/s` | Frío glacial severo. |
| $D_{\text{bioma}}$ | El Abismo del Núcleo Luminoso (N3 - Accesos) | `3.0 °T/s` | Corrientes heladas abismales. |
| $M_{\text{ciclo/clima}}$ | Día Ártico Despejado (10 min) | `1.0x` | Multiplicador estándar. |
| $M_{\text{ciclo/clima}}$ | Noche Polar (10 min) | `1.5x` | Caída térmica nocturna. |
| $M_{\text{ciclo/clima}}$ | **Evento: Ventisca Ciega** | `2.5x` | Tormenta extrema con visibilidad mínima. |
| $R_{\text{abrigo}}$ | Abrigo Térmico Acolchado Nivel 1 (Base) | `0 %` | Equipo inicial verde oliva con borrego. |
| $R_{\text{abrigo}}$ | Abrigo Térmico Acolchado Nivel 2 | `25 %` | Primera mejora en tienda. |
| $R_{\text{abrigo}}$ | Abrigo Térmico Acolchado Nivel 3 | `50 %` | Segunda mejora en tienda. |

---

## 2. Guirnaldas Luminosas y Balizas Térmicas (Burbujas de Calor)

En lugar de antorchas tradicionales, la ingeniería de Boreatek utiliza dos dispositivos desplegables:

1. **Guirnaldas Luminosas (`HeatRegen: +5.0 °T/s`, `Radio: 6m`):**
   * Se tienden formando cadenas de focos navideños tanto sobre la nieve como en las paredes de los **Túneles de Ventilación** y cavernas.
   * Cumplen doble propósito: crean estaciones de descanso térmico y trazan un hilo conductor luminoso para regresar a la aldea durante una **Ventisca Ciega**.
2. **Balizas Térmicas Portátiles (`HeatRegen: +12.0 °T/s`, `Radio: 10m`):**
   * Calefactores industriales de alta potencia alimentados por Cristal de Caramelo.
   * Son el único dispositivo portátil capaz de **derretir el hielo sólido de los Cofres Congelados** en el *Cementerio de Trineos*.

---

## 3. Estados de Enfriamiento y Rescate por Hipotermia

* **Estado Normal (`100 °T` a `31 °T`):** Movimiento y velocidad de picado al `100 %`.
* **Advertencia de Escarcha (`30 °T` a `1 °T`):** Se activa el borde congelado (*Vignette* azul) en pantalla, suena la alarma de presión del tanque-mochila y la velocidad de movimiento cae un `15 %`.
* **Hipotermia Crítica (`0 °T`):**
  * El jugador dispone de una ventana de emergencia de `5 segundos` para beber un **Termo de Chocolate Caliente** o entrar en una burbuja de calor.
  * Si no lo logra, cae inconsciente y una brigada automática lo **rescata de vuelta a la Aldea Industrial**, perdiendo el **30 % de los minerales no asegurados** en su tanque-mochila.
