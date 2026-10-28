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

La implementación actual congela recursos en el JSON contractual, pero aún no dispone de estado económico operativo. La futura etapa 2 requerirá datos por ClubId/SeasonId y una inicialización económica en la misma publicación transaccional de carrera, sin migrar automáticamente partidas antiguas. La propuesta no es un catálogo cargado por Core/Godot.

## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](INITIAL_CONTRACT_SIMPLIFICATION.md).

## Corrección contractual vigente — 27/10/2026

La validación actual sustituye las reglas de bloqueo descritas en los apartados históricos: solo exige identidad válida, fechas válidas, salario positivo, moneda admitida y contenido/prioridad de obligaciones expresamente exigibles. Las preferencias orientativas pendientes no bloquean ni se guardan como compromisos aceptados. La duración se calcula en meses desde las fechas. Extensión de carrera 2 para nuevas carreras; carga de extensión 1 en solo lectura. Las veinte ofertas reales siguen pendientes de fecha final, salario anual y moneda.

Validación: Debug y Release, 209 pruebas superadas; compilación de cuatro proyectos sin errores ni advertencias. Prueba de escena Godot sin ventana superada; no equivale a revisión visual manual. Véase [informe de corrección](CONTRACT_TERMS_CORRECTION.md).

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

## Nueva fase: perfiles, ofertas y creación de carrera

Core incorpora Careers: CoachProfileId, perfil/experiencia descriptiva, CareerCreationFlow, CoachId propio por carrera, oferta/contrato de entrenador y puertos ICoachProfileStore/ICareerStore. No depende de Godot ni SQLite. La selección es temporal; la confirmación tiene SaveId estable para idempotencia y reintento.

Persistence implementa perfiles separados y carreras como copia del mundo aprobado. Reutiliza SqliteWorldFiles.Publish con visibilidad internal; conserva world user_version=4 y scripts 001–004. Profiles001 crea esquema propio 1; Career001 añade una extensión 1 solo a carreras nuevas. FK/estado, transacción y rename sin sobrescritura protegen atomicidad. Perfil y condiciones aceptadas quedan congelados; perfil no sustituye CoachId. Ninguna partida anterior se migra automáticamente.

Main.Careers compone pantallas con servicios; CoachProfileForm/CoachAvatar/ManagerOfferDocument reutilizan estilos y recursos. InitialManagerOfferSource adapta ClubDirectionProposalCatalog y GetFinancialResources, la misma proyección que Economía. La firma se bloquea por datos no aprobados; paro directo/rechazo sí crean carreras válidas. ConfigureCareerStorage permite aislar pruebas antes de entrar al árbol; no es una preferencia del juego. Cargar desde menú y mecánicas de empleo siguen futuras. Véanse COACH_PROFILES.md, CAREER_CREATION.md y MANAGER_CONTRACTS.md.


## Alineaciones — 28/10/2026

La entrega de alineaciones añade Equipo → Plantilla / Alineación, selección inicial reproducible y persistencia independiente por carrera. La apertura de carreras antiguas no migra ni genera alineaciones automáticamente. Véanse [arquitectura, reglas e inventario de archivos](LINEUPS.md) y [formaciones iniciales de los veinte clubes](INITIAL_FORMATIONS.md). Esta sección actualiza las referencias anteriores a roles sin asignar y al orden inicial por rol: ahora las carreras nuevas tienen once y se ordenan por posición futbolística.
