# Calendario semanal de Entrenamiento
Fecha: 01/11/2026.

## Diseño y funcionamiento
Planificación, Campo, Gimnasio, Porteros, Jugadas ensayadas, Recuperación y Tecnología deportiva utilizan el mismo componente y los mismos registros. La tecnología ya existía en el modelo y ahora dispone de acceso explícito por pestaña; no se habilita una instalación inexistente ni se elimina su validación de disponibilidad.

Orden: encabezado, equipo, pestañas, formulario de sesión, navegación y filtro semanal, calendario de siete columnas. Grupos y delegación se mantienen debajo en Planificación. El formulario conserva fecha, hora, duración, ejercicio, carga, ámbito/participantes, excepciones, objetivo, frecuencia prevista, responsable, oposición y referencia táctica. Fecha/hora y duración/intensidad se agrupan en una cuadrícula. Las excepciones y el formulario pueden contraerse.

El calendario usa lunes a domingo y la fecha persistida de la carrera. Resalta el día deportivo actual sin consultar el reloj del ordenador. Cada actividad se identifica con un dibujo vectorial original, hora y tooltip: ejercicio, tipo, estado, duración, intensidad, número de participantes, muestra de nombres y grupo si existe. Un clic carga los datos en el formulario. La fila completa de siete días sustituye las siete tarjetas verticales.

Hasta tres sesiones se muestran directamente, en orden cronológico con desempate estable por ID. +N despliega las demás sin duplicarlas. Un día vacío solo ofrece añadir; no equivale a descanso. El descanso utiliza el ejercicio y carga ya existentes. El estado realizada requiere evidencia de training_attendance; guardar planificación nunca la crea.

Todas las sesiones es el modo inicial. Solo esta categoría consulta el mismo plan mediante un filtro, conservado entre semanas. En Planificación se muestra siempre el plan completo y el filtro de categoría no está disponible, pues esa pestaña no representa una categoría deportiva. Una actividad de cualquier categoría puede consultarse y editarse desde el calendario común.

## Protección de cambios
Los borradores son de interfaz, en memoria y separados por archivo de carrera y equipo. Se conservan durante navegación semanal, cambio de categoría, contracción y reconstrucción de la pantalla. No se serializan como sesiones ni se ejecutan. Abrir otra sesión, seleccionar un día para crear o cancelar un borrador modificado exige confirmar el descarte. Eliminar una sesión exige confirmación independiente. Cancelar la confirmación conserva los datos.

Los borradores no son un autoguardado: para conservar una sesión tras cerrar la aplicación es necesario pulsar Guardar. No se ha añadido recuperación de formularios después de cerrar el proceso.

La edición conserva el identificador de sesión, participantes, opciones y referencia de jugada, incluida su localización cuando exista. Las restricciones existentes de carga, horario, elegibilidad, instalación y sesiones realizadas siguen vigentes.

## Persistencia y compatibilidad
No hay esquema nuevo, migraciones, publicaciones ni dependencias. El calendario lee TrainingPlan ya existente. CompletedSessions es una consulta de solo lectura de evidencias existentes; en carreras sin esquema deportivo devuelve vacío sin crear tablas. El guardado utiliza SavePlan y sus transacciones/revisión existentes. Los 480 jugadores y sus atributos, fatiga observada y desarrollo no se modifican por planificar.

## Archivos de este encargo
| Archivo | Función |
|---|---|
| src/Furvo.Core/Development/TrainingCalendar.cs | Semana, filtros, orden y estados públicos del calendario. |
| src/Furvo.Persistence/SqliteTrainingExecutionStore.cs | Consulta de identificadores con ejecución acreditada. |
| src/Furvo.Godot/Scripts/Main.Training.cs | Sustitución de lista vertical por el planificador común; siete pestañas. |
| src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs | Formulario compacto, borradores, confirmaciones, filtros e integración. |
| src/Furvo.Godot/Scripts/Components/TrainingWeekCalendar.cs | Cuadrícula compartida con selección y +N. |
| src/Furvo.Godot/Scripts/Components/TrainingActivityIcon.cs | Iconos vectoriales sin recursos externos. |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos centralizados en español. |
| tests/Furvo.Tests/TrainingCalendarTests.cs | Catorce casos nuevos de fechas, categorías, orden, estados, lectura y persistencia. |
| tools/GodotUiSmoke.gd | Pruebas nativas del calendario, borradores, filtros, edición y geometría. |
| docs/TRAINING_WEEK_CALENDAR.md | Decisiones, inventario y validación. |
| docs/VALIDATION.md, docs/INVENTORY.md | Referencias al estado de esta entrega. |

El árbol contiene trabajo anterior sin commit. Esta relación identifica únicamente el alcance del encargo.

## Comandos en Windows
```powershell
dotnet build Furvo.sln --no-restore
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
& 'C:\Users\panio\Documents\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'src/Furvo.Godot' --script '../../tools/GodotUiSmoke.gd' -- --capture --analysis-only
```

Las pruebas nativas usan carreras aisladas bajo artifacts. Capturas: artifacts/career-visual/career-training-form-* y career-training-calendar-*.


## Resultados de validación

- Antes: **431 pruebas Debug superadas**, registro calendar-before.trx.
- Después: **445 pruebas Debug y 445 Release superadas**, cero fallidas y cero omitidas. Se añaden 14 casos y se conservan los 431 anteriores. Registros calendar-debug.trx y calendar-release.trx bajo artifacts/test-results. El filtro de calendario ejecutó 21 casos (incluye siete anteriores), todos correctos.
- Compilaciones finales Debug y Release: cero errores y cero advertencias.
- Godot nativo 4.7.2 .NET: recorrido enfocado con código 0 y marcador FURVO_TRAINING_CALENDAR_OK. También pasan sus regresiones de navegación, perfiles/carreras, Análisis e informes. No se ha repetido el recorrido completo de arrastre táctico en este encargo.
- Se comprobaron siete columnas, orden del formulario, varias sesiones, +N, tooltips, carga/edición, confirmación de eliminación, borradores, filtro persistente y geometría en 1280×720, 1600×900 y 1920×1080. Las capturas del calendario y del formulario fueron inspeccionadas visualmente en los tamaños indicados.
- La primera prueba nativa del borrador necesitó emitir el evento de edición que corresponde a la interacción del usuario; la repetición pasó. Se corrigió asimismo un contenedor auxiliar sin padre.
- Cinco publicaciones SQLite iniciales intactas por SHA-256. Consultar el calendario sin esquema deportivo mantiene el archivo de carrera intacto. Sin nuevas migraciones ni dependencias. git diff --check correcto.
- Furvo se volvió a lanzar en ventana normal de 1280×720 al terminar y se dejó ejecutándose.

No existe avance temporal de carrera: las sesiones no se realizan al guardarlas. Las opciones de tecnología conservan las restricciones de instalación existentes. Los borradores sobreviven a la navegación durante esta ejecución, pero no se guardan automáticamente al cerrar el juego.
