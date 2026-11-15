# Cantera — Entrega 1: seguimiento individual e historial

Fecha: 04/11/2026. Alcance: observaciones humanas, consulta de informes e historial existente y conservación del contexto visual. No se implementan nuevos movimientos, simulación ni generación de jugadores.

## Funcionalidades implementadas

Desde la ficha de un canterano, **Seguimiento e historial** abre tres apartados compactos: Observaciones, Informes y Recomendaciones y movimientos. La ficha, tablas, permisos de carrera y fuentes SQLite existentes se reutilizan.

### Observaciones

Cada registro conserva ID propio estable, PlayerId, equipo inferior de origen, fecha de carrera, autor real y nombre conservado, texto, última modificación opcional y revisión de concurrencia. Se puede crear, consultar, editar, ordenar por fecha y eliminar con confirmación. La consulta siempre filtra por ID del jugador, nunca por su nombre. En fechas iguales se usa el ID como desempate estable, sin inventar una hora de registro.

La interfaz registra al entrenador principal conectado como autor. El servicio admite además al entrenador institucional real del equipo; no permite escribir en nombre de responsables inventados. Solo el autor autorizado en ese contexto o el entrenador principal pueden editar o eliminar. El autor y fecha original no cambian al editar. Las revisiones rechazan escrituras y borrados obsoletos. Una observación vacía no se guarda y mantiene abierto el formulario.

No se generan textos ni evaluaciones automáticamente. Las observaciones de los recorridos de prueba se escriben únicamente en sus carreras aisladas.

### Informes

La consulta busca informes existentes de todos los equipos del club que contengan al jugador, incluidas instantáneas colectivas e individuales. No genera un informe al abrir la pantalla. Se reutilizan SqliteSportsStore y el presentador extraído de Análisis, RenderStoredAnalysisReport.

Se muestran fecha, responsable conservado en la instantánea, prioridades y datos públicos del jugador. La consulta desde Seguimiento es de solo lectura; el botón Abrir este informe en Análisis conserva su identidad y ofrece regreso directo al mismo seguimiento. Los informes del mismo día se distinguen también mediante una referencia corta de su ID.

La comparación elige dos instantáneas registradas y presenta los 51 atributos visibles de ambas. Se valida la exclusión de ocultos; no se persiste la comparación, se calcula una valoración global ni se atribuyen los cambios al entrenamiento. También puede compararse información del mismo día, identificada como comparación de instantáneas y no como evolución temporal. Los informes anteriores sin instantánea pública completa pueden consultarse, pero no se inventan datos para compararlos.

### Recomendaciones y movimientos

Se reutilizan movement_recommendations y movement_history. La nueva proyección History del almacén existente solo consulta. La línea cronológica distingue Recomendación y Movimiento ejecutado, conserva los estados reales del modelo, muestra origen/destino, motivo, responsable conocido, decisión y referencia disponibles.

Una recomendación pendiente o rechazada no aparece como movimiento realizado. Los intentos bloqueados que nunca se persistieron no se convierten en eventos históricos ficticios. El resumen normativo registrado se muestra como parte de los datos existentes.

El formato histórico no registra explícitamente autor y fecha de cada decisión ni la relación causal completa entre una decisión y una fila de movimiento: se indica No registrado y no se deduce del entrenador actual. No se modifica ese formato ni se reescriben decisiones para completar datos ausentes. La pantalla de consulta no ofrece controles de aceptación ni ejecución; el bloqueo normativo actual permanece intacto.

## Contexto de navegación

AcademyRosterContexts mantiene en memoria, por carrera y equipo, jugador seleccionado, columna y dirección de orden, filtros de nombre/posición principal y sección abierta. Se reutiliza DataTable con selección por ID. El regreso desde ficha, seguimiento, Análisis y la consulta previa de movimientos recupera ese contexto. La lista general recuerda también el equipo seleccionado.

Si el jugador ya no aparece en la plantilla o en el filtro actual, se limpia la selección de forma segura. Al cambiar de carrera se limpian los contextos; no se añaden preferencias visuales a SQLite ni se promete conservarlas tras cerrar el proceso. Las observaciones e informes sí persisten al reabrir.

