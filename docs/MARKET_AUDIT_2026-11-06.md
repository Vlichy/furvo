# FURVO — Auditoría integral de Mercado

Fecha: 06/11/2026. Incluye la ampliación del Bloque XV — Cesiones.

## 1. Dictamen ejecutivo

**No preparado: existen bloqueos importantes.**

Mercado es una base operativa de consulta y construcción/persistencia de propuestas entre clubes. No es todavía un sistema completo de contratación. El buscador, las fichas públicas, los borradores, los pagos propuestos, el selector de intercambios, las cláusulas informativas y el historial tienen implementación real. La aceptación entre clubes solo alcanza PendingPlayers mediante un proveedor de respuesta suministrado; no existe proveedor negociador de producción, acuerdo personal ni cierre del fichaje.

**Las cesiones de futbolistas entre clubes no están implementadas.** No hay oferta de cesión, acuerdo temporal, reparto salarial, opción/obligación de compra, regreso ni formalización. La movilidad temporal entre equipos del mismo club no constituye una cesión.

Antes de conectar agentes deben corregirse los reintentos de respuesta y la semántica temporal de propuestas/cláusulas. También hay que acordar el modelo de contrato personal y la frontera de autorización económica y formalización, incluyendo desde el diseño la cesión. No es necesario tener terminada toda la IA o todos los derechos económicos para una primera negociación manual; sí evitar que una aceptación personal se trate como permiso para un traspaso sin esas comprobaciones.

No se ha observado corrupción de partidas, movimiento indebido ni descuento económico en los circuitos disponibles. Esto no valida un ejecutor de pagos o traspasos: tales ejecutores todavía no existen en Mercado.

## 2. Alcance, versión y método

- Directorio: C:/Users/panio/Documents/Getion Furvo.
- Rama local main; HEAD 3354ea68531e9f7c760016ace2159fb410b7676c, más numerosos cambios locales previos modificados y sin seguimiento. El dictamen corresponde al árbol de trabajo, no únicamente a ese commit ni a una supuesta copia remota.
- Tecnologías comprobadas: C#/.NET 10, Godot 4.7.2 .NET, SQLite y xUnit.
- No se han editado código de producto, pruebas existentes, migraciones ni configuraciones. No se han corregido incidencias, creado agentes, implementado cesiones, realizado commits o push.
- Solo se crea este informe y evidencia/arneses temporales bajo artifacts/market-audit. Las sondas reutilizan por reflexión fixtures ya existentes; no se agregan casos a la suite oficial. Todas las escrituras de prueba usan carreras aisladas o el directorio temporal de las fixtures, nunca carreras personales.
- SHA-256 verificado para los 277 archivos de fuente/configuración registrados al comenzar: ninguno cambió. Las cinco publicaciones iniciales conservan sus huellas previas. Evidencia: artifacts/market-audit/source-baseline.json e integrity-results.json.

### Evidencia utilizada

| Código | Evidencia |
|---|---|
| T | Suite existente: 837 pruebas Debug y 837 Release, sin modificarla. Incluye SQLite, migraciones, contratos anteriores, aislamiento, datos públicos, estados e integridad. |
| V1 | GodotUiSmoke.gd con --market-only: buscador, filtros, orden, identidad, ficha y regreso en tres resoluciones. Log godot-search.log. |
| V2 | --market-columns-only: 51 columnas, valores tecleados, orden numérico, preferencias por posición y regreso. Log godot-columns.log. |
| V3 | --market-exclusion-only: primer equipo/filiales/cantera propios excluidos, externos presentes y acceso propio conservado. Log godot-exclusion.log. |
| V4 | Arnés temporal MarketAudit.gd, heredando las pruebas existentes sin editarlas: oferta completa, rescisión, selector, fechas, tablas, historial, Derechos económicos y casos de navegación. Log godot-full.log y ui-observations.json. |
| P | Sondas temporales de APIs/Core sobre fixtures: reintentos, calendario al día siguiente, objetivos, redondeo y derechos. Program.cs y probe-results.json en artifacts/market-audit. |
| S | Inspección del código, modelos, SQL y documentación existentes. Las conclusiones estáticas se identifican; no equivalen a ejecución de una función ausente. |

Los cuatro recorridos Godot terminaron con código 0. Se abrió la aplicación real y se inspeccionaron capturas, no solo escenas o código. Resoluciones: 1280×720, 1600×900 y 1920×1080. El escenario actual contiene 1894 futbolistas tras inicializar cantera; para Mosaico, 108 propios excluidos y 1786 externos. Estas cifras pertenecen a esa fixture, no se presuponen para todas las carreras antiguas.

## 3. Matriz de funcionalidades — Bloques I a XI

Completa significa completa dentro del alcance concreto de la fila. Persistir una propuesta no convierte su ejecución económica/deportiva en una función completa. Prioridades: Alta, Media, Baja; — indica sin corrección detectada para ese alcance.

