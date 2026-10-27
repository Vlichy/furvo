# Modelo del mundo y catálogo de atributos — Entrega 4

## Datos iniciales

| Entidad | Datos |
| --- | --- |
| Club | ClubId, nombre, ciudad, país, reputación propia (1–5) y ClubProfile. |
| Player | PlayerId, nombre, nacimiento DateOnly, nacionalidad, grupo principal, PlayerPositions opcional y PlayerAttributes. |
| Contract | ContractId, PlayerId, ClubId, fecha inicial y final inclusivas. |
| Coach | CoachId, nombre y ClubId elegido. |
| Competition | CompetitionId, nombre y país. |
| Season | SeasonId, nombre opcional y fechas inicial/final. |
| Registration | Clave compuesta ClubId + CompetitionId + SeasonId. |

WorldId identifica el origen del mundo; SaveId identifica una partida independiente. Los GUID de clubes, competición y temporada están escritos explícitamente en el JSON v2. Los de jugadores y contratos se derivan determinísticamente del generador, semilla, ClubId y plaza; no de los nombres. Se conservan al cargar, verificar otra copia o crear partidas; no se derivan de nombres. La afiliación de jugadores no se duplica en Player. Los contratos simultáneos para jugadores distintos son válidos; los solapamientos del mismo jugador se rechazan.

Los campos opcionales en Club, Player y Contract permiten leer filas históricas del esquema 1. Para guardar una entidad nueva o actualizarla se requieren sus campos descriptivos y fechas. Los atributos pueden estar ausentes: las filas históricas no reciben valoraciones inventadas. El nuevo mundo v3 sí genera los 61 valores de cada jugador.

## Perfil del club y escalas

ClubProfile agrupa ShortName, Region, FoundationYear, SportingIdentity, PrimaryColor, SecondaryColor, StadiumName, StadiumCapacity e InitialCategory. Cuando existe, el perfil debe estar completo, con año entre 1 y 9999 y capacidad positiva. Los perfiles antiguos inexistentes permanecen null, sin datos inventados. La carga inicial requiere y aporta todos los campos para cada uno de los 20 clubes.

| Reputación | Nivel |
| ---: | --- |
| 1 | Muy baja |
| 2 | Baja |
| 3 | Media |
| 4 | Alta |
| 5 | Élite |

La reputación no es una media de atributos ni una función de capacidad, historia o plantilla. Los 61 atributos usan 1–20 incluso cuando son ocultos. No se asignan valores por defecto a entidades incompletas; v3 utiliza generación explícita documentada.

El nombre de temporada se guarda como 2026/27 y es opcional para conservar temporadas antiguas cuyo esquema solo guardaba fechas. La competición y las 20 inscripciones usan las entidades existentes.

## Uso del almacenamiento

Estas llamadas son la API, no un generador ni un script ejecutado automáticamente:

~~~csharp
// destination es una ruta absoluta nueva y su directorio debe existir.
// data contiene los datos aportados por el llamador, sin valores automáticos.
var initial = AdjustedInitialWorld2026.LoadOrCreate(sourceV3, destination); // Nueva publicación v4 desde el origen v3 de solo lectura.
// Para otra definición propia: SqliteWorldFiles.CreateInitialWorld(destination, worldId, data).
var save = SqliteWorldFiles.CreateSave(destination, savePath, SaveId.New());

WorldData snapshot = save.Load();
save.Apply(changes); // Lote de altas/actualizaciones; una transacción completa.

// Si se dispone de una partida del esquema 1:
var migrated = SqliteWorldFiles.MigrateCopy(legacyPath, newPath);
~~~

Core define IWorldStore y WorldData; Persistence proporciona estas implementaciones. SqliteSaveStore.Initialize continúa disponible para crear una partida vacía y leer metadatos; no transforma una partida antigua automáticamente. La utilidad tools/BuildInitialSquads.cs crea/verifica v3 mediante InitialSquads2026.LoadOrCreate. La entrega 4 consulta v4 desde Godot mediante IWorldQueries y proyecciones sin atributos ocultos.

