# Contraste de auditoría y especificación previa de Economía F1

Revisión de análisis y planificación realizada el 18/11/2026. Rama `main`, HEAD `3c9df44a0eb27873c82dcbd0cc22ba3b368bb62e`. No se inicia implementación. Las referencias de línea corresponden a ese código. Este informe contrasta los hallazgos enumerados en el encargo atribuidos a Claude; no presupone acceso a otro informe independiente completo.

## 1. Estado y método de revisión

Al comenzar, Git mostraba únicamente `docs/STAFF_ONBOARDING_DESIGN.md` y `docs/ECONOMY_FINANCE_DESIGN.md` sin seguimiento. Son trabajo previo y se conservan íntegros. Se consultaron `ARCHITECTURE.md`, `DECISIONS.md`, `V1_CONTRACT.md`, `ECONOMY_FINANCE_AUDIT.md`, ambos diseños pendientes y los servicios, modelos y migraciones citados a continuación.

La evidencia es inspección estática de fuentes, esquemas y pruebas existentes. Los comportamientos futuros se distinguen como riesgos inferidos. No se abrieron bases personales ni se ejecutaron cambios sobre partidas. La cifra de 852 pruebas superadas pertenece a la última ejecución comunicada; este análisis no constituye una ejecución nueva ni una demostración de rollover, insolvencia o automatización.

Las decisiones explícitas de este encargo son continuidad de caja con presupuestos por ejercicio, fuente económica común conciliable, vacantes laborales reales y caja no negativa salvo financiación autorizada. La falta de liquidez debe conservar deuda vencida e impagada. Las consecuencias de insolvencia y las decisiones económicas de clubes IA quedan para sistemas futuros. Las recomendaciones restantes requieren aprobación.

## 2. Verificación de los hallazgos principales

La gravedad describe el impacto dentro del estado actual y el riesgo al habilitar dependencias. Alta indica posibilidad de incompatibilidad o inconsistencia relevante; media, una limitación localizada o una garantía ausente; latente señala un sistema hoy bloqueado o sin flujo comercial completo. No implica que se haya observado corrupción en una partida.

| ID | Evidencia concreta | Veredicto y gravedad real | Consecuencia y recomendación |
|---|---|---|---|
| A1 | `SqliteSportsStore.cs:121–126`, `SqliteEconomyStore.cs:11,24,88–89`, `Economics001_Initial.sql:4–9` | Confirmado. Una escritura de caja de negocio y la apertura inicial, sin diario común. Media hoy, alta para integrar más operaciones. | Compra tecnológica guarda pedido y activo junto al cargo, pero faltan movimiento financiero y conciliación general. Proponer diario común y saldo materializado comprobable. |
| A2 | `SqliteCareerStore.cs:116,124–129`, `CareerWorkspace.cs:22,30`, `Career001_Initial.sql:47` | Confirmado como bloqueo de evolución. Alta antes de habilitar temporadas, latente hoy. | La carga económica exige exactamente 20 filas de la temporada actual y el contrato inicial exige la misma temporada. Separar estado inicial, contrato aceptado, temporada activa y ejercicios financieros. |
| A3 | `SqliteInitialStaffProfession.cs:17,27–33`, `StaffProfile.cs:14–18`, `SqliteStaffDirectory.cs:26,61`, `SqliteSportsStore.cs:19`, `AcademyModels.cs:14` | Confirmado con matices. Alta al cruzar el vencimiento, latente con el reloj bloqueado. | Todos los contratos iniciales efectivamente generados terminan al fin de temporada; algunas carreras pueden carecer de ellos. Caducidad y vinculación institucional no resuelven igual la disponibilidad. Diseñar vacantes e IA sin renovación universal. |
| A4 | `Economics001_Initial.sql:7`, cargo condicionado en `SqliteSportsStore.cs:126` | Criterio compatible con la caja persistida. La deuda impagada todavía no está representada por un servicio común. | Mantener saldo no negativo y registrar obligaciones pendientes cuando no pueda pagarse. No usar caja negativa como sustituto de crédito. |
| B1 | `SqliteTransferStore.cs:52–75,101`, `Transfer001_Negotiations.sql`, `SqliteStaffOffers.cs:Draft/Move/Persist`, `SqliteEconomyStore.cs:36–46` | Parcialmente confirmado. Mercado ya tiene replay por operación; Staff tiene revisión optimista; Economía solo un caso exclusivo de compromiso. Media para el estado actual, alta para pagos. | Reutilizar patrones, sin equiparar control de revisión con recuperación idempotente. Proponer registro financiero de operaciones y claves de negocio. |
| B2 | `SqliteEconomyStore.cs:11`, `SqliteSportsStore.cs:123–126`, `TransferNegotiation.cs:61–69`, `TransferPaymentPlans.cs:24–33`, `TransferOfferPresets.cs:24–33` | Riesgo de truncamiento confirmado en el conversor general; varios llamadores ya validan. Media y localizada, sin pérdida histórica demostrada. | Conversión exacta a céntimos y política separada de reparto. Validar adquisición antes de convertir. Identidad estable de obligaciones independiente de recrear un calendario. |
| B3 | `SqliteStaffOffers.cs:113–119`, `ClubEconomicPlan.cs:33–50`, `TransferPaymentPlans.cs:45–53`, `SqliteSportsStore.cs:123–126` | Confirmado. Hay límites/previsiones y comparación informativa, sin libro general de compromisos. Alta para nuevas autorizaciones reales. | Validar presupuesto neto de ejecución y compromiso aplicable por periodo, y caja por separado. Conciliar costes iniciales para evitar dobles sumas. |

### A1. Inventario completo de escrituras de caja localizadas

La búsqueda de `cash_cents` y de INSERT/UPDATE/DELETE sobre `club_economics` en `src` identifica los siguientes caminos.

