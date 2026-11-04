# Entrenamiento — Correcciones funcionales, Entrega 1

Fecha: 04/11/2026. Alcance exclusivo: borradores de grupos, eliminación segura y jugadas ensayadas por ubicación.

## A. Borradores de grupos

Antes, el formulario almacenaba la edición solo en sus controles. Ahora existe un borrador identificado por carrera y equipo, con identificador de grupo, nombre, objetivo, integrantes, responsable y copia del grupo guardado. Se distinguen grupo nuevo, grupo guardado y edición pendiente. Cambiar de pestaña conserva el borrador; consultar o editar no escribe SQLite.

Cambiar de equipo, sección o carrera, volver al inicio, cerrar la aplicación o sustituir/cancelar una edición pendiente ofrece Guardar cambios, Descartar cambios y Cancelar y seguir editando. Guardar valida antes de persistir: un grupo incompleto mantiene el diálogo y muestra el campo que falta. Cancelar conserva el borrador. Cancelar edición de grupo afecta únicamente a ese borrador. Se comprueba que el grupo guardado no haya cambiado externamente antes de sobrescribirlo.

El mismo editor se reutiliza desde Análisis; se adapta exclusivamente esa llamada compartida para que no existan dos formularios ni se pierda el borrador. Los puntos de navegación y cierre de Carrera se modifican solo para proteger la edición de Entrenamiento.

## B. Eliminación y participantes históricos

Eliminar grupo usa su identificador estable y solicita confirmación. Se bloquea si existen referencias en actividades, descansos históricos archivados, evidencias de ejecución o informes. El mensaje identifica las referencias con fecha y descripción. No se borran actividades, informes ni participantes para permitir la operación. El repositorio impide también el intento de retirar grupo y ejercicios conjuntamente mediante SavePlan.

Guardar integrantes actualiza únicamente actividades pendientes desde la fecha de carrera. Las sesiones pasadas y realizadas mantienen su instantánea completa. Las realizadas se validan contra su evidencia original al escribir, conservando su bloqueo incluso si la rutina táctica ya no está configurada. El ejecutor existente excluye las sesiones ya realizadas de la revalidación contra la composición actual; no se cambian sus fórmulas ni se activa simulación.

Un selector de ejercicio que conserve un grupo eliminado deja esa selección pendiente en lugar de sustituirla automáticamente por otro grupo. Si una composición nueva produce solapamientos o deja una actividad sin participantes elegibles, se rechaza el guardado y se conserva el borrador: no se modifica la agenda de forma silenciosa.

## C. Jugadas ensayadas

Antes, el selector mezclaba todos los tipos y no ofrecía nuevas ubicaciones de faltas. Ahora Core define tipos compatibles por ejercicio y Godot consulta la configuración real del equipo para el plan Inicial, como hacía el almacén anterior. Córners, faltas, penaltis y saques no se mezclan.

Las faltas permiten seleccionar ubicación y muestran la jugada realmente configurada. Los tipos sin diseño se identifican y deshabilitan; una ubicación pendiente muestra un mensaje específico y bloquea Guardar. Al editar se recuperan tipo, ubicación y referencia. Entrenamiento no escribe Táctica.

Las referencias anteriores sin ubicación conservan su significado y se pueden consultar como Referencia anterior (sin ubicación), sin conversión automática. Los penaltis mantienen la excepción previa del modelo, que no utiliza una rutina posicional: no se crea una rutina ficticia ni se modifican especialistas.

## Inventario

