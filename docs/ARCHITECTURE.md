# Arquitectura — V0.1, entrega 4

## Separación conservada

Furvo.Core contiene dominio, puertos ISaveStore/IWorldStore y generación pura sin dependencias externas. Furvo.Persistence implementa SQLite, recursos de datos, migraciones y publicación. Furvo.Godot implementa la consulta visual mediante proyecciones públicas y Furvo.Tests prueba dominio y persistencia. Se conservan las capas y los cuatro proyectos. No se añaden ORM ni paquetes.

Las entidades conservan GUID tipados e inmutabilidad; Registration usa su clave compuesta club/competición/temporada. Player no guarda club: la pertenencia se obtiene del contrato válido en una fecha. Las fechas contractuales son inclusivas y los solapamientos de un jugador se rechazan mediante dominio/persistencia. No se implementan fichajes ni renovaciones.

## Generación y procedencia

InitialWorld2026.ReadDefinition lee el recurso ficticio v2; su API pública sigue siendo el catálogo de clubes. InitialSquadGenerator recibe una instantánea sin jugadores y SquadGenerationSettings, genera jugadores/contratos en memoria y devuelve WorldData. InitialSquads2026 compone esa operación con publicación SQLite, sin acoplar Core al almacenamiento.

El recurso initial-squads-2026-27.json registra versión furvo-squads-1, semilla 20262703, fuente v2, perfiles editoriales y repertorios culturales. SplitMix64 recibe estados SHA-256 de contextos explícitos. Clubes, culturas, atributos y resultados se ordenan canónicamente; no se consulta reloj, cultura del sistema ni orden SQLite. Jugadores y contratos reciben GUID derivados de versión/semilla/ClubId/plaza, sin depender del nombre generado.

El parámetro interno de nivel solo distribuye atributos durante generación. No se incorpora a Player ni SQLite como valoración general. Las bases editoriales por club varían, con dispersión individual, terceros porteros jóvenes y excepciones destacadas en clubes modestos. La reputación no interviene en una fórmula de puntuación. Los atributos siguen siendo la fuente individual de información para la futura resolución por acciones, sin efectos de simulación actuales.

La publicación guarda versión, semilla, hash de parámetros, hash de catálogo y WorldId fuente en generation_metadata. Su WorldId derivado distingue v3 de v2; clubes, competición y temporada conservan IDs. Fingerprint ordena y serializa todas las entidades, posiciones y atributos, incluidos ocultos, para verificar contenido. El informe humano solo expone VisibleValues. Cambios de algoritmo deben publicar otra versión y destino.

## Persistencia y transacciones

Load obtiene todas las entidades en una transacción de lectura. Apply aplica lotes mediante upsert en orden de referencias dentro de una transacción; ante error revierte todo. Conserva atributos omitidos y perfiles omitidos, incluidos ocultos. PlayerPositions aportado sustituye el conjunto completo: elimina secundarias antes de principal, actualiza jugador e inserta principal antes de secundarias. Si se omite, conserva el detalle existente.

Las conexiones activan FK y no usan pooling. SQL valida rangos, unicidad, referencias, contratos y concordancia de posiciones. El esquema no limita la cantidad de jugadores. PlayerPositions conserva los cuatro grupos anteriores y añade trece detalles; no representa un sistema táctico.

CreateInitialWorld, CreateSave y MigrateCopy escriben en temporal exclusivo y publican mediante rename sin sobrescritura. La generación v3 incluye entidades y procedencia en la misma transacción. Una publicación existente solo se verifica; los cambios requieren otra ruta. CreateSave usa backup SQLite, conserva WorldId/entidades y asigna SaveId del llamador. Nunca se usa el archivo inicial como partida editable. La entrega 4 materializa una copia verificada del SQLite publicado en user://catalogs al solicitar la consulta; no genera datos ni crea partidas.

## Migraciones y compatibilidad

PRAGMA user_version identifica el esquema actual **4**. Las migraciones 001–003 no se reescriben. 004 añade player_positions y generation_metadata, con FK, principal única y triggers que exigen principal antes de secundarias, validan grupos y evitan mezclar portería/campo. No añade posiciones, jugadores ni atributos a datos históricos.

MigrateCopy actualiza exclusivamente un archivo de destino nuevo y valida foreign_key_check. Preserva identidades, metadatos, atributos y origen. Initialize y ReadMetadata no migran automáticamente una partida existente. Load/Apply/CreateSave exigen esquema actual; una copia de esquema 1–3 requiere MigrateCopy antes de esas operaciones. Versiones futuras desconocidas se rechazan. Una incompatibilidad de escalas al atravesar 003 se informa sin convertir valores ni publicar archivos parciales.