| Camino | Escritura | Trazabilidad y protección actuales |
|---|---|---|
| `EconomicInitialWorld2026.LoadOrCreate`, `SqliteEconomyStore.cs:88–89` | INSERT del saldo inicial y presupuesto por club/temporada | Catálogo, hash, publicación y origen comprobados. Publicación transaccional sin sobrescribir. No es un ingreso periódico. |
| `SqliteSportsStore.CommitAcquisition`, `SqliteSportsStore.cs:126` | UPDATE que descuenta compra e instalación de la caja | Pedido en `sport_acquisitions` y estado de instalación en `sport_facilities` en la misma transacción. Exige solicitud PendingQuote, oferta Approved, importes positivos y autorización externa. |
| `SqliteEconomyStore.CommitManager`, `SqliteEconomyStore.cs:44–45` | UPDATE exclusivo de `manager_cost_cents` | No cambia caja. Guarda un compromiso inicial ya incluido en la planificación de personal. |

No se localizaron otras escrituras productivas de caja en `src`. Las pruebas pueden alterar copias para verificar aislamiento; no son operaciones del juego. El cargo tecnológico sí tiene un documento de negocio, por lo que describirlo como carente de toda trazabilidad sería excesivo. Falta una fecha de cargo y un movimiento contable general con saldo anterior/posterior o reconstrucción por diario. La fecha de solicitud y AvailableFrom no prueban por sí solas la fecha del pago.

Recomendación propuesta: caja canónica por club/moneda, materializada para lectura y reconstruible mediante apertura y movimientos confirmados. La operación tecnológica deberá escribir a través del servicio común, compartiendo transacción con el pedido. La publicación inicial permanece como fuente inmutable. Alternativas son derivar cada saldo en consulta, con mayor coste, o mantener la caja directa y un registro accesorio, que exigiría conciliar dos fuentes susceptibles de divergir. Se recomienda diario autoritativo más proyección materializada y actualización atómica.

Pruebas necesarias: saldo inicial igual al corte, compra y pedido en la misma transacción, rollback en cada escritura, replay tras perder la respuesta, reconstrucción idéntica al saldo y bloqueo ante discrepancia. Incluir caja insuficiente y adquisición histórica que ya descontó dinero.

### A2. Temporada, carrera y contrato inicial

Hay cuatro bloqueos o supuestos independientes.

1. `frozen_career_start` impide UPDATE de `career_state`, incluida fecha y temporada. Es una protección explícita de esta versión, no un fallo de un calendario implementado.
2. `SqliteCareerStore.Load` rechaza un contrato cuya oferta inicial pertenece a otra temporada. Cambiar el SeasonId actual conservando correctamente la oferta firmada histórica ya infringiría esa condición.
3. La carga exige 20 recursos económicos y que todos pertenezcan a la temporada activa. Mantener dos ejercicios históricos en esa tabla daría 40 filas y sería rechazado.
4. `CareerWorkspace` repite la igualdad entre temporada de la oferta firmada y la temporada activa. Además, `CommitAcquisition` consulta recursos por club con `SingleOrDefault` y su UPDATE filtra por club; múltiples temporadas podrían fallar antes de escribir o afectar varias filas si se reutilizara el SQL. `SqliteTransferStore.Currency` usa la primera fila del club y necesitará una selección explícita.

Invariantes propuestas: identidad de carrera permanente; instantánea inicial inmutable; temporada activa existente y coherente con la fecha; contrato referido a la temporada y condiciones en que se firmó; vínculo laboral actual resuelto por fechas e historial; tesorería única por club/moneda; presupuesto identificado por club/ejercicio con históricos conservados. Un cambio de ejercicio no concede un nuevo contrato, reinicia caja o borra obligaciones.

La estrategia aditiva recomendada es conservar `career_state` como instantánea de origen y proponer una extensión versionada de estado vigente con fecha/temporada activas. Solo una de esas fuentes gobierna el estado actual según versión; los formatos anteriores utilizan un adaptador a la instantánea original. Se requiere adaptar Load, Workspace y lectores de fecha al habilitar la extensión. La igualdad contrato-temporada se reemplazará por pertenencia y vigencia correctas, manteniendo integridad de la oferta histórica. No hay SQL definitivo ni autorización para modificar estas reglas ahora.

Alternativa: migrar el estado actual existente y sustituir el trigger preservando una instantánea inicial aparte. Reduce lecturas duplicadas, pero toca una tabla central y necesita más pruebas. Recomendación aditiva condicionada al diseño final del calendario.

Pruebas necesarias: presupuestos de dos temporadas y una sola caja, lectura histórica, contrato firmado en la primera temporada conservado, nueva fecha/temporada válidas, detección de estado vigente incoherente y compatibilidad v1/v2. Usar copias de prueba, nunca quitar el trigger de una partida personal. F1 puede verificar el almacén financiero entre dos periodos sin habilitar Continuar. El recorrido completo de cambio de temporada y reapertura de carrera depende del futuro adaptador temporal y seguirá pendiente hasta su implementación.

### A3. Vencimiento del Staff y vacantes reales

`SqliteInitialStaffProfession.Create` obtiene el final de la temporada única que contiene el inicio de la carrera y lo transmite a todos los contratos generados. Con base económica compatible se guardan los contratos; `InitialStaffProfession.Employment` puede devolver null si faltan plan o capacidad según su regla inicial. La evidencia anterior de 68 contratos corresponde a una carrera compatible, no a todas las partidas posibles.

Después del final inclusivo, `StaffProfile.ContractStatus` devuelve Free, pero el directorio continúa proyectando Club y Team desde `academy_staff` mientras permanezca Institutional. La persona podría aparecer libre a efectos del filtro y seguir vinculada visualmente al equipo. `SqliteSportsStore.Staff` y `AcademyTeamState.EffectiveManagement` dependen de ese estado institucional, no del vencimiento del contrato. Las delegaciones persistidas se conservan y podrían seguir resolviendo ese técnico.

