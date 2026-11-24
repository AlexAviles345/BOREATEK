# Runbook: Compilación, Pruebas y Empaquetado (Build & Release)

---

## 1. Verificación Previa al Build (Pre-Flight Check)

Antes de generar un ejecutable para pruebas o entrega de la **Mini Game Jam**:
1. Limpiar la consola de Unity y pulsar **Play** en la escena principal para confirmar que no existan `NullReferenceException` ni referencias faltantes en el Inspector.
2. Verificar que los multiplicadores de depuración de `DayNightCycleManager.cs` estén en tiempo real (`1200s` por ciclo de 20 minutos).
3. Confirmar en `Edit > Project Settings > Quality` que el perfil por defecto para PC apunte a `PC_RPAsset`.

---

## 2. Generación del Ejecutable en Unity 6

1. Ir a `File > Build Profiles`.
2. Seleccionar el perfil **Windows (x86_64)**.
3. En **Scene List**, verificar que la escena principal del juego esté marcada y en la posición `0`.
4. En **Player Settings**:
   * `Company Name`: `Boreatek Team`
   * `Product Name`: `BOREATEK`
   * `Fullscreen Mode`: `Fullscreen Window`
5. Hacer clic en **Build** y seleccionar la ruta `Builds/Windows/BOREATEK.exe` (la carpeta `Builds/` ya está ignorada en `.gitignore`).

---

## 3. Empaquetado para Distribución (Game Jam)

1. Comprimir el contenido de `Builds/Windows/` (incluyendo `BOREATEK.exe`, `UnityPlayer.dll` y la carpeta `BOREATEK_Data/`) en un archivo `BOREATEK_Win64_v0.1.zip`.
2. Descomprimir en una carpeta temporal limpia y ejecutar `BOREATEK.exe` para validar el inicio en frío, controles de ratón/teclado y rendimiento a `60 FPS`.