| Funcionalidad | Estado | Evidencia | Problemas | Prioridad |
|---|---|---|---|---|
| Nombre, apellidos, nombre completo y fragmentos | Completa | T, V1/V2; MarketQueries.NormalizeName/Search | Normaliza tildes, mayúsculas y espacios. El buscador externo aplica con Buscar/Enter; no se confunde con el selector instantáneo. | — |
| Edad, club y posición principal; filtros combinados | Completa | T, V1 | La posición es principal, como indica la etiqueta; no hay filtro por secundarias. | — |
| Filtro por valor de mercado | No implementada | S: MarketFilter y Main.Market | No hay valor estimado ni servicio de valoración monetaria. | Media |
| Filtro por atributo público y rango 1–20 | Completa | T, V1/V2 | No acepta atributo oculto ni rango inválido. No hay constructor de múltiples filtros de atributo simultáneos. | — |
| Recuento y orden por nombre/edad/club/posición/contrato/atributo | Completa | T, V1/V2 | Se conservan identidad y selección por ID en el buscador. | — |
| Exclusión de todo el club dirigido | Completa | T, V3; SqliteMarketQueries y MarketQueries | Se excluye antes de la proyección; no existe interruptor de incluir propios. | — |
| Cambiar de carrera y consultar otro club dirigido | Completa | T; MarketExclusionTests | Carreras independientes con distintos empleadores verificadas. | — |
| Cambio efectivo de empleo dentro de la misma carrera | No verificable | S, T | El empleo inicial está protegido/inmutable; solo se prueban cambios de contexto sintéticos y otras carreras. No hay un cambio de empleo operativo que recorrer. | Media |
| 51 atributos públicos y protección de 10 ocultos | Completa | T, V1/V2/V4 | Los DTO no entregan Player/PlayerAttributes internos. No se observa valoración global numérica en Mercado. | — |
| Columnas individuales, preferencias por posición y persistencia | Completa | T, V2 | Persistencia de usuario separada de partidas; protección de códigos públicos. | — |
| Ficha: identidad, edad, nacionalidad, posiciones y club | Completa | T, V1/V4 | Reutiliza PlayerSheet; conserva los atributos y navegación. | — |
| Valor económico estimado en ficha y selector | No implementada | S; MarketContract/MarketPlayer | Se muestra ausencia de dato, no una cifra inventada. | Media |
| Fechas contractuales disponibles | Completa | T, V1/V4 | Puede mostrarse el último contrato como histórico, con estado; no se inventa contrato para cantera sin registro. | — |
| Rescisión: conocida/ausente/desconocida/no vigente | Completa | T, V4 | Completa como consulta informativa, con control de conocimiento y condiciones pendientes. Los contratos antiguos continúan desconocidos. | — |
| Ejecución de rescisión | Bloqueada | S: puerto IReleaseProcedureChecks | No hay procedimiento económico, consentimiento, jurisdicción ni inscripción; no hay botón funcional. | Alta antes de habilitar ejecución |
| Buscar → ficha → Hacer oferta; identificación de partes | Completa | T, V4 | Revalida carrera y pertenencia; no se ofrece por un propio desde el buscador externo. | — |
| Borrador válido: crear/editar/guardar/recuperar | Completa | T, V4 | No guarda formularios arbitrariamente incompletos: exige términos coherentes. Reintentos con identidad cambiada tienen la incidencia I03. | Media |
| Enviar/retirar/cancelar propuestas | Completa | T, V4 | Son cambios de estado e historial, sin ejecutar operaciones. Enviar guarda primero una revisión de borrador; cancelar la confirmación conserva ese borrador. | — |
| Dinero: negativos, caracteres, desbordamiento, decimales y máximo | Completa | T, V4 | Límite técnico individual predeterminado de 1.000.000.000; no es presupuesto del club. | — |
| Formato monetario español | Completa | T, V4 | Se formatea al perder foco y en resúmenes; el campo en edición admite importe sin separadores de miles. | — |
| Liquidez y presupuesto para la oferta | Parcial | S, T: TransferFinancialReview | Comparación informativa de anticipo/tesorería e importe fijo/presupuesto conocido. No reserva fondos ni comprueba toda la solvencia. | Alta para cierre futuro |
| Compromisos futuros y exposición conjunta a variables | No implementada | S | No hay libro de obligaciones ejecutables ni suma comprometida por todas las negociaciones. | Alta para cierre futuro |
| Pago único, 2/3/4 pagos y plan editable | Completa | T, V4 | Meses/porcentajes editables, último residuo automático, 36 meses ordinarios. Fechas propuestas, no pagos realizados. | — |
| Redondeo, suma exacta y ausencia de cuotas negativas | Completa | T, P, V4 | La suma es exacta. El importe mínimo de 0,01 genera el caso I05; no se impone precio mínimo universal. | Media |
| Calendarios antiguos y excepcionales | Parcial | T, S | Se conservan; el formato antiguo admite calendarios largos. No es una nueva plantilla estándar ni una política de aceptación financiera excepcional. | Media |
| Vigencia de propuestas al avanzar el día | Parcial | P: live-draft-next-day/live-clause-next-day | Validaciones atadas a la fecha de creación impiden continuar propuestas no caducadas. I01. El reloj de juego aún no avanza. | Alta |
| Selector visual, búsqueda instantánea y filtros deportivos | Completa | T, V4 | Primer equipo/filial/juvenil se obtienen de la carrera; no se deducen por nombre. | — |
| Selección múltiple, contador, confirmar/cancelar y retirar tarjetas | Completa | T, V4 | La selección provisional es independiente de la confirmada y conserva IDs al filtrar/reabrir. | — |
| Pertenencia, duplicados y revalidación al guardar/enviar | Completa | T; ParticipantsValid | Comprueba club actual de cada ofrecido; rechazo si ha cambiado y retirada aún disponible. No ejecuta intercambios. | — |
| Elegibilidad competitiva completa de jugadores ofrecidos | Bloqueada | S, V4 | Sin registro completo de ventanas/licencias/cupos. El formulario advierte la validación pendiente. Pertenencia no equivale a elegibilidad. | Alta para formalizar |
| Interés real del vendedor y orden prioritario | Parcial | T, S: RecordedTransferInterest/Candidates | Funciona con registros suministrados en pruebas; no hay registro persistente/productor operativo. En UI no se inventan etiquetas. | Media |
| Constructor y persistencia de variables | Parcial | T, V4 | Datos estructurados reales; compatibilidad semántica objetivo/competición incompleta. I07. | Media |
| Evaluación y cobro de variables | Bloqueada | S | Ningún objetivo se evalúa realmente sin estadísticas/motor competitivo. | Alta antes de pagos |
| Futura venta 10–100 y plusvalía separada | Completa | T, V4; TransferOfferPresets | Completa como cláusula propuesta y guardada, con definiciones estándar versionadas. No implica derecho activado. | — |
| Recompra y tanteo: selección, validación, guardar/recuperar, resumen | Completa | T, V4 | Alcance de configuración de la propuesta; inactivos se omiten. Anteriores se conservan. I01 afecta usos en otro día. | — |
| Ejercicio efectivo de recompra/tanteo | Bloqueada | S | No hay ejecutor, aceptación personal, inscripción o pago. | Alta para ejecución futura |
| Listado Derechos económicos | Solo interfaz | V4, S: ShowTransferRights | Es una explicación/estado no operativo, no un gestor ni una consulta de un libro activo. | Media |
| Derechos activos, varios beneficiarios y modificaciones | Parcial | T, P, S | Registros y reducción puros en Core; faltan almacenamiento activo, historial económico y aplicación transaccional. I06. | Alta antes de operar derechos |
| Estados e historial inmutable por carrera | Completa | T, V4; SQL Transfer001 | Borrador, enviada, pendiente, contraoferta, aceptada entre clubes, rechazada, retirada, caducada, cancelada y pendiente del jugador están modelados. No todos tienen una vía de producción/UI. | — |
| Contraofertas/respuestas de clubes en producción | Parcial | T, P, S | Puerto y persistencia funcionan con fixtures; no hay fuente real/IA/UI de vendedor. Reintentos de Respond: I02. | Alta |
| Caducidad automática | Bloqueada | S: Expire y CanAdvanceTime=false | Existe validación/acción explícita de servicio; no hay calendario que la dispare ni reloj avanzable. | Alta para negociación temporal |
| Tablas de ofertas/negociaciones y selección de detalle | Completa | T, V4 | Columnas compartidas, proporciones, importes derecha y versiones centradas; 1/8/150 filas visuales comprobadas. Estado vacío: I04. | Media |
| Guardar cambios al navegar fuera del formulario | Parcial | V4, S | El botón Volver tiene guardia; la navegación principal/submenú descarta el formulario sin aviso. I08. | Media |
| Lo antes posible/próxima ventana/fecha futura y validez | Parcial | T, V4 | Selección y persistencia operativas desde fecha de carrera. La ventana no tiene fechas reglamentarias verificadas. | Alta para incorporación real |
| Acuerdo entre clubes → acuerdo personal → cierre | Bloqueada | T, S | La aceptación genera AcceptedBetweenClubs y después PendingPlayers; no hay continuación operativa. | Alta |
| No pagos/movimientos/generación de jugadores al proponer | Completa | T, V4 | Huellas de negocio conservadas; no se han ejecutado traspasos en esta auditoría. | — |
| Evitar pagos duplicados de una operación finalizada | No verificable | S | No hay ejecutor de pagos ni finalización de fichajes. No se confunde ausencia de pagos con garantía de idempotencia de un futuro ejecutor. | Alta |
| Compatibilidad con formatos antiguos admitidos | Parcial | T, S | Fixtures de esquemas y contratos anteriores pasan; las lecturas no migran implícitamente. No se han inspeccionado carreras personales ni todo archivo histórico posible. | Media |