En cambio, `SqliteStaffDirectory.TrainingProfiles` excluye perfiles con contrato conocido fuera de fecha. El ejecutor puede conservar una asignación institucional mientras pierde su contribución profesional y aplica el estímulo base. Esto no equivale a cese laboral coherente. Si se elimina físicamente `academy_staff`, el JOIN obligatorio de `SqliteAcademyStore.Read` podría hacer desaparecer el equipo de la consulta. Debe permitirse una vacante sin borrar equipo o identidad.

No se localizó un proceso de IA que renueve o sustituya esos contratos de forma operativa. Los clubes no dirigidos comparten las fechas iniciales de sus técnicos y tendrán el mismo problema cuando se procese el reloj. Con el tiempo bloqueado, es un riesgo de continuidad demostrado por reglas estáticas y no una incidencia comercial ya ejecutada.

Política futura propuesta: cada club evalúa los puestos documentados antes del vencimiento, según necesidad, disponibilidad, resultados acreditados y presupuesto. Puede proponer renovación, buscar sustituto, cubrir temporalmente con alguien compatible o dejar vacante. La renovación requiere nuevo documento y consentimiento; la IA utiliza el mismo servicio laboral y financiero que el usuario. No se generan técnicos ni renovaciones para garantizar ocupación universal. Las prioridades, horizonte de evaluación y límites de decisión requieren aprobación, sin fórmulas arbitrarias aquí.

Al llegar el vencimiento, terminar autorizaciones incompatibles, resolver control al mánager autorizado o marcar tarea pendiente, y preservar sesiones e informes históricos. La contratación entrante no hereda automáticamente delegaciones conforme a P9. Varias vacantes simultáneas deben ser un estado válido y visible, con tareas bloqueadas o pendientes donde no haya personal apto.

Alternativas: retorno efectivo sin cambiar filas históricas más eventos de cese, o historial formal de ocupaciones con proyecciones por fecha. Se recomienda el segundo modelo para incorporación, usando el primero únicamente como compatibilidad de lectura. Renovar todo automáticamente queda descartado por decisión del usuario.

Pruebas necesarias: último día y día posterior; técnicos libres con identidad conservada; equipo sin ocupante visible; delegación efectiva sin empleado caducado; sesiones previas preservadas; 20 clubes sin renovación universal; renovación denegada por presupuesto y sustitución que no clona persona.

### A4. Liquidez e insolvencia

El CHECK de caja no negativa y el UPDATE condicionado por saldo suficiente son compatibles con el criterio provisional. Faltan obligaciones vencidas, estado de impago y crédito autorizado. La falta de caja futura debe impedir el pago y conservar íntegra su obligación, vencimiento e historial. No constituye una razón para deshacer automáticamente el contrato firmado o eliminar el gasto devengado.

La consecuencia económica inmediata propuesta es pendiente exigible y motivo de fallo. Intereses de demora, sanciones, consecuencias deportivas, procedimientos de insolvencia y financiación requieren reglas y hechos propios. Caja cero no prueba insolvencia patrimonial. Alternativa de saldo negativo sin contrato de financiación contradice el criterio seleccionado. Un descubierto futuro autorizado deberá representarse como deuda y disponibilidad expresa, no como tolerancia global a números negativos.

Pruebas necesarias: pago justo al saldo, insuficiente por un céntimo, dos pagos concurrentes, reintento posterior, y obligación vencida preservada con caja no negativa. La regla de deuda pendiente se probará cuando F2 implemente obligaciones; no se da por operativa en F1.

### B1. Idempotencia y transacciones compartidas

| Servicio | Garantía actual | Diferencia respecto a F1 |
|---|---|---|
| `SqliteTransferStore` | `operation_id` UNIQUE, Replay por solicitud, comprobación de acción, identidad y revisión; devuelve resultado ya guardado, incluido PendingPlayers tras aceptación. Transacción propia. | Patrón útil para solicitudes reintentables. No es un registro de pagos ni un coordinador financiero compartido. |
| `SqliteStaffOffers` | ID de oferta y revisión, comparación optimista del último JSON, control de carrera y transacción en Persist. | No recibe token de operación ni devuelve replay del resultado de una acción repetida. Draft crea Guid nuevo y bloquea otra negociación activa; reintentar Move con revisión vieja produce conflicto. |
| `SqliteEconomyStore` | CommitManager actualiza solo si el compromiso está NULL dentro de la transacción del creador. | Repetir ese helper no devuelve el contrato previo. La idempotencia completa está en `SqliteCareerStore.Create`, que comprueba request_hash del archivo existente. |
| `CommitAcquisition` | Exige solicitud pendiente y rechaza una ya comprometida; cargo y pedido atómicos. | Previene un segundo cargo por el mismo pedido, pero no recupera por token el resultado al perder una respuesta. |

Se propone un registro financiero de operación con ID estable, carrera, tipo, origen, huella de solicitud, versiones esperadas, resultado y fecha efectiva. El mismo ID y contenido devuelve el resultado; con otro contenido, conflicto. Una unicidad de negocio por origen/concepto impide el mismo cargo con otro ID. Las revisiones protegen ediciones concurrentes y no sustituyen ninguna de esas garantías.

Las transacciones financieras deben compartir conexión y transacción con el efecto de negocio. Reutilizar `WorldDatabase.Command` y métodos internos que aceptan la unidad de trabajo. No crear una capa genérica para todos los repositorios ni reescribir ahora Staff o Mercado. Primero extraer el mínimo patrón de idempotencia para F1 y probarlo en adquisiciones. Los IDs se crean en la frontera del comando y se persisten antes de reintentar.

Pruebas necesarias: solicitud repetida, ID reutilizado con otros términos, otro token para el mismo pedido, revisión obsoleta, cierre antes de commit y respuesta perdida después, y rollback de cargo/pedido/resultado. Como alternativa, usar solo revisión optimista sería más simple pero no cumpliría la recuperación del resultado solicitada.

