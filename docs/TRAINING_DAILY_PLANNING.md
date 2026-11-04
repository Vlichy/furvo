# Lista diaria interactiva de Entrenamiento
Fecha: 01/11/2026.

## Flujo de pantalla
Se reorganiza el planificador existente: equipo/pestañas, botones semanales juntos, calendario, lista del día y formulario. Al entrar en Entrenamiento no hay día seleccionado ni formulario de bloque. Elegir una celda o su encabezado mantiene un contorno amarillo y abre únicamente esa fecha. Los grupos y la configuración de delegación existentes se mantienen como herramientas adicionales de Planificación.

La selección se aísla por carrera/equipo. Cambiar de semana limpia la fecha seleccionada y oculta los paneles diarios. Si hay cambios sin guardar se pide confirmación antes de descartarlos; cancelar conserva la fecha y el borrador. Cambiar de pestaña conserva el contexto del día y sus datos sin copiar registros.

## Lista y acciones
TrainingDailyPlanning proyecta el TrainingPlan existente. No hay otra tabla, caché persistente ni copia de ejercicios por pestaña. Ordena por inicio e ID estable, agrupa por mañana/tarde y conserva una agrupación pendiente cuando una actividad histórica no tiene turno determinable.

La lista muestra icono, rango horario, nombre, duración, participantes/ámbito, intensidad y responsable con nombre/cargo real cuando está disponible. Las filas contienen Editar y Eliminar. Editar utiliza el ID original; eliminar exige confirmación y retira únicamente ese bloque. No se elimina la configuración de la jornada ni otras fechas. Si se elimina el bloque que se estaba editando, el formulario vuelve a un borrador nuevo del mismo día.

Un día vacío presenta el mensaje correspondiente y Añadir otro ejercicio; no se convierte en descanso. La sesión única muestra su turno guardado, la doble muestra ambos, incluidos los vacíos.

## Totales globales y simultaneidad
Los indicadores de mañana, tarde y total suman minutos de **actividades de trabajo guardadas**, excluyendo huecos y bloques de descanso. Se identifican expresamente como globales: incluyen todas las categorías aunque el filtro de la lista muestre solo una.

Se distingue esa suma del tiempo efectivo de calendario (unión de intervalos) y del máximo individual planificado. Por ejemplo, dos grupos distintos trabajando 60 minutos simultáneamente representan 120 minutos de actividades, 60 minutos de intervalo activo y 60 por futbolista de cada grupo. Ninguna cifra representa fatiga observada ni un cálculo nuevo de desarrollo físico.

Los bloques con turno histórico pendiente se identifican aparte; no se asignan arbitrariamente a mañana o tarde. Las identidades duplicadas se rechazan en lugar de contarlas dos veces.

## Formulario y horarios
Fecha fija de solo lectura. Inicio y fin comparten fila; duración calculada. Un intervalo inválido muestra un aviso junto a los controles, una raya en lugar de duración negativa y deshabilita el botón. La validación de Core también impide guardar si se intenta eludir la interfaz.

TrainingSchedulePolicy incorpora horario operativo inicial 07:00–20:00 e intervalo de selección de 15 minutos. Se reutilizan los límites y validaciones anteriores, incluyendo trabajo máximo, participantes y recursos. Las horas antiguas se añaden exactamente a los selectores y pueden conservarse al editar sin cambiar su horario, aunque estén fuera de la ventana nueva. No hay redondeo de registros existentes.

Al guardar se actualizan lista, calendario y totales, manteniendo la misma fecha. Añadir otro ejercicio prepara un borrador y propone un hueco posterior disponible; si después del último no cabe, busca otro hueco del turno. La propuesta evita actividades existentes del equipo y respeta horario y máximos temporales. Es conservadora: los grupos distintos pueden elegir manualmente simultaneidad que supere las validaciones reales. El resto de las restricciones de carga experimental, instalaciones y participantes se verifica al guardar.

La propuesta nunca se guarda por sí sola ni sustituye horarios manuales al modificar un desplegable. Puede rechazarse o modificarse. No existe ejecución temporal ni fatiga nueva al planificar.

Se mantienen campos existentes complementarios: tipo de jornada, frecuencia informativa, oposición, referencia táctica y excepciones. No hay repetición automática de actividades.

## Persistencia y compatibilidad
Sin migraciones ni dependencias nuevas. Se reutiliza SavePlan, su transacción y revisión. Los filtros son exclusivamente de consulta; el guardado valida el plan completo, por lo que no ocultan un conflicto con otra categoría o equipo. Consultar no modifica SQLite.

