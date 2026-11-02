# Bloodborne Cinematic — Lantern 0.9.39 beta 29

Esta actualización conserva el launcher cinematográfico y suma las nuevas opciones gráficas.
Incluye FSR automático, XeSS, DLSS Less Wobble, el preset Custom con su escala de renderizado,
sesgo de texturas, AMD Anti-Lag 2, iluminación, sombras y ray tracing experimental.
También permite activar o desactivar por separado los componentes nuevos del renderer.

Graphics mantiene el orden Upscalers, Frame Generation, Graphics Detail y Engine Performance.
Los controles de iluminación están en Effects. Los efectos nuevos empiezan desactivados si
no hay una preferencia anterior. No se modifica el modelo DLSS K ni se añade su selector.
Half Refresh Rate sigue oculto y desactivado al guardar. Output Resolution continúa oculto
en el menú nativo; la resolución se cambia desde Display.

## Instalación

1. Extraer el paquete completo en una carpeta nueva. No mezclarlo con la instalación 0.9.37.
2. Conservar la instalación anterior. Para la imagen cinematográfica, copiar su carpeta Assets
   a la carpeta nueva si no se encuentra automáticamente.
3. Abrir Launcher.exe. En el mismo equipo conserva las rutas y preferencias del launcher.
   Si no existe lantern.ini en la carpeta nueva, copia el de la instalación anterior conocida;
   el archivo de origen no se modifica. Se conservan comentarios y claves desconocidas.
4. Confirmar las carpetas del juego y de las partidas, revisar las opciones y usar Save Settings.
5. Probar el arranque, cámara, Frame Generation y menús. Es una beta: una compilación y sus
   verificaciones automáticas no sustituyen la prueba dentro del juego.

El paquete no contiene juego, saves ni artwork. Abrir el launcher no copia partidas.
El botón Backup conserva el origen y destino fijos del proyecto anterior.
El launcher comprueba la identificación del paquete para evitar usar por accidente otro engine.

XeSS necesita libxess.dll en engine/xess. El ZIP oficial no incluye esa biblioteca y usa FSR 3.1
como alternativa si falta. Los efectos de iluminación y ray tracing dependen del hardware;
el engine comprueba las capacidades. Los cambios se aplican en el siguiente arranque.

Cooperativo, editor de partidas, creación de menús 4K y otras funciones del launcher oficial
quedan para una actualización posterior de nuestra interfaz.

Upstream: https://github.com/Awakenchan/LanternEngine/releases/tag/v0.9.39-beta.29
Commit: e6b943dd969d99561545315f3d801f10e4a6cf8d.