### B2. Política de dinero y conversiones

`checked((long)(amount*100))` detecta desbordamiento pero el cast decimal→long trunca hacia cero; no rechaza fracciones de céntimo. Por semántica de la expresión, 1,239 EUR podría convertirse en 123 céntimos. Este ejemplo es análisis de la expresión, no una ejecución sobre una partida.

Los planes iniciales validan dos decimales y la cláusula de rescisión pasa por `ContractReleaseRules.Validate` antes de otra conversión semejante. Eso limita el riesgo en esos caminos. `CommitAcquisition` exige signos y datos, pero no comprueba explícitamente escala decimal de Purchase/Installation antes de Cents. Una autorización externa que solo aprobase el importe podría dejar pasar fracciones de céntimo. No hay evidencia de cargos históricos erróneos en esta revisión.

Política propuesta para F1: EUR como única moneda operativa; entero con signo de 64 bits para unidades menores, cantidad de operación no negativa y máximo técnico comprobado. Antes de convertir, exigir valor × 100 entero y dentro del rango, además de política de signo y límite por concepto. La entrada de tres decimales se rechaza con error específico y se conserva para corrección; no se redondea silenciosamente. Se permite representar 1,230 como 1,23 porque el valor es exactamente convertible. Un acumulado puede desbordar aunque sus partidas sean válidas; comprobar también sumas.

Cálculos de prorrateo futuros podrán producir fracciones de céntimo y necesitarán una política de redondeo versionada. Se propone distinguirlos de la validación de entrada exacta. La elección de punto medio en ese cálculo queda pendiente. Para repartir un total cerrado, calcular cuotas en precisión suficiente, fijar unidades menores y asignar el residuo a la última cuota, como ya hacen los generadores de Mercado.

`TransferPaymentPlans.Generate` y `TransferOfferPresets.Payments` conservan el total, omiten cuotas cero y pueden reutilizar IDs previos por orden. Si faltan los previos, crean nuevos GUID. `SameSchedule` compara cantidades, orden, fechas y estado, sin exigir el mismo ID. Por tanto, no usar regeneraciones sucesivas como identidad de una obligación ejecutable. Al formalizar, asignar y persistir IDs estables por operación y vencimiento, aun si una cuota mínima queda omitida.

Alternativas: decimal como importe persistido facilita lectura, pero requiere garantía exacta de serialización y precisión en todas las rutas; REAL no sirve como autoridad monetaria. La recomendación son enteros EUR más moneda explícita, manteniendo el campo de moneda para una extensión posterior aprobada. No activar multimoneda ni convertir contratos históricos.

Pruebas necesarias: 0, 0,01, 1,23, fracción de céntimo, negativo, máximo, suma que desborda, ida y vuelta exacta, reparto 100,00 en tres cuotas, 0,01 en cuatro cuotas, ID estable tras reapertura y conservación de antiguos calendarios. Los límites de Staff y de cláusulas existentes no se igualan automáticamente al límite de caja.

### B3. Capacidad de firma, presupuestos y compromisos

| Operación | Comprobación económica actual | Carencia para formalizar |
|---|---|---|
| Staff | Salario anual dentro de mínimo configurado y StaffBudget; moneda del plan. Comparación individual. | Sin descuento agregado de compromisos, coste patronal real, prima, compensación, sustitución y distribución por periodo. |
| Futbolistas | Contrato deportivo con IDs, fechas y rescisión informativa. | No hay salario personal persistido ni nómina/compromiso individual. El acuerdo entre clubes pasa a PendingPlayers. |
| Traspasos | FinancialReview compara anticipo/caja e importe fijo/TransferRemaining; es informativo. | Falta consumir presupuesto y registrar obligaciones al formalizar, junto al contrato y traslado. |
| Tecnología | Caja, moneda, importes de propuesta, solicitud pendiente y `IFacilityPurchaseAuthorization.CanCommit`. | El bool externo no aporta un libro común de partidas, compromisos futuros o mantenimiento. Coste inmediato solo compra+instalación. |

Criterio seleccionado: el disponible debe contemplar compromisos firmes aplicables al periodo y separarse de caja. Propuesta técnica para una partida cuya base sea gasto reconocido: disponible = autorizado − ejecutado neto − compromiso firme aún no ejecutado. Devengar un importe comprometido reduce el pendiente y aumenta el ejecutado una sola vez. Pagarlo después no consume otra vez el presupuesto. En fichajes e inversiones debe aprobarse una base de consumo específica, separada de amortización o resultado contable.

Los planes iniciales ya contienen personal y otros gastos previstos. Importar contratos no aumenta esa previsión por segunda vez. Se enlaza cada coste conocido a su compromiso y se reconcilia su cobertura con la partida. La masa agregada de jugadores no se reparte en salarios ficticios: el margen contractual permanece desconocido. Etiquetar un técnico como Cantera no permite sumar el mismo salario también como otro gasto independiente de Staff.

Alternativa de descontar todo el contrato al presupuesto de la temporada actual sobrecargaría ese periodo; descontar solo caja ignoraría compromisos futuros. Se recomienda distribución por periodos y exposición futura explícita, con aprobación presupuestaria y solvencia separadas. Pruebas necesarias: dos firmas que compiten por el mismo margen, paso compromiso→ejecución, cancelación expresa, varias temporadas, periodo sin presupuesto aprobado y masa salarial incompleta sin margen inventado.

## 3. Revisiones adicionales

### Reputación de clubes

La conclusión «reputación solo en catálogo estático» sería incompleta. `AdjustedInitialWorld2026` conserva en el catálogo JSON la reputación interna 0–100 y deriva Visible. `SqliteWorldStore.Load`, alrededor de su línea 48, lee `clubs.reputation`, que la publicación y la copia de carrera almacenan como valor visible 1–5. `003_ClubUniverseAndScales.sql` lo comprueba. El ID del club y su reputación visible ya forman parte del estado persistente; la interna no tiene columna propia y la evolución temporal no está implementada.