No se alteran publicaciones iniciales, perfiles, atributos ni observaciones físicas. Los borradores son de interfaz y se deben guardar para conservarlos al cerrar el juego.

## Archivos del encargo
| Archivo | Función |
|---|---|
| src/Furvo.Core/Development/TrainingDailyPlanning.cs | Lista por fecha, turnos, totales y propuestas de horario sin guardar actividades. |
| src/Furvo.Core/Development/TrainingSchedule.cs | Horario operativo y paso configurables, preservando registros antiguos. |
| src/Furvo.Godot/Scripts/Main.Training.cs | Política compartida de planificación. |
| src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs | Nuevo orden, selección, formulario, guardado y confirmaciones. |
| src/Furvo.Godot/Scripts/Main.TrainingDaily.cs | Lista y totales globales del día. |
| src/Furvo.Godot/Scripts/Main.Navigation.cs | Entrada a Entrenamiento sin una selección antigua visible. |
| src/Furvo.Godot/Scripts/Components/TrainingDailyExerciseRow.cs | Fila compacta con acciones individuales. |
| src/Furvo.Godot/Scripts/Components/TrainingWeekCalendar.cs | Contorno de selección y selección desde la celda. |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos españoles centralizados. |
| tests/Furvo.Tests/TrainingDailyPlanningTests.cs | Diecinueve casos nuevos de consulta, tiempos, simultaneidad, persistencia y separación. |
| tools/GodotUiSmoke.gd | Recorrido diario y regresiones adaptadas al flujo nuevo. |
| docs/TRAINING_DAILY_PLANNING.md | Decisiones, inventario y validación. |
| docs/VALIDATION.md, docs/INVENTORY.md | Referencias al estado de la entrega. |

El árbol contiene trabajo previo sin commit; este inventario corresponde solo al encargo actual.

## Validación reproducible en Windows
```powershell
dotnet build Furvo.sln --no-restore
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
& 'C:\Users\panio\Documents\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'src/Furvo.Godot' --script '../../tools/GodotUiSmoke.gd' -- --capture --analysis-only
```

El smoke utiliza carreras aisladas en artifacts. Capturas: artifacts/career-visual/career-training-daily-calendar-*, career-training-daily-list-* y career-training-daily-form-*.

## Pruebas automatizadas ejecutadas
Antes: **465 pruebas Debug superadas** (training-daily-before.trx). Después: **484 Debug y 484 Release superadas**, sin fallos ni omisiones. Diecinueve casos nuevos conservan los anteriores. El filtro específico ejecutó 19 casos correctos. Resultados bajo artifacts/test-results: training-daily-debug.trx, training-daily-release.trx y training-daily-focused.trx.

Compilaciones Debug y Release correctas, cero errores y cero advertencias. Se probaron creación de varios registros, edición por ID, eliminación individual, otras fechas intactas, actualización del calendario, persistencia, separación de carreras y consulta sin escrituras.


## Validación nativa y visual

Godot 4.7.2 .NET con renderizado nativo finalizó con código 0. Registro: artifacts/training-daily-native.log, marcadores FURVO_TRAINING_DAILY_OK y FURVO_TRAINING_DAILY_LAYOUT_OK. Se verificaron calendario inicial sin lista/formulario, contorno persistente, navegación semanal, fecha fija, inicio/fin en la misma fila, ausencia de duraciones negativas, crear varios ejercicios, editar sin duplicar, eliminar solo uno, totales inmediatos y confirmación de descarte. También se comprobó un responsable realmente asignado con nombre/cargo y la reapertura desde SQLite.

El caso de prueba intenta deliberadamente crear un solapamiento contra una categoría no visible: el mensaje de rechazo queda en el log como WARNING esperado y el bloque no se guarda. No es una excepción sin gestionar ni una actividad ejecutada.

Se generaron capturas de calendario, lista y formulario en 1280×720, 1600×900 y 1920×1080. Se inspeccionaron capturas representativas de las tres vistas y de cada resolución; no se declara revisión exhaustiva de todos los datos posibles. El recorrido enfocado incluye regresiones de navegación, perfiles, carreras, grupos, delegación, informes y análisis de atributos. No se repitió el recorrido completo de arrastre táctico de las pruebas gráficas generales.

Las cinco publicaciones SQLite fundacionales mantienen sus SHA-256 anteriores. git diff --check pasó. No hay migraciones ni dependencias nuevas. Los borradores continúan en memoria y deben guardarse antes de cerrar. No se añade ejecución de sesiones, IA de planificación, cambios de atributos ni observaciones físicas.


Furvo se volvió a lanzar al terminar, en ventana normal de 1280×720, y se dejó ejecutándose. Registro: artifacts/training-daily-final-run.log.