## 4. Variables: alcance real

| Objetivo | Propuesta estructurada actual | Evaluación/pago actual |
|---|---|---|
| Partidos oficiales, titularidades, goles y asistencias | Admitidos con umbral, importe, intervalo, máximo de activaciones, acumulación y competición | Bloqueada: no hay contador deportivo conectado |
| Porterías a cero | Admitidas solo para objetivo con posición principal de portero | Bloqueada: no se evalúa ningún partido |
| Internacionalidades | Enum y control deshabilitado; Persistence rechaza | No implementada |
| Ascenso y permanencia | Admitidos contra un ID de competición existente | Bloqueada; el ascenso en Liga Cumbre se acepta aunque no hay estructura que lo fundamente, I07 |
| Clasificación europea | Control deshabilitado y rechazo en Persistence | No implementada |
| Título de liga | Admitido con referencia de competición | Bloqueada: no hay temporada competitiva resuelta |
| Título de copa o internacional | Deshabilitados y rechazados por falta de referencia fiable | No implementada |

El constructor guarda condiciones, no calcula cumplimiento. El máximo económico de variables es una proyección y no una deuda ya activada. No hay estadísticas ficticias.

Las bases estándar son gross-fixed-cash-v1 (importe fijo monetario bruto de la próxima transferencia completada, sin variables/valoraciones de intercambios) y positive-fixed-cash-surplus-v1 (máximo de cero y ese importe menos el fijo de adquisición congelado). No se suman porcentajes de bases distintas. Las fórmulas libres anteriores permanecen legibles, pero no existe un evaluador semántico que demuestre qué obligaciones económicas equivalentes representan; hace falta resolverlo antes de ejecutar derechos.