Los futuros ingresos necesitarán reglas de procedencia que indiquen si usan reputación interna, visible, resultados o contratos. No convertir un 2/5 histórico en un 32/100 supuesto ni consultar siempre el catálogo inicial para evolucionar una carrera. Propuesta futura: diseñar un estado reputacional por carrera con origen y cambios acreditados y proyección visible derivada, preservando la semilla histórica; su relación concreta con `clubs` requiere decisión antes de una tabla nueva. F1 no necesita evolución reputacional.

Gravedad media para ingresos y progresión, sin bloqueo de la caja inicial. Alternativa de usar solo escala visible pierde precisión y debe aprobarse. Pruebas futuras: valores aprobados de origen, carrera independiente, visible derivado, histórico desconocido preservado y ausencia de cambios en publicaciones.

### Salarios de jugadores

`Domain/Entities.cs:67` define Contract con contrato, jugador, club, inicio/fin y cláusula de rescisión opcional. Las tablas y modelos actuales no acreditan salario anual individual, moneda salarial, pagas, coste empresarial, primas, revisiones, devengos ni aplicaciones de nómina. El valor de rescisión no es salario ni precio de mercado. La negociación de traspaso entre clubes tampoco constituye un contrato personal aceptado.

Falta un documento económico personal versionado, enlazado al contrato deportivo y a su fecha de efecto, junto a las políticas de nómina y obligaciones. Su futura adición puede ser una extensión, preservando IDs y datos existentes. Hasta entonces, mostrar presupuesto y previsión agregada, y margen contractual desconocido. Gravedad alta para nómina y capacidad salarial real, sin necesidad de inventar salarios para implementar F1.

### Migraciones y compatibilidad

Hay un esquema de mundo con `PRAGMA user_version` hasta 4 y extensiones independientes de carrera, economía, deporte, Staff y Mercado. `WorldDatabase.Open` activa FK y usa solo lectura por defecto; `Command` acepta la transacción del llamador. `CareerJson.Migrate` ejecuta recursos SQL, pero la selección/versionado de cada extensión recae en sus servicios. No es un orquestador automático de todas las migraciones.

`SqliteWorldFiles.MigrateCopy` copia y publica sin sobrescribir. `Career002_ContractTerms.sql` sí reconstruye tablas, pero comprueba que las tablas de carrera estén vacías y solo se utiliza al crear una carrera nueva. Las carreras v1 existentes se leen sin ejecutar esa reconstrucción. No debe reutilizarse ese script como migrador de carreras pobladas.

Recomendación: extensión financiera aditiva versionada, más adaptadores explícitos de lectores/escritores. Conservar tablas y snapshots históricos; incorporar metadatos de apertura y única autoridad de saldo. La migración debe instalarse por operación expresamente definida, no al consultar. El estado nuevo requiere declarar compatibilidad mínima para impedir que una versión antigua vuelva a escribir caja por fuera del libro. Adición de tablas por sí sola no resuelve esa compatibilidad de comportamiento.

Reconstruir tablas se reservaría para restricciones imposibles de ampliar, con copia, validación de recuentos, FK, JSON y recuperación. F1 no requiere decidir el SQL de puestos/identidad Staff. Pruebas futuras sobre copias v1/v2 y extensiones presentes/ausentes, rollback de migración, esquema futuro rechazado y hashes de publicación preservados.

## 4. Comparación de arquitectura contable

| Criterio | A. Libro simplificado por operación y concepto | B. Partida doble interna mínima |
|---|---|---|
| Complejidad inicial | Menor para entradas/salidas de caja. Necesita reglas adicionales para deuda, anticipos y devengos. | Mayor al definir cuentas y líneas, pero con una comprobación central de equilibrio. |
| Conciliación de caja | Puede ser exacta si cada movimiento está enlazado y cerrado. | Exacta y vinculada a su contrapartida económica. |
| Obligaciones y pagos | Necesita reconciliar registros de pendientes con movimientos y devengos por separado. | Deuda y su liquidación se conectan mediante cuentas a pagar/cobrar y documentos. |
| Préstamos e inversión | Requiere evitar por reglas dispersas que toda entrada sea ingreso o salida gasto. | Principal, interés, activo y gasto tienen naturaleza separada desde el registro. |
| Rectificación | Operación inversa y validación específica de efectos. | Contrapartida enlazada y equilibrada, manteniendo documento y aplicaciones. |
| Riesgo técnico | Divergencias entre saldos, gasto y pendiente si las reglas se duplican por módulo. | Catálogo o clasificación incorrectos pese a que el asiento cuadre; equilibrio no sustituye validación de origen. |
| Evolución | Viable si se mantiene como registro completo, pero puede acabar recreando un modelo equivalente. | Más natural para nóminas, deuda, anticipos e inversiones previstos en Furvo. |
| Interfaz | Resumen comprensible por conceptos. | El mismo resumen; débitos/créditos solo en auditoría opcional. |

Recomendación pendiente de aprobación: B, con un diario reducido y sin plan contable legal exhaustivo. En F1 bastan tesorería, contrapartida técnica de apertura y cuenta de adquisición según política aprobada; cuentas a pagar/cobrar, devengos, deuda y otras clases se incorporan cuando se necesiten. La apertura técnica no declara beneficios históricos, capital social o patrimonio inventado. El asiento equilibrado y su origen son imprescindibles para el núcleo propuesto; detalle fiscal, consolidación, bancos múltiples y contabilidad avanzada quedan fuera de F1.

La recomendación coincide con el diseño financiero previo, pero se fundamenta aquí frente a una alternativa viable. La autorización del presupuesto, el gasto reconocido y el pago seguirán siendo registros distintos incluso con partida doble.

