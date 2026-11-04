# Bloodborne Cinematic

Esta revision conserva todos los graficos de Lantern v0.9.39-beta.29 y deja solo Edit Save para editar los personajes de la carpeta de partidas activa.
Se quitaron Save Manager, creacion y cambio de perfiles, importacion y restauracion. Se conserva el boton Backup original del menu principal con su origen y destino fijos.
Si hay un solo personaje, el editor se abre directamente. Si hay varios, se elige uno de la misma partida activa.
Se conservan los atributos, ecos, insight, inventario, objetos, equipo, jefes y lamparas del editor.
Se mantiene la copia interna previa a una edicion, el bloqueo con el juego abierto y la verificacion de cambios externos.
Los directorios historicos de backups no se borran ni se ofrecen para recuperar partidas.

Entregas separadas: Launcher-Con-Editor.exe y Launcher-Solo-Graphics.exe.
La segunda es el binario exacto anterior a la integracion de saves, con los graficos nuevos.
Cerrar el launcher, renombrar la version elegida como Launcher.exe y copiarla a BB Port.
Conservar engine, Assets, lantern.ini, mods, partidas y preferencias.

Leer CONSIGNA-FUTURO.md y BloodborneMinimalLauncher/AGENTS.md antes de tocar la interfaz.
121 comprobaciones locales aprobadas: 28 de graficos y 93 de partidas y navegacion.
No se realizo una prueba completa dentro del juego.

## Engine Y Menus

La entrega del 2026-11-04 recompila juntos bb-probe, bb-engine-assets y bb-shader-lab con sus DLL correspondientes.
Esto elimina la mezcla de Protobuf que provocaba Entry Point Not Found en el helper de shaders.
La correccion provisional con bbproto_old.dll queda documentada en runtime-fixes para instalaciones anteriores;
el paquete completo nuevo usa directamente el helper recompilado, con su runtime compatible.
La entrega comprueba imports y exports de todas las DLL internas y ejecuta --recompile-cache sobre una carpeta vacia.

Se ocultan solo los comandos de System que abren PC Controls, Key Bindings, PC Lighting,
PC Lighting Detail y PC Looks. Output Resolution permanece oculto como antes.
Las definiciones de las paginas, los handlers, controles PC, opciones graficas y configuracion permanecen activas.
No se deshabilita ningun efecto ni opcion al ocultar el acceso visual. Se conservan las otras paginas nativas.
La comprobacion de codigo verifica que las asignaciones g_offered y los bindings no se alteran.
No se modifican partidas, MOD 4K, artwork ni preferencias.
