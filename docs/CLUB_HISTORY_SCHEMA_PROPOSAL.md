# Esquema propuesto para identidad e historia — pendiente de aprobación

**Documento de diseño, sin SQL ejecutable ni migraciones aplicadas.** El esquema actual sigue siendo 4 y las publicaciones v1–v4 permanecen intactas. Esta propuesta no autoriza cargar CLUB_HISTORY_PROPOSAL.md ni crear entidades históricas jugables.

## Frontera actual de lectura

IWorldQueries añade GetClubDetails y GetClubHistory. La primera proyecta Club/ClubProfile e inscripciones vigentes existentes; no consulta SQLite desde Godot. La segunda valida el ClubId y devuelve IsApprovedDataAvailable=false, listas vacías y contadores null. No convierte Seasons o Registration en puestos finales, títulos, temporadas disputadas ni ascensos. Las colecciones vacías significan que no hay datos aprobados disponibles, no que el club nunca haya ganado o tenido leyendas.

ClubSeasonStanding es una proyección futura, no una entidad persistida nueva. Distingue Origin.AuthoredBefore2026 (temporadas cuyo inicio es anterior a 2026/27, incluido 2025/26) y Origin.SimulatedSave. No se genera ninguna fila de esos tipos para el catálogo actual. El gráfico y la tabla reciben colecciones, sin tres temporadas de ejemplo codificadas; podrán crecer al recibir futuras filas aprobadas sin rehacer la interfaz.

## Extensiones a revisar antes de implementar

| Área | Estructura propuesta | Integridad y significado |
| --- | --- | --- |
| Identidad | club_identity_extension, ClubId como PK/FK, apodo y denominación de afición opcionales | No duplicar nombre, fundación ni colores ya existentes. Ausencia = desconocido. |
| Rivalidades | club_rivalries con par canónico de ClubId distintos | FK a clubes aprobados, par único; representar simetría sin dos filas contradictorias. No crear clubes espejo reales. |
| Abonados | dato inicial aprobado y estado por temporada/partida cuando exista el sistema correspondiente | No inferir de la capacidad ni generar cantidades; procedencia y fecha explícitas. |
| Estadio | ciudad aprobada del estadio, asociada al club mientras no exista entidad de estadio aprobada | No asumir igualdad con ciudad del club. No duplicar nombre/capacidad actuales. |
| Asistencia | métricas por club/temporada: asistencia total, encuentros contabilizados y récord | Media y ocupación serían derivados de datos completos y capacidad aplicable; no implementados ahora. |
| Historia editorial | authored_club_season_standings: edición de catálogo, ClubId, SeasonId o identidad histórica estable aprobada, categoría, puesto y estadísticas completas opcionales | Solo contenido revisado, inmutable y previo a la temporada inicial. FK, unicidad club/temporada y posición válida para participantes de esa edición. |
| Historia de partida | save_club_season_standings: ClubId, SeasonId y resultados consolidados | Solo en archivos kind=save, al finalizar una temporada mediante transacción. No escribir en la publicación inicial. |
| Palmarés | authored_club_honours y save_club_honours, con competición/temporada/club | Distinguir origen, evitar títulos duplicados y no crear competiciones excluidas de V1.0. |
| Personajes de archivo | historical_people y relaciones club/período/función, con identificador propio | Personajes editoriales distintos de PlayerId/CoachId jugables. No añadirlos a la plantilla de 480 ni generar atributos, contratos o biografías sin aprobación. |
| Récords | cifras y titular de récord con competición, período y origen verificables | No inferir récords de las listas de leyendas; desconocido permanece null. |

Los nombres de tablas son **propuestas**, no contratos de una migración publicada. El modelo definitivo, tipos, identificadores históricos y reglas de nulabilidad deben aprobarse antes de escribir scripts. Cualquier cambio de esquema será numerado y probado; no se reescriben 001–004. El contenido inicial aprobado requerirá una publicación nueva, nunca sobrescribir v4 ni transformar partidas existentes automáticamente.

## Consulta unificada sin mezclar orígenes

La vista de historia combinaría editorial aprobada y resultados de partida por ClubId, temporada y competición. El corte se basa en la temporada inicial de la partida: no usar el reloj. Rechazar superposiciones de origen para el mismo registro, conservar trazabilidad y nunca regenerar resultados de temporadas ya disputadas al cargar. Las propuestas no aprobadas no forman parte de esa unión.

Tabla y gráfico ya soportan temporadas ordenadas y posiciones ausentes. Una temporada sin puesto no produce punto ni una línea atravesando el hueco. Primera y Segunda tienen bandas visuales separadas, con primero arriba. Las flechas de cambio de división solo se representan entre temporadas consecutivas con ambos resultados conocidos; no generan ascensos/descensos del simulador.

## Clasificación histórica acumulada

Criterio editorial propuesto y visible: puntos normalizados de Primera = 3×victorias + empates. Desempates: diferencia de goles, goles a favor, victorias y ClubId ordinal. Requiere estadísticas aprobadas y completas; posiciones y títulos por sí solos no bastan. Segunda se clasifica por separado. No utilizar reputación, presupuesto ni calidad de plantilla como sustitutos. Actualmente no se calcula ni publica una posición histórica.

## Economía

La pestaña es una estructura visual con No disponible. Tesorería es saldo líquido; presupuestos son límites de planificación; balance es diferencia entre ingresos y gastos del período. No se proponen todavía tablas financieras, importes, salarios, deuda, transacciones ni reglas provisionales. Ese sistema requiere su propio diseño y autorización posteriores.

## Validación exigida a una implementación posterior

Pruebas de migración a copia y preservación de original, FK/uniquidad, cortes de origen, continuidad de identidad tras ascensos, ausencia de duplicados, palmarés coherente y persistencia/reapertura. Probar consulta de toda la colección histórica y ampliación con temporadas nuevas sin modificar el diseño de pantalla. Esa implementación no se inicia en este encargo.