| Archivo | Cambio |
|---|---|
| src/Furvo.Core/Development/TrainingGroups.cs — nuevo | Borrador portable por contexto, decisiones de salida, referencias, actualización pendiente y eliminación |
| src/Furvo.Core/Development/TrainingRoutines.cs — nuevo | Compatibilidad y resolución de referencias por ubicación o legado |
| src/Furvo.Core/Development/Training.cs | Validación compartida de tipos y ubicaciones |
| src/Furvo.Persistence/SqliteSportsStore.cs | Guardar/eliminar grupos, consultar referencias/táctica y preservar sesiones realizadas |
| src/Furvo.Persistence/SqliteTrainingExecutionStore.cs | Validación de actividades pendientes conservando evidencias ejecutadas |
| src/Furvo.Godot/Scripts/Main.TrainingGroups.cs — nuevo | Editor único, borrador, diálogo de tres opciones y eliminación explicada |
| src/Furvo.Godot/Scripts/Components/TrainingRoutinePicker.cs — nuevo | Selectores dependientes y estado del diseño real |
| src/Furvo.Godot/Scripts/Main.Training.cs | Composición del editor reutilizable |
| src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs | Protección de salida, grupo ausente y selector táctico |
| src/Furvo.Godot/Scripts/Main.Navigation.cs | Protección de cambio de sección/carrera |
| src/Furvo.Godot/Scripts/Main.cs | Protección del inicio y cierre normal de la aplicación |
| src/Furvo.Godot/Scripts/Main.AnalysisReports.cs | Adaptación estricta de la llamada al editor de grupos compartido |
| src/Furvo.Godot/Resources/UiStrings.resx | Mensajes centralizados en español |
| tests/Furvo.Tests/TrainingGroupCorrectionsTests.cs — nuevo | 13 casos de borradores, eliminación, referencias e historial |
| tests/Furvo.Tests/TrainingRoutineCorrectionsTests.cs — nuevo | 11 casos de compatibilidad, ubicaciones y referencias anteriores |
| tools/GodotUiSmoke.gd | Recorridos nuevos y capturas, manteniendo regresiones anteriores |
| docs/TRAINING_FUNCTIONAL_FIXES_1.md, INVENTORY.md y VALIDATION.md | Informe e inventario |

Los cambios previos del checkout se han conservado. No hay nuevos paquetes, esquemas SQL ni publicaciones del mundo. Se reutiliza training_plans.data_json y los archivos históricos existentes.

## Pruebas y compilación

Base inicial: **521 pruebas Debug correctas**, compiladas y ejecutadas antes de editar. Se añaden **24 pruebas**, sin eliminar ni desactivar las anteriores. Resultado Debug final: **545 superadas, 0 fallidas, 0 omitidas**. Resultado Release final: **545 superadas, 0 fallidas, 0 omitidas**. Compilaciones reales Debug y Release de la solución correctas, sin advertencias ni errores; se compilan también los cambios finales de presentación.

Los ensayos de historial usan fixtures aislados con el mismo formato JSON del almacén. No se modifica la fecha de carrera ni se retiran sus protecciones: los registros de fecha anterior se preparan únicamente en la base temporal de prueba.

El recorrido nativo de Godot ha terminado con código 0 y prueba: borrador entre pestañas, rechazo de Guardar incompleto, Guardar/Descartar/Cancelar al salir, grupo usado bloqueado con fecha, eliminación confirmada y reapertura, falta diseñada desde el editor real de Táctica de una carrera aislada, rechazo de ubicación sin diseño, guardado y recuperación de la ubicación. También se ejecutan las regresiones de calendario, descanso, modalidades, participantes, delegación, Análisis y navegación.

Se han generado capturas de grupos, diálogo y faltas en 1280×720, 1600×900 y 1920×1080. Durante la revisión se detectó y corrigió una superposición del error del diálogo; el recorrido final se ha repetido con código 0 y las capturas corregidas se han revisado en las tres resoluciones, sin superposición. El aviso de solapamiento de la prueba negativa preexistente es esperado, no un error de consola.

Evidencia: artifacts/test-results/training-fixes1-{before,debug,release}.trx; artifacts/training-fixes1-native.log; artifacts/career-visual/career-training-fixes1-*.png.

~~~powershell
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-restore
~~~

## Límites

Los borradores son temporales, en memoria; el cierre normal está protegido, pero no se añade recuperación ante caída del proceso. No existe un historial versionado de composición de grupos: se conservan las instantáneas de actividades y evidencias. La eliminación de un grupo con referencias se mantiene bloqueada hasta resolverlas expresamente. Las partidas personales no se utilizan para validar; Táctica solo se diseña en fixtures y en la carrera aislada de Godot. Quedan fuera disponibilidad, plantillas reutilizables, progreso de atributos, simulación, fatiga e IA. La segunda revisión de cierre queda pendiente de autorización.
