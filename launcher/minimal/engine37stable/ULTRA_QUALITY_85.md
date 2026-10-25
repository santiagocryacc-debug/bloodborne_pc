# Ultra Quality - Lantern Engine 0.9.37 estable

Agrega Ultra Quality entre Native y Quality en el launcher WPF.
Conserva los valores Native=0, Quality=1, Balanced=2, Performance=3 y Ultra Performance=4.
Usa el modo Custom: preset=5, render_scale=1.176471 (salida/render).
La escala se escribe solamente cuando se guarda Ultra Quality.

Resolucion interna calculada por el motor (redondeo a multiplos de cuatro):
- Salida 1920x1080: escena 1632x916.
- Salida 2560x1440: escena 2176x1224.
- Salida 3840x2160: escena 3264x1836.

El parche ultra_quality_85.py se aplica sobre el commit oficial
 d901fbd2ea4bea114b37c26a2a9278d1e97f7b46 antes de compilar:
1. Conserva seis decimales de render_scale al guardar desde el motor.
2. Usa DLSS MaxQuality para una escena reducida cuyo modelo cercano es NativeAA.
   Record() sigue eligiendo DLAA cuando entrada y salida tienen el mismo tamano.
   No cambia la seleccion del modelo K ni los demas modelos DLSS.

El launcher y el motor parcheado deben distribuirse juntos.
El launcher por si solo no incluye la correccion DLSS ni la persistencia del motor.
El menu nativo seguira llamando Custom al preset 5.

Verificacion:
python launcher/minimal/engine37stable/verify_ultra_quality_85.py upstream/engine
python launcher/minimal/engine37stable/ultra_quality_85.py upstream/engine
python launcher/minimal/engine37stable/verify_ultra_quality_85.py upstream/engine --cxx clang++

El workflow build-lantern-stable-cinematic.yml compila el launcher WPF y el motor,
y genera el paquete completo. Compilar no reemplaza la prueba visual en el juego.