Una actualización de jugador conserva los atributos no incluidos en su lote. No existen operaciones de borrado. La pertenencia por contrato depende de una fecha; no se añade un campo de club duplicado ni una noción implícita de fecha actual del mundo.

## Catálogo aprobado

Los valores son enteros de 1 a 20. Los códigos persisten como identidades y no deben renombrarse para traducir una etiqueta. La ausencia de una fila significa desconocido, no cero, una media ni un valor aleatorio. Las puntuaciones concretas usadas en pruebas solo verifican límites y persistencia.

### Técnicos (17)

| Código estable | Atributo |
| --- | --- |
| ball_control | Control del balón |
| first_touch | Primer toque |
| technique | Técnica |
| dribbling | Regate |
| short_passing | Pase corto |
| long_passing | Pase largo |
| crossing | Centros |
| finishing | Remate |
| long_shots | Disparo lejano |
| shot_power | Potencia de disparo |
| heading | Cabeceo |
| tackling | Entradas |
| marking | Marcaje |
| penalties | Penaltis |
| free_kicks | Faltas |
| corners | Córneres |
| throw_ins | Saques de banda |

### Mentales (16)

| Código estable | Atributo |
| --- | --- |
| anticipation | Anticipación |
| concentration | Concentración |
| decisions | Decisiones |
| vision | Visión de juego |
| defensive_positioning | Colocación defensiva |
| off_the_ball | Desmarques |
| teamwork | Trabajo en equipo |
| work_rate | Esfuerzo |
| determination | Determinación |
| composure | Serenidad |
| bravery | Valentía |
| aggression | Agresividad |
| leadership | Liderazgo |
| creativity | Talento / creatividad |
| tactical_discipline | Disciplina táctica |
| tactical_understanding | Comprensión táctica |

### Físicos (8)

| Código estable | Atributo |
| --- | --- |
| pace | Velocidad |
| acceleration | Aceleración |
| stamina | Resistencia |
| strength | Fuerza |
| agility | Agilidad |
| balance | Equilibrio |
| jumping | Salto |
| coordination | Coordinación |

### Portero (10)

| Código estable | Atributo |
| --- | --- |
| reflexes | Reflejos |
| handling | Blocaje |
| punching | Despejes |
| one_on_ones | Uno contra uno |
| aerial_claims | Salidas aéreas |
| rushing_out | Salidas a los pies |
| goalkeeping_positioning | Colocación en portería |
| command_of_area | Mando del área |
| hand_distribution | Saque con la mano |
| defensive_communication | Comunicación defensiva |

### Ocultos (10)

| Código estable | Atributo |
| --- | --- |
| professionalism | Profesionalidad |
| ambition | Ambición |
| consistency | Regularidad |
| big_matches | Rendimiento en grandes partidos |
| adaptability | Adaptabilidad |
| injury_proneness | Propensión a lesiones |
| pressure_resistance | Resistencia a la presión |
| temperament | Temperamento |
| loyalty | Lealtad |
| learning_ability | Capacidad de aprendizaje |

## Visibilidad y evolución

Player.Attributes.VisibleValues contiene exclusivamente valores de las cuatro categorías visibles. AllValues también incluye los diez ocultos para almacenamiento y lógica interna; la interfaz normal solo recibe su proyección pública mediante IWorldQueries. La ficha muestra únicamente los 51 atributos visibles.

Se incorporan únicamente las definiciones del catálogo a una base nueva: no se crean jugadores ni se asigna un solo valor sin que el llamador lo aporte. La tabla player_attributes relaciona jugador, código y valor, con FK y CHECK de rango.

Para ampliar el catálogo en una versión futura: conservar los códigos existentes, añadir la definición C# y una nueva migración que inserte su definición SQLite, y probar migración/persistencia. No se requieren nuevas columnas ni reconstruir players. No se modifica el script 002 ya publicado. Las nuevas definiciones no asignan automáticamente valores a futbolistas antiguos.

