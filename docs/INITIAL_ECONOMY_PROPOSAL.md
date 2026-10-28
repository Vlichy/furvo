## Aprobación posterior — 28/10/2026

El usuario aprobó expresamente esta propuesta y sus hipótesis. Se ha integrado su copia de ejecución en la publicación v5. El texto original que sigue documenta la etapa 1 y no representa un bloqueo actual. [Informe de etapa 2](ECONOMY_IMPLEMENTATION.md).

# Propuesta económica inicial — etapa 1

**Estado: PROPUESTA NO APROBADA. No se utiliza en el juego ni se importa a SQLite.**

Fecha: 28/10/2026. Temporada: 2026/27. Importes de las tablas en millones de EUR; el JSON asociado contiene euros enteros. Cada fila está vinculada al ClubId estable en [datos de propuesta](proposals/initial-economy-2026-27.draft.json). No contiene cifras oficiales de clubes reales.

## Diagnóstico del proyecto

- Core expone ClubFinancialResources con seis presupuestos/márgenes y moneda, todos actualmente desconocidos en la consulta inicial. No modela tesorería, deuda, previsiones ni movimientos.
- Economía y Contrato consumen la misma consulta GetFinancialResources; sus desconocidos son coherentes. Economía tiene etiquetas preparadas para conceptos adicionales, pero no fuentes de importes ni cálculo contable.
- Los contratos de los 480 jugadores solo contienen vinculación y fechas: no existen salarios comprometidos suficientes para calcular un margen salarial real.
- La fuente de oferta ya fija 01/07/2026–30/06/2027, anota un año desde las fechas, resuelve EUR por país y conserva la firma real bloqueada por salario no aprobado. Los antecedentes nuevos son debutante/exfutbolista y no afectan el sueldo.
- SqliteCareerStore crea una copia temporal, aplica extensiones de carrera 1/2, inserta entrenador/contrato/estado en transacción y publica sin sobrescribir. Conserva condiciones aceptadas en offer_json. Todavía no inicializa un estado económico consultable independiente para el club: los presupuestos de fixtures solo viajan en la instantánea contractual.
- Mundo esquema 4, perfiles 1, carreras 1/2. No hay tablas económicas nuevas ni migraciones en esta etapa. Las pruebas existentes validan firma con fixtures y compatibilidad; no deben confundirse con habilitación de salarios reales.
- El archivo histórico docs/decisions/club-reputations-2026-27.json conserva Fragua=14. La propuesta usa la corrección vigente de la publicación v4: Fragua=32, sin editar el archivo histórico ni reputaciones del juego.

## Criterios de diseño

Asignación manual por club, no una función de reputación. Se combinan reputación aprobada, todos con 24 jugadores de Primera, aforo existente, perfil institucional/territorial y trayectoria del universo ficticio. La base social y capacidad comercial de estas cifras son hipótesis de diseño propias, no un censo verificado. Los referentes de desarrollo se usan para diferenciar grandes marcas, clubes históricos endeudados, estructuras formativas y recién ascendidos ficticios; no para copiar balances, deudas o salarios reales.

Horizonte/Brisa concentran marca y recursos. Brisa tiene mayor deuda pese a tesorería menor. Aljarafe/Lumbre soportan tensiones históricas ficticias. Ceramista supera en fichajes a clubes de mayor reputación por patrocinio/estructura. Ferrería y Mareal priorizan formación; modestos mantienen liquidez y límites inferiores. Fragua conserva reputación 32 y un volumen coherente con su continuidad deportiva. No hay ingresos internacionales presupuestados, coherentemente con el alcance inicial.