## 5. Incidencias verificadas y clasificación

No se ha observado una incidencia Crítica en el alcance ejecutable. Las ausencias de agentes, cesiones o ejecución son carencias de alcance/dependencias, no fallos de una función que ya estuviera implementada.

### I01 — Alta: propuestas y cláusulas no sobreviven coherentemente al cambio de día

Evidencia P, sonda live-draft-next-day: plan de 3 pagos creado el 01/07/2026, caducidad 15/07/2026. Valida ese día; el 02/07/2026, sin haber caducado, falla con Plazos o vencimientos inválidos. El primer vencimiento quedó anclado al día de elaboración y PaymentPlan.ReferenceDate exige igualdad con la fecha actual para incorporación inmediata.

También una propuesta antigua con cuotas todas futuras deja de validar al día siguiente cuando incluye recompra o futura venta iniciadas el día anterior: Recompra incompleta / Base, activación o vigencia de futura venta incompletas. TransferTerms.Validate exige From >= fecha actual, aunque el periodo siga vigente.

Fuentes: src/Furvo.Core/Market/TransferNegotiation.cs, líneas 28–45; TransferPaymentPlans.ValidateTerms. Es un bloqueo latente reproducido con fechas explícitas de Core, no mediante modificación del reloj de una carrera personal. El juego actual no permite avanzar el calendario. Antes de respuestas temporizadas se deben separar fecha de propuesta, envío, formalización, vigencia contractual y vencimientos, sin reescribir silenciosamente lo acordado.

### I02 — Alta para integración asíncrona: Respond no es idempotente al reintentar una respuesta ya registrada

Sonda response-idempotency: una aceptación de fixture pasa de revisión 2 a PendingPlayers, revisión 4. Repetir Respond con la misma revisión original y operation_id devuelve Revisión desactualizada antes de llegar a la comprobación del recibo de operación. Hay 4 versiones, no duplicadas; el proveedor se invoca una sola vez.

Fuente: src/Furvo.Persistence/SqliteTransferStore.cs, Respond, líneas 96–103, frente al recibo existente en Write, líneas 58–64. No se observa corrupción ni doble pago: el error es que un reintento legítimo se comunica como fallo pese a haberse registrado la aceptación. Se necesita un protocolo de reintentos consistente antes de agentes/eventos/colas.

### I03 — Media: reutilizar una operación de borrador con otro futbolista no se rechaza

Sonda draft-retry-other-target: Draft(id, 0, jugador A, términos, operación X); después Draft con el mismo id/términos/X pero jugador B propio. Devuelve la propuesta original de A y mantiene una versión, sin rechazar que el objetivo solicitado cambió. No se crea una oferta por B ni se mueve a nadie.

Fuente: SqliteTransferStore.cs, líneas 58–64; la devolución por operación repetida no compara target. El control de cambio de objetivo de la línea 74 queda después de ese retorno. Debe verificarse toda la identidad de la orden antes de usar los recibos en la futura negociación personal.

### I04 — Media: filtro vacío con acción habilitada y sin mensaje

En Mis ofertas, seleccionar la negociación existente y filtrar Borrador cuando solo hay una retirada deja 0 filas, Consultar negociación habilitado, ningún ID seleccionado y ningún mensaje de resultados vacíos. El clic es inocuo por la guarda de SelectedId, pero no hace nada.

Reproducido en Godot y ui-observations.json. Fuente: Main.Transfers.cs, Render y botones, líneas 66–68. La selección se borra al reconstruir Tree y el botón no vuelve a deshabilitarse; el mensaje vacío depende de all.Count y no del resultado filtrado.

### I05 — Media, caso límite: anticipo nominal positivo con anticipo monetario cero

Sonda rounding-tiny: 0,01 EUR con plantilla de 4 pagos, anticipo nominal 40 %. Se obtiene un único pago de 0,01 EUR en 2029 y anticipo efectivo 0. La suma es exacta y la UI sí muestra el anticipo efectivo; no hay dinero perdido ni ocultación de un pago realizado.

Fuente: TransferPaymentPlans.Generate. Falta una política explícita para un reparto que no pueda expresarse en céntimos sin cuotas cero. Recomendar pago único/menos cuotas con confirmación sería una alternativa a aprobar; no se propone imponer un precio mínimo universal ni se cambia el algoritmo durante esta auditoría.

### I06 — Media, módulo preparatorio: moneda insuficientemente validada al reducir derechos

ContractualTransferRight.Reduce acepta Currency=123 porque solo comprueba longitud 3. La sonda devuelve reducción 50→20. Es una función pura sin persistencia ni pagos, no una vía económica activa.

Fuente: TransferNegotiation.cs, línea 92. Corregir y cubrir antes de habilitar adquisición/extinción real de derechos. No exige habilitar todo ese subsistema antes de una primera negociación personal manual.

