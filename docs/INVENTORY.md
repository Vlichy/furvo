# Inventario — ficha institucional e historia propuesta, Entrega 4

**15 archivos creados y 9 modificados**. Se amplía lectura/presentación, sin paquetes, proyectos, tablas, migraciones, publicaciones nuevas ni mecánicas deportivas. Persistence, entidades previas y v1–v4 mantienen sus hashes. DataTable y PlayerSheet permanecen intactos.

## Archivos creados o modificados

| Archivo | Estado | Función / cambio |
| --- | --- | --- |
| README.md | Modificado | Recorrido actualizado y resultados actuales; ejecución al terminar. |
| docs/ARCHITECTURE.md | Modificado | Consultas institucionales y separación de propuestas, historia de partida y presentación. |
| docs/CLUB_HISTORY_PROPOSAL.md | Creado | Borrador NO APROBADO para veinte clubes: identidades, cronologías, palmarés y sesenta personajes propuestos. |
| docs/CLUB_HISTORY_SCHEMA_PROPOSAL.md | Creado | Diseño de persistencia pendiente de aprobación, sin SQL ni migraciones aplicadas. |
| docs/CLUB_SCREEN.md | Creado | Guía de navegación, fuentes de datos y alcance real de historia/economía. |
| docs/INVENTORY.md | Modificado | Inventario de esta ampliación y archivos mantenidos. |
| docs/VALIDATION.md | Modificado | 129 pruebas, escena Godot, conservación y revisión visual pendiente. |
| src/Furvo.Core/Queries/ClubDetails.cs | Creado | DTO institucionales opcionales y contrato histórico con orígenes separados; sin entidades persistentes nuevas. |
| src/Furvo.Core/Queries/WorldQueries.cs | Modificado | Consulta del perfil e inscripciones existentes; devuelve historia no disponible sin datos inventados. |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado | Textos españoles de las cuatro pestañas, estados, gráfico y conceptos económicos. |
| src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs | Creado | Campos económicos no disponibles y distinción conceptual de saldo, presupuesto y balance. |
| src/Furvo.Godot/Scripts/Components/ClubEmblem.cs | Creado | Emblema vectorial provisional original de Furvo. |
| src/Furvo.Godot/Scripts/Components/ClubHeader.cs | Creado | Cabecera institucional común basada en la consulta. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryChart.cs | Creado | Representación genérica de temporadas, divisiones y huecos, con origen en el tooltip. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryPage.cs | Creado | Bloques, tabla reutilizada y gráfico sin resultados o leyendas inventadas. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryPlot.cs | Creado | Ordenación y coordenadas puras; no genera resultados ni conecta temporadas desconocidas. |
| src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs | Creado | Datos existentes de identidad y estadio; campos nuevos sin fuente marcados pendientes. |
| src/Furvo.Godot/Scripts/Components/ClubUi.cs | Creado | Paneles, campos y desplazamiento comunes, con estados pendientes explícitos. |
| src/Furvo.Godot/Scripts/Components/StadiumPreview.cs | Creado | Imagen vectorial genérica del estadio, sin fotografías ni atribución a instalaciones reales. |
| src/Furvo.Godot/Scripts/Main.cs | Modificado | Entrada a Información, cuatro pestañas y reutilización de plantilla/selección/regreso. |
| tests/Furvo.Tests/ClubDetailsQueryTests.cs | Creado | Cinco casos sobre fuente SQLite, campos desconocidos, historia vacía, IDs y lectura sin cambios. |
| tests/Furvo.Tests/ClubHistoryPlotTests.cs | Creado | Cuatro casos del gráfico con fixtures separados del catálogo. |
| tests/Furvo.Tests/Furvo.Tests.csproj | Modificado | Enlaza la política pura del gráfico para pruebas sin dependencias nuevas. |
| tools/GodotUiSmoke.gd | Modificado | Verifica Información inicial, cuatro pestañas, ClubId, datos pendientes, geometría y regresión de futbolistas. |

## Archivos mantenidos

