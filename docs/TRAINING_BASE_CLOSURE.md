# Cierre de la estructura base de Entrenamiento — Entrega 2

Fecha: 04/11/2026. Proyecto existente C:\Users\panio\Documents\Getion Furvo. Se conserva la arquitectura Core/Persistence/Godot, .NET 10 y los cambios locales anteriores. Sin commits, push ni dependencias nuevas.

## Implementación

Se añade disponibilidad administrativa por futbolista y fecha, reutilizable desde Core. La pertenencia a plantilla o grupo no equivale a disponibilidad. No se deducen restricciones de atributos, sanciones ni observaciones físicas y no se generan ausencias automáticamente.

- **Disponible**: no hay restricciones activas para el jugador en la fecha consultada.
- **No disponible**: existe una restricción completa activa; impide cualquier participación durante ese intervalo.
- **Parcial**: todas las restricciones activas deben autorizar expresamente el código del ejercicio. Sin una lista estructurada de actividades autorizadas, la participación queda pendiente de resolución explícita.

Los motivos Lesión, Enfermedad, Convocatoria internacional, Permiso personal, Restricción médica y Otra causa son etiquetas administrativas. Una lesión no provoca por sí sola una prohibición de recuperación activa: se aplica el estado y la autorización registrados. Una sanción de competición no introduce una restricción de entrenamiento.

## Modelo y reglas

TrainingRestriction conserva ID estable propio, PlayerId, estado, motivo, inicio, final opcional, notas, origen opcional, ejercicios expresamente permitidos y revisión para concurrencia. Se emplea DateOnly; ambos extremos son inclusivos. Sin fecha final, la restricción sigue vigente hasta su modificación. Finalizar conserva el registro y fija el último día restringido; el siguiente día se evalúa sin esa restricción.

Varias restricciones simultáneas se combinan de forma conservadora: una completa prevalece; entre parciales, el ejercicio debe estar autorizado en todas. No existe evaluación médica, inferencia de compatibilidad por categoría, diagnóstico ni cálculo de recuperación. La autorización es el dato administrativo introducido explícitamente, no una certificación médica verificada por el programa.

TrainingAvailability.Check, Select, Conflicts y Validate comparten la misma regla. No contienen atributos de futbolistas ni acceden a los diez ocultos. Las restricciones futuras se consultan en la fecha del ejercicio, independientemente de la fecha actual de carrera.

## Planificación e interfaz

El acceso compacto **Disponibilidad de futbolistas — Consultar y registrar restricciones** se encuentra dentro de Entrenamiento, sin pestaña principal nueva. Abre una ventana modal con desplazamiento: jugador identificado por ID y nombre/posición, registros previos, estado, motivo, fechas, notas, origen y actividades autorizadas para restricciones parciales. Permite crear, consultar, editar, guardar y finalizar. Las fechas incorrectas mantienen el formulario abierto con error, sin escritura parcial.

Los selectores de participantes y excepciones conservan los IDs y muestran nombre, posición, estado y motivo cuando corresponde. La selección individual incompatible queda deshabilitada. Equipo, demarcación y grupo pasan por la comprobación individual: se muestran las exclusiones propuestas y Guardar las confirma únicamente para ese ejercicio mediante su lista de excepciones. No se retiran jugadores de la plantilla ni de los grupos. No se permite guardar una actividad sin participantes elegibles.

Una restricción posterior no borra ni modifica ejercicios existentes. Se muestran conflictos fechados, con ejercicio, jugador y motivo. Una actividad nueva o modificada debe cumplir la disponibilidad al guardarse; guardar otros aspectos del plan conserva las actividades intactas aunque estén pendientes de revisión. La validación se repite dentro de la transacción contra las restricciones vigentes, evitando depender exclusivamente del estado de los controles.

Las sesiones pasadas y realizadas conservan sus instantáneas y participantes. El consumidor de ejecución ya existente recibe la misma validación como protección de integración, sin habilitar ejecución ni modificar sus fórmulas.