### I07 — Media: objetivo y ámbito competitivo no se validan semánticamente

La sonda bonus-scope-semantics guarda una variable Promotion sobre Liga Cumbre de España. Se comprueba que el ID de competición existe, pero no que el objetivo sea aplicable a ella. En el universo actual no hay un sistema que permita/evalúe ese ascenso.

Fuente: SqliteTransferStore.ParticipantsValid, líneas 41–44, y TransferTerms.Validate. No se paga ni se simula cumplimiento. Debe impedirse interpretar existencia de metadatos como verificación reglamentaria antes de evaluar obligaciones.

### I08 — Media: navegación principal descarta cambios no guardados sin aviso

Se abre Hacer oferta, se escribe 1234,56 y se pulsa Negociaciones. El formulario desaparece, no aparece TransferDiscard y no se conserva ese borrador en memoria. Reproducido en ui-observations.json. No se pierde una propuesta previamente guardada: se pierde trabajo sin guardar.

Fuente: Main.Transfers.cs, guardia Back del formulario, frente a NavigateCareer. La advertencia solo cubre el botón de retorno del propio formulario. Extender la protección a cambios de área/submenú debe abordarse como corrección posterior.

### Observaciones de prioridad Baja

- Desde el detalle abierto en Negociaciones, Volver a la lista vuelve a Mis ofertas: no conserva el origen de navegación (Main.Transfers.cs, línea 97).
- A 1280×720 el buscador deja visibles pocas filas con nombres partidos; funciona el desplazamiento, pero conviene compactar filtros/cabecera. No se detectó pérdida de columnas: el desplazamiento horizontal es intencional.
- El resumen largo de la oferta presenta separación vertical generosa entre líneas en Windows; no afecta al registro pero aumenta el desplazamiento.
- La etiqueta externos disponibles significa resultados consultables, no elegibilidad contractual/reglamentaria verificada. Sería más precisa esa distinción.
- Derechos económicos tiene un área vacía amplia porque es una pantalla de estado, no un gestor operativo. No se contabiliza como fallo de un gestor inexistente.

## 6. Derechos económicos: casos A, B y C

| Caso | Representación actual | Persistencia/ejecución actual |
|---|---|---|
| A: venta con 50 % de futura venta | FutureFeeClause al 50 % se configura y guarda en una propuesta; base y beneficiario definidos | Sí, como cláusula propuesta en versiones. No hay venta formalizada ni creación de derecho activo |
| B: reducir 50 % a 20 % mediante compensación | ContractualTransferRight.Reduce y RightAmendmentAgreement permiten 30 puntos de extinción, precio y consentimiento de ambas partes; sonda 50→20→Extinguished | No hay almacén de derechos activos, pago de compensación ni historial económico persistente de la modificación |
| C: dos beneficiarios y bases diferentes | Se pueden construir registros de derechos independientes y distinguir FutureTransferFee/ContractualProfit; existe prueba conceptual | No hay libro agregado de obligaciones ni cálculo/distribución de cobros. Una nueva propuesta solo permite al vendedor como beneficiario; no debe reutilizarse esa lista para representar todos los derechos históricos |

La separación conceptual entre inscripción deportiva y cobros contractuales es correcta. No se representa al futbolista como propiedad económica compartida. Lo pendiente es el ciclo de vida y la ejecución transaccional, no añadir porcentajes arbitrariamente a una pantalla.

## 7. Persistencia, economía y carreras

Transfer001_Negotiations añade cabeceras y versiones JSON por carrera, operación única e historial protegido contra UPDATE/DELETE. CAS de revisión impide sobrescritura por un editor obsoleto. Las cabeceras tienen FK a clubes y las versiones a la negociación; player_id no tiene una FK única a todos los futbolistas porque mundo y cantera siguen en fuentes distintas. La integridad de participantes depende también de ParticipantsValid. Una importación o ejecutor futuro debe preservar esa comprobación y resolver la identidad canónica, no insertar cabeceras directamente.

ContractRelease001_Information es optativa, con FK al contrato, céntimos enteros y triggers de vigencia/referencias. Lecturas antiguas sin extensión siguen desconocidas. No convierte null a Sin cláusula ni altera publicaciones. Los tests cubren rollback, ausencia de escrituras en consultas y conservación de datos históricos admitidos.

Los recursos económicos existentes incluyen tesorería y presupuestos aprobados, previsiones y compromiso del entrenador. **Los salarios individuales de futbolistas no están implementados**: el gasto salarial previsto no es una suma de contratos personales. No hay libro de reservas/obligaciones por fichaje, comisiones, primas o pagos futuros activados.

Enviar y aceptar entre clubes no descuenta dinero ni cambia contratos/plantillas. La aceptación persiste dos hitos, AcceptedBetweenClubs y PendingPlayers, dentro de la misma transacción. No hay paso final que pueda verificarse como fichaje cerrado. No hay pruebas de ausencia de pagos duplicados de un ejecutor inexistente.

Riesgos principales: semántica temporal; protocolo de reintentos; condiciones libres históricas sin evaluador; referencias de futbolistas repartidas entre fuentes; ausencia de liquidación/reservas y de una transacción de cierre. No se han abierto ni modificado partidas personales. Compatibilidad demostrada en fixtures y copias temporales, no certificación universal de todos los archivos de usuarios.