## 5. Modelo financiero mínimo propuesto

### Invariantes

- Una tesorería canónica por carrera, club y moneda, continua entre temporadas. El campo materializado tiene versión y secuencia de último movimiento.
- Presupuestos y compromisos por periodo, con una base de consumo documentada. Los periodos anteriores se conservan.
- Negociar no inmoviliza fondos conforme a P5. Firmar o autorizar revalida bajo la misma transacción y registra compromiso firme.
- Una obligación conserva origen, acreedor/deudor, moneda, importe, vencimiento o condición, aplicaciones e historial. Registrar compromiso no significa devengo ni pago.
- Caja no negativa sin financiación aprobada; impago conserva deuda. Un desembolso necesita autoridad, documento y política.
- Lectura sin escrituras, ajuste sin borrar historia y una sola fuente económica para todos los módulos.

### EUR y ampliación futura

Se recomienda EUR como única moneda operativa en F1, con campo explícito de moneda en documentos, cuentas y claves. Rechazar otras monedas en el límite del servicio F1 con error específico. No crear conversiones ni mezclar saldos. La representación por unidades menores y política versionada permite una ampliación posterior sin reinterpretar céntimos EUR como unidades de otra moneda. La decisión de activar nuevas monedas deberá definir precisión, conversión y asientos de cambio.

### Plurianuales y recurrencias

Propuesta: un compromiso contractual padre con periodos, total firme y reglas de recurrencia versionadas. Para cuotas fijas de traspaso, obligaciones identificadas desde la formalización. Para salarios recurrentes, plantilla contractual que materializa cada periodo con clave única contrato+concepto+periodo y fechas calculadas conforme a la política aprobada. No generar una obligación nueva cada vez que se abre la pantalla.

El generador de obligaciones puede producir programación futura, pero el devengo requiere prestación o hecho acreditado y el pago requiere fecha/caja y autorización. Las condiciones sin motor capaz de evaluarlas mantienen exposición condicionada. No exigir toda la caja plurianual al firmar; sí comprobar pagos inmediatos y compromisos por periodo según política de solvencia pendiente. F1 solo necesita la relación de compromiso y sus periodos, sin nómina automática ni ejecutor diario.

## 6. Dependencias y procesador futuro de calendario

| Sistema | Lo que aporta | Lo que debe consumir de Finanzas |
|---|---|---|
| Staff | Revisión aceptada, contrato, puesto, vigencia y liberación | Capacidad, compromiso, obligaciones, devengo y pago en transacción compartida con firma/alta cuando se implemente. |
| Mercado | Acuerdo entre clubes y personal, operación y calendario pactado | Presupuesto y caja diferenciados, cuotas y aplicaciones. Empezar por traspaso simple según hoja de ruta. |
| Cantera/Entrenamiento | Equipos, técnicos, sesiones y servicios acreditados | Imputación de costes sin duplicar masa salarial, autorización efectiva por fecha y coste solo por hecho real. |
| Infraestructuras | Contrato, proyecto, autorización, hitos y mantenimiento | Compromisos, anticipos, obligaciones por hitos, pagos y financiación futura. Mantiene su módulo independiente posterior. |
| Calendario | Fecha real de carrera, secuencia de eventos y temporadas | Vencimientos, cierres, presupuesto activo, devengos y decisiones recuperables. |
| Clubes IA y directiva | Necesidades, permisos y decisiones aprobadas | La misma evaluación de capacidad y formalización que usa el usuario. |

Se propone un procesador de eventos con ID de origen, tipo, versión de regla, fecha efectiva y fase. Orden estable por fecha, fase, club y ID, con dependencias explícitas entre hechos. Cada evento confirmado guarda resultado en la transacción del efecto; al reabrir se reanudan los pendientes. Una decisión IA usa entradas persistidas y semilla/versionado cuando requiera aleatoriedad, sin depender de DateTime.Now ni del orden de ejecución paralelo.

Al finalizar un ejercicio deben estar resueltos los hechos del periodo o registradas incidencias, conciliados los saldos y preservados los impagados. La apertura siguiente mantiene caja/deuda, activa presupuesto aprobado e importa compromisos que ya afectaban a ese periodo sin duplicarlos. Vencer un contrato termina su vigencia; renovar necesita decisión y nuevo documento. La secuencia concreta de vencimiento, resolución de puestos, decisiones IA y activación de sustituciones debe diseñarse antes de habilitar el reloj; una vacante entre eventos es válida.

Los cobros presupuestados no se generan por el mero cambio de temporada. Los pagos tardíos conservan vencimiento y registran su fecha efectiva. Ante error antes del commit no hay efecto parcial; después, el replay devuelve el resultado. Para saltos de varios días se procesa cada fecha efectiva en orden. F1 prepara claves/fechas, pero no implementa cierre comercial, agenda completa, IA o avance temporal.

## 7. Alcance V1.0 y decisiones posteriores

`V1_CONTRACT.md` exige economía, contratos, directiva y entrenadores, pero no define expresamente el ciclo completo del mercado de empleados ni el organigrama laboral ampliado. Las entregas posteriores ya desarrollaron Staff, negociación, perfiles y diseño de incorporación. La ausencia de la palabra Staff en la tabla comercial no acredita una exclusión aprobada.

