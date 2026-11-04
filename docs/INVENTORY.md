Inventario de la ampliación de instrucciones: [TACTICAL_INSTRUCTIONS.md](TACTICAL_INSTRUCTIONS.md).

## Estado vigente — planes tácticos y seis disposiciones

Inicial, Ganando y Perdiendo tienen dos disposiciones propias y un único once por plan; mentalidad e instrucciones independientes y balón parado común. Se conserva una fuente canónica de composición, con lectura compatible y conversión transaccional al guardar. [Modelo, migración, archivos y comprobación](TACTICAL_PLANS.md). Las descripciones anteriores de una táctica única se conservan como historial.

## Segunda iteración de Táctica

Arrastre nativo, formación reubicada, selector Con balón/Sin balón, diagramas enlazados y nuevas asignaciones de balón parado. Extensión táctica versión 2 bajo guardado explícito, compatible con lecturas v1. [Informe e inventario de esta iteración](TACTICS_ITERATION_2.md). Este apartado actualiza el alcance descrito a continuación, que se conserva como historial.

## Estado vigente — Equipo → Táctica

La alineación existente se integra en Táctica junto con Estilo de juego, Transiciones y Balón parado. Mentalidad común y configuración estructurada persistente por carrera. Extensión SQLite independiente versión 1, creada al guardar; lecturas sin migraciones. [Decisiones, archivos y comprobación](TACTICS.md). Los apartados siguientes conservan el historial; las referencias a Equipo → Alineación se entienden ahora como Táctica → Alineación.

## Estado vigente — plantilla de seguimiento e Inicio simplificado (28/10/2026)

Equipo > Plantilla tiene doce columnas: Rol, Posición, Jugador, Calidad, Estado físico, Ritmo, Moral, Forma (últimos 5), PJ, G, A y Media. Indicadores reutilizables sin valoraciones inventadas; los atributos individuales siguen en la ficha. En el estado inicial confirmado sin partidos: PJ/G/A=0, Media — y cinco círculos de forma inactivos. Calidad, condición, ritmo y moral continúan sin datos. La distribución inicial de roles está aprobada pero pendiente de reglas y persistencia: se mantiene Sin asignar. Inicio abre directamente el escritorio; Bandeja se abre desde la cabecera y tiene regreso al escritorio, sin submenú redundante.

[Componentes, contratos de datos, archivos, validación y dependencias](SQUAD_INDICATORS.md). Debug/Release: 265 pruebas superadas y compilaciones sin errores ni advertencias. Recorrido Godot y salida en pantalla completa superados; capturas revisadas a tres resoluciones. No hay cambios SQLite. Los apartados anteriores sobre ocho columnas y submenú de Inicio quedan como historial y son sustituidos por este estado.

## Corrección vigente — menú, Club propio y plantilla deportiva (28/10/2026)

La cabecera de carrera tiene solo Menú de partida como acceso al menú; Guardar/Cargar siguen deshabilitados. Club abre el empleador contratado y en paro muestra Sin club, sin selector público ni Elegir club. Equipo > Plantilla es una pantalla independiente con ocho columnas: Rol, Posición, Jugador, Calidad, Condición y tres atributos reales por demarcación. Roles Sin asignar y calidad/condición — hasta disponer de sistemas persistidos; sin atributos ocultos ni valoraciones inventadas. El explorador inicial previo a contratar se conserva.

[Informe, correspondencias por posición, archivos y validación](NAVIGATION_SQUAD_CORRECTION.md). Debug/Release sin errores ni advertencias; 249 pruebas superadas por configuración y regresión Godot superada, incluida salida en pantalla completa. Catorce bases preexistentes intactas; no hay migraciones. Las descripciones anteriores de un selector público dentro de carrera quedan sustituidas por esta corrección.

## Estado vigente — escritorio y navegación de carrera (28/10/2026)

Tras iniciar una carrera se abre el escritorio contratado o en paro, con seis áreas superiores y subnavegación contextual. La identidad, fecha, club y economía proceden de la copia SQLite de esa carrera. Se reutilizan plantillas, jugadores, información/estadio, economía, historia y contrato. Los permisos de Core no dependen del club consultado. Mercado, Competición y Continuar quedan deshabilitados con explicación; no hay avance temporal, ofertas ficticias ni nuevas mecánicas.

[Mapa, capturas, inventario, decisiones futuras y comprobación manual](CAREER_NAVIGATION.md). Debug/Release: 244 pruebas superadas por configuración y compilaciones sin errores ni advertencias. Pruebas Godot y salida en pantalla completa superadas; capturas de ambos escritorios revisadas a tres resoluciones. Doce bases preexistentes intactas; sin migraciones. Los apartados siguientes conservan el historial de entregas anteriores.

