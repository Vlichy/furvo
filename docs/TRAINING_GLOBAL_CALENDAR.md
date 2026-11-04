# Calendario global en todas las pestañas — 01/11/2026

## Corrección

El filtrado introducido en la entrega anterior ocultaba las actividades ajenas al área activa. Se elimina el selector del calendario y su estado de interfaz. Resumen, Campo, Gimnasio, Porteros, Jugadas ensayadas, Recuperación y Tecnología deportiva consultan siempre TrainingCalendarWeek.Create sin filtro de categoría. La lista diaria también es global. La categoría solo limita los ejercicios disponibles en el formulario.

No se crean servicios ni agendas nuevas. Se utiliza el mismo TrainingPlan de SqliteSportsStore. No hay escrituras al cambiar de pestaña, copias de actividades ni cambios de categoría por navegación. El calendario conserva orden, iconos específicos, tooltip, estado de jornada, fecha seleccionada y semana visible. Seleccionar una actividad de otra categoría abre el formulario original por identificador, con la misma fecha. Los cambios sin guardar continúan protegidos por confirmación.

Se conservan las validaciones globales de participantes efectivos, excepciones, responsables e instalaciones, la simultaneidad compatible, duración sin límites deportivos arbitrarios, descanso como jornada completa, recuperación activa y fecha de carrera. Guardar planificación no ejecuta entrenamientos.

## Archivos de este encargo

- src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs: se retiran selector, estado y filtro de las consultas visibles; se conserva el catálogo especializado y la edición cruzada.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: se retira el reinicio del estado de filtro eliminado.
- tests/Furvo.Tests/TrainingGlobalCalendarTests.cs (nuevo): ejercicio de Campo 07:00–13:15 junto a Gimnasio/Porteros, orden y jornada, edición y eliminación por ID, categoría original, reapertura, lectura sin escrituras y separación de carreras.
- tools/GodotUiSmoke.gd: comparación de los siete calendarios y listas diarias, ausencia del selector, fecha/semana/jornada estables, selección de Campo desde Gimnasio y eliminación visible desde Porteros; capturas en tres resoluciones.
- docs/TRAINING_GLOBAL_CALENDAR.md, TRAINING_SHARED_SUMMARY.md, INVENTORY.md y VALIDATION.md: decisión vigente, inventario y resultados.

No se modifica Core ni Persistence en este encargo: ya proporcionan las consultas globales y las validaciones necesarias. Las consultas filtradas de dominio conservan compatibilidad para otros usos; el calendario principal no las utiliza. No hay migraciones, dependencias ni publicaciones SQLite nuevas. Los otros cambios del checkout pertenecen a encargos anteriores y se mantienen.

## Validación

Antes: 512 pruebas Debug correctas. Después: **515 Debug y 515 Release correctas**, sin fallos ni omisiones; tres casos nuevos conservan los anteriores. Compilaciones Debug y Release sin errores ni advertencias. Godot nativo finaliza con código 0 y confirma la igualdad de las siete vistas y la edición cruzada. Capturas revisadas en 1280×720, 1600×900 y 1920×1080; el desplazamiento permite ver el contenido completo. El WARNING de sesiones solapadas corresponde a la prueba negativa deliberada. Las cinco bases iniciales conservan exactamente su SHA-256. Furvo se ha abierto normalmente tras la validación y permanece ejecutándose; registro artifacts/training-global-final-run.log. Se registran antes y después en artifacts/test-results/training-global-{before,debug,release}.trx. La comprobación nativa compara la misma lista de identificadores y tooltips en las siete pestañas, incluyendo la jornada y el resaltado de fecha. artifacts/training-global-native.log registra el resultado. Las capturas career-training-global-gym-* muestran Gimnasio con todas las actividades, incluidas las de Campo y Recuperación.

~~~powershell
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
~~~

Para revisión local, abrir una carrera y comprobar Campo → Gimnasio → Porteros → Resumen sin cambiar de fecha; los ejercicios deben mantenerse. Seleccionar desde Gimnasio una actividad de Campo debe abrir Campo con el registro original. El recorrido automático utiliza carreras aisladas y no modifica las carreras personales.
