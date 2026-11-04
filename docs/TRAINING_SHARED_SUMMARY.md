> Decisión vigente: el calendario y la lista diaria son globales en todas las pestañas, sin selector. El filtrado descrito en este informe histórico queda sustituido por [Calendario global](TRAINING_GLOBAL_CALENDAR.md).
# Entrenamiento: Resumen y agenda compartida — 01/11/2026

## Resultado

Resumen sustituye a Planificación como primera pestaña. Consulta todas las actividades del equipo en un calendario semanal de siete columnas, sin panel diario antes de seleccionar una fecha. El día seleccionado mantiene su contorno amarillo; la lista conserva horarios, categoría explícita, participantes, responsable, duración e identidad, agrupados por mañana/tarde. Los indicadores de la semana cuentan ejercicios, jornadas explícitas o con actividades, minutos de actividades, distribución por categoría y turno y descansos confirmados. Los minutos no son fatiga observada ni carga individual.

Campo, Gimnasio, Porteros, Jugadas ensayadas, Recuperación y Tecnología deportiva conservan el formulario común, con catálogo de su especialidad y vista filtrada por defecto. Se puede consultar la agenda completa mediante el filtro. Resumen siempre es global. Todas las vistas leen el mismo TrainingPlan del SqliteSportsStore, una sola vez por registro. No hay una segunda agenda ni botón de sincronización.

Desde Resumen, Editar abre la categoría del ejercicio y conserva su identificador y fecha. Guardar actualiza ese registro. Añadir permite elegir categoría y abre su formulario; Resumen no reproduce esos formularios. Volver a Resumen conserva fecha y semana. Tras guardar también se conserva el turno y se propone un hueco calculado con la agenda actualizada. Los cambios sin guardar requieren confirmación al cambiar pestaña, semana, fecha, equipo o sección. Entrar en otra carrera limpia el contexto de borradores.

Confirmar día de descanso está disponible desde Resumen y requiere confirmación, con aviso específico si retira ejercicios. No cambia datos al seleccionar una opción. El descanso carece de ejercicios y turnos; la recuperación activa sigue siendo una actividad. No hay nuevos límites deportivos de duración.

## Arquitectura y compatibilidad

Core calcula el resumen y proporciona la categoría de destino; Godot presenta y navega. La validación existente resuelve participantes efectivos y consulta la agenda completa, incluyendo otras categorías y equipos. El conflicto conserva los dos registros y permite mostrar sus ejercicios y horas en español. Se mantienen las comprobaciones de responsable, instalación, elegibilidad y tecnología disponible. Las actividades simultáneas con participantes diferentes siguen permitidas si no comparten otros recursos incompatibles.

No se añaden migraciones, tablas, paquetes ni campos persistidos. Se conservan los identificadores, los contratos, las excepciones, las referencias tácticas, las carreras anteriores y la compatibilidad de descansos históricos. Guardar no ejecuta entrenamiento ni modifica atributos. Las cinco publicaciones iniciales conservan su SHA-256.

## Archivos de este encargo

- src/Furvo.Core/Development/TrainingAgenda.cs (nuevo): resumen semanal y enrutamiento por categoría.
- src/Furvo.Core/Development/Training.cs: conflicto identificable, conservando InvalidDataException y las validaciones previas.
- src/Furvo.Godot/Scripts/Main.Training.cs: pestañas, protección del cambio de equipo y presentación del conflicto.
- src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs: Resumen sin formulario, selección de categoría, edición cruzada, descanso y contexto compartido.
- src/Furvo.Godot/Scripts/Main.TrainingDaily.cs: indicadores semanales.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: protección al abandonar Entrenamiento y limpieza del contexto al cargar carrera.
- src/Furvo.Godot/Scripts/Components/TrainingDailyExerciseRow.cs: categoría textual y metadatos de la fila.
- src/Furvo.Godot/Resources/UiStrings.resx: Resumen y nuevos textos centralizados.
- tests/Furvo.Tests/TrainingAgendaTests.cs (nuevo): nueve casos de resumen, rutas, creación/edición/eliminación, solo lectura, simultaneidad, conflictos e independencia.
- tools/GodotUiSmoke.gd: recorrido actualizado al flujo Resumen → categoría → edición → Resumen, con confirmaciones, descanso y reapertura.
- docs/TRAINING_SHARED_SUMMARY.md, docs/INVENTORY.md y docs/VALIDATION.md: decisiones, inventario y resultados.

Los restantes cambios del checkout proceden de encargos previos y se han conservado.

## Validación por fases

- Base anterior: 503 pruebas Debug correctas.
- A: compilación Release correcta y 9 pruebas seleccionadas correctas, resumen global.
- B: compilación Release correcta y 10 pruebas seleccionadas correctas, filtrado compartido.
- C: compilación Release correcta y 5 pruebas seleccionadas correctas, identidad/edición/persistencia.
- D: compilaciones Debug y Release correctas sin advertencias; 512 pruebas Debug correctas (9 nuevas), sin fallos ni omisiones. Las 512 pruebas Release también pasan, sin fallos ni omisiones.

Godot nativo ha completado el recorrido con código 0. Las capturas de calendario/lista se han revisado en 1280×720, 1600×900 y 1920×1080; el desplazamiento vertical permite consultar el contenido. El recorrido final con categoría textual y cancelación del cambio de equipo también termina con código 0. El WARNING de sesiones solapadas pertenece a la prueba negativa deliberada contra otra categoría.

Evidencia: artifacts/test-results/training-summary-{before,phase-a,phase-b,phase-c,debug,release}.trx; artifacts/training-summary-native.log; artifacts/career-visual/career-training-summary-*.png.

## Comprobación local

~~~powershell
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
~~~

Abrir src/Furvo.Godot/project.godot y consultar una carrera propia: Equipo → Entrenamiento → Resumen. Seleccionar fecha, añadir desde una categoría, volver a Resumen, editar por ID y comprobar la confirmación de cambios sin guardar y descanso. El recorrido automatizado utiliza carreras aisladas; no sustituye una revisión de usabilidad con datos personales. No se implementan fatiga observada, consecuencias médicas ni nuevos sistemas deportivos. Furvo se ha lanzado normalmente en 1280×720 tras la validación y se ha dejado ejecutándose; registro artifacts/training-summary-final-run.log.