| Materia | Contrato histórico | Evolución posterior y decisión por revisar |
|---|---|---|
| Empleados | Entrenador, cambios y sistemas mínimos relacionados; detalle laboral no desarrollado | Seleccionar el mínimo de Staff comercial, incluidos puestos, vacantes, salarios e incorporación si se mantiene este flujo. |
| Agentes | Exigencias básicas; agentes avanzados excluidos | Hoja de ruta prioriza operación simple antes de cláusulas/cesiones/agentes, sin derogar por sí sola los requisitos mínimos comerciales. |
| Cantera | Juveniles y viabilidad; gestión exhaustiva excluida | Entregas de cantera ampliadas requieren concretar qué constituye soporte mínimo y qué se aplaza. |
| Infraestructuras | Niveles numéricos; construcción detallada excluida | Estadios, obras, ciudades deportivas y concursos mencionados en planes futuros requieren aprobación comercial expresa. Economía ofrece puertos, no los incluye automáticamente. |
| Control de alcance | Una ampliación importante requiere reducir o aplazar otra comparable | Presentar una revisión de alcance y coste antes de cambiar V1.0; no reescribir el contrato en esta tarea. |

El informe recomienda revisar un mínimo laboral estable que evite puestos caducados activos y permita formalización coherente, pero no declara aprobado ese alcance comercial. La condición de publicar 30 temporadas y viabilidad económica seguirá necesitando calendario y pruebas reales.

## 8. Especificación propuesta de F1 por entregas

El alcance candidato completo es demasiado amplio para una única entrega pequeña. Se propone dividirlo en F1.1–F1.4, con decisiones de aprobación en cada puerta. La integración de una operación real se alcanza en F1.4; antes de ese punto no se declara completada la prueba de extremo a extremo del sistema común.

| Entrega | Componentes | Criterio de salida |
|---|---|---|
| F1.1 | Política exacta EUR y contrato de operación idempotente | Importes exactos, sumas controladas y semántica de ID/huella/origen definida y probada. |
| F1.2 | Apertura financiera, diario mínimo y saldo materializado conciliable | Saldo actual preservado, apertura repetible sin duplicar y lectura por fecha desde el corte. |
| F1.3 | Presupuestos y compromisos por periodo, proyección de capacidad | Partidas conocidas, margen incompleto explícito y concurrencia de compromisos sin sobreautorizar. Continuidad financiera entre dos periodos. |
| F1.4 | Adaptador del cargo existente CommitAcquisition al servicio común | Cargo, pedido, asiento y resultado atómicos, repetibles y conciliados. Sin nuevas infraestructuras ni botón comercial nuevo. |

No basta con posponer adquisiciones a F1.4 y permitir mientras tanto dos escritores sobre caja. Propuesta de transición: hasta activar el backend financiero nuevo, conservar ruta antigua; en una carrera cuya extensión nueva esté activada, toda escritura de caja debe pasar por él y el camino antiguo debe adaptarse o quedar bloqueado explícitamente. La activación en una carrera de usuario requiere F1.4 y sus pruebas, no solo nuevas tablas. Las pruebas intermedias pueden trabajar con copias aisladas y la política de cutover acordada.

### Componentes y contratos de servicio

| Componente propuesto | Entrada → salida | Alcance F1 |
|---|---|---|
| Política de dinero en Core | Decimal, moneda y regla de signo/límite → entero exacto o error | EUR, sin cambios de importes históricos. |
| Documento de operación | ID, carrera, club, origen, versión esperada, fecha, moneda, líneas y huella → resultado estable | Base para apertura y adquisición. |
| Consulta financiera | Club/fecha/periodo → saldo, último movimiento, presupuesto, compromiso y estado de completitud | Solo lectura. Sin generar obligaciones futuras. |
| Unidad de trabajo financiera en Persistence | Documento y transacción compartida → diario, saldo y resultado | Atomiza con el pedido existente. Sin reescribir los motores de negociación. |
| Consulta/registro de capacidad | Periodo, partida, compromiso autorizado y revisión → apto/no apto/pendiente | Reglas genéricas; no habilita firma Staff ni traslado de jugadores. |
| Conciliador | Apertura, diario y proyección → igualdad o incidencia | Autoriza nuevas escrituras solo si el ámbito es coherente. |

El identificador de operación y su origen deben generarse/persistirse en el límite del comando. La solicitud repetida recupera resultado antes de aplicar otra vez cambios. Una autorización externa de tecnología deberá revalidarse sobre la versión actual del presupuesto bajo transacción, conservando las comprobaciones existentes de espacio, precios y plazo. Si no hay una partida/capacidad acreditada para ese caso, no fabricar un presupuesto: devolver pendiente y documentar el criterio de integración que se apruebe.

### Persistencia conceptual, sin SQL definitivo

- Contexto y versión financieros por carrera; fecha de apertura, procedencia y estado de activación.
- Cuentas por club/moneda y proyección de saldo/versionado, independientes de la temporada.
- Operaciones con token, huella, tipo, origen y resultado; clave de negocio única para apertura o pedido.
- Diario y líneas equilibradas enlazadas a documento y fecha; rectificaciones con referencia al original.
- Periodos, partidas y revisiones, con compromisos por periodo y base de consumo.

Las obligaciones futuras necesitarán FK o enlaces tipados a origen, acreedor/deudor y aplicaciones; F1 reserva esos contratos conceptuales sin crear nóminas o pagos aplazados operativos. La ruta concreta de esquema y migración se decide tras aprobar la arquitectura. No reconstruir tablas de Staff ni añadir salarios ficticios para cumplir F1.

### Apertura y operación de adquisición

En una carrera compatible, importar el saldo actual a fecha de corte y registrar una contrapartida de apertura documentada. No volver a usar InitialCash si ya hubo cargos. Mantener los documentos previos de adquisición y su estado; no reproducir su cargo ni inventar fechas de movimientos anteriores desconocidas. El historial consultable del nuevo diario comienza en el corte; el pasado conserva su evidencia disponible.

La adquisición posterior al corte validará cantidades exactas, moneda y autoridad, reabrirá solicitud pendiente, comprobará capacidad y saldo, y registrará cargo, asiento, pedido e instalación dentro de la misma transacción. La cuenta de contrapartida para compra e instalación necesita aprobación, por ejemplo activo/adquisición pendiente de clasificación; no asignar automáticamente gasto corriente si la política no está definida. El mantenimiento futuro queda como dato de propuesta, fuera del pago inmediato de compra e instalación.