## Persistencia y compatibilidad

Nueva migración incremental **TrainingAvailability001_Initial.sql**, esquema de disponibilidad versión 1: tabla de versión, tabla de restricciones e índice por jugador/fechas. Se aplica transaccionalmente solo al guardar expresamente la primera restricción. Consultar una carrera antigua devuelve una colección vacía y no crea tablas ni registros.

El aislamiento es por archivo de carrera y PlayerId, sin referencias a perfiles compartidos. La pertenencia se comprueba con el almacén general de plantillas: los futbolistas de cantera existentes no residen todos en la tabla players, por lo que no se introduce una clave foránea incorrecta que los excluya. Las identidades no se pueden cambiar al editar y la revisión impide sobrescribir cambios concurrentes. Un guardado inválido revierte también la migración si era la primera escritura.

No se modifican las publicaciones del mundo ni se ejecutan migraciones sobre carreras personales. Las cinco bases fundacionales conservan exactamente su SHA-256 previo; no se han cambiado los 480 jugadores, sus atributos, contratos ni reputaciones. Las pruebas utilizan bases temporales y la interfaz una carrera aislada en artifacts.

## Archivos de esta entrega

| Archivo | Estado y función |
|---|---|
| src/Furvo.Core/Development/TrainingAvailability.cs | Nuevo: restricciones, decisiones, selección y conflictos de dominio |
| src/Furvo.Persistence/SqliteTrainingAvailabilityStore.cs | Nuevo: lectura sin escritura, guardado transaccional, edición y finalización |
| src/Furvo.Persistence/Migrations/TrainingAvailability001_Initial.sql | Nuevo: extensión incremental por carrera |
| src/Furvo.Persistence/SqliteSportsStore.cs | Modificado: comprobación de actividades nuevas/modificadas bajo transacción |
| src/Furvo.Persistence/SqliteTrainingExecutionStore.cs | Modificado: protección del consumidor existente sin activar simulación |
| src/Furvo.Godot/Scripts/Main.TrainingAvailability.cs | Nuevo: acceso compacto, conflictos y editor administrativo modal |
| src/Furvo.Godot/Scripts/Main.Training.cs | Modificado: integración del panel en las siete vistas existentes |
| src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs | Modificado: disponibilidad por fecha/ejercicio, etiquetas y exclusiones confirmadas |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado: textos españoles centralizados |
| tests/Furvo.Tests/TrainingAvailabilityTests.cs | Nuevo: 30 casos de dominio, selección, persistencia y protección histórica |
| tools/GodotUiSmoke.gd | Modificado: recorrido aislado de disponibilidad y capturas, manteniendo regresiones |
| docs/TRAINING_BASE_CLOSURE.md | Nuevo: este informe |
| docs/INVENTORY.md | Modificado: entrada de inventario |
| docs/VALIDATION.md | Modificado: resultados de cierre |

El estado Git incluye trabajo previo de Entrenamiento; el inventario anterior distingue exclusivamente esta entrega. No se han reconstruido los módulos existentes.

## Compilación y pruebas reales

Referencia antes de editar: dotnet test Furvo.sln -c Debug --no-restore, con compilación: **545/545 correctas**, 0 fallidas y 0 omitidas.

Compilaciones de solución Debug y Release correctas, **0 advertencias y 0 errores**. Después de los últimos ajustes de presentación, se recompila también Furvo.Godot en ambas configuraciones, con el mismo resultado. Las pruebas finales se ejecutan con compilación habilitada, sin --no-build.

| Ejecución final | Total | Superadas | Fallidas | Omitidas |
|---|---:|---:|---:|---:|
| Debug | 575 | 575 | 0 | 0 |
| Release | 575 | 575 | 0 | 0 |