## Persistencia y compatibilidad

Nueva migración incremental **AcademyFollowup001_Initial.sql**, versión 1 del seguimiento: tablas academy_followup_schema y academy_observations e índice por jugador/fecha. Solo se ejecuta transaccionalmente al guardar la primera observación. Leer una carrera anterior no crea tablas ni datos.

El equipo tiene clave foránea a academy_teams. La identidad y pertenencia del jugador y la autorización del actor se verifican mediante los servicios existentes, porque los jugadores de cantera y primer equipo no residen todos en la misma tabla. Editar y borrar conservan el aislamiento de archivo de carrera y utilizan revisiones. Eliminar una observación no borra informes ni movimientos.

No hay nuevas dependencias. No se modifican las cinco publicaciones fundacionales, los 480 jugadores originales ni el catálogo de 68 equipos. Las cinco bases conservan su SHA-256 previo y el catálogo no presenta diferencias. Las pruebas no usan carreras personales.

## Inventario de esta entrega

| Archivo | Cambio |
|---|---|
| src/Furvo.Core/Academy/AcademyFollowup.cs | Nuevo: observación, validación y comparación pública de instantáneas |
| src/Furvo.Core/Development/AcademyMobility.cs | Proyección de fila histórica, sin nuevas operaciones |
| src/Furvo.Persistence/SqliteAcademyFollowupStore.cs | Nuevo: CRUD autorizado y consulta de informes existentes |
| src/Furvo.Persistence/Migrations/AcademyFollowup001_Initial.sql | Nuevo: extensión incremental por carrera |
| src/Furvo.Persistence/SqliteAcademyMobilityStore.cs | Consulta de movement_history por jugador |
| src/Furvo.Godot/Scripts/Components/AcademyRosterContext.cs | Nuevo: estado visual puro, sin dependencia de Godot ni SQLite |
| src/Furvo.Godot/Scripts/Main.AcademyFollowup.cs | Nuevo: seguimiento, formulario, informes e historial de consulta |
| src/Furvo.Godot/Scripts/Main.Academy.cs | Acceso desde ficha, filtros y recuperación de selección/orden/sección |
| src/Furvo.Godot/Scripts/Main.AnalysisReports.cs | Presentador reutilizable; foco por jugador y modo de solo lectura |
| src/Furvo.Godot/Scripts/Main.SportsAnalysis.cs | Regreso contextual a Seguimiento |
| src/Furvo.Godot/Scripts/Main.Navigation.cs | Limpieza por carrera y control de la ruta de regreso |
| src/Furvo.Godot/Scripts/Main.PlayerMovement.cs | Solo regreso a la sección original de Cantera; permisos y ejecución intactos |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos españoles centralizados |
| tests/Furvo.Tests/AcademyFollowupTests.cs | Nuevo: 19 casos de seguimiento, lecturas, permisos y contexto |
| tests/Furvo.Tests/Furvo.Tests.csproj | Enlace de la política visual pura para probar su fuente real |
| tools/GodotUiSmoke.gd | Recorrido de seguimiento y selección aislada --academy-followup-only |
| docs/ACADEMY_FOLLOWUP_DELIVERY_1.md | Nuevo: este informe |
| docs/INVENTORY.md y docs/VALIDATION.md | Inventario y validación |

## Pruebas y compilación

Antes de modificar: repositorio limpio y **575 pruebas Debug superadas**, con compilación nueva.

| Ejecución | Total | Superadas | Fallidas | Omitidas |
|---|---:|---:|---:|---:|
| Nuevas pruebas focalizadas Debug | 19 | 19 | 0 | 0 |
| Solución Debug | 594 | 594 | 0 | 0 |
| Solución Release | 594 | 594 | 0 | 0 |

Se añaden 19 casos sin retirar ni desactivar pruebas. Cubren CRUD/reapertura, aislamiento, lectura antigua sin migración, identidad/texto/autor, permisos de otros técnicos, concurrencia, filtrado por PlayerId, informes colectivos/individuales sin reescritura, comparación sin ocultos, historial independiente de recomendaciones, bloqueo normativo y contexto de tabla en los seis órdenes y filtros.