## Estado vigente — economía aprobada y firma habilitada (28/10/2026)

El usuario aprobó la propuesta initial-economy-2026-27-draft-v1. La nueva publicación v5 contiene los veinte estados económicos y conserva los 480 jugadores sin cambios. Economía y Contrato consultan los mismos datos; la firma crea una carrera independiente y registra el compromiso del entrenador en una transacción, sin duplicar gastos ni efectuar un pago anual ficticio. Margen salarial real de jugadores: desconocido hasta disponer de contratos salariales. Las publicaciones/partidas antiguas no se modifican.

[Implementación, migración, inventario, pruebas y límites](ECONOMY_IMPLEMENTATION.md). Debug/Release sin errores ni advertencias; 234 pruebas superadas por configuración. Godot con firma y tres carreras aisladas superado; capturas renderizadas de Economía/Contrato revisadas a 1280×720, 1600×900 y 1920×1080. Los apartados siguientes conservan el historial y sus bloqueos anteriores quedan sustituidos por este estado.

## Estado vigente — propuesta económica, etapa 1 (28/10/2026)

Se ha preparado una propuesta ficticia de los veinte clubes para aprobación; no se incorpora a SQLite ni se habilita la firma real. Los presupuestos/previsiones no son saldos ni movimientos. El margen contractual de jugadores permanece desconocido; la reserva de planificación del borrador no lo sustituye. El sueldo propuesto del entrenador está incluido una sola vez en personal. No hay cambios funcionales ni de esquema.

[Diagnóstico, veinte filas, fórmulas y aprobación pendiente](INITIAL_ECONOMY_PROPOSAL.md). Datos: docs/proposals/initial-economy-2026-27.draft.json, estado AwaitingUserApproval, sin uso en ejecución. Esta nota sustituye las indicaciones anteriores sobre el estado de aprobación; los apartados siguientes conservan el historial.

Archivos añadidos: docs/INITIAL_ECONOMY_PROPOSAL.md (informe/tablas), docs/proposals/initial-economy-2026-27.draft.json (borrador en EUR con IDs estables) y tools/ValidateEconomicProposal.ps1 (validación documental sin SQLite). Archivos modificados: este inventario y ARCHITECTURE.md, CAREER_CREATION.md, MANAGER_CONTRACTS.md, VALIDATION.md. Sin modificaciones a código del juego, migraciones, escenas, tests existentes ni paquetes.

## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](INITIAL_CONTRACT_SIMPLIFICATION.md).

## Corrección contractual vigente — 27/10/2026

La validación actual sustituye las reglas de bloqueo descritas en los apartados históricos: solo exige identidad válida, fechas válidas, salario positivo, moneda admitida y contenido/prioridad de obligaciones expresamente exigibles. Las preferencias orientativas pendientes no bloquean ni se guardan como compromisos aceptados. La duración se calcula en meses desde las fechas. Extensión de carrera 2 para nuevas carreras; carga de extensión 1 en solo lectura. Las veinte ofertas reales siguen pendientes de fecha final, salario anual y moneda.

Validación: Debug y Release, 209 pruebas superadas; compilación de cuatro proyectos sin errores ni advertencias. Prueba de escena Godot sin ventana superada; no equivale a revisión visual manual. Véase [informe de corrección](CONTRACT_TERMS_CORRECTION.md).

# Inventario — perfiles y creación de carreras

## Archivos de esta entrega

**18 archivos creados y 13 modificados.** No se añaden proyectos, herramientas ni paquetes NuGet.

