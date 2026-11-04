# Gestión y delegación — 03/11/2026

## Presentación

Se sustituye la antigua tabla y formulario al final de Resumen por un único panel transversal inmediatamente debajo de las pestañas. Comienza contraído. La cabecera muestra Gestión y delegación y el estado realmente guardado: Control manual, Gestión mixta o Delegada; los borradores se identifican como pendientes. Expandir o contraer no guarda datos.

La tabla reúne área, modalidad y responsable. Utiliza las nueve responsabilidades reales: planificación semanal, colectivo, individual, demarcación, grupos, gimnasio, porteros, jugadas ensayadas y recuperación. No se inventa una responsabilidad Tecnología: el modelo actual la resuelve según el ámbito individual/colectivo. No hay una segunda configuración.

Cada fila permite Manual, Delegada y Mixta. Los responsables son técnicos reales, con nombre y cargo, filtrados por responsabilidades y excluyendo al propio entrenador principal como delegado. Si no hay técnico, se indica y no pueden elegirse modalidades delegadas. No puede guardarse una asignación no manual sin técnico elegible. Manual se representa mediante ausencia de asignación, como antes.

Los cambios se guardan explícitamente y se confirma el resultado. Control manual en todas las áreas modifica un borrador tras confirmación y también requiere Guardar cambios. Contraer un panel con cambios o cambiar de equipo/sección avisa antes de descartarlos. Cancelar conserva el borrador. Cambiar de pestaña conserva tanto el borrador como fecha, semana y agenda global. La tabla tiene desplazamiento interno y altura limitada, con columnas compactas y más espacio para el responsable. Los grupos conservan su sección propia en Resumen.

## Dominio y persistencia

TrainingDelegation incorpora Mode opcional (Delegated por defecto, Mixed para colaboración con control del entrenador). Se guarda en el mismo JSON de TrainingPlan y mediante el mismo SqliteSportsStore.SavePlan. Los JSON anteriores sin Mode se leen como delegaciones completas; no se reinterpretan ni reescriben al consultar. Se validan modo y técnico en Core. El estado global solo es Delegada si todas las áreas están delegadas plenamente; cualquier área mixta produce Gestión mixta.

No se requieren migraciones SQL ni tablas, servicios, publicaciones o dependencias nuevos. Guardar relee la agenda y aplica exclusivamente las asignaciones: conserva sesiones/grupos guardados durante el borrador. Si las delegaciones cambiaron externamente, se avisa y no se sobrescriben. Se conserva la comprobación transaccional de revisión del repositorio existente. Las modalidades no ejecutan IA, decisiones deportivas ni entrenamientos automáticamente. En las áreas manuales con personal disponible, la columna Responsable identifica al entrenador principal; si no hay técnicos elegibles se mantiene el aviso específico y el control manual.

## Archivos de este encargo

- src/Furvo.Core/Development/Training.cs: modo por área, valor compatible y validaciones.
- src/Furvo.Godot/Scripts/Main.TrainingDelegation.cs (nuevo): panel único, tabla, borradores, elegibilidad, guardado y confirmaciones.
- src/Furvo.Godot/Scripts/Main.Training.cs: ubicación transversal y retirada de los controles antiguos.
- src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs: protección de borradores al cambiar equipo.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: protección al abandonar Entrenamiento y limpieza por carrera.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados.
- tests/Furvo.Tests/TrainingDelegationPanelTests.cs (nuevo): seis casos de modos, JSON anterior, validación, sesiones e independencia.
- tools/GodotUiSmoke.gd: apertura/cierre, filas reales, técnico válido, guardado explícito, modalidad mixta, manual global, navegación protegida y tres resoluciones.
- docs/TRAINING_DELEGATION_PANEL.md, INVENTORY.md y VALIDATION.md: decisiones e inventario.

## Validación

Antes: 515 pruebas Debug. Después: 521 pruebas Debug correctas, sin fallos ni omisiones. Compilaciones Debug/Release sin errores ni advertencias. Las 521 pruebas Release también pasan sin fallos ni omisiones. El recorrido nativo final termina con código 0. Capturas finales revisadas en 1280×720, 1600×900 y 1920×1080; tabla con desplazamiento interno y estado contraído que recupera el calendario. Las cinco publicaciones iniciales mantienen su SHA-256. Furvo abierto normalmente y dejado ejecutándose, registro artifacts/training-delegation-final-run.log. El aviso de solapamiento pertenece a una prueba negativa esperada.

Evidencia: artifacts/test-results/training-delegation-{before,debug,release}.trx, artifacts/training-delegation-native.log y artifacts/career-visual/career-training-delegation-{panel,collapsed}-*.png.

~~~powershell
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
~~~

El recorrido nativo usa carreras aisladas. No se modifican automáticamente carreras personales ni bases fundacionales. La delegación queda configurada, sin simular decisiones futuras de los técnicos.