Los atributos continúan normalizados por código en attribute_definitions/player_attributes (61 códigos, cinco categorías, rango 1–20). Un atributo futuro se añade mediante catálogo y migración nueva sin reconstruir Player. Los datos históricos ausentes permanecen desconocidos. VisibleValues excluye los diez ocultos; AllValues se reserva para lógica y almacenamiento.

Las publicaciones v1 y v2 permanecen en esquema histórico y con sus hashes originales. v3 es una publicación independiente, no una migración que inyecte plantillas en partidas existentes. No se han migrado partidas reales del usuario. Véanse [validación](VALIDATION.md) e [informe](SQUADS_2026_27.md).

## Entrega 4: consulta y presentación

IWorldQueries define una frontera pública en Core. WorldQueries recibe una instantánea WorldData y una fecha de referencia explícita. Devuelve ClubSummary, PlayerSummary y PlayerDetails; ningún DTO contiene Player, PlayerAttributes, AllValues ni contratos internos. La ficha proyecta solo VisibleValues, valida los 51 atributos y entrega cuatro grupos inmutables. Una ficha incompleta produce un error explícito, no una pantalla aparentemente completa. Los diez códigos ocultos no existen en las proyecciones ni en los recursos de textos visibles.

La afiliación se consulta por contratos inclusivos a ReferenceDate; PublishedWorldQueries.Open utiliza el comienzo de la única temporada del catálogo. La ordenación de nombre usa OrdinalIgnoreCase, de posición el código de posición detallada y de edad el número calculado; los desempates usan nombre e ID estable ascendentes. No depende de la cultura del proceso ni del orden SQLite. La selección de la tabla se conserva por PlayerId, nunca por índice.

PublishedWorldQueries utiliza SqliteWorldStore.ReadInfo/Load en modo de solo lectura y rechaza partidas como catálogo. Godot incluye como EmbeddedResource los bytes de data/initial-world-2026-27.v4.db, sin copiar listas a código ni regenerar jugadores. Materialize crea una caché con nombre SHA-256 bajo user://catalogs, publica mediante temporal/rename y verifica los bytes existentes sin sobrescribir discrepancias. Es empaquetado de la publicación existente, no otra versión del mundo, un SaveId o un flujo de nueva partida. La base fuente y los archivos guardados no reciben escrituras.

Main carga en segundo plano y descarta finalizaciones de carga de pantallas abandonadas. Controla inicio, clubes, plantilla y ficha, con regreso entre ellas y errores contextualizados/reintento. DataTable centraliza columnas, filas, metadatos de ID, selección, activación y cabeceras ordenables. Las tablas largas desplazan sus filas; las categorías de atributos se ajustan a su contenido para usar únicamente la barra exterior de ficha. Los textos y 51 etiquetas visibles residen en Resources/UiStrings.resx, español neutro y claves compatibles con futuros recursos de idioma. No se implementan otros idiomas ni pantallas de juego adicionales.

## Contrato del futuro motor determinista (documentado, no implementado)

- Cada encuentro recibe semilla independiente derivada por SHA-256 de semilla de partida, identificador de temporada, jornada, ClubId local/visitante, versión del motor y, cuando sea necesario para desambiguar, ID del encuentro. Se fijará una codificación canónica UTF-8 con delimitación inequívoca y formato invariante; se versionarán orden de campos, extracción de bytes y versión del esquema de entrada.
- Inyectar SplitMix64 por encuentro. No usar Random global, DateTime.Now ni Guid.NewGuid dentro de simulación. El SplitMix64 actual de generación es un precedente, no un motor de partidos ya implementado.
- Orden estable y explícito de acciones, participantes y desempates. No depender de consultas sin ORDER BY, iteraciones de diccionarios o tareas que terminen antes. Sin paralelismo dentro de un encuentro; sí entre encuentros independientes, con publicación de resultados en orden estable.
- Persistir marcador, eventos ordenados, semilla, versión del motor/entradas y todos los datos reproducibles relevantes: alineaciones, parámetros y instantánea de atributos/contexto utilizados. Los IDs solos no bastan si los datos han cambiado. No recalcular automáticamente encuentros disputados al abrir una partida o actualizar el motor.
- Verificar misma entrada/versión/semilla → mismos resultados y eventos, independencia del orden de ejecución entre encuentros y reproducción tras guardar/cargar. Preparar referencia aprobada y versionada de **1.000 partidos**, con huellas esperadas; no sustituirla por comparar dos veces un resultado nuevo contra sí mismo. Registrar plataforma/versión y el presupuesto de rendimiento antes de publicar.