## 8. Bloque XV — Cesiones de futbolistas (apartados 23–32)

### Estado y evidencia

Búsqueda en modelos, SQL, servicios y recursos: no hay LoanAgreement, propuesta de cesión, tipo de operación de cesión en TransferTerms, tablas de cesiones ni flujo Traspaso definitivo/Cesión. Los recursos MarketReadOnly y AcademyDevelopmentNotice indican que las cesiones están pendientes.

Sí existe MobilityKind.Temporary en AcademyMobility y una asignación deportiva por jugador. Su ámbito es intraclub. SqliteAcademyMobilityStore opera sobre Teams del club dirigido y mantiene club_id al cambiar team_id; SqliteMarketQueries.cs, línea 27, rechaza una asignación cuyo club difiera de la pertenencia contractual conocida. No es una cesión parcialmente funcional entre clubes ni permite suplirla cambiando solo el equipo.

| Funcionalidad de cesión | Estado | Evidencia | Problemas | Prioridad |
|---|---|---|---|---|
| Cesión simple sin opción | No implementada | S: modelos/SQL/UI | Falta acuerdo temporal entre clubes y consentimiento del jugador | Alta para alcance de cesiones |
| Cesión con opción de compra voluntaria | No implementada | S | No hay precio/periodo/estado de ejercicio ligados a un préstamo; la recompra no es esta opción | Alta al incorporar modalidad |
| Cesión con obligación de compra | No implementada | S | No hay obligación, disparador, validación del cumplimiento ni formalización | Alta; bloqueada por eventos/reglas |
| Cesión gratuita | No implementada | S | Oferta definitiva de importe cero no es cesión. Sin tasa de cesión no significa sin coste salarial | Alta al incorporar modalidad |
| Cesión con contraprestación económica | No implementada | S | Falta cuota/tasa de cesión y vínculo con responsabilidades salariales | Alta |
| Recuperación anticipada pactada | No implementada | S | Sin cláusula, ventana de recuperación, consentimiento ni autorización reglamentaria | Media; requiere reglas verificadas |
| Inicio, final y vencimiento del acuerdo | No implementada | S | Expires actual caduca una oferta, no una cesión | Alta |
| Fin de temporada / una temporada / varias temporadas | No implementada | S | No hay validador de duración de cesiones; no se presupone permiso para varias temporadas | Alta |
| Incorporación en ventana válida | Bloqueada | S: ArrivalKind.NextWindow | Solo intención genérica; sin ventanas e inscripción operativas | Alta |
| Pago único/aplazado de tasa de cesión | Parcial | S, T: componentes de pagos existentes | Hay generador reutilizable, pero no aplicación ni registro de pago de cesión | Alta para integración |
| Reparto salarial en porcentajes/importes exactos | No implementada | S: Contract sin salario | No puede verificarse suma salarial 100 % ni doble cobro de una cesión | Alta |
| Primas, variables, penalizaciones y compensaciones de cesión | No implementada | S | Un TransferBonus genérico no define estas obligaciones ni su acreedor/deudor | Media/Alta antes de ejecutarlas |
| Minutos/partidos mínimos y rol deportivo pactado | No implementada | S | No hay compromisos de cesión ni contador de cumplimiento. Promesa no puede equivaler a titularidad garantizada | Media |
| Restricción frente al club de origen | No implementada | S | No hay regla contractual/reglamentaria verificada para aplicarla | Media; no asumir validez |
| Contrato de origen conservado e inscripción temporal en receptor | Bloqueada | S: vínculo contractual y asignaciones intraclub | Hace falta separar empleador contractual, destino deportivo y elegibilidad temporal | Alta |
| Regreso, rescisión del préstamo y coherencia tras vencimiento | No implementada | S | Sin operación transaccional ni política para contrato original caducado | Alta |
| No duplicados / historial íntegro al ceder y regresar | No verificable | S | Identificadores estables son una base útil, pero no existe el ciclo operativo para probarlo | Alta |
| Cupos, límites y elegibilidad de cesiones | Bloqueada | S | No hay sistema completo de inscripción/calendario ni reglas verificadas de préstamo | Alta |
| Selector Traspaso definitivo / Cesión | No implementada | V4, S | Hacer oferta solo compone negociación actual; no hay formulario específico | Alta para UI de cesiones |
| Cesión en Mis ofertas, Negociaciones, ficha e historial | No implementada | V4, S | No hay discriminador persistido ni estados específicos; no confundir estancia en filial con cedido | Alta |
| Representante, consentimiento y contrato del cedido | Bloqueada | S | No hay negociación personal ni representación; un acuerdo entre clubes no equivale a aceptar una cesión | Alta |

La fila Parcial de pagos solo reconoce infraestructura reutilizable: **no significa que haya pagos de cesión implementados**. Las filas No verificable no se dan por correctas por ausencia de ejecución. No se ha registrado ni ejecutado ninguna cesión de prueba o personal; se ha inspeccionado que el flujo no existe.

### Riesgos y dependencias específicos

