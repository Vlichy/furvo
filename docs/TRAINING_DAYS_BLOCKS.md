# Jornadas, turnos y bloques de Entrenamiento
Fecha: 01/11/2026.

## Modelo y compatibilidad
Se amplía el módulo existente, no se reconstruye. Una jornada es una fecha y su configuración (TrainingDayPlan); una sesión es uno de los dos turnos Morning/Afternoon; un bloque es una actividad concreta. Por compatibilidad de API y JSON, el tipo histórico TrainingSession y la colección TrainingPlan.Sessions conservan sus nombres, pero representan **bloques**. TrainingTurnSummary agrupa y resume la sesión de mañana o tarde.

La jornada puede configurarse como descanso, sesión única con turno o doble sesión. Una jornada sin bloques ni configuración sigue sin planificación. Guardar descanso como tipo de jornada no fabrica una actividad ni una ejecución: persiste su configuración. Los bloques de descanso anteriores se conservan.

Cada bloque mantiene ID, ejercicio, inicio, minutos, participantes, excepciones, responsable, objetivo, frecuencia, oposición y referencia táctica. El turno explícito es opcional en datos históricos. El fin y la duración se calculan; no hay otro importe de duración independiente que pueda divergir.

## Horarios y reglas
Los selectores ofrecen cuartos de hora. Cuando se consulta un bloque antiguo se incorporan sus valores exactos a las opciones, sin redondear. El fin 24:00 está disponible y es posterior al inicio; un fin anterior o igual se rechaza.

Parámetros de TrainingSchedulePolicy:
- Bloque nuevo o con horario cambiado: 15–120 minutos.
- Trabajo efectivo por turno: hasta 180 minutos.
- Trabajo efectivo por jornada: hasta 300 minutos.
- Dos turnos posibles: mañana y tarde.
- Frontera de clasificación inicial: 14:00.
- Aviso orientativo de intervalo corto entre turnos: menos de 60 minutos.

Son parámetros iniciales de diseño, no recomendaciones médicas ni límites deportivos universales. Los valores temporales se pueden proporcionar al servicio de persistencia mediante la política, sin introducir otra dependencia. El aviso de intervalo corto es informativo y no calcula fatiga.

La frontera de 14:00 es una **convención explícita**: termina hasta las 14:00 → mañana; comienza a partir de las 14:00 → tarde. Un bloque antiguo que atraviesa la frontera queda pendiente. No se cambia su horario, no se inventa un turno ni una doble sesión para resolverlo. La jornada se presenta pendiente si contiene un horario no clasificable. Guardar sus cambios descriptivos sin reclasificar conserva ese estado; reclasificar requiere seleccionar los datos correspondientes.

Los límites nuevos no obligan a reescribir bloques antiguos cuando sus horarios no cambian. Los cambios puramente descriptivos tampoco alteran la duración histórica. Una nueva actividad o cambio de horario sí se valida con la política actual.

El tiempo efectivo es la unión de intervalos de trabajo: no incluye huecos ni bloques de descanso; los grupos que trabajan simultáneamente no duplican el tiempo del turno. El intervalo transcurrido va del primer comienzo al último final, incluyendo huecos y pausas programadas. Se muestran ambas cifras al consultar el día.

Se mantienen los controles anteriores de solapamiento por jugador, responsable e instalación, edad, disponibilidad y carga planificada experimental. Por ello, las nuevas duraciones máximas **no garantizan** que cualquier combinación de bloques sea admisible: también debe superar esas reglas existentes. No se deduce fatiga observada de la planificación.

## Responsables y delegación
El servicio consulta la identidad del entrenador desde el perfil correspondiente a career_state.coach_id, y el entrenador institucional existente de cada equipo inferior. Se muestran nombre y cargo. No se crean especialistas. Los candidatos se filtran por las responsabilidades ya disponibles para la actividad y su ámbito.

Si una actividad necesita un especialista que el club aún no tiene, solo aparece Pendiente de asignación. Un responsable antiguo que ya no resulte elegible se muestra marcado, evitando sustituirlo silenciosamente por otra persona.

La delegación anterior se conserva. Una tabla resume áreas y responsables, y un único editor permite cambiar el área seleccionada. El control manual general exige confirmar la retirada de delegaciones. El estado se calcula de las asignaciones: Manual sin áreas delegadas, Mixta con algunas y Delegada cuando todas cuentan con responsable. No se permite una asignación a un empleado inexistente o incompetente para el área.

Con el personal actual puede no ser posible delegar todas las áreas. No se inventa personal para lograr ese estado. La delegación configura responsabilidades y conserva la edición manual; esta entrega no añade una IA que genere o ejecute planes automáticamente.

## Interfaz
El formulario permanece sobre el calendario de siete columnas. Su orden es fecha, tipo de jornada, turno, inicio, fin y duración calculada, ejercicio, participantes, responsable, intensidad y campos existentes complementarios, objetivo, excepciones y acciones. Se conserva la frecuencia informativa y la configuración táctica; no se generan repeticiones automáticas.

El calendario muestra tipo de jornada, mañana/tarde, iconos y rangos horarios. En doble sesión se indican ambos turnos aunque alguno no tenga bloques. +N permite consultar los restantes. Pulsar el encabezado del día abre su planificación completa con trabajo efectivo e intervalo transcurrido.

Cambiar a descanso o sesión única no borra bloques inmediatamente. Al guardar se identifica qué bloques resultan incompatibles y se exige confirmación explícita antes de retirarlos. Los bloques realizados siguen protegidos por la transacción existente. Los borradores sobreviven a la navegación en memoria, sin convertirse en actividades persistidas hasta guardar.