El caso de historial prepara una fila sintética en la base temporal para representar un registro anterior; no ejecuta una promoción nueva ni modifica una carrera real. Las comprobaciones de hash verifican que consultar informes, comparaciones e historial no escriba datos.

Compilaciones reales de la solución Debug y Release correctas, **0 errores y 0 advertencias**, también después de los últimos ajustes de presentación. Las pruebas se ejecutan con compilación habilitada, sin --no-build.

Resultados: artifacts/test-results/academy-followup-{before,focused,debug,release}.trx.

## Godot y revisión visual

El recorrido final específico --academy-followup-only termina con código 0, FURVO_ACADEMY_OK, FURVO_ACADEMY_FOLLOWUP_OK y FURVO_UI_SMOKE_OK. Se ejecuta la escena C# real con controles y eventos de Godot en una carrera aislada. No se presenta como una prueba manual exhaustiva.

Verificados: crear/validar/editar/eliminar observaciones con confirmación y cancelación, reapertura, vacíos de informes e historial, consulta de dos informes creados expresamente mediante Análisis, comparación de 51 visibles, regreso desde Análisis y conservación de jugador, equipo, sección, filtro y orden. La recomendación de prueba permanece pendiente y no se ejecuta ningún movimiento. Se comprueban también la independencia táctica y las seis disposiciones existentes.

Capturas generadas y revisadas en 1280×720, 1600×900 y 1920×1080: Observaciones, Informes y Recomendaciones y movimientos. El desplazamiento conserva el acceso a información extensa y los botones de regreso permanecen fuera del contenido desplazable. Una primera ejecución del ensayo no emitía TextChanged al asignar el filtro por código; se corrigió únicamente esa simulación de entrada y el recorrido final pasó.

Registro específico final: artifacts/academy-followup-native-final.log. Capturas: artifacts/career-visual/career-academy-followup-*.png.

El recorrido completo se ha ejecutado y termina con código 1 después de superar Cantera y Seguimiento: reaparece **Ubicación sin diseñar permite guardar**, en check_training_free_kicks (tools/GodotUiSmoke.gd:2072). La secuencia previa check_instruction_expansion configura las cuatro ubicaciones de faltas, mientras esa prueba negativa presupone una ubicación sin diseño. Se mantiene como incidencia de aislamiento del recorrido global; no se presenta como un error demostrado de Cantera ni se declara superada la ejecución global. No se modifica Entrenamiento ni se elimina o desactiva la comprobación. Registro: artifacts/academy-followup-full-native.log.

## Validación local

~~~powershell
Set-Location -LiteralPath 'C:\Users\panio\Documents\Getion Furvo'
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-restore
~~~

En una carrera de prueba: Cantera → equipo → Plantilla o Desarrollo → futbolista → Seguimiento e historial. Crear una nota, editarla, abrir un informe existente en Análisis y regresar. Comprobar el filtro/orden/selección de la tabla, reabrir y eliminar una nota con confirmación. Los estados vacíos son normales cuando no hay registros.

## Limitaciones y siguiente entrega

No se completa retrospectivamente información de decisiones que nunca se persistió. No se añade auditoría de todas las revisiones del texto ni recuperación de borradores tras caída. La edición se realiza en una ventana modal y solo Guardar persiste. El contexto visual dura la sesión de interfaz y los informes antiguos sin instantánea completa no se convierten a otro formato.

Quedan pendientes para futuras instrucciones las relaciones temporales con el primer equipo, convocatorias, nuevos movimientos efectivos, normativa adicional, contratos juveniles, competiciones y simulación. No se generan juveniles, progresión, potencial numérico ni decisiones de IA. No hay commits ni push.

Furvo se ha abierto normalmente al terminar y se deja ejecutándose. Registro: artifacts/academy-followup-final-run.log.

## Estado de la entrega

- **Seguimiento individual: completado.**
- **Consulta de informes: completada, reutilizando Análisis.**
- **Historial de recomendaciones y movimientos: completado como consulta de registros existentes, con ausencias de datos explícitas.**
- **Conservación del contexto de navegación: completada durante la sesión de interfaz.**

Se detiene el trabajo en esta entrega; no se inician las relaciones temporales entre Cantera y primer equipo.