**30 casos nuevos**, sin retirar ni desactivar pruebas. Cubren ausencia de restricciones, fechas inclusivas/futuras/finalizadas/sin fin, datos inválidos, restricciones parciales y superpuestas, cuatro ámbitos de participantes, compatibilidad por ejercicio, conflictos posteriores sin escritura, registro/modificación/finalización/reapertura, concurrencia, carrera antigua sin migración de lectura, aislamiento y conservación de sesiones pasadas/realizadas y atributos.

Evidencia: artifacts/test-results/training-closure-before.trx, training-closure-debug.trx y training-closure-release.trx. Una primera ejecución focalizada detectó un error en el fixture al construir un ID vacío; se corrigió el fixture usando default y las ejecuciones completas finales incluyen el caso correcto.

## Godot y evidencia visual

El recorrido nativo final de Godot 4.7.2 .NET termina con **código 0** y FURVO_UI_SMOKE_OK. Se usan controles reales de la aplicación mediante automatización de señales y selección, en una carrera aislada. No se presenta como una prueba manual exhaustiva con ratón.

Verificado: navegación entre siete pestañas, calendario compartido, sesiones únicas/dobles, creación/edición, descanso completo, grupos y borradores, eliminación segura, delegación, faltas por ubicación, registro de restricciones y errores de fechas, conflicto sobre ejercicio previo, reapertura, estado parcial, exclusiones colectivas, recuperación activa autorizada y finalización inclusiva. Se mantienen los recorridos anteriores de Análisis y navegación.

Capturas finales generadas y revisadas en **1280×720, 1600×900 y 1920×1080**: editor, autorizaciones con desplazamiento, participantes y calendario con Delegación contraída. Se corrigieron un encabezado innecesario y el exceso de altura del acceso a disponibilidad que desplazaba el calendario. La repetición final conserva las comprobaciones geométricas de Delegación; no se elimina ni relaja esa prueba.

Registro: artifacts/training-closure-native.log. Capturas: artifacts/career-visual/career-training-closure-{editor,authorization,participants}-*.png y career-training-delegation-collapsed-*.png. El aviso de solapamiento entre ejercicios pertenece a una prueba negativa preexistente esperada. El recorrido final no presenta errores inesperados.

Furvo se ha abierto normalmente al terminar y se deja ejecutándose. Registro de arranque: artifacts/training-closure-final-run.log.

## Validación local y límites

~~~powershell
Set-Location -LiteralPath 'C:\Users\panio\Documents\Getion Furvo'
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-restore
~~~

Para revisión manual adicional, utilizar una carrera de prueba: crear un ejercicio futuro, registrar una ausencia coincidente, comprobar que el conflicto no borra el ejercicio, resolver explícitamente sus participantes y reabrir. Revisar una parcial con recuperación activa autorizada y finalización inclusiva. No hace falta usar carreras personales para esta comprobación.

La autorización parcial es por código de ejercicio; no modela límites médicos de intensidad, minutos o esfuerzo. No se añade auditoría completa de versiones de restricciones ni recuperación de borradores ante caída. El editor guarda o cancela expresamente; los cambios sin guardar no son registros persistidos. Las restricciones se registran manualmente y todavía no las originan servicios médicos, selecciones ni permisos automatizados.

Quedan expresamente aplazados simulación de entrenamiento, progresión, fatiga dinámica, lesiones automáticas, recuperación médica simulada, carga avanzada, IA autónoma, informes posteriores a sesiones, plantillas semanales reutilizables y nuevas mecánicas de Cantera. La entrega no produce consecuencias deportivas por guardar ejercicios.

## Conclusión

**SÍ. Está cerrada la estructura base de Equipo → Entrenamiento y se puede continuar con Equipo → Cantera dentro del alcance aprobado.** Planificación, grupos, responsables, jugadas ensayadas y disponibilidad administrativa están integrados y validados. No se ha iniciado Cantera en esta entrega; se detiene el trabajo hasta una nueva instrucción.
