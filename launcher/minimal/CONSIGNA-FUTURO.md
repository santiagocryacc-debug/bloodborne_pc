# Consigna Del Launcher

Estas decisiones son requisitos del usuario para todos los cambios futuros.

- Interfaz en ingles, con Title Case en cada palabra: `Preset To Apply`, `Save Manager`.
- Tecnologias y siglas siempre en MAYUSCULAS: DLSS, FSR, XESS, AMD, NVIDIA, CPU, GPU, UI, PC, FPS, TAA, GTAO, MIP.
- Ningun tooltip ni descripcion al pasar el mouse. Ninguna explicacion, ayuda, subtitulo instructivo o nota debajo de las opciones.
- Sin guiones en textos visibles: `Frame Generation`. Sin texto entre parentesis: `FSR`, nunca `FSR (Auto)`.
- Botones y mensajes breves: `Save Changes`, `Save Updated`, `Backup Complete`.
- Mantener el estilo cinematografico oscuro, dorado y rojo. No agregar funciones descartadas: cooperativo, voz, integraciones ni instalador de mods. El MOD 4K ya esta instalado.
- No cambiar nombres de archivos, rutas, claves de configuracion, IDs ni datos de las partidas para aplicar el formato visual.
- Mantener el launcher en la carpeta BB Port para conservar los accesos directos.
- Backup principal: Game Files/Saves/savedata/1/CUSA00207/SPRJ0005. Destino fijo: Game Files/Backups.
- Recuperacion, importacion y edicion deben conservar primero una copia del estado actual. No borrar backups antiguos. No mezclar perfiles ni guardar con el juego abierto.
- No incluir partidas reales, archivos del juego, configuracion personal ni credenciales en ZIP de fuentes o GitHub.

El AGENTS.md de BloodborneMinimalLauncher aplica estas reglas al codigo del launcher.

Actualizacion de alcance: dejar unicamente Edit Save para los personajes de la partida activa. No volver a agregar el gestor de perfiles, importacion ni restauracion de saves viejas. Conservar siempre el boton Backup original del menu principal, con su origen y destino fijos. La copia de seguridad interna previa a editar conserva la proteccion de datos.

Menus ingame: ocultar visualmente PC Controls, Key Bindings, PC Lighting, PC Lighting Detail y PC Looks, ademas de Output Resolution. Conservar todos sus settings, handlers y funciones por detras. No usar interruptores globales que deshabiliten efectos o controles para ocultar entradas.