| Archivo | Estado | Función |
| --- | --- | --- |
| docs/COACH_PROFILES.md | Creado | Validaciones, catálogo estable, almacenamiento e instantáneas de perfil. |
| docs/CAREER_CREATION.md | Creado | Flujo, extensión versionada, atomicidad, idempotencia y validación. |
| docs/MANAGER_CONTRACTS.md | Creado | Modelo contractual, compromisos, recursos y bloqueo por reglas pendientes. |
| src/Furvo.Core/Careers/CoachProfiles.cs | Creado | CoachProfileId, perfil validado, experiencia descriptiva y puerto de perfiles. |
| src/Furvo.Core/Careers/ManagerOffers.cs | Creado | Oferta, condiciones, compromisos/táctica y bloqueo de firma. |
| src/Furvo.Core/Careers/CareerCreation.cs | Creado | Identidad de entrenador de carrera, estados, contrato, puerto y flujo temporal. |
| src/Furvo.Core/Queries/ClubFinancialResources.cs | Creado | Única proyección de recursos de club/temporada para Economía y contrato. |
| src/Furvo.Persistence/CareerJson.cs | Creado | Serialización de instantáneas y GUID tipados, y carga de migraciones separadas. |
| src/Furvo.Persistence/SqliteCoachProfileStore.cs | Creado | Altas y lectura de perfiles sin sobrescritura ni duplicación de solicitud. |
| src/Furvo.Persistence/SqliteCareerStore.cs | Creado | Copia SQLite independiente, transacción de inicio, publicación atómica y reapertura. |
| src/Furvo.Persistence/Migrations/Profiles001_Initial.sql | Creado | Esquema de perfiles 1, nacionalidades, nulabilidad, longitud y edad. |
| src/Furvo.Persistence/Migrations/Career001_Initial.sql | Creado | Extensión de carrera 1 con FKs, estados y congelación de instantáneas. |
| src/Furvo.Godot/Scripts/Main.Careers.cs | Creado | Composición de selección/creación de entrenador, oferta, confirmación y bienvenida. |
| src/Furvo.Godot/Scripts/Components/CoachAvatar.cs | Creado | Avatar genérico seguro reutilizable sin PlayerId ni rutas del perfil. |
| src/Furvo.Godot/Scripts/Components/CoachProfileForm.cs | Creado | Formulario con validación, reintento y protección de doble pulsación. |
| src/Furvo.Godot/Scripts/Components/InitialManagerOfferSource.cs | Creado | Adapta los mismos borradores y proyección financiera del explorador. |
| src/Furvo.Godot/Scripts/Components/ManagerOfferDocument.cs | Creado | Documento desplazable de condiciones, compromisos, táctica y recursos pendientes. |
| tests/Furvo.Tests/CareerCreationTests.cs | Creado | 24 casos de perfiles, carreras, firma sintética, rechazo, aislamiento, snapshots y fallos. |
| src/Furvo.Core/Queries/WorldQueries.cs | Modificado | GetFinancialResources valida club/temporada y devuelve recursos desconocidos sin inventarlos. |
| src/Furvo.Persistence/SqliteWorldFiles.cs | Modificado | Publicación existente accesible internamente para reutilizar el mecanismo atómico. |
| src/Furvo.Godot/Scripts/Main.cs | Modificado | Nueva partida abre perfiles, conserva ruta de catálogo e integra las nuevas acciones. |
| src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs | Modificado | Recibe recursos desde la misma consulta del contrato; staff/jugadores separados. |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado | Textos españoles de perfiles, nacionalidades, oferta y carrera; aviso de inicio actualizado. |
| tests/Furvo.Tests/Furvo.Tests.csproj | Modificado | Enlace del adaptador puro real de ofertas para pruebas sin cargar Godot. |
| tools/GodotUiSmoke.gd | Modificado | Recorrido aislado completo, formularios, firma bloqueada, paro directo y rechazo. |
| tools/GodotApplicationSmoke.gd | Modificado | Nuevo paso por perfiles en la prueba nativa, almacenamiento aislado en artifacts. |
| docs/ARCHITECTURE.md | Modificado | Separación de perfil/carrera, extensión, fuentes compartidas y compatibilidad. |
| docs/INVENTORY.md | Modificado | Inventario actual y conservación de inventarios históricos. |
| docs/VALIDATION.md | Modificado | Resultados Debug/Release y límites de pruebas de Godot. |
| docs/CLUB_SCREEN.md | Modificado | Integración del explorador cerrado con perfiles y ofertas. |
| README.md | Modificado | Estado actual, recorridos, almacenamiento local y bloqueo de firma. |

Estado: 179 pruebas Debug/Release, smokes de Godot correctos; firma real bloqueada por reglas pendientes. [Informe](CAREER_CREATION.md).

## Inventarios de fases anteriores

### Inventario — ficha institucional e historia propuesta, Entrega 4

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

## Filosofía y expectativas — ampliación vigente

6 archivos creados y 9 modificados en este encargo; se conservan los cambios locales previos del rediseño.