Saldo actual después = saldo anterior − compra − instalación, con movimiento único y asiento equilibrado. El presupuesto se afecta conforme a la partida y base aprobadas, sin sumar de nuevo la inversión como consumo por el mero pago si ya estaba comprometida. No introducir un límite financiero artificial para sustituir datos ausentes.

### Límites de F1

EUR operativo, diario mínimo, consulta, capacidad por periodos y cargo real ya existente. No incluye nómina completa, patrocinios, préstamos, traspasos complejos, incorporación efectiva de Staff, infraestructura nueva, contabilidad avanzada o pantallas financieras extensas. Tampoco activa reloj, IA de personal o simulación de insolvencia. El soporte financiero de dos periodos se verifica sin prometer un rollover completo de carrera con el loader actual.

## 9. Matriz de pruebas propuesta

| Prueba | Resultado verificable |
|---|---|
| Conciliación de caja | Apertura + movimientos = saldo materializado para cada club/fecha. Diferencia detectada y sin ajuste silencioso. |
| Operación duplicada | Igual token/contenido devuelve resultado; otro contenido falla; otro token para mismo pedido no duplica cargo. |
| Fallo antes de commit | Ningún cambio de caja, asiento, pedido, presupuesto o resultado. |
| Respuesta perdida después de commit | Replay devuelve exactamente la operación confirmada tras reapertura. |
| Concurrencia presupuestaria | Dos compromisos compiten por el mismo disponible; solo se aceptan combinaciones dentro de autorización. |
| Concurrencia de caja | Dos cargos que exceden juntos el saldo no producen negativo. |
| Precisión | Céntimos exactos y suma de cuotas sin pérdidas; tres decimales no convertibles rechazados; overflow controlado. |
| Publicaciones y carreras históricas | Hash de publicaciones intacto; carreras sin economía solo consultadas, sin inicialización retrospectiva. |
| Adquisición ya ordenada | Apertura usa caja existente y conserva pedido; repetición no descuenta otra vez. |
| Adquisición nueva | Autorización real, cargo y pedido bajo transacción; moneda incorrecta, fracción de céntimo y saldo insuficiente rechazados. |
| Dos periodos financieros | Misma caja antes/después de activar presupuesto siguiente; compromisos distribuidos, históricos conservados y ninguna nueva apertura monetaria. |
| Cambio de temporada de carrera | Cuando exista adaptador temporal, Load/Workspace admiten oferta histórica y nueva temporada; no se elimina ni reescribe el contrato original. |
| Staff vencido y vacante | Cuando se implemente ciclo laboral, ID e historial intactos, equipo visible y sin autorización de empleado caducado. |
| Impago y plurianuales | En F2, obligación vencida conservada, pago posterior único y compromiso plurianual sin exigir toda la caja hoy. |
| Compatibilidad entre versiones | Nuevo formato declara restricciones de escritura; un lector/escritor antiguo no altera caja fuera del libro. |

Estas pruebas se añadirán donde corresponda al implementar, usando copias aisladas. El cierre de cada entrega requerirá compilación y pruebas adecuadas; el cierre de F1, suite completa Debug/Release y prueba de adquisición con persistencia/reapertura. No se eliminan pruebas existentes para lograrlo.

## 10. Puertas de aprobación y hoja de ruta

1. Aprobar B como contabilidad mínima o elegir A con garantías equivalentes; confirmar EUR operativo, conversión exacta y contrapartida de apertura.
2. Aprobar el modelo canónico de caja y la transición de escritores/lectores, así como activación compatible sobre copias y política para partidas sin información.
3. Autorizar F1.1 y F1.2 de manera acotada. Con sus pruebas, revisar política de presupuesto/compromisos de F1.3 y la integración de la adquisición de F1.4.
4. Completar F1.3/F1.4 y validar caja, capacidad y pedido. Solo entonces considerar la base común apta para una operación económica real del alcance actual.
5. Diseñar e implementar obligaciones y devengos de F2 mediante encargo posterior, con reglas de vencimiento y pagos aprobadas. Preparar adaptador de carrera/calendario por una entrega propia antes de habilitar avance.
6. Con contratos/puestos y capacidad financiera preparados, incorporar Staff y el traspaso simple por sus entregas autorizadas. Las operaciones posteriores e Infraestructuras mantienen sus dependencias y alcance separado.
7. Aprobar la revisión comercial de V1.0 y concretar política de renovaciones/sustituciones IA, ingresos y sostenibilidad antes de validar temporadas completas.

La primera autorización recomendada es un bloque técnico pequeño F1.1, seguido de apertura conciliada F1.2 una vez resueltas sus decisiones. Esta división afina la propuesta anterior F1a de apertura y no reemplaza silenciosamente los diseños históricos. La implementación no se inicia con este informe.

## 11. Riesgos principales y resultado de esta tarea

Los bloqueos de mayor impacto futuro son los supuestos de temporada única de Load/Workspace, disponibilidad de Staff separada de su contrato y escritores de caja sin diario común. Las oportunidades reutilizables son las transacciones compartidas existentes, replay de Mercado y documentación de procedencia económica. El conversor de céntimos necesita una garantía exacta antes de convertirse en puerto común.

La alternativa recomendada es un diario equilibrado mínimo con caja materializada, EUR en F1 y presupuestos/compromisos por periodo, sujeto a aprobación. Las vacantes reales y la regla de impago con caja no negativa proceden del encargo. El SQL, las consecuencias de insolvencia, la política de renovaciones IA y el alcance comercial adicional de Staff siguen pendientes de definición o aprobación.

Se crea únicamente este informe. Se preservan los dos diseños previos, la auditoría histórica, código, esquemas, partidas y pizarra. No se ejecutan compilaciones o suite por esta tarea documental. No hay commits, push ni implementación de F1.
