# Bloodborne Cinematic

Launcher cinematografico con Lantern v0.9.39-beta.29 y las opciones graficas nuevas.
Incluye Save Manager y Save Editor. Reglas de interfaz en CONSIGNA-FUTURO.md.

## Partidas

- Saves abre el gestor. Profile permite seleccionar Default o un perfil creado con New Profile o Copy Profile. Use Profile activa la seleccion para el proximo arranque.
- New Profile crea una carpeta vacia. Copy Profile duplica las partidas del perfil seleccionado sin tocar el original.
- Edit Save abre atributos, Blood Echoes, Insight, inventario, objetos, armas, armaduras, jefes y lamparas. Save Changes guarda; Discard Changes descarta. Los atributos ajustan tambien el nivel del personaje.
- Import acepta una carpeta SPRJ0005, una carpeta Saves o un arbol savedata de CUSA00207. No importa archivos SL2 de consola ni ZIP.
- Backup y Restore usan la misma carpeta Game Files/Backups del proyecto. Las copias anteriores SPRJ0005_fecha aparecen en Default. Las copias nuevas se asocian al perfil correspondiente y se verifican con SHA256.
- Cada edicion, importacion o restauracion conserva antes la partida actual. No hay borrado automatico de backups. Se bloquean los cambios mientras bb-start o bb-probe estan abiertos.
- Los perfiles nuevos se guardan dentro de Saves/profiles. Default conserva la partida y la ruta existentes. El boton Backup del menu principal sigue copiando siempre la partida original fija, independientemente del perfil activo.

## Instalacion

Copiar Launcher.exe junto a engine/ en BB Port, con el launcher y el juego cerrados.
Conservar Assets, lantern.ini, preferencias y mods existentes. El engine de este paquete es el mismo
Lantern v0.9.39-beta.29 ya integrado; esta entrega agrega el editor y el gestor a nuestra interfaz.
Para una instalacion nueva, usar el contenido completo de BB Port incluido en Bloodborne.zip.
No se incluyen juego, partidas, artwork personal ni el MOD 4K.

## Graficos Conservados

Graphics mantiene Upscalers, Frame Generation, Graphics Detail y Engine Performance.
Effects incluye Lighting y los presets. DLSS mantiene el modelo K sin selector.
Custom sigue disponible con su relacion de renderizado. No se agrego un preset Ultra Quality separado.
Half Refresh Rate sigue desactivado y Output Resolution sigue oculto en el menu nativo.
XESS requiere libxess.dll en engine/xess; si falta, el engine usa FSR 3.1.
Los cambios graficos se aplican en el proximo arranque. La validacion automatica no incluye una partida de juego completa.

Upstream: https://github.com/Awakenchan/LanternEngine/releases/tag/v0.9.39-beta.29
Commit: e6b943dd969d99561545315f3d801f10e4a6cf8d.
Los modulos de lectura de saves y catalogo son GPL-3.0-or-later; ver THIRD-PARTY-SAVES.md y LICENSE-SAVES.txt.