| Archivo | Estado | Función |
| --- | --- | --- |
| docs/CLUB_DIRECTION_PROPOSAL.md | Creado | Veinte propuestas, fuentes de contexto, aprobación pendiente y validación. |
| src/Furvo.Core/Queries/ClubDirectionProposal.cs | Creado | Contratos de borrador, prioridades y horizontes explícitos; sin evaluadores. |
| src/Furvo.Godot/Resources/ClubDirectionProposals.json | Creado | Fuente editorial española de vista previa: 80 principios y 60 objetivos por ClubId. |
| src/Furvo.Godot/Scripts/Components/ClubDirectionProposalCatalog.cs | Creado | Carga de lectura con validación y rechazo de supuesta aprobación. |
| src/Furvo.Godot/Scripts/Components/ClubDirectionPanels.cs | Creado | Dos paneles adaptables con avisos, prioridades y vencimientos. |
| tests/Furvo.Tests/ClubDirectionProposalTests.cs | Creado | Doce casos: cobertura, reproducción, aislamiento y rechazo de documentos no válidos. |
| src/Furvo.Godot/Furvo.Godot.csproj | Modificado | Empaqueta el recurso editorial, sin dependencias nuevas. |
| src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs | Modificado | Inserta los dos paneles entre estadio/identidad e historia. |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado | Avisos, encabezados, prioridades, horizontes y formatos españoles. |
| tests/Furvo.Tests/Furvo.Tests.csproj | Modificado | Enlace del cargador real y recurso para pruebas. |
| tools/GodotUiSmoke.gd | Modificado | Cobertura de paneles, ClubId, borradores, distribución y regresión. |
| README.md | Modificado | Estado actual y acceso al documento de propuestas. |
| docs/CLUB_SCREEN.md | Modificado | Recorrido y separación entre borrador y datos aprobados. |
| docs/VALIDATION.md | Modificado | 150 pruebas Debug y resultado de escena. |
| docs/INVENTORY.md | Modificado | Inventario de esta ampliación. |

150 pruebas Debug, smoke correcto y cuatro bases intactas. Sin migraciones ni datos definitivos de directiva. Ver [propuestas y validación](CLUB_DIRECTION_PROPOSAL.md).

## Corrección de maquetación y botón Elegir este club

Creado docs/UI_LAYOUT_FIX.md con el inventario de este encargo. Modificados Main.cs, ClubUi.cs, ClubHistorySections.cs, ClubHistoryPage.cs, ClubInformationPage.cs, UiStrings.resx y GodotUiSmoke.gd; actualizados README.md, CLUB_SCREEN.md, VALIDATION.md y este inventario. DataTable se reutiliza sin cambios. Los bancos visuales locales de artifacts están excluidos. No se modifican datos, esquemas ni mecánicas. Estado y límites visuales: [informe](UI_LAYOUT_FIX.md).

## Posiciones, menús y economía — estado vigente

7 archivos nuevos y 9 modificados en este encargo. El [informe](NAVIGATION_POSITIONS_FINANCE.md) enumera cada archivo y función. Nuevos componentes: SeasonSelector, WindowUi, ExplorationMenu y DisplayOptionsWindow; nueva prueba NavigationFinanceQueryTests y smoke GodotApplicationSmoke.gd. WorldQueries incorpora GetSeasons y agrupación estable de posiciones; Main integra los menús. Sin cambios de datos SQLite, entidades, esquemas o dependencias. 155 pruebas Debug; smoke headless y dos smokes con ventana correctos.


## Alineaciones — 28/10/2026

La entrega de alineaciones añade Equipo → Plantilla / Alineación, selección inicial reproducible y persistencia independiente por carrera. La apertura de carreras antiguas no migra ni genera alineaciones automáticamente. Véanse [arquitectura, reglas e inventario de archivos](LINEUPS.md) y [formaciones iniciales de los veinte clubes](INITIAL_FORMATIONS.md). Esta sección actualiza las referencias anteriores a roles sin asignar y al orden inicial por rol: ahora las carreras nuevas tienen once y se ordenan por posición futbolística.


## Roles e instrucciones tácticas individuales

Se amplía el editor existente con 35 presets por demarcación, personalización por puesto/plan/fase, presión avanzada, capitanes y jerarquías de especialistas propias de cada equipo. Cantera hereda los comportamientos por puesto sin copiar futbolistas. Rival y familiaridad quedan como modelos preparatorios sin simular partidos ni entrenamiento.

Catálogo, reglas, inventario de archivos, migraciones y límites: [Sistema táctico ampliado](TACTICAL_BEHAVIOURS.md).


## Desarrollo deportivo integrado

