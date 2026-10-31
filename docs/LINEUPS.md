## Estado vigente — planes tácticos y seis disposiciones

Inicial, Ganando y Perdiendo tienen dos disposiciones propias y un único once por plan; mentalidad e instrucciones independientes y balón parado común. Se conserva una fuente canónica de composición, con lectura compatible y conversión transaccional al guardar. [Modelo, migración, archivos y comprobación](TACTICAL_PLANS.md). Las descripciones anteriores de una táctica única se conservan como historial.

## Estado vigente — Equipo → Táctica

La alineación existente se integra en Táctica junto con Estilo de juego, Transiciones y Balón parado. Mentalidad común y configuración estructurada persistente por carrera. Extensión SQLite independiente versión 1, creada al guardar; lecturas sin migraciones. [Decisiones, archivos y comprobación](TACTICS.md). Los apartados siguientes conservan el historial; las referencias a Equipo → Alineación se entienden ahora como Táctica → Alineación.

# Alineaciones iniciales y edición de carrera

Entrega del 28/10/2026. No incorpora partidos, calidad oficial ni instrucciones tácticas.

## Datos y selección
El catálogo embebido de Core `Lineups/formations.json` define once esquemas, cada uno con once puestos incluido un portero, coordenadas normalizadas sobre el campo horizontal y asociación de los veinte ClubId canónicos. Véase [tabla editorial](INITIAL_FORMATIONS.md). Siete suplentes es una configuración inicial del proyecto, no una regla legal de competición.

Al firmar y crear una carrera se genera una sola alineación. Semilla: primeros 16 caracteres hexadecimales de SHA-256 UTF-8 de `FURVO/lineup/v1/` + SaveId en formato D. Se guarda con la carrera. SplitMix64 local, sin Random global; jugadores ordenados por identificador y puestos por catálogo. Misma semilla y datos producen los mismos miembros y puestos, independientemente del orden de entrada.

Política `lineup-selection-v1`: asignación húngara rectangular. Coste por pareja: penalización de compatibilidad × 1.000.000.000 − suma de tres atributos visibles relevantes × 100.000 + variación SplitMix64 de 0 a 65.535. Penalización: natural 0, secundaria 1, otro puesto de campo 100, incompatibilidad portero/campo 10.000 (rechazada). La clasificación pública de compatibilidad usa 0, 1, 2 y 99 respectivamente. Evitar un futbolista fuera de posición prima sobre todas las coincidencias secundarias del once. El coste global prioriza la elegibilidad antes que la puntuación provisional. La variación decide empates sin introducir calidad ficticia. Los atributos relevantes por demarcación están explícitos en LineupSelection.Relevant; no se promedian los 51 ni se leen los diez ocultos.

Once titulares; suplentes hasta el límite (incluido un portero de respaldo cuando existe) y resto reservas. Banquillo: se conserva el anterior cuando es posible y se completa por puntuación provisional de su posición principal e identificador estable. La puntuación no se muestra como calidad ni se persiste: se sustituirá cuando exista el evaluador aprobado.

## Interacción
Equipo ofrece Plantilla y Alineación. La tabla empieza ordenada por posición futbolística, con nombres e identificadores como desempates estables. Roles provienen de la alineación guardada; las estadísticas siguen sin inventarse. Los indicadores se centran en su celda.

El campo reutiliza FootballPitch y datos de formación. Hover muestra candidatos; clic o activación por teclado fija el puesto. Desfijar recupera el hover. Los candidatos se separan en naturales y secundarios; orden provisional descendente, nombre ordinal e identificador como desempates. Se conservan las posiciones registradas.

Una elección desde banquillo/reservas intercambia ese rol con el titular desplazado. Si el candidato ocupa otro puesto titular, se intercambian solo si ambos puestos son compatibles; en caso contrario se rechaza con mensaje y se conserva el estado anterior.

Cambio de formación: mismo esquema conserva el estado exacto; otro esquema mantiene titulares en la misma demarcación compatible, primero en su puesto exacto, y completa mediante asignación global. No cambia atributos ni posiciones del futbolista. Las carencias de elegibilidad pueden requerir un jugador de campo fuera de posición y se informa de su número; nunca un portero de campo ni un puesto vacío.

Cada cambio confirmado se guarda inmediatamente. No hay borrador no guardado al abandonar la pantalla. La revisión indica el estado persistido.

## Persistencia y compatibilidad
Extensión independiente `Lineups001_Initial.sql`, versión 1. No cambia el esquema general del mundo ni las publicaciones iniciales. Tablas: lineup_schema_version, career_lineup_seed, career_lineups y career_lineup_members. Persisten semilla, fotografía de formación/puestos/coordenadas, límite de suplentes, política, revisión y pertenencia de todos los futbolistas.

Claves foráneas a clubes y jugadores; unicidad de jugador y puesto por club; restricciones rol/puesto; triggers de propietario empleado y pertenencia contractual en la fecha de carrera. El servicio valida once, portero, plantilla completa y exclusividad. Las escrituras reemplazan miembros en una transacción y usan revisión optimista: una edición obsoleta se rechaza sin perder la vigente.

La generación inicial comparte la transacción y publicación atómica de creación de carrera. Load y navegación son solo lectura: no migran ni recalculan. En carreras anteriores sin extensión se ofrece Inicializar alineación, acción explícita, transaccional e idempotente. No sobrescribe contratos, economía ni alineaciones existentes. Carreras sin empleo no reciben un once. No existe guardado/carga general nuevo en esta entrega.

## Archivos de esta entrega
- Core: Furvo.Core.csproj, Careers/CareerWorkspace.cs, Lineups/LineupModels.cs, Lineups/LineupSelection.cs y Lineups/formations.json.
- Persistence: CareerJson.cs, SqliteCareerStore.cs, SqliteLineupStore.cs y Migrations/Lineups001_Initial.sql.
- Godot: Main.Lineups.cs, Main.Navigation.cs, Main.Squad.cs, Components/FootballPitch.cs, InteractiveLineupPitch.cs, SportingIndicator.cs y Resources/UiStrings.resx.
- Pruebas: tests/Furvo.Tests/LineupTests.cs y tools/GodotUiSmoke.gd.
- Documentación: INITIAL_FORMATIONS.md, LINEUPS.md, ARCHITECTURE.md, CAREER_CREATION.md, CAREER_NAVIGATION.md, INVENTORY.md, VALIDATION.md y README.md.

No se modifica ninguna publicación SQLite ni los 480 futbolistas.
