# Duración libre y descanso de jornada — 01/11/2026

Se eliminan los límites de 15–120 minutos por bloque, 180 por turno y 300 por día, y las prohibiciones físicas por intensidad juvenil o carga ponderada diaria sin un modelo observado. Se mantienen duración positiva, final posterior al inicio, mismo día, turnos coherentes, horarios configurables (07–20; selector de 15 minutos), duplicados, participantes, responsables e instalaciones. Los huecos no producen registros ni minutos. La recuperación activa sigue siendo una actividad. Guardar planificación no ejecuta ejercicios ni calcula lesiones o fatiga; queda un punto de integración para avisos futuros basados en evidencia.

El descanso completo es exclusivamente configuración de jornada. Oculta los campos de ejercicio e intensidad, requiere Confirmar día de descanso y muestra un único icono sin turnos pendientes. Cambiar una jornada con ejercicios exige confirmación; cancelar conserva la planificación. La operación de sustitución es transaccional. Al modificar manualmente las horas se oculta la propuesta horaria anterior.

## Compatibilidad

SportsTraining004_RestArchive.sql añade un archivo versionado de registros históricos de descanso por carrera. La lectura es pura y no migra al consultar. Al guardar se conserva el registro original completo y cualquier evidencia de ejecución. Solo una jornada explícitamente marcada como descanso, sin trabajo ni ejecución, permite conversión inequívoca. Los casos ambiguos o con evidencia quedan visibles para revisión y excluidos del trabajo planificado; no se eliminan ni ejecutan. El decodificador antiguo de intensidad cero y ejercicio rest permanece únicamente para lectura histórica. El editor de grupos conserva también este archivo. No se modifican automáticamente carreras personales ni publicaciones del mundo.

## Inventario de esta corrección

- Core/Development/Training.cs: catálogo activo, validación y compatibilidad del plan.
- Core/Development/TrainingSchedule.cs: política temporal sin topes arbitrarios.
- Core/Development/TrainingDailyPlanning.cs y TrainingCalendar.cs: proyecciones, propuestas y totales.
- Core/Development/TrainingRestCompatibility.cs (nuevo): archivo histórico, resolución y punto de integración de avisos.
- Persistence/Migrations/SportsTraining004_RestArchive.sql (nuevo): esquema versionado del archivo.
- Persistence/SqliteSportsStore.cs y SqliteAnalysisReportService.cs: lectura pura, guardado transaccional y conservación de evidencias.
- Godot/Scripts/Main.TrainingCalendar.cs y Main.TrainingDaily.cs: formulario, confirmación y lista diaria.
- Godot/Scripts/Components/TrainingWeekCalendar.cs y TrainingActivityIcon.cs: icono y aviso de revisión.
- Godot/Resources/UiStrings.resx: textos en español.
- Tests/TrainingRestCorrectionTests.cs (nuevo): 19 casos de duración, descanso, compatibilidad y aislamiento.
- Tests/TrainingScheduleTests.cs, TrainingDailyPlanningTests.cs, TrainingCalendarTests.cs y SportsDevelopmentTests.cs: expectativas actualizadas a las nuevas reglas; no se eliminan pruebas.
- tools/GodotUiSmoke.gd: recorrido nativo y capturas en tres resoluciones.
- docs/INVENTORY.md, VALIDATION.md y este documento: inventario y evidencia.

Las rutas de código son relativas a src/Furvo.Core, src/Furvo.Persistence, src/Furvo.Godot y tests/Furvo.Tests respectivamente. Los cambios anteriores de navegación y pantalla de entrenamiento pertenecen a trabajos previos y se conservan.

## Validación

Antes: 484 pruebas Debug correctas. Después: 503 Debug correctas, incluidas 19 nuevas. Compilaciones Debug y Release correctas, sin errores ni advertencias. Godot nativo finaliza con código 0: prueba bloques de 150/180 minutos, total de 420, confirmación y cancelación del descanso, recuperación activa, reapertura y regresiones de navegación y Análisis. Capturas revisadas en 1280×720, 1600×900 y 1920×1080, con desplazamiento vertical. El aviso de solapamiento corresponde a un caso negativo intencionado. Las 503 pruebas Release también pasan, sin fallos ni omisiones. Furvo se ha abierto normalmente después de la validación y permanece ejecutándose. Cinco publicaciones SQLite iniciales conservan exactamente su SHA-256. No se ha implementado ejecución deportiva ni riesgo de lesión.

Comandos locales:

~~~powershell
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
~~~