1. Reutilizar el club del contrato como único club deportivo haría desaparecer al cedido del receptor o rompería la lectura de asignaciones. Se requiere una relación temporal explícita, conservando un PlayerId y el contrato de origen.
2. El préstamo, la opción de compra y la obligación deben ser acuerdos distintos enlazados; Buyback es un derecho del vendedor tras venta y no una opción del receptor de una cesión.
3. El reparto salarial necesita salario contractual real conocido, moneda y obligaciones por club; los porcentajes deben sumar 100 % y no duplicar el devengo/cobro. Actualmente no hay campos ni reglas para comprobarlo.
4. Inicio/fin/retorno requieren calendario de carrera, ventanas y reglas de inscripción verificadas. La fecha del ordenador no puede sustituirlos. El contrato original puede vencer antes o durante la cesión: hay que definir esa coherencia antes de formalizar.
5. Las obligaciones de compra y compensaciones por participación necesitan eventos verificables e historial. No deben ejecutarse por existir una condición guardada.
6. Acuerdo entre clubes, consentimiento del jugador, mandato del representante, condiciones personales, honorarios y formalización/inscripción son hitos separados.

**Recomendación:** definir el modelo común de cesiones antes de cerrar la arquitectura de agentes y desarrollar la cesión simple junto con la primera fase de contratos/consentimiento personal. No hace falta terminar todas las modalidades antes de modelar un representante, pero tampoco crear un sistema personal rígido que solo admita adquisición definitiva. Opción/obligación de compra, recuperación y cláusulas complejas pueden escalonarse después, manteniendo bloqueada su ejecución mientras falten reglas y evaluadores. No se propone sustituir los contratos actuales ni modificar las asignaciones existentes durante esta auditoría.

## 9. Preparación para agentes — dependencias reales

| Área | Existe | Falta para una negociación operativa | Imprescindible inicialmente |
|---|---|---|---|
| Representantes | Nada específico | AgentId, identidad ficticia, mandato agente–jugador y vigencia; ausencia confirmada frente a representante desconocido | Sí para representación; aceptar jugador sin agente confirmado |
| Contrato personal | ContractId, jugador, club, fechas y rescisión opcional | Salario/moneda, prima, comisión, condiciones, rol prometido y aceptación | Sí para firmar; valores y reglas deben aprobarse |
| Propuestas personales | Nada específico | Agregado propio con revisiones, actores, plazos, contraofertas y recibos idempotentes | Sí |
| Handoff desde clubes | PendingPlayers y referencias a las partes | Vínculo estable acuerdo entre clubes ↔ negociación personal, invalidación y revalidación de condiciones | Sí |
| Economía | Recursos y previsiones aprobadas; coste del entrenador | Compromisos reales de futbolistas, comisiones, primas, tasas, reservas y obligaciones futuras | Sí antes de habilitar firma/finalización |
| Calendario y elegibilidad | Fecha explícita de carrera y puertos parciales | Avance/eventos, ventanas e inscripción; resolver I01 | Sí para tiempos y formalización; no simular que están disponibles |
| Cesiones | Movilidad intraclub y pagos genéricos reutilizables | Acuerdo de préstamo, doble relación contractual/deportiva, salarios compartidos, consentimiento y regreso | Diseño común desde el inicio; primera modalidad junto a contratos |
| Director deportivo | Personal de entrenamiento/análisis/cantera con roles | Figura/contratación/mandato de mercado, delegación, límites de autorización y devolución del control | No para negociación manual por el usuario; sí antes de delegación |
| IA | Reputación de club, atributos y algunas magnitudes económicas | Preferencias aprobadas, pretensiones económicas fundamentadas, memoria/relaciones y reglas de decisión | No es necesaria IA avanzada para validar manualmente el protocolo; no inventar valores |
| Mercado monetario/scouting | DTO públicos y control conservador de rescisión | Valoración monetaria y conocimiento granular | Posponibles; mantener desconocidos y no filtrar datos internos |
| Derechos activos | Modelos puros y cláusulas propuestas | Libro de obligaciones y liquidación de operaciones | Posponible si la primera versión no ejecuta esos derechos |

Los contratos y salarios del entrenador ya implementados no son contratos personales de futbolistas ni una política salarial que pueda copiarse sin aprobación. La presencia de ambición/profesionalidad u otros atributos internos no constituye un modelo aprobado de preferencias de negociación, y no autoriza mostrarlos.

## 10. Arquitectura recomendada — propuesta, sin implementación

Mantener Core/Persistence/Godot. Extender de forma modular, no reconstruir Mercado:

1. Un contexto de operación con tipo definitivo/cesión y referencias estables a carrera, jugador, contrato de origen, clubes y acuerdo entre clubes. Separar el empleador contractual del destino deportivo.
2. Un agregado de negociación personal separado de TransferTerms: oferta, contraoferta, aceptación, rechazo, caducidad, emisor, receptor y representante con mandato válido. Los estados de ambos acuerdos se relacionan, no se confunden.
3. Un servicio de condiciones económicas/autoridad con datos aprobados y estados pendientes explícitos. No derivar salario ficticio del valor de mercado o de una valoración global no aprobada. Contratar director deportivo y delegar serían capacidades posteriores con límites por actor.
4. Un coordinador de formalización que solo actúe con ambos acuerdos válidos, fondos/compromisos, normativa, consentimiento e inscripción verificados. Una transacción de partida, control de revisión y claves de operación idempotentes para impedir duplicidades. Ninguna aceptación aislada debe mover jugadores.
5. Persistencia aditiva y versionada, conservando propuestas previas e historial. Proyecciones públicas específicas; condiciones confidenciales permanecen en Core/Persistence.
6. Cesión con identidad propia, fechas, obligaciones salariales y vínculo al contrato original; opción y obligación de compra como relaciones diferenciadas. Eventos de retorno y cumplimiento solo con calendario/estadísticas reales.
7. Adaptadores de IA/representantes sobre esos servicios, sin SQL directo y con decisiones reproducibles/auditables. Permitir inicialmente decisiones manuales o fixtures de integración, sin presentarlas como IA de producción.

