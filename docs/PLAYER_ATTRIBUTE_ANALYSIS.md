# Equipo → Análisis → Jugadores
Fecha: 31/10/2026.

## Comportamiento
Se conserva la tabla superior. El panel inferior empieza con una indicación de selección, sin desplegar los atributos de toda la plantilla. Una selección por identificador estable muestra nombre, posición principal y edad; al cambiar de equipo se descarta cualquier selección ajena.

Cuatro controles compactos permiten consultar Técnicos (17), Mentales (16), Físicos (8) y Portero (10). Cada fila presenta nombre, número sobre 20 y barra proporcional. Se reutilizan los colores de AttributeValueStyle, sin depender de ellos para interpretar el número. La interfaz permite contraer y expandir el panel.

La comparación solo ofrece otros futbolistas de la plantilla consultada; no genera un informe ni escribe una configuración persistente. Se distinguen las columnas mediante los nombres de ambos futbolistas. El doble clic conserva el acceso a la ficha existente.

Nombre, valor y relevancia tienen orden reproducible y dirección reversible. Relevancia significa pertenencia al catálogo compartido PositionInsightCatalog según la posición principal del jugador seleccionado; no hay pesos ni media global. Los empates se resuelven por nombre español y código estable.

## Privacidad y lectura
PlayerAttributeAnalysis consume PlayerDetails públicos, no entidades Player ni diccionarios internos. Además aplica una lista explícita de códigos públicos por categoría, valida escala y duplicados y rechaza la categoría Hidden. La misma protección se aplica a la comparación y al historial. Un código oculto introducido erróneamente dentro de otra categoría tampoco llega a la vista.

Solo se reutilizan SqliteClubSquadStore.Details, observaciones físicas e informes existentes. La selección, ordenación y comparación permanecen en memoria. No se modifica SQLite, el desarrollo, los atributos, los contratos ni los datos fundacionales. No hay migraciones ni dependencias nuevas.

## Evolución
El botón de evolución consulta instantáneas públicas ya guardadas en informes. Solo muestra diferencias entre fechas distintas cuando existen datos registrados. No infiere progreso de acumuladores, entrenamiento previsto ni tiempo de uso del editor.

Sin cambios verificables se informa expresamente. Si dos registros de un mismo día contienen valores contradictorios y no hay hora que permita establecer su orden, ese día se omite del cálculo: no se elige una supuesta última observación. Los resultados son diferencias entre registros, no una atribución causal a un entrenamiento. No se añade un sistema nuevo de registro histórico.

## Archivos de este encargo
| Archivo | Cambio |
|---|---|
| src/Furvo.Core/Queries/PlayerAttributeAnalysis.cs | Selección, comparación, orden y diferencias históricas exclusivamente públicas. |
| src/Furvo.Godot/Scripts/Main.AnalysisPlayers.cs | Panel interactivo y estado de selección en memoria. |
| src/Furvo.Godot/Scripts/Main.SportsAnalysis.cs | Delega la pestaña Jugadores al panel nuevo, manteniendo el resto. |
| src/Furvo.Godot/Scripts/Components/AnalysisAttributeRow.cs | Fila reutilizable con barras y valores numéricos. |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos centralizados en español. |
| tests/Furvo.Tests/PlayerAttributeAnalysisTests.cs | Nueve casos sobre privacidad, selección, comparación, orden, lectura e historial. |
| tools/GodotUiSmoke.gd | Interacción real con la escena, categorías, comparación, contracción y geometría en tres resoluciones. |
| docs/PLAYER_ATTRIBUTE_ANALYSIS.md | Decisiones, inventario y validación. |
| docs/VALIDATION.md, docs/INVENTORY.md | Estado y referencia a esta ampliación. |

El proyecto ya contenía cambios anteriores sin commit; esta relación identifica únicamente los archivos de este encargo.

## Comprobación local
Desde la raíz del proyecto:
```powershell
dotnet build Furvo.sln --no-restore
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
& 'C:\Users\panio\Documents\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'src/Furvo.Godot' --script '../../tools/GodotUiSmoke.gd' -- --capture --analysis-only
```

El smoke utiliza carreras aisladas bajo artifacts. Capturas: artifacts/career-visual/career-player-analysis-1280x720.png, 1600x900.png y 1920x1080.png (con el mismo prefijo).


## Resultados de validación — 01/11/2026

- Compilaciones Debug y Release correctas, cero errores y cero advertencias.
- Nueve pruebas específicas superadas. Suite completa: **431 pruebas Debug y 431 Release**, sin fallos ni omisiones. Se mantienen las 422 previas. TRX: artifacts/test-results/player-analysis-debug.trx y player-analysis-release.trx.
- Godot 4.7.2 .NET con renderizado nativo: smoke enfocado completo con código 0; marcador FURVO_PLAYER_ANALYSIS_OK en artifacts/player-analysis-native.log. Se comprobaron categorías 17/16/8/10, selección estable, comparación, ordenación, ausencia de evolución ficticia, contracción y navegación existente.
- Capturas inspeccionadas a 1280×720, 1600×900 y 1920×1080: barras, números y encabezados legibles y alineados. El desplazamiento vertical permite consultar las filas restantes.
- La prueba de lectura conserva SHA-256 del archivo de carrera y comprueba que no se crean informes. Las cinco publicaciones SQLite fundacionales conservan sus SHA-256 anteriores.
- git diff --check sin incidencias.
- Furvo se ha vuelto a lanzar en ventana normal de 1280×720 y se ha dejado en ejecución.

La evolución se limita a diferencias realmente registradas; no se añade una serie temporal automática ni se fabrican valores históricos. No se ha repetido en este encargo el recorrido nativo completo de arrastre táctico: sí las suites completas y la prueba enfocada de Análisis con navegación y módulos deportivos.
