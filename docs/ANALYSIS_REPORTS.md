# Equipo → Análisis → Informes
Fecha: 31/10/2026.

## Comportamiento
El ámbito y el contenido son independientes. Colectivo selecciona toda la plantilla; Individual, un jugador propio; Por puesto filtra la demarcación principal y permite incluir expresamente las secundarias; Por grupo utiliza los mismos TrainingGroup de Entrenamiento. Un ámbito vacío, ajeno o con duplicados se rechaza. El contenido ofrece informe técnico, resumen y detalle de atributos; el antiguo valor numérico Individual=3 se conserva como detalle de atributos para evitar alterar documentos anteriores.

El selector obligatorio de técnico usa empleados ya existentes: entrenador principal y, en un equipo inferior, su responsable institucional. No se generan analistas ni empleados adicionales. Actualmente los tres contenidos describen información disponible y no requieren una especialidad que el modelo de personal todavía no tiene. Si no hay responsable elegible, no puede generarse el informe. Se distinguen los selectores de técnico y futbolista.

Los datos proceden de la carrera: identidad, 51 atributos públicos, fortalezas y debilidades descriptivas por posición, planificación de los próximos siete días y observaciones físicas registradas. La carga es **prevista**, no realizada. La fatiga desconocida aparece como raya; no se infieren progreso, partidos ni familiaridad. Los cambios de atributos observados se conservan en la instantánea cuando existan. No hay valoración global ni atributos ocultos.

## Persistencia y compatibilidad
Se amplía el documento JSON de sport_analysis_reports mediante Context, FormatVersion=2, con identificador de carrera, ámbito, técnico, equipo, nombre de grupo, integrantes y datos originales. Estado: Complete. No se necesita una nueva tabla ni migración SQL; se reutiliza Sports001 existente únicamente al escribir por primera vez en una carrera que aún carezca del módulo. Consultar no migra ni escribe.

Los documentos anteriores sin Context siguen siendo legibles y admiten cambios de prioridades. La interfaz avisa de que no constan técnico ni integrantes originales. No se reconstruyen retrospectivamente a partir de la plantilla actual. El contenido histórico es inmutable; solo se actualizan prioridades válidas. La comprobación se repite dentro de la transacción de escritura. No se sobrescriben publicaciones del mundo ni se modifican carreras personales para validar.

Editar un grupo utiliza el mismo almacenamiento de Entrenamiento con revisión optimista. Se conservan las sesiones y participantes existentes; si una sesión lo referencia, RequiresReview queda activado para una revisión explícita. Los informes congelan nombre e integrantes y no cambian al editar el grupo. Guardar prioridades no escribe tácticas ni planificación.

## Presentación
DataTable añade una opción explícita para limitar columnas sin cambiar el comportamiento de las demás tablas. Las de Análisis comparten proporciones, alineaciones de títulos/celdas, filas de 36 píxeles y recorte de nombres con tooltip. Nombre y posición se alinean a la izquierda, edad y fatiga al centro, carga a la derecha, con decimales es-ES.

PriorityChoice separa el CheckBox sin texto de una etiqueta con ajuste de línea. Se conservan foco, selección mediante teclado y clic en etiqueta, y el indicador utiliza los estados nativos de Godot. No se cambia el tema global ni la lógica táctica.

## Archivos de este encargo
| Archivo | Función |
|---|---|
| src/Furvo.Core/Development/SportsAnalysis.cs | Ámbitos, selección y validación de instantáneas públicas. |
| src/Furvo.Core/Queries/PositionInsightCatalog.cs | Reglas descriptivas existentes compartidas desde Core. |
| src/Furvo.Persistence/SqliteAnalysisReportService.cs | Generación, empleados reales, grupos y guardado de prioridades. |
| src/Furvo.Persistence/SqliteSportsStore.cs | Compatibilidad e inmutabilidad transaccional. |
| src/Furvo.Godot/Scripts/Main.AnalysisReports.cs | Formulario, histórico y resultados. |
| src/Furvo.Godot/Scripts/Main.SportsAnalysis.cs | Integración de informes y alineación de jugadores. |
| src/Furvo.Godot/Scripts/Components/DataTable.cs | Restricción opcional de columnas. |
| src/Furvo.Godot/Scripts/Components/PriorityChoice.cs | Indicador y descripción separados. |
| src/Furvo.Godot/Scripts/Components/PositionInsightRules.cs | Adaptador compatible con la ficha existente. |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos centralizados en español. |
| tests/Furvo.Tests/AnalysisReportTests.cs | Ámbitos/contenidos, staff, grupos, históricos y aislamiento. |
| tests/Furvo.Tests/SportsDevelopmentTests.cs | Conservación de la prueba previa de informes mediante el nuevo servicio. |
| tools/GodotUiSmoke.gd | Comprobación nativa de informes y regresiones. |
| docs/ANALYSIS_REPORTS.md | Decisiones, inventario y validación de esta entrega. |
| docs/VALIDATION.md, docs/INVENTORY.md | Referencias al estado de esta corrección. |

El directorio contiene trabajo anterior sin commit; esta lista identifica solo el alcance de este encargo, no todos los cambios de git status.

## Validación local
Desde PowerShell en la raíz:
```powershell
dotnet build Furvo.sln --no-restore
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
& 'C:\Users\panio\Documents\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'src/Furvo.Godot' --script '../../tools/GodotUiSmoke.gd' -- --capture --analysis-only
```

El modo enfocado conserva las pruebas de navegación previas a la carrera y las de Entrenamiento/Análisis/Tecnología, omitiendo únicamente el recorrido táctico de arrastre ajeno a esta pantalla. El recorrido completo sigue disponible sin --analysis-only. Las carreras de prueba se crean en artifacts, nunca en datos personales.

## Resultados automatizados
Compilaciones Debug y Release sin errores ni advertencias. 422 pruebas correctas en Debug (1 min 44 s) y 422 en Release (1 min 46 s), sin fallos ni omitidas. TRX: artifacts/test-results/analysis-debug.trx y analysis-release.trx. Son las 403 anteriores y 19 casos nuevos. Se corrigieron un bloqueo SQLite detectado por pruebas y la preparación de una fixture de grupos; los resultados indicados corresponden a las suites finales completas.

Furvo se abrió al terminar en una ventana normal de 1280×720 y se dejó ejecutándose (artifacts/analysis-final-run.log).

## Evidencia de validación nativa
El recorrido completo de Godot finalizó con código 0 (artifacts/analysis-full-retry.log), incluyendo arrastre real, los seis esquemas tácticos, cantera, grupos e Informes. El primer intento se detuvo en un arrastre anterior; la repetición completa pasó sin modificar lógica táctica. El modo enfocado también terminó con código 0 (artifacts/analysis-focused.log).

Se generaron capturas a 1280×720, 1600×900 y 1920×1080 en artifacts/career-visual, prefijo career-analysis-. Se inspeccionaron capturas de controles, resultados y prioridades, incluidos estados marcado, sin marcar, foco y deshabilitado. Las aserciones verificaron alineaciones, coma decimal, integrantes, ausencia de selector individual en Colectivo y selección de ámbitos.

Las cinco bases iniciales (original y v2–v5) son idénticas por SHA-256 respecto al comienzo del encargo. git diff --check pasó.

## Límites
No hay estadísticas de partidos, analistas contratables ni recomendaciones de capacidad especializada. No se aplica automáticamente ninguna prioridad al entrenamiento. El motor y las tácticas existentes no se han modificado. La revisión humana final de usabilidad sigue siendo recomendable; las capturas y aserciones geométricas no sustituyen esa aceptación.