Equipo incorpora Plantilla, Táctica, Entrenamiento, Análisis y Cantera; Club añade Instalaciones y Tecnología deportiva. Hay planificación persistente, grupos, configuración de delegación, dos capitanes, movilidad con normativa explícita e informes de datos existentes. La ejecución temporal, normativa incompleta, personal especializado y compras sin aprobación permanecen bloqueados o preparados, sin simular su disponibilidad.

[Informe, archivos, migraciones y límites](SPORTS_DELIVERY.md) · [Fuentes](SPORTS_RESEARCH.md).


## Corrección de Análisis → Informes — 31/10/2026

Cuatro ámbitos independientes del contenido, empleados reales, grupos compartidos e histórico inmutable; componente PriorityChoice y columnas restringidas en Análisis. [Inventario de este encargo y compatibilidad](ANALYSIS_REPORTS.md). Sin nuevas dependencias ni migraciones SQL.


## Análisis interactivo de atributos — 31/10/2026

Panel de selección/comparación por jugador, cuatro categorías públicas y barras 1–20, con reglas de consulta en Core. [Archivos, decisiones e historial real](PLAYER_ATTRIBUTE_ANALYSIS.md). Sin persistencia nueva.


## Calendario semanal de Entrenamiento — 01/11/2026

Siete columnas y un formulario compacto compartidos por todas las categorías, iconos vectoriales, filtro común y borradores de interfaz. [Inventario de archivos, decisiones y validación](TRAINING_WEEK_CALENDAR.md). Sin migraciones ni publicaciones nuevas.


## Jornadas, turnos y bloques — 01/11/2026

Horarios seleccionables, duración efectiva, responsables reales y delegación por área. Nueva migración SportsTraining003_Days.sql para configuraciones de jornada; bloques y delegaciones anteriores conservados. [Archivos, decisiones, compatibilidad y validación](TRAINING_DAYS_BLOCKS.md).


## Lista diaria interactiva de Entrenamiento — 01/11/2026

Calendario inicial, selección de día, filas editables, totales globales y formulario inferior con fecha fija. Proyección del mismo plan, sin esquema nuevo. [Archivos, reglas y validación](TRAINING_DAILY_PLANNING.md).


## Duración libre y descanso completo — 01/11/2026

Política temporal sin topes arbitrarios y descanso como estado de jornada, con archivo histórico versionado y confirmación transaccional. [Inventario y compatibilidad](TRAINING_REST_CORRECTION.md).


## Entrenamiento: Resumen y agenda compartida — 01/11/2026

Calendario global, indicadores reales, vistas por categoría, edición cruzada por ID y navegación protegida. Sin migraciones ni almacenamiento nuevo. [Archivos, decisiones y evidencia](TRAINING_SHARED_SUMMARY.md).


## Calendario global en todas las pestañas — 01/11/2026

Retirado el filtro por categoría del calendario y la lista diaria. Formularios especializados y agenda única conservados. [Archivos y validación](TRAINING_GLOBAL_CALENDAR.md).


## Presentación de Entrenamiento — 03/11/2026

Pestañas con estado activo destacado, horarios compactos con popup desplazable y participantes con posición real e identificador estable. [Archivos, decisiones y validación](TRAINING_VISUAL_CONTROLS.md).


Porteros: modalidades Individual y Por demarcación, selección de porteros reales, enlace explícito de TrainingScope y conservación de registros anteriores hasta edición expresa. Véase la corrección en TRAINING_VISUAL_CONTROLS.md.


## Gestión y delegación de Entrenamiento — 03/11/2026

Panel transversal desplegable bajo las pestañas, nueve responsabilidades reales y tabla única de modalidad/responsable. Modo mixto compatible en el JSON existente, sin migración SQL. [Archivos, compatibilidad y validación](TRAINING_DELEGATION_PANEL.md).


## Correcciones funcionales de Entrenamiento — Entrega 1, 04/11/2026

Borradores protegidos por carrera/equipo, eliminación por ID sin cascadas, instantáneas históricas conservadas y faltas por ubicación real. Sin nuevas migraciones SQL. [Inventario, compatibilidad, pruebas y límites](TRAINING_FUNCTIONAL_FIXES_1.md).


## Cierre base de Entrenamiento — Entrega 2 (04/11/2026)

Modelo TrainingAvailability, almacén SqliteTrainingAvailabilityStore, migración TrainingAvailability001_Initial, panel Main.TrainingAvailability y 30 pruebas nuevas. Integración en planificación y consumidor existente; textos y prueba nativa ampliados. [Inventario detallado y cierre](TRAINING_BASE_CLOSURE.md). Sin cambios en las publicaciones iniciales ni dependencias nuevas.