El [informe agregado de LALIGA 2024/25](https://www.laliga.com/es-NG/noticias/laliga-alcanza-un-nuevo-maximo-historico-de-ingresos-y-supera-los-17-millones-de-espectadores) sirve únicamente como contexto sobre variedad de fuentes (audiovisual/comercial/estadio), sin trasladar cifras reales ni distribución por club. Los aforos e identidades se contrastan con los recursos existentes del proyecto.

## Tabla comparativa para aprobación

ND = desconocido. Reserva plan = parte del techo salarial no asignada por el escenario, no margen libre contractual. El margen disponible real permanece ND hasta disponer de compromisos salariales aprobados.

| Club | Rep. | Tesorería | Fichajes | Salarios jugadores | Margen real / reserva plan | Staff | Cantera | Deuda CP | Deuda LP | Sueldo entrenador |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Horizonte | 98 | 95 | 60 | 180 | ND / 27 | 24 | 12 | 30 | 180 | 4,5 |
| Brisa | 96 | 45 | 35 | 165 | ND / 24,75 | 22 | 14 | 50 | 300 | 4 |
| Pedernal | 86 | 32 | 25 | 95 | ND / 14,25 | 13 | 6 | 18 | 110 | 2,2 |
| Aljarafe | 76 | 12 | 8 | 52 | ND / 7,8 | 8 | 5 | 20 | 85 | 1,1 |
| Ferrería | 74 | 30 | 18 | 60 | ND / 9 | 9 | 7 | 6 | 20 | 1,4 |
| Lumbre | 72 | 9 | 6 | 45 | ND / 6,75 | 7 | 4 | 18 | 95 | 0,95 |
| Azahar | 68 | 18 | 14 | 50 | ND / 7,5 | 7,5 | 5 | 10 | 45 | 1,1 |
| Mareal | 66 | 26 | 17 | 47 | ND / 7,05 | 7 | 7 | 5 | 20 | 1 |
| Ceramista | 64 | 28 | 22 | 54 | ND / 8,1 | 8 | 8 | 4 | 12 | 1,3 |
| Faro | 54 | 10 | 7 | 27 | ND / 4,05 | 4,2 | 3,2 | 8 | 22 | 0,6 |
| Ensenada | 52 | 14 | 10 | 32 | ND / 4,8 | 4,8 | 4,2 | 7 | 25 | 0,7 |
| Litoral | 47 | 10 | 8 | 31 | ND / 4,65 | 4,5 | 3,5 | 12 | 48 | 0,65 |
| Senda | 44 | 12 | 6 | 25 | ND / 3,75 | 3,8 | 3,2 | 5 | 18 | 0,55 |
| Mosaico | 34 | 8 | 4 | 20 | ND / 3 | 3,2 | 2,8 | 4 | 12 | 0,4 |
| Abra | 32 | 7 | 3,5 | 18 | ND / 2,7 | 3 | 2,5 | 4 | 10 | 0,35 |
| Acequia | 30 | 6 | 3 | 19 | ND / 2,85 | 3 | 2,7 | 7 | 23 | 0,38 |
| Umbral | 28 | 7 | 3 | 21 | ND / 3,15 | 3,2 | 2 | 3 | 8 | 0,42 |
| Robledal | 26 | 7 | 3 | 18 | ND / 2,7 | 2,8 | 2,2 | 3 | 9 | 0,35 |
| Palmeral | 24 | 8 | 3,5 | 18,5 | ND / 2,775 | 2,8 | 2,4 | 2 | 8 | 0,36 |
| Fragua del Sur | 32 | 10 | 4 | 23 | ND / 3,45 | 3,4 | 2 | 4 | 15 | 0,45 |

## Previsiones de ingresos anuales, no ingresos realizados

Ventas de jugadores: ND en todos los clubes, sin operaciones. No se usa como recurso necesario para viabilidad. No se introduce venta ficticia. Abonos y taquilla son categorías disjuntas: abonos cubren socios de pago; taquilla solo entradas no incluidas en abonos. Comercial excluye patrocinios ya identificados. No se añade una partida genérica de otros ingresos que duplique conceptos.

| Club | TV | Abonos | Taquilla | Patrocinios | Comercial | Total recurrente |
|---|---:|---:|---:|---:|---:|---:|
| Horizonte | 110 | 30 | 18 | 90 | 100 | 348 |
| Brisa | 105 | 32 | 16 | 85 | 95 | 333 |
| Pedernal | 80 | 18 | 9 | 38 | 28 | 173 |
| Aljarafe | 55 | 9 | 5 | 16 | 10 | 95 |
| Ferrería | 60 | 15 | 5 | 17 | 14 | 111 |
| Lumbre | 52 | 10 | 6 | 13 | 9 | 90 |
| Azahar | 55 | 14 | 7 | 17 | 12 | 105 |
| Mareal | 53 | 9 | 4 | 15 | 10 | 91 |
| Ceramista | 56 | 5 | 3 | 20 | 12 | 96 |
| Faro | 40 | 7 | 3 | 7 | 4 | 61 |
| Ensenada | 44 | 6 | 3 | 9 | 5 | 67 |
| Litoral | 43 | 7 | 3 | 10 | 6 | 69 |
| Senda | 40 | 6 | 2,5 | 7 | 4 | 59,5 |
| Mosaico | 36 | 6 | 2,5 | 6 | 4 | 54,5 |
| Abra | 35 | 4,5 | 2 | 5 | 3 | 49,5 |
| Acequia | 36 | 4,5 | 2 | 5 | 3 | 50,5 |
| Umbral | 38 | 3 | 1,5 | 6 | 3 | 51,5 |
| Robledal | 35 | 3,5 | 1,5 | 5 | 3 | 48 |
| Palmeral | 35 | 4 | 2 | 5 | 3 | 49 |
| Fragua del Sur | 39 | 3,5 | 1,5 | 6 | 4 | 54 |

## Previsiones de gastos anuales, no gastos realizados

Jugadores = asignación de planificación del 85 % del presupuesto anual, no suma de contratos. Personal = 85 % de staff e incluye una sola vez al entrenador y sus costes estimados. Cantera es un centro de coste aparte (incluido su personal juvenil), excluido del staff y operaciones. Mantenimiento cubre estadio; operaciones recoge el resto de logística, administración deportiva y funcionamiento, excluyendo salarios, cantera, estadio e intereses. Es una simplificación de diseño, no cuenta anual auditada.

Intereses = 4 % anual sobre deuda inicial total: hipótesis ficticia uniforme del escenario, no tipo contractual histórico. Compras: desembolso y reconocimiento contable permanecen ND sin operaciones. No se contabiliza el presupuesto de fichajes como gasto; no se fija amortización ni vida útil antes de existir contratos. No hay movimientos reales generados.

| Club | Jugadores plan | Personal (incl. entrenador) | Cantera | Estadio | Operaciones | Intereses | Total recurrente |
|---|---:|---:|---:|---:|---:|---:|---:|
| Horizonte | 153 | 20,4 | 12 | 20 | 25 | 8,4 | 238,8 |
| Brisa | 140,25 | 18,7 | 14 | 22 | 26 | 14 | 234,95 |
| Pedernal | 80,75 | 11,05 | 6 | 12 | 15 | 5,12 | 129,92 |
| Aljarafe | 44,2 | 6,8 | 5 | 7 | 9 | 4,2 | 76,2 |
| Ferrería | 51 | 7,65 | 7 | 8 | 10 | 1,04 | 84,69 |
| Lumbre | 38,25 | 5,95 | 4 | 8 | 8 | 4,52 | 68,72 |
| Azahar | 42,5 | 6,375 | 5 | 8 | 9 | 2,2 | 73,075 |
| Mareal | 39,95 | 5,95 | 7 | 6 | 8 | 1 | 67,9 |
| Ceramista | 45,9 | 6,8 | 8 | 5 | 9 | 0,64 | 75,34 |
| Faro | 22,95 | 3,57 | 3,2 | 4 | 12 | 1,2 | 46,92 |
| Ensenada | 27,2 | 4,08 | 4,2 | 4,5 | 13 | 1,28 | 54,26 |
| Litoral | 26,35 | 3,825 | 3,5 | 5,5 | 12 | 2,4 | 53,575 |
| Senda | 21,25 | 3,23 | 3,2 | 3,5 | 12 | 0,92 | 44,1 |
| Mosaico | 17 | 2,72 | 2,8 | 4 | 12 | 0,64 | 39,16 |
| Abra | 15,3 | 2,55 | 2,5 | 3 | 11 | 0,56 | 34,91 |
| Acequia | 16,15 | 2,55 | 2,7 | 3,5 | 11 | 1,2 | 37,1 |
| Umbral | 17,85 | 2,72 | 2 | 2,5 | 12 | 0,44 | 37,51 |
| Robledal | 15,3 | 2,38 | 2,2 | 2,5 | 11 | 0,48 | 33,86 |
| Palmeral | 15,725 | 2,38 | 2,4 | 3 | 11 | 0,4 | 34,905 |
| Fragua del Sur | 19,55 | 2,89 | 2 | 2,5 | 12 | 0,76 | 39,7 |

## Relaciones entre magnitudes y controles

- Tesorería: saldo disponible al inicio, no resultado del ejercicio ni suma de presupuestos. No se añade automáticamente ninguna previsión a caja.
- Deuda total = corto plazo + largo plazo. No se resta toda la deuda a tesorería como si venciera el primer día.
- Reserva salarial plan = presupuesto jugadores − previsión jugadores. Margen contractual real = presupuesto − compromisos efectivos aprobados: actualmente ND. Una reserva plan nunca se importa como margen contractual real sin resolver esta diferencia.
- Salario anual del entrenador: propuesta manual por club. Margen de la regla existente = sueldo propuesto − mínimo anual configurado; todos son estrictamente positivos. En etapa 2 se necesitará resolver la regla por ClubId en vez de aplicar un margen uniforme a todos. No hay diferencias por nacionalidad o antecedente.
- Coste empleador del entrenador en el escenario = bruto anual × 1,30. El 30 % es una provisión ficticia simplificada de diseño, no una cotización legal exacta. Se incluye dentro de la previsión de personal y del techo staff, no se suma nuevamente al gasto.
- Otros costes de personal propuestos = previsión de personal − coste empleador entrenador; todos positivos. Staff restante real seguirá desconocido sin contratos de personal. No se carga el salario del entrenador al margen de jugadores.
- Superávit de planificación recurrente = previsión ingresos recurrentes − previsión gastos recurrentes. No equivale a beneficio contable, flujo de caja completo ni resultado cerrado: faltan impuestos, amortización, calendario de cobros/pagos y operaciones.
- Prueba de tensión simplificada = tesorería inicial + superávit de planificación − todo el presupuesto de fichajes desembolsado − toda deuda CP amortizada en el ejercicio. Presume cobro/pago de recurrentes y no contempla traspasos vendidos ni financiación nueva. No es proyección mensual ni garantía de solvencia. Amortizar principal reduce caja/deuda, no se vuelve a incluir en gastos.

| Club | Deuda total | Coste entrenador incl. provisión | Resto personal plan | Superávit plan | Caja de tensión |
|---|---:|---:|---:|---:|---:|
| Horizonte | 210 | 5,85 | 14,55 | 109,2 | 114,2 |
| Brisa | 350 | 5,2 | 13,5 | 98,05 | 58,05 |
| Pedernal | 128 | 2,86 | 8,19 | 43,08 | 32,08 |
| Aljarafe | 105 | 1,43 | 5,37 | 18,8 | 2,8 |
| Ferrería | 26 | 1,82 | 5,83 | 26,31 | 32,31 |
| Lumbre | 113 | 1,235 | 4,715 | 21,28 | 6,28 |
| Azahar | 55 | 1,43 | 4,945 | 31,925 | 25,925 |
| Mareal | 25 | 1,3 | 4,65 | 23,1 | 27,1 |
| Ceramista | 16 | 1,69 | 5,11 | 20,66 | 22,66 |
| Faro | 30 | 0,78 | 2,79 | 14,08 | 9,08 |
| Ensenada | 32 | 0,91 | 3,17 | 12,74 | 9,74 |
| Litoral | 60 | 0,845 | 2,98 | 15,425 | 5,425 |
| Senda | 23 | 0,715 | 2,515 | 15,4 | 16,4 |
| Mosaico | 16 | 0,52 | 2,2 | 15,34 | 15,34 |
| Abra | 14 | 0,455 | 2,095 | 14,59 | 14,09 |
| Acequia | 30 | 0,494 | 2,056 | 13,4 | 9,4 |
| Umbral | 11 | 0,546 | 2,174 | 13,99 | 14,99 |
| Robledal | 12 | 0,455 | 1,925 | 14,14 | 15,14 |
| Palmeral | 10 | 0,468 | 1,912 | 14,095 | 16,595 |
| Fragua del Sur | 19 | 0,585 | 2,305 | 14,3 | 16,3 |

Las veinte filas tienen importes no negativos, reservas plan menores que techo salarial, salario superior al suelo general y coste de entrenador inferior al personal previsto. La prueba de tensión no da caja negativa; Aljarafe y Lumbre tienen menos holgura. No se infiere estabilidad a 30 temporadas: no existe simulación económica operativa.

## Base social y fundamento específico por club

Para comprobar orden de magnitud del estadio se propone, solo en el escenario, un 65 % de aforo en abonados redondeado a centenas y un 80 % de asistencia media total. No son datos existentes ni compromisos de abonos efectivos. Con las hipótesis propuestas, los ingresos unitarios implícitos abarcan aproximadamente 211–568 EUR por abono anual y 24–78 EUR por entrada no abonada; son controles de orden de magnitud, no tarifas implementadas. Las previsiones de abonos/taquilla se fijan manualmente; su ingreso unitario implícito se comprueba usando 19 partidos de liga en casa, sin crear calendario. Precios, elasticidad y renovación de abonos no están implementados.

| Club | Aforo existente | Abonados plan | Asistencia media plan | Fundamento ficticio |
|---|---:|---:|---:|---|
| Horizonte | 81200 | 52800 | 64960 | Escala nacional e internacional de marca; gran estadio y capacidad comercial; deuda de infraestructura asumible. |
| Brisa | 87200 | 56700 | 69760 | Gran marca y base social; fuerte inversión juvenil y deuda estructural elevada que limita liquidez. |
| Pedernal | 68400 | 44500 | 54720 | Dimensión metropolitana, estadio grande y ambición deportiva; deuda relevante y recursos inferiores a los dos líderes. |
| Aljarafe | 42100 | 27400 | 33680 | Histórico con presión de deuda y gasto heredado; reputación superior a su liquidez disponible. |
| Ferrería | 50600 | 32900 | 40480 | Base social local fuerte y gran estadio; planificación prudente con inversión formativa y deuda moderada. |
| Lumbre | 49200 | 32000 | 39360 | Histórico de gran afición con tensión financiera; fichajes restringidos aunque conserva capacidad comercial. |
| Azahar | 57500 | 37400 | 46000 | Estadio amplio y afición numerosa; crecimiento comercial con deuda intermedia. |
| Mareal | 37600 | 24400 | 30080 | Estructura equilibrada y fuerte apuesta formativa; solvencia relativa y estadio de escala media. |
| Ceramista | 22600 | 14700 | 18080 | Aforo menor compensado por patrocinio y estructura deportiva; cantera y recursos superiores a su reputación relativa. |
| Faro | 28600 | 18600 | 22880 | Histórico recién ascendido en el mundo ficticio; buena base social, expansión prudente y deuda contenida. |
| Ensenada | 25800 | 16800 | 20640 | Escala regional consolidada, formación y estadio medio; recursos equilibrados. |
| Litoral | 38200 | 24800 | 30560 | Mercado metropolitano competitivo y estadio amplio; deuda que reduce flexibilidad. |
| Senda | 21800 | 14200 | 17440 | Afición local estable y aforo limitado; sostenibilidad y desarrollo por encima de gasto de mercado. |
| Mosaico | 32200 | 20900 | 25760 | Recién ascendido ficticio, estadio y afición con potencial; presupuesto de adaptación conservador. |
| Abra | 22400 | 14600 | 17920 | Recién ascendido ficticio de base regional; estructura ajustada y fichajes selectivos. |
| Acequia | 24700 | 16100 | 19760 | Club urbano modesto con deuda intermedia; liquidez y mercado limitados. |
| Umbral | 13700 | 8900 | 10960 | Estadio pequeño y arraigo de barrio; peso audiovisual alto y deuda baja. |
| Robledal | 19800 | 12900 | 15840 | Aforo reducido y dimensión regional; estructura prudente y liquidez de seguridad. |
| Palmeral | 29300 | 19000 | 23440 | Aforo relativamente amplio para su dimensión; baja deuda e inversión gradual. |
| Fragua del Sur | 16800 | 10900 | 13440 | Aforo pequeño con continuidad deportiva; recursos algo superiores a otros modestos y reputación vigente 32. |

## Referencia legal y aprobación salarial

Referencia consultada el 28/10/2026: [Real Decreto 126/2026, artículo 3.1](https://www.boe.es/eli/es/rd/2026/02/18/126/con), mínimo general anual bruto a jornada completa 17.094 EUR en 2026 (1.221 × 14). Los salarios de esta propuesta se expresan en la misma base anual bruta; no son salarios históricos ni observados de clubes reales. Esta referencia no sustituye posibles mínimos específicos, complementos o convenios. La referencia para 2027 no se inventa: queda pendiente de configuración cuando corresponda; no se certifica cumplimiento futuro usando automáticamente el dato 2026.

La regla Core ya admite país, moneda, año y fuente como instantánea y calcula desde un margen aprobado. La configuración de ejecución continúa sin margen aprobado. Este borrador no se carga desde InitialManagerOfferSource ni GetFinancialResources. EUR se conserva como código; la presentación EUR (€) podrá añadirse con textos centralizados al integrar la etapa 2.

## Etapa 2 propuesta, NO ejecutada

Tras aprobación expresa de versión/cifras: diseñar persistencia económica versionada por ClubId/SeasonId y una nueva publicación del mundo sin sobrescribir v1–v4; registrar presupuesto y saldo como categorías diferentes; inicializar estado económico de todos los clubes en cada carrera nueva (incluido inicio en paro) en la misma publicación atómica. Ampliar Core/Persistence conservando la única consulta pública para Economía y Contrato. El contrato congela lo aceptado; el estado económico posterior será independiente de la instantánea.

Resolver antes la interpretación de salarios de jugadores: aprobar importes reales por contrato o mantener compromisos/margen ND. No asignar nóminas ficticias a jugadores para rellenar el escenario. Resolver salarios por ClubId desde el conjunto aprobado y validar capacidad de staff sin doble contabilización; fijar la política de rescisión únicamente si se aprueba. No convertir filosofía/táctica borrador en obligaciones. Las preferencias orientativas seguirán sin bloquear.

Firma: perfil/club válidos, condiciones y salario aprobados; copia del mundo, contrato, instantánea e inicialización económica dentro del mismo temporal/transacción; publicación final única e idempotente. Rechazo y paro directo no generan contrato activo ni asignan presupuesto al entrenador. Volver no escribe nada. No migrar contratos firmados ni partidas existentes automáticamente.

No se implementarán todavía negociaciones, movimientos, partidos, fichajes, renovaciones, calendario ni economía por turnos. No se habilita la firma real en esta etapa.

## Archivos de la etapa 1

- docs/INITIAL_ECONOMY_PROPOSAL.md: diagnóstico, tablas y decisiones para aprobación.
- docs/proposals/initial-economy-2026-27.draft.json: importes en EUR enteros, ClubId estable, datos plan separados de desconocidos/operaciones, estado AwaitingUserApproval y RuntimeUseAllowed=false.
- tools/ValidateEconomicProposal.ps1: controles aritméticos y de estado del borrador, sin escritura SQLite.
- docs/ARCHITECTURE.md, CAREER_CREATION.md, MANAGER_CONTRACTS.md, VALIDATION.md, INVENTORY.md: estado de etapa 1 y enlace al borrador.

No se cambia código funcional, escenas, pruebas existentes, dependencias ni migraciones. Se mantienen los cambios de encargos anteriores. No hay commit ni push.

## Pendiente de aprobación expresa

Aprobar o corregir las veinte filas completas, incluyendo previsiones, hipótesis, sueldos, deuda y tesorería. La aprobación deberá identificar la versión initial-economy-2026-27-draft-v1. Confirmar el tratamiento de la reserva de planificación frente al margen contractual desconocido, y la provisión de coste empleador del 30 %. No se interpretará esta propuesta como aprobación automática de datos de plantilla ni de obligaciones institucionales. La etapa 2 solo empezará tras esa aprobación.

## Validación y comprobación local

Debug y Release: cuatro proyectos compilan con 0 errores y 0 advertencias; 222 pruebas superadas, 0 fallidas y 0 omitidas por configuración. GodotUiSmoke.gd: salida 0 y marcadores FURVO_CAREER_UI_OK/FURVO_UI_SMOKE_OK. Validador del borrador: veinte filas correctas. Nueve bases preexistentes conservan SHA-256, incluidas las cuatro publicaciones. Las verificaciones del JSON revisan propuestas, no datos operativos. No se ha realizado inspección visual manual.

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\ValidateEconomicProposal.ps1
dotnet build Furvo.sln --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

La inspección visual manual a 1280×720, 1600×900 y 1920×1080 no se declara por ejecutar pruebas sin ventana. La app seguirá mostrando desconocidos económicos, por diseño, hasta aprobación e integración.