## Migración y persistencia
Nueva migración: **SportsTraining003_Days.sql**. Crea:
- training_day_schema, versión 1.
- training_day_plans, con clave compuesta equipo/fecha, tipos y turnos restringidos y referencia al plan correspondiente.

Se aplica dentro de SavePlan, solo al escribir una carrera, sin migrar por consultar. Los bloques y delegaciones continúan en el JSON existente. La configuración de jornadas reside en la tabla nueva; Days no se duplica dentro del JSON persistido. SaveGroup comparte esa separación.

El guardado de plan, revisión y jornadas es transaccional. Una revisión concurrente, bloque realizado o fallo de integridad revierte la operación. No se cambian perfiles ni publicaciones del mundo inicial. Las carreras independientes conservan sus archivos y configuraciones propias.

## Archivos creados o modificados en este encargo
| Archivo | Función |
|---|---|
| src/Furvo.Core/Development/Training.cs | Turno opcional de bloques, jornadas del plan y cargo del staff. |
| src/Furvo.Core/Development/TrainingSchedule.cs | Política, horarios, jornadas, resúmenes, compatibilidad y elegibilidad. |
| src/Furvo.Core/Development/TrainingCalendar.cs | Proyección de tipo de jornada y turno. |
| src/Furvo.Persistence/SqliteSportsStore.cs | Identidad real del staff, consulta y escritura transaccional de jornadas. |
| src/Furvo.Persistence/SqliteAnalysisReportService.cs | Guardado de grupos sin duplicar la configuración de jornadas. |
| src/Furvo.Persistence/Migrations/SportsTraining003_Days.sql | Extensión versionada compatible. |
| src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs | Horarios seleccionables, duración calculada, tipo de jornada, responsables y detalle completo. |
| src/Furvo.Godot/Scripts/Main.Training.cs | Tabla y editor único de delegación. |
| src/Furvo.Godot/Scripts/Components/TrainingWeekCalendar.cs | Turnos y rangos horarios en la cuadrícula existente. |
| src/Furvo.Godot/Resources/UiStrings.resx | Terminología y mensajes centralizados. |
| tests/Furvo.Tests/TrainingScheduleTests.cs | Veinte casos nuevos de dominio, migración, persistencia y compatibilidad. |
| tools/GodotUiSmoke.gd | Recorrido de horarios, duración, responsables, cambio a descanso y delegación. |
| docs/TRAINING_DAYS_BLOCKS.md | Decisiones, inventario, comandos y resultados. |
| docs/VALIDATION.md, docs/INVENTORY.md | Referencias al estado de la ampliación. |

El repositorio contiene cambios anteriores sin commit; el inventario anterior corresponde únicamente a este encargo.

## Comandos de validación
Desde la raíz del proyecto en PowerShell:
```powershell
dotnet build Furvo.sln --no-restore
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
& 'C:\Users\panio\Documents\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'src/Furvo.Godot' --script '../../tools/GodotUiSmoke.gd' -- --capture --analysis-only
```

Las carreras de prueba son aisladas bajo artifacts. No se utilizan perfiles ni carreras personales.


## Pruebas automatizadas y conservación de datos

Antes del cambio: **445 pruebas Debug correctas** (training-blocks-before.trx). Después: **465 pruebas correctas en Debug y 465 en Release**, sin fallos ni omisiones. Se conservan las 445 anteriores y se añaden veinte casos. Registros: artifacts/test-results/training-blocks-debug.trx y training-blocks-release.trx. La compilación final de ambas configuraciones no presenta errores ni advertencias.

La migración se probó sobre una carrera aislada con horarios antiguos de 09:07, duración de cinco minutos y delegación existente. Se conservan exactamente; la consulta previa no escribe y el guardado crea la extensión versionada. Se comprobó separación entre carreras y ausencia de ejecución por guardar. Las cinco publicaciones fundacionales conservan sus SHA-256.

Durante la comprobación hubo un bloqueo temporal al compilar el ensamblado de pruebas mientras otra suite lo utilizaba. Se esperó su finalización y se recompiló correctamente; los recuentos anteriores corresponden a ejecuciones completas posteriores.


## Validación visual y ejecución final

Godot 4.7.2 .NET ejecutó el recorrido enfocado final con código 0: training-blocks-native.log incluye FURVO_TRAINING_BLOCKS_OK, FURVO_TRAINING_CALENDAR_OK y las regresiones de Análisis/navegación. Se comprobaron inicio/fin, duración, responsable real, ambos turnos, consulta de jornada y cancelación segura del cambio a descanso. Se comprobaron 1280×720, 1600×900 y 1920×1080 y se inspeccionaron capturas del formulario, jornada y delegación.

La revisión detectó un ancho mínimo excesivo en 1280. Se corrigió la cuadrícula del formulario (dos columnas por debajo de 1400 píxeles y cuatro en resoluciones mayores), el tamaño de desplegables y la tabla/editor de delegación. La comprobación final verifica que los controles y la cabecera caben en pantalla.

Capturas bajo artifacts/career-visual: career-training-blocks-form-*, career-training-day-overview-* y career-training-delegation-*. La prueba enfocada conserva navegación, perfiles, carreras, calendario e informes, pero no se ha repetido el recorrido completo de arrastre táctico en este encargo.

Sin nuevas dependencias ni ejecución temporal de sesiones. La gestión completamente delegada exige personal elegible en todas las áreas; si falta, se mantiene control manual o mixto. No se inventan especialistas ni decisiones automáticas. Los borradores son de interfaz y requieren guardar antes de cerrar.


Furvo se lanzó al finalizar en ventana normal de 1280×720 y se dejó ejecutándose. Registro: artifacts/training-blocks-final-run.log.