El motor, sus migraciones, los pesos de valoración y sus pruebas de referencia se implementarán en entregas posteriores autorizadas. La valoración interna prevista y el plan de probadores están en [DECISIONS.md](DECISIONS.md); el alcance comercial está en [V1_CONTRACT.md](V1_CONTRACT.md). No cambian las decisiones de resolución deportiva por acciones individuales.

## Ajustes finales de la entrega 4

PlayerSheet conserva la cabecera aprobada y muestra cuatro bloques en tres columnas, con Físicos y Portero apilados en la tercera. No hay pestañas. DataTable añade alineación configurable de encabezados/celdas y salto de línea opcional; clubes usa 45/30/25 con nombre a la izquierda y ciudad/reputación centrados, mientras plantilla conserva su configuración anterior.

AdjustedInitialWorld2026 valida la fuente v3 de solo lectura y publica una copia lógica v4 con el catálogo manual club-reputations-2026-27.v2.json. Los valores internos del catálogo generan los visibles con min(5, entero/20 + 1), tras validar 0–100. Solo Fragua cambia a 32/100 y 2/5. Los jugadores, contratos, atributos —incluidos ocultos— y todas las otras entidades se copian sin recalcularlos. La procedencia en generation_metadata identifica la revisión y el WorldId fuente v3; no representa otro algoritmo de generación de jugadores.

El nuevo WorldId es 72026027-0000-4000-8000-000000000005. No hay migración ni cambio del esquema 4. El valor interno reside en el catálogo versionado, SQLite conserva 1–5. La publicación usa el mecanismo transaccional/temporal existente y rechaza destinos incompatibles. Nunca modifica fuentes o partidas. Los cargadores anteriores y sus recursos permanecen para reproducir las publicaciones previas.

## Ampliación visual inferior de ficha

PlayerSheet conserva su cabecera y las instancias de los cuatro bloques de atributos. Envuelve la zona desplazable en un contenedor vertical y añade debajo PitchPositionsPanel y PlayerInsightsPanel. La cabecera y el regreso permanecen fuera del scroll. Los paneles están en dos columnas con ancho disponible ≥1000 y se apilan por debajo de ese límite. Main, DataTable y AttributeSection no cambian.

PositionPitch dibuja un campo no interactivo mediante CanvasItem, con una marca naranja para la principal y azules para las secundarias recibidas. PitchPositionLayout contiene únicamente coordenadas normalizadas y validación de posiciones existentes; no asigna jugadores ni deduce capacidades. PlayerInsightsPanel usa PositionInsightRules, una política pura de presentación: listas explícitas de códigos públicos por posición, límites descriptivos 14/7, hasta tres elementos y desempate ordinal. No comparte pesos con la valoración interna futura, no calcula medias, no persiste resultados y no consume atributos ocultos.

Los tests compilan las políticas puras desde su fuente real sin añadir paquetes Godot ni referencias del motor a Core. Se contrasta la selección con las 480 fichas, posiciones, valores y recursos españoles. Detalles de configuración y alcance en [PLAYER_SHEET_PANELS.md](PLAYER_SHEET_PANELS.md). No se modifica ningún servicio de Core/Persistence, publicación, contrato o reputación.

## Ficha institucional de club

El listado abre Información mediante ShowClub, con cabecera común y pestañas Información/Plantilla/Historia/Economía. ShowSquad conserva el caso de regreso y selecciona la pestaña 1; la tabla y sus consultas se reutilizan. La identidad seleccionada se mantiene en ClubId, y el PlayerId/orden se conserva entre pestañas y al volver de la ficha individual.

IWorldQueries incorpora GetClubDetails (proyección del perfil e inscripciones vigentes) y GetClubHistory (sin datos aprobados en el esquema actual). Nuevos DTO opcionales distinguen campos desconocidos; no crean entidades o tablas. ClubHistoryOrigin distingue autoría previa al inicio de 2026/27 de historia de partida. La historia propuesta se limita a documentos externos al juego y no se embebe en recursos comerciales.

ClubHistoryPlot/Chart ordenan una colección y no convierten huecos en puestos: bandas de división separadas, primero arriba, sin unión de años desconocidos. ClubEconomyPage solo presenta estados no disponibles; no añade lógica financiera. El esquema propuesto y sus criterios deben aprobarse antes de otra migración/publicación. Véanse [CLUB_SCREEN.md](CLUB_SCREEN.md) y [CLUB_HISTORY_SCHEMA_PROPOSAL.md](CLUB_HISTORY_SCHEMA_PROPOSAL.md).