No existen valoraciones generales almacenadas, ponderaciones por posición ni fórmulas de rendimiento. La reputación del club no representa la calidad de su plantilla. El diseño mantiene los atributos finos y deja preparada su utilización por valores derivados y por acciones individuales de la futura simulación, sin implementarla.

Esta entrega incorpora los 20 clubes, la Liga Cumbre de España y la temporada 2026/27 (2026-07-01 a 2027-06-30). Las plantillas publicadas contienen 24 jugadores por club y 480 en total. No se fija un calendario de partidos.

## Esquema 3 y compatibilidad de escalas

La migración 003 se aplica únicamente a una copia nueva con MigrateCopy. Antes de cambiar el esquema se detectan todas las reputaciones incompatibles con 1–5 y todos los valores incompatibles con 1–20. SchemaCompatibilityException informa ID, campo, valor y rango; el original permanece intacto y no se publica el destino fallido. No hay política de conversión automática. Los valores válidos, los códigos y las categorías se conservan.

Las migraciones publicadas 001/002 no se reescriben. La tabla de valores individuales se reconstruye transaccionalmente con CHECK 1–20, copiando sus filas válidas sin cambiarlas. La reputación incorpora triggers de inserción y actualización 1–5 junto a la restricción histórica.

El universo y sus IDs completos se describen en [CLUBS_2026_27.md](CLUBS_2026_27.md). La carga inicial rechaza diferencias frente a una plantilla publicada y nunca se aplica sobre partidas.

## Fuente de clubes y publicación actual

El catálogo ficticio v2 y sus veinte ClubId se conservan sin cambios. La publicación activa es v4, con esquema 4 y WorldId propio; v1/v2/v3 permanecen intactos. El JSON v2 y los parámetros de generación son recursos de Persistence. La correspondencia real-ficticia se limita a desarrollo/Tests. Véanse [clubes](CLUBS_2026_27.md), [arquitectura](ARCHITECTURE.md) e [inventario](INVENTORY.md).

## Posiciones detalladas y generación inicial

PlayerPositions conserva una Primary y una colección inmutable Secondary, sin duplicados ni repetición de la principal. PlayerRole distingue Goalkeeper, CentreBack, RightBack, LeftBack, DefensiveMidfielder, CentralMidfielder, AttackingMidfielder, RightMidfielder, LeftMidfielder, Striker, RightWinger, LeftWinger y SecondStriker. GroupOf conserva el grupo PlayerPosition anterior; se valida la concordancia. No se mezclan portería y posiciones de campo. No son roles tácticos ni ponderaciones de rendimiento.

Player.Positions es opcional para leer datos históricos; aportar posiciones sustituye el conjunto de forma atómica, omitirlo conserva las anteriores. El esquema 4 añade player_positions, FK, unicidad de principal y triggers de concordancia. Las filas antiguas permanecen sin detalle. AgeOn(DateOnly) calcula la edad sin almacenarla ni consultar el reloj.

La plantilla inicial usa 24 plazas por club exclusivamente en InitialSquadGenerator. Sus 480 contratos cubren la temporada y no duplican afiliación en Player. No hay límites permanentes de plantilla, medias oficiales ni efectos deportivos. Procedencia y estadísticas: [SQUADS_2026_27.md](SQUADS_2026_27.md).

## Publicación corregida v4

La corrección aprobada fija Fragua del Sur (ClubId 72026027-0003-4000-8000-000000000005) en interna 32/100 y visible derivada 2/5. El catálogo activo de autoría está en src/Furvo.Persistence/Data/club-reputations-2026-27.v2.json. Los otros diecinueve valores no cambian. Club.Reputation y el esquema SQLite siguen almacenando 1–5; no se añade una columna interna. v4 conserva todos los IDs y los 480 jugadores de v3 sin cambios deportivos, y no se aplica a partidas existentes. Las publicaciones anteriores permanecen intactas.