| Archivo | Función |
| --- | --- |
| .editorconfig | Convenciones de formato y codificación. |
| .gitignore | Incluye la publicación v4 sin permitir el versionado accidental de partidas. |
| .vscode/extensions.json | Extensión C# recomendada. |
| .vscode/launch.json | Depuración de Godot mediante GODOT4. |
| .vscode/tasks.json | Añade Crear/verificar plantillas iniciales v3; conserva tareas previas. |
| data/initial-world-2026-27.db | SQLite histórico v1 intacto, preservado con su huella original. |
| data/initial-world-2026-27.v2.db | Publicación histórica v2 intacta, esquema 3, veinte clubes sin jugadores. |
| data/initial-world-2026-27.v3.db | Nueva publicación SQLite: 480 jugadores y contratos, con 61 valores por jugador. |
| data/initial-world-2026-27.v4.db | Nueva publicación: Fragua visible 2, resto de entidades y atributos idénticos a v3. |
| Directory.Build.props | Propiedades C# y versión interna compartidas. |
| docs/ARCHITECTURE.md | Consultas institucionales y separación de propuestas, historia de partida y presentación. |
| docs/CLUB_HISTORY_PROPOSAL.md | Borrador NO APROBADO para veinte clubes: identidades, cronologías, palmarés y sesenta personajes propuestos. |
| docs/CLUB_HISTORY_SCHEMA_PROPOSAL.md | Diseño de persistencia pendiente de aprobación, sin SQL ni migraciones aplicadas. |
| docs/CLUB_SCREEN.md | Guía de navegación, fuentes de datos y alcance real de historia/economía. |
| docs/CLUBS_2026_27.md | Reputación visible corregida, publicación v4 y comando correspondiente. |
| docs/DECISIONS.md | Corrige la tabla activa a Fragua 32/2 y conserva el manifiesto original como histórico. |
| docs/decisions/club-reputations-2026-27.json | Tabla manual aprobada de veinte valores 0–100 por ClubId y tramos visibles. |
| docs/development/CLUB_CORRESPONDENCE_2026_27.md | Tabla de veinte equivalencias, ocho conservados, doce retirados y decisión de IDs/publicación. |
| docs/development/club-reference-2026-27.json | Correspondencia real-ficticia de desarrollo, fuente oficial y condición de ascendido. |
| docs/development/DELIVERY4_VALIDATION.json | Registro de resultados reales, hashes protegidos y límites de validación. |
| docs/development/source-documentation-review.json | Comparación registrada de las treinta fuentes anteriores sin comentarios. |
| docs/development/SQUAD_REPORT_2026_27.json | Informe real generado: conteos y trece ejemplos sin atributos ocultos. |
| docs/INVENTORY.md | Inventario de esta ampliación y archivos mantenidos. |
| docs/PLAYER_SHEET_PANELS.md | Diseño, configuración completa de relevancia y alcance no táctico. |
| docs/PLAYER_SHEET.md | Actualiza la ficha con los dos paneles y el contenido desplazable real. |
| docs/SQUADS_2026_27.md | Estadísticas completas, algoritmo, semilla, ejemplos y riesgos. |
| docs/V1_CONTRACT.md | Contrato comercial aprobado: sistemas mínimos, exclusiones y criterios medibles. |
| docs/VALIDATION.md | 129 pruebas, escena Godot, conservación y revisión visual pendiente. |
| docs/WORLD_MODEL.md | Modelo y ejemplos coherentes con el catálogo activo v4, sin nuevas columnas. |
| Furvo.sln | Solución raíz con los cuatro proyectos. |
| global.json | Selección del SDK .NET 10 (10.0.402). |
| NuGet.Config | Origen de paquetes explícito para esta solución. |
| README.md | Recorrido actualizado y resultados actuales; ejecución al terminar. |
| src/Furvo.Core/Domain/ClubId.cs | Identificador GUID tipado de Club. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/ClubProfile.cs | Datos institucionales, identidad, colores, estadio y categoría del club. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/CoachId.cs | Identificador tipado de entrenador. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/CompetitionId.cs | Identificador tipado de competición. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/ContractId.cs | Identificador GUID tipado de Contract. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/DomainRules.cs | Escalas separadas: reputación 1–5 y atributos individuales 1–20. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/Entities.cs | Posiciones opcionales del jugador y edad calculada sin reloj. |
| src/Furvo.Core/Domain/PlayerAttributes.cs | 61 códigos y categorías conservados, con validación 1–20. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/PlayerId.cs | Identificador GUID tipado de Player. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/PlayerPositions.cs | Desglose aprobado de posiciones principales/secundarias y mapeo compatible a cuatro grupos. |
| src/Furvo.Core/Domain/SaveId.cs | Identificador GUID tipado de Save. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/SeasonId.cs | Identificador tipado de temporada. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Domain/WorldId.cs | Identificador del origen del mundo. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Furvo.Core.csproj | Biblioteca independiente de dominio. |
| src/Furvo.Core/Generation/DeterministicRandom.cs | SplitMix64, contextos SHA-256 e identidades estables. |
| src/Furvo.Core/Generation/InitialSquadGenerator.cs | Generación pura de plantillas, nombres, atributos y contratos reproducibles. |
| src/Furvo.Core/Generation/SquadGenerationSettings.cs | Parámetros editoriales y repertorios culturales validados e inmutables. |
| src/Furvo.Core/Queries/ClubDetails.cs | DTO institucionales opcionales y contrato histórico con orígenes separados; sin entidades persistentes nuevas. |
| src/Furvo.Core/Queries/WorldQueries.cs | Consulta del perfil e inscripciones existentes; devuelve historia no disponible sin datos inventados. |
| src/Furvo.Core/Saves/ISaveStore.cs | Puerto de almacenamiento y metadatos de partida. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Core/Worlds/IWorldStore.cs | Puerto de mundo, instantáneas/lotes y metadatos de plantilla o partida. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Godot/Assets/Portraits/avatar.svg | Silueta vectorial original de Furvo, sin recursos externos ni fotografías. |
| src/Furvo.Godot/Assets/Portraits/avatar.svg.import | Configuración de importación del SVG generada por Godot; sin cambiar datos del mundo. |
| src/Furvo.Godot/Furvo.Godot.csproj | Empaqueta v4 como catálogo activo, conservando el nombre lógico del recurso. |
| src/Furvo.Godot/project.godot | Conserva inicio 1280 × 720 y añade mínimo 960 × 640. |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos españoles de las cuatro pestañas, estados, gráfico y conceptos económicos. |
| src/Furvo.Godot/Scenes/Main.tscn | Escena inicial que aloja el controlador funcional de consulta. |
| src/Furvo.Godot/Scripts/Components/AttributeSection.cs | Panel compacto de nombres/valores públicos, alineación y color. |
| src/Furvo.Godot/Scripts/Components/AttributeSection.cs.uid | UID de recurso C# generado por Godot; no es un identificador de futbolista. |
| src/Furvo.Godot/Scripts/Components/AttributeValueStyle.cs | Umbrales y paleta de presentación centralizados; no transforma puntuaciones. |
| src/Furvo.Godot/Scripts/Components/AttributeValueStyle.cs.uid | UID de recurso C# generado por Godot; no es un identificador de futbolista. |
| src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs | Campos económicos no disponibles y distinción conceptual de saldo, presupuesto y balance. |
| src/Furvo.Godot/Scripts/Components/ClubEmblem.cs | Emblema vectorial provisional original de Furvo. |
| src/Furvo.Godot/Scripts/Components/ClubHeader.cs | Cabecera institucional común basada en la consulta. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryChart.cs | Representación genérica de temporadas, divisiones y huecos, con origen en el tooltip. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryPage.cs | Bloques, tabla reutilizada y gráfico sin resultados o leyendas inventadas. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryPlot.cs | Ordenación y coordenadas puras; no genera resultados ni conecta temporadas desconocidas. |
| src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs | Datos existentes de identidad y estadio; campos nuevos sin fuente marcados pendientes. |
| src/Furvo.Godot/Scripts/Components/ClubUi.cs | Paneles, campos y desplazamiento comunes, con estados pendientes explícitos. |
| src/Furvo.Godot/Scripts/Components/DataTable.cs | Alineación de títulos/celdas configurable y salto de línea opcional en nombres largos. |
| src/Furvo.Godot/Scripts/Components/PitchPositionLayout.cs | Coordenadas normalizadas y marcas exactas de posiciones registradas, sin deducciones. |
| src/Furvo.Godot/Scripts/Components/PitchPositionsPanel.cs | Panel de campo, encabezado y leyenda textual. |
| src/Furvo.Godot/Scripts/Components/PlayerInsightsPanel.cs | Fortalezas/debilidades visibles, valores originales y mensajes de ausencia. |
| src/Furvo.Godot/Scripts/Components/PlayerPortrait.cs | Retrato uniforme 3:4, carga segura y fallback provisional. |
| src/Furvo.Godot/Scripts/Components/PlayerPortrait.cs.uid | UID de recurso C# generado por Godot; no es un identificador de futbolista. |
| src/Furvo.Godot/Scripts/Components/PlayerSheet.cs | Integra paneles debajo de atributos, compartiendo scroll y adaptando columnas al ancho. |
| src/Furvo.Godot/Scripts/Components/PlayerSheet.cs.uid | UID de recurso C# generado por Godot; no es un identificador de futbolista. |
| src/Furvo.Godot/Scripts/Components/PortraitResourcePaths.cs | Rutas locales de futuras imágenes PNG/WebP derivadas del PlayerId estable. |
| src/Furvo.Godot/Scripts/Components/PortraitResourcePaths.cs.uid | UID de recurso C# generado por Godot; no es un identificador de futbolista. |
| src/Furvo.Godot/Scripts/Components/PositionInsightRules.cs | Configuración explícita de relevancia por posición y selección descriptiva 14/7, sin medias ni ocultos. |
| src/Furvo.Godot/Scripts/Components/PositionPitch.cs | Campo vectorial reutilizable de solo presentación y marcas principal/secundarias. |
| src/Furvo.Godot/Scripts/Components/StadiumPreview.cs | Imagen vectorial genérica del estadio, sin fotografías ni atribución a instalaciones reales. |
| src/Furvo.Godot/Scripts/Main.cs | Entrada a Información, cuatro pestañas y reutilización de plantilla/selección/regreso. |
| src/Furvo.Godot/Scripts/UiText.cs | Resolución central de recursos españoles y formatos. |
| src/Furvo.Persistence/AdjustedInitialWorld2026.cs | Publicación v4 desde v3 de lectura, derivación visible, validación e idempotencia sin sobrescritura. |
| src/Furvo.Persistence/Data/club-reputations-2026-27.v2.json | Catálogo manual activo de veinte reputaciones internas; solo Fragua cambia a 32. |
| src/Furvo.Persistence/Data/initial-squads-2026-27.json | Semilla, versión, perfiles y repertorios culturales propios. |
| src/Furvo.Persistence/Data/initial-world-2026-27.json | Fuente histórica v1 intacta; solo se copia para pruebas, no se embebe en el juego. |
| src/Furvo.Persistence/Data/initial-world-2026-27.v2.json | Fuente ficticia de clubes conservada sin cambios, reutilizada por v3. |
| src/Furvo.Persistence/Furvo.Persistence.csproj | Embebe el nuevo catálogo sin cambiar dependencias. |
| src/Furvo.Persistence/InitialSquads2026.cs | Composición, publicación idempotente, procedencia y huella canónica de v3. |
| src/Furvo.Persistence/InitialWorld2026.cs | Expone internamente la definición v2 para reutilizar clubes sin alterar su API pública. |
| src/Furvo.Persistence/Migrations/001_Initial.sql | Esquema original versionado, sin datos ficticios. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Persistence/Migrations/002_World.sql | Esquema del mundo, restricciones, triggers y catálogo inicial congelado. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Persistence/Migrations/003_ClubUniverseAndScales.sql | Restricciones definitivas, preservación de atributos y ampliación de clubes/temporada. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Persistence/Migrations/004_PlayerPositionsAndGeneration.sql | Esquema 4: posiciones, restricciones y procedencia de generación. |
| src/Furvo.Persistence/PublishedWorldQueries.cs | Apertura SQLite de solo lectura y caché atómica SHA-256 sin crear partidas. |
| src/Furvo.Persistence/SchemaCompatibilityException.cs | Informa entidades, campos y valores incompatibles sin conversión automática. Comentarios explicativos en español; ver revisión de código. |
| src/Furvo.Persistence/SqliteSaveStore.cs | Metadatos e inicialización sin migración automática de archivos existentes. |
| src/Furvo.Persistence/SqliteWorldFiles.cs | Publica datos y procedencia v3 en la misma transacción. |
| src/Furvo.Persistence/SqliteWorldStore.cs | Lectura y sustitución transaccional de posiciones; conserva datos omitidos. |
| src/Furvo.Persistence/WorldDatabase.cs | Registro y ejecución de migración 004; exige esquema actual 4. |
| tests/Furvo.Tests/AdjustedWorldTests.cs | Seis casos: diferencia única, veinte valores exactos, tramos, publicación segura, consultas e integridad SQL. |
| tests/Furvo.Tests/ClubDetailsQueryTests.cs | Cinco casos sobre fuente SQLite, campos desconocidos, historia vacía, IDs y lectura sin cambios. |
| tests/Furvo.Tests/ClubHistoryPlotTests.cs | Cuatro casos del gráfico con fixtures separados del catálogo. |
| tests/Furvo.Tests/ClubReferenceTests.cs | Cinco pruebas de correspondencia, ascendidos, IDs, exclusión de referencias y preservación. |
| tests/Furvo.Tests/ClubUniverseTests.cs | 20 clubes completos, escalas, idempotencia, publicación, reapertura e independencia. Comentarios explicativos en español; ver revisión de código. |
| tests/Furvo.Tests/Furvo.Tests.csproj | Enlaza la política pura del gráfico para pruebas sin dependencias nuevas. |
| tests/Furvo.Tests/PitchPositionTests.cs | Seis casos de marcas, secundarias, orientación, separación y recursos. |
| tests/Furvo.Tests/PlayerAttributeTests.cs | Pruebas anteriores de atributos, adaptadas a límites 1–20. Comentarios explicativos en español; ver revisión de código. |
| tests/Furvo.Tests/PlayerSheetPresentationTests.cs | Doce casos de límites cromáticos, IDs/rutas y avatar vectorial sin imágenes externas. |
| tests/Furvo.Tests/PositionInsightsTests.cs | Seis casos de relevancia, límites, empates, ocultos y contraste con 480 fichas. |
| tests/Furvo.Tests/ReputationDecisionTests.cs | Dos casos: veinte valores exactos/IDs y cobertura de los tramos 0–100. |
| tests/Furvo.Tests/SaveStoreTests.cs | Cuatro pruebas de identidad, persistencia y aislamiento. Comentarios explicativos en español; ver revisión de código. |
| tests/Furvo.Tests/ScaleMigrationTests.cs | Expectativa de versión usa CurrentSchemaVersion; conserva las pruebas previas. |
| tests/Furvo.Tests/SquadGenerationTests.cs | Pruebas de generación, identidades, perfiles, rangos, edades y determinismo. |
| tests/Furvo.Tests/SquadPersistenceTests.cs | Pruebas de publicación, migración, restricciones, reapertura y aislamiento. |
| tests/Furvo.Tests/WorldQueryTests.cs | Catorce casos de consultas, 480 fichas visibles, orden, fechas, errores, caché y recursos. |
| tests/Furvo.Tests/WorldStoreTests.cs | Pruebas anteriores, con fixtures de reputación y versión de esquema adaptados. Comentarios explicativos en español; ver revisión de código. |
| tools/BuildAdjustedWorld.cs | Utilidad .NET de creación/verificación v4, sin regenerar jugadores. |
| tools/BuildInitialSquads.cs | Utilidad .NET 10 de creación/verificación v3 e informe con atributos visibles. |
| tools/BuildInitialWorld.cs | Utilidad de desarrollo .NET 10 para crear/verificar SQLite, sin quinto proyecto. Comentarios explicativos en español; ver revisión de código. |
| tools/GodotUiSmoke.gd | Verifica Información inicial, cuatro pestañas, ClubId, datos pendientes, geometría y regresión de futbolistas. |