Esta es una propuesta técnica a aprobar, no código añadido durante la auditoría.

## 11. Validación técnica y visual ejecutada

| Comprobación | Resultado |
|---|---|
| dotnet build Furvo.sln -c Debug --no-restore | Correcta, 0 errores, 0 advertencias |
| dotnet build Furvo.sln -c Release --no-restore | Correcta, 0 errores, 0 advertencias |
| dotnet test Furvo.sln -c Debug --no-build --no-restore | 837 superadas, 0 fallidas, 0 omitidas; 3 m 32 s |
| dotnet test Furvo.sln -c Release --no-build --no-restore | 837 superadas, 0 fallidas, 0 omitidas; 3 m 20 s |
| Godot buscador, columnas, exclusión y flujo extendido | Cuatro recorridos terminados correctamente; no se modificaron las pruebas existentes |
| Sondas de auditoría | Se reproducen I01/I02/I03/I05/I06/I07; no son fallos añadidos a xUnit ni correcciones de la suite |
| Observación de tabla/navegación | I04/I08 reproducidas en Godot con JSON de observaciones |
| Integridad de código/configuración registrada | 277 archivos sin cambios |
| Publicaciones iniciales | Cinco SHA-256 sin cambios |

Hay cero pruebas oficiales fallidas. Que pasen 837 pruebas no elimina los problemas reproducidos por sondas fuera de su cobertura. Los errores iniciales al compilar el arnés temporal se corrigieron únicamente dentro de artifacts; no fueron fallos de la solución ni se modificó una prueba del proyecto para conseguir verde.

Se revisaron capturas reales de Buscar jugadores, ficha, Hacer oferta, selector, recompra/tanteo/incorporación, Mis ofertas, Negociaciones, historial y Derechos económicos. Los ensayos de tablas de 150 elementos son fixtures visuales, no una prueba de carga de 150 negociaciones persistidas. No se certifica rendimiento a decenas de miles de jugadores, un ciclo económico completo ni compatibilidad de partidas personales no inspeccionadas.

Comandos y evidencias principales:
- artifacts/market-audit/test-results/market-audit-debug.trx y market-audit-release.trx.
- artifacts/market-audit/godot-full.log, godot-search.log, godot-columns.log y godot-exclusion.log.
- artifacts/market-audit/Probe/Program.cs, probe-results.json y ui-observations.json.
- artifacts/market-audit/source-baseline.json e integrity-results.json.
- artifacts/career-visual/career-audit-*.png y las capturas de ofertas/contratos/selector producidas por el ensayo existente.

Las capturas de rescisión con importes conocidos son fixtures sintéticas del sistema de QA existente. No se incorporaron esos importes a los contratos publicados ni a carreras personales.

## 12. Orden recomendado y decisión de cierre

### Obligatorio antes de conectar respuestas de agentes a acuerdos reales

1. Resolver I01: semántica temporal, propuesta/envío/formalización, vigencia de cláusulas y validación a lo largo de varios días. Añadir cobertura multifecha cuando se autorice corregir.
2. Resolver I02/I03: reintentos idempotentes con identidad y contenido completos, recibos coherentes y protección CAS. Probar aceptación, rechazo y contraoferta repetidas.
3. Aprobar el modelo personal y de representación, los datos/reglas salariales y el contexto de operación definitivo/cesión. No posponer la separación contractual/deportiva hasta después de implementar agentes.
4. Definir handoff, autoridad y condiciones económicas de finalización. Mientras no exista coordinador transaccional con inscripción/consentimiento y obligaciones verificadas, mantener bloqueada cualquier firma que complete el fichaje.

### Correcciones posteriores priorizadas

- I04/I08: vacíos/selección y protección de borradores al navegar.
- I07: coherencia objetivo–competición antes de activar evaluación de variables.
- I05: política explícita de repartos irrepresentables en céntimos, sin precio mínimo universal.
- I06 y libro de derechos antes de compra/extinción/liquidación de derechos activos.
- Mejoras visuales de prioridad Baja y ampliación de pruebas de carga cuando exista volumen real.

**Respuesta final: No preparado: existen bloqueos importantes.** Existe una base útil para diseñar la próxima fase, pero no para tratar acuerdos personales como una continuación segura y operativa del mercado actual. Las cesiones están evaluadas expresamente y siguen pendientes. No se ha iniciado ni implementado ninguna de estas correcciones, agentes o cesiones. La auditoría termina aquí y queda a la espera de instrucciones.

## 13. Archivos producidos en esta auditoría

Único documento de proyecto creado: docs/MARKET_AUDIT_2026-11-06.md. No se modifica README, inventarios anteriores, código, migraciones ni pruebas. Los demás archivos nuevos son evidencia/arneses temporales en artifacts/market-audit y capturas de QA en artifacts/career-visual; no se integran en el producto. No hay commits, push ni cambios de partidas personales.