## Registros y límites

Los documentos históricos anteriores siguen conservados; el estado actual está en [VALIDATION.md](VALIDATION.md). La propuesta de historia no es un catálogo aprobado ni se carga en el juego. El esquema propuesto requiere aprobación antes de persistirlo. Se excluyen bin/obj/.godot, logs de artifacts y la copia histórica src/furvo-(4.5). Los metadatos UID que Godot pueda generar al importar se refieren a recursos, no a IDs de clubes/jugadores.

Al finalizar se ejecuta la versión actual, según la instrucción del usuario. La ejecución no sustituye la inspección visual manual pendiente ni acredita una exportación Windows.

## Rediseño de ficha de club — 27/10/2026 (estado vigente)

**9 archivos creados y 18 modificados.** Las secciones anteriores describen entregas previas; este bloque identifica exclusivamente el rediseño actual.

| Archivo | Estado | Función |
| --- | --- | --- |
| docs/CLUB_PRESENTATION_REDESIGN.md | Creado | Contrato previo, implementación, diferencias, validación y límites. |
| docs/STADIUM_CATEGORY_PROPOSAL.md | Creado | Veinte categorías editoriales propuestas por ClubId, sin cargar en el juego. |
| src/Furvo.Godot/Resources/ClubVisuals.json | Creado | Tres equipaciones y motivo/monograma provisionales por ClubId. |
| src/Furvo.Godot/Scripts/Components/ClubHistorySections.cs | Creado | Bloques compartidos de trayectoria, palmarés, leyendas y récords. |
| src/Furvo.Godot/Scripts/Components/ClubShirt.cs | Creado | Componente de camiseta vectorial reutilizable sin marcas. |
| src/Furvo.Godot/Scripts/Components/ClubVisualCatalog.cs | Creado | Carga y validación de identidades visuales por ID y fallback neutral. |
| src/Furvo.Godot/Scripts/Components/FootballPitch.cs | Creado | Campo horizontal vectorial con áreas solo en los extremos. |
| src/Furvo.Godot/Scripts/Components/FootballPitchGeometry.cs | Creado | Geometría pura de referencia para marcas y pruebas. |
| tests/Furvo.Tests/ClubPresentationRedesignTests.cs | Creado | Nueve casos: recursos por club, fallback, áreas/arcos, categorías y ausencia de datos inventados. |
| README.md | Modificado | Estado y validación vigentes. |
| docs/CLUB_HISTORY_PROPOSAL.md | Modificado | Aclara separación entre propuesta editorial e interfaz. |
| docs/CLUB_SCREEN.md | Modificado | Guía vigente de composición y estados pendientes. |
| docs/VALIDATION.md | Modificado | 138 pruebas, smoke y límites de inspección visual. |
| docs/INVENTORY.md | Modificado | Este inventario de entrega. |
| src/Furvo.Core/Queries/ClubDetails.cs | Modificado | Contratos de lectura opcionales institucionales, estadio y archivo histórico; sin entidades persistidas nuevas. |
| src/Furvo.Godot/Furvo.Godot.csproj | Modificado | Empaqueta el recurso visual sin paquetes nuevos. |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado | Textos españoles, pies de imagen y etiquetas centralizadas. |
| src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs | Modificado | Cinco bloques de consulta no disponible, sin mecánicas financieras. |
| src/Furvo.Godot/Scripts/Components/ClubEmblem.cs | Modificado | Forma, colores y monograma diferenciados y provisionales. |
| src/Furvo.Godot/Scripts/Components/ClubHeader.cs | Modificado | Integra identidades y tres camisetas por club. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryPage.cs | Modificado | Cronología, detalle y reutilización de secciones históricas. |
| src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs | Modificado | Composición 36/64, tres vistas de estadio y síntesis inferior. |
| src/Furvo.Godot/Scripts/Components/ClubUi.cs | Modificado | Espaciado compacto y contenedores adaptables compartidos. |
| src/Furvo.Godot/Scripts/Components/StadiumPreview.cs | Modificado | Dos ilustraciones geométricas originales provisionales. |
| src/Furvo.Godot/Scripts/Main.cs | Modificado | Entrega los datos históricos ya consultados a Información. |
| tests/Furvo.Tests/Furvo.Tests.csproj | Modificado | Enlaza políticas puras y recurso editorial para pruebas sin dependencias nuevas. |
| tools/GodotUiSmoke.gd | Modificado | Comprueba vistas, categorías pendientes, cinco bloques, treinta campos y cuatro resoluciones. |

Sin cambios en Persistence, migraciones, bases SQLite, datos JSON de mundo/plantillas/reputación, DataTable, PlayerSheet ni PositionPitch. Los nuevos recursos son exclusivamente presentación provisional, no una publicación nueva. Pruebas: 138 Debug/Release; smoke Godot correcto. Ver [informe](CLUB_PRESENTATION_REDESIGN.md).
