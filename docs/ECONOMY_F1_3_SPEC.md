# Economía F1.3 — Presupuestos y compromisos

Estado: especificación para revisión, sin implementación ni aprobación implícita de políticas nuevas. Fecha: 20/11/2026. Base inspeccionada: main, `53f74792fde07249884a63ee55ea5f150eb69ecb`.

## 1. Cierre y base existente

F1.2 ya está confirmada en ese commit: los cinco archivos corresponden al diario, migración v2, pruebas y documento de auditoría. No se crea un commit duplicado. La frase «cinco archivos sin confirmar» de ECONOMY_F1_2.md describe el instante histórico de aquella auditoría.

Evidencia ejecutada en la auditoría sobre el mismo código: Debug sin errores ni advertencias; 914 pruebas completas y 38 específicas superadas, cero fallidas/omitidas; SQLite íntegro, migración y rollback en copias; Godot headless código 0. En esta tarea se revisan Git, commit y documentación; esas cifras no son una ejecución nueva.

F1.1 aporta EuroAmount (long en céntimos, decimal exacto, signo explícito) y SqliteFinancialOperations (identidad, términos, recibo y transacción del consumidor). F1.2 aporta SqliteJournal, diario v2 inmutable, partida doble y proyección conciliada dentro del corte de observación. No es todavía la autoridad global de tesorería. SqliteEconomyStore conserva recursos iniciales y presupuestos legacy; el escritor CommitAcquisition sigue fuera del diario. No hay sistema común operativo de compromisos por periodos. El avance completo de temporada tampoco está disponible. Los diseños generales no acreditan implementación.

## 2. Alcance y exclusiones

Recomendar F1.3 como núcleo compartido de autorizaciones presupuestarias, compromisos firmes y consultas por periodos explícitos. Implementar posteriormente mediante autorización propia, sin activar automáticamente productores de Staff/Mercado. Preparar categorías configurables: fichajes, salarios de jugadores, Staff, operaciones, cantera e infraestructuras; no imponer todas a cada club. Las políticas operativas específicas de cada departamento llegarán con su integración.

Quedan fuera: pagos, obligaciones exigibles operativas, préstamos, ingresos recurrentes, insolvencia, impuestos, automatización de temporadas, nuevas pantallas y CommitAcquisition (F1.4). No recopilar ahora cuentas de los veinte clubes ni sustituir los datos publicados. Reservas previas a firma permanecen aplazadas por P5; se especifica su eventual ciclo para no improvisarlo después.

## 3. Fidelidad financiera transversal

Cada club comenzará desde una fotografía individualizada de su referencia real, identificada por fecha y ejercicio. Reputación y clasificación no sustituyen estados financieros. No hay categorías artificiales de riqueza ni ajustes ocultos para restaurar una situación histórica. Tras el inicio, únicamente los acontecimientos de la carrera cambian su economía; jugador e IA comparten las mismas invariantes.

La futura fotografía abarcará ingresos ordinarios/extraordinarios (audiovisuales, comerciales, estadio, competiciones), gastos operativos/financieros, salarios deportivos/no deportivos, activos, pasivos, patrimonio, caja, deuda, operaciones con clubes, vencimientos, inversiones y particularidades institucionales. Ausencia no significa cero. Los casos Madrid, Barcelona, Málaga, Sevilla y Getafe requieren investigación por ejercicio; este documento no afirma sus situaciones actuales.

Propuesta de dato: SnapshotId/version, ClubId estable, ejercicio fiscal, temporada relacionada, fecha de corte, perímetro individual/consolidado, concepto, moneda/unidad, importe nullable, fuente/URL/página, fecha de publicación y verificación, naturaleza observado/derivado/estimado/desconocido, fiabilidad razonada y método/version/entradas de cálculo. Separar saldos a una fecha de flujos de un intervalo.

Jerarquía: cuentas auditadas; registros y documentación oficial; informes del club; organismos y ligas; fuentes secundarias contrastadas; estimaciones justificadas. Resolver conflictos por perímetro, fecha y calidad, registrando ambas evidencias. No mezclar ejercicio fiscal y temporada sin puente documentado. Estimar solo con entradas acreditadas, fórmula reproducible y supuestos visibles, con revisión humana; no completar diferencias patrimoniales inventando caja. Si no hay base suficiente, conservar desconocido y bloquear solo la autorización que lo necesite.

Publicaciones inmutables con hash, versión y manifiesto de fuentes; la carrera fija esa versión y copia su estado inicial. Actualizaciones solo para nuevas carreras salvo migración expresamente aprobada. La carga real, licencias de fuentes, estimaciones concretas y conciliación patrimonial completa son fases futuras. F1.3 prepara referencias de procedencia y admite datos incompletos; no declara que las semillas actuales satisfagan este estándar.

## 4. Conceptos y autoridades

| Concepto | Significado y fuente de verdad propuesta |
|---|---|
| Tesorería | Fondos efectivos; legacy hasta transición F1.4, diario solo en su ámbito declarado |
| Autorizado | Límite aprobado por órgano competente, revisiones presupuestarias |
| Disponible | Autorizado menos consumido firme y reservas activas; admite negativo |
| Reserva | Retención temporal de capacidad, sin gasto ni pago; aplazada |
| Compromiso | Condición firme de contrato/operación, historial versionado |
| Obligación | Acreedor/deudor, importe pendiente y vencimiento; registro futuro F2 |
| Deuda financiera | Financiación pendiente, distinta de cuentas comerciales por pagar |
| Ingreso/gasto | Reconocimiento por devengo, independiente del cobro/pago |
| Activo/pasivo | Recursos y obligaciones reconocidos contablemente |
| Resultado/patrimonio | Saldos derivados con signo; pérdidas y patrimonio negativo válidos |

Préstamo recibido: caja y pasivo aumentan, no ingreso ordinario. Un gasto puede quedar pendiente sin pago. Importes contractuales y principal adeudado no negativos; saldos/ajustes autorizados pueden tener signo. Mantener cash_cents >= 0 mientras no exista descubierto/financiación explícita; nunca sanear con saldo ficticio.

## 5. Política presupuestaria recomendada, pendiente de aprobación

Elegir capacidad por categoría y periodo con base de medición explícita. Para fichajes, recomendar consumir el precio fijo íntegro al compromiso en el periodo de adquisición; cuotas futuras afectan liquidez, no descuentan otra vez capacidad. Para salarios, distribuir compromiso por periodo de prestación. La regla de prorrateo salarial y redondeo necesita aprobación antes de activar salarios. No mezclar presupuesto de caja y de devengo en una misma partida.

Definir A=autorizado vigente, U=consumo firme neto, R=reservas activas (cero inicialmente), D=A-U-R. U incluye la parte ya ejecutada: ejecutado es un subconjunto informativo, nunca otra resta. Al cumplir una obligación, no liberar U; al cancelar legalmente una parte no ejecutada, registrar liberación trazable de esa parte. Cancelar contrato no elimina deuda ni gasto ya devengado. D puede ser negativo tras reducción aprobada o importación de compromisos superiores a A; no borrar compromisos para ocultarlo.

Asignación inicial necesita fuente, plan financiero y autorización identificada. Caja alta no aumenta A. Revisiones incrementales con motivo, actor autorizado, fecha de carrera y versión esperada. Una excepción requiere autorización explícita revisada o permiso acotado por importe/periodo/causa, registrado y consumible una sola vez; no eludir integridad ni financiar caja inexistente. No se propone porcentaje automático de ingresos o reputación.

Reservas futuras: crear activa reduce D; convertir a firme sustituye R por U atómicamente; liberar/caducar reduce R una vez; reintentos no repiten liberación. Sin reservas en negociaciones iniciales: consultar capacidad y revalidar al firmar, de acuerdo con Staff P5.

## 6. Modelo y servicios propuestos

Core: BudgetPeriod (fechas sin solapamiento para la misma base), BudgetCategory, BudgetAuthorizationRevision, EconomicCommitment y CommitmentAllocation (categoría/periodo/importe/base), CommitmentAmendment, FinancialSourceReference. Identidades permanentes, carrera/club/moneda/origen/revisión, motivos y actor. Compromiso confirmado se conserva; modificaciones son revisiones, no sobrescrituras. Estados propuestos: firme, parcialmente liberado, cumplido, cancelado; cancelación con efectos pendientes no equivale a liquidación.

Servicios de consulta: capacidad, compromisos por periodo, proyección de necesidades. Comandos: autorizar/revisar presupuesto, confirmar/modificar/liberar compromiso. Reusar EuroAmount y FinancialOperationRequest; firmas conceptuales reciben conexión/transacción mediante adaptador Persistence, nunca tipos SQLite en Core. Godot consume DTO públicos, no modifica saldos directamente.

Persistence futuro: cabeceras, revisiones y asignaciones con FK por carrera/club/periodo; unicidad de origen de negocio + revisión + efecto, no solo token de transporte. Recibo F1.1 y cambios en una única transacción de escritura SQLite. Releer capacidad dentro de la transacción; versión esperada evita edición perdida. Un fallo revierte todo; respuesta perdida recupera recibo. Lecturas no inicializan datos.

No escribir SQL ahora. Migración futura aditiva y explícita sobre copias. Importar solo autorizaciones acreditadas; el remaining legacy no demuestra por sí solo qué compromisos lo consumieron. Donde falte trazabilidad, marcar capacidad desconocida y requerir conciliación. No convertir automáticamente remanentes en presupuesto nuevo ni mantener dos escritores. F1.3 puede operar aislada; autoridad funcional de adquisiciones se transfiere en F1.4 con corte probado.

## 7. Operación obligatoria: adquisición de 15 millones

Ejemplo sintético de diseño, no dato de club. Contrato fijo 15 M€, 5 M€ ahora, 5 M€ a doce meses y 5 M€ a veinticuatro. Negociación: ninguna inmovilización. Firma: revalidar capacidad y registrar compromiso de 15 M€ (política recomendada), asociado a la revisión exacta. No exigir 15 M€ de caja hoy; validar fondos para pago inmediato y plan de vencimientos cuando existan esos sistemas.

En futura formalización, si procede reconocer derechos como activo: debe Activos 15 M€, haber Obligaciones 15 M€. Pago inicial: debe Obligaciones 5 M€, haber Caja 5 M€; quedan 10 M€ pendientes en dos vencimientos. Pagos posteriores reducen obligación y caja, sin nueva adquisición ni nueva resta presupuestaria. Amortización, costes accesorios y tratamiento concreto del activo requieren diseño posterior; no introducirlos como reglas aprobadas.

Contrato, compromiso, obligaciones, asiento y pago tienen IDs propios enlazados al mismo origen. Formalización y primer pago serían atómicos según alcance F1.4/F2, nunca llamadas independientes sin coordinación. Cambiar precio/fechas exige revisión aceptada; reprogramar cuotas conserva principal, historial y pagos realizados. Compensaciones y cancelaciones tienen causa y acreedor explícitos.

## 8. Obligaciones, deuda y periodos futuros

Proponer contrato financiero/origen, acreedor/deudor, principal, moneda, calendario, interés separado, vencimientos e imputaciones de pagos parciales. Pendiente=obligación reconocida más ajustes válidos menos pagos imputados; no negativo por sobrepago inadvertido. Un pago puede repartirse entre obligaciones mediante asignaciones cuya suma sea exacta. Intereses no se confunden con principal.

Vista de juego: próximo, intermedio y lejano, umbrales por aprobar. Clasificación contable corriente/no corriente se determina por política de cierre y vencimiento, no por etiqueta de temporada; reclasificar no crea deuda. Mantener IDs durante décadas. Reestructuraciones e insolvencia quedan fuera; futuras revisiones conservarán obligaciones originales y trazabilidad. La misma caja atraviesa periodos: presupuesto nuevo no genera apertura contable nueva. Rollover de carrera requiere adaptador temporal independiente.

## 9. Integraciones e invariantes

Staff y Mercado aportarán contratos aceptados; Cantera/Entrenamiento gastos solo si existe acto económico real; Infraestructuras pedidos e inversiones autorizados. No crear costes por consultar pantallas. IA usa los mismos comandos; puede elegir mal y asumir riesgo autorizado, nunca saltarse caja, generar dinero o borrar deuda. Ascenso/descenso no activa multiplicadores económicos inventados.

Invariantes: identidad económica única; replay igual devuelve mismo resultado y términos diferentes fallan; reserva/compromiso/cancelación no duplican capacidad; pago no recrea obligación; firme no desaparece sin revisión válida; referencias completas; operación compuesta atómica; rollback total; concurrencia revalida; céntimos exactos y overflow controlado; migración conserva historia; caja restringida a financiación existente; pérdidas y patrimonio negativo admitidos. Aplicarlas a humano e IA. Dominio valida reglas; SQLite garantiza unicidad/FK/transacción; consultas reconcilian proyecciones con historial.

Presupuestos no generan asientos por ser autorizados o reservados. El diario F1.2 sigue siendo el único diario; registro presupuestario es historial de autorizaciones, no contabilidad paralela. No publicar como ejecutada una obligación sin asiento/pago real. No activar productores legacy sobre capacidad nueva hasta F1.4.

## 10. Matriz diseñada, no ejecutada

En cada fila, persistencia exige reapertura y mismos IDs/importes; en rechazo, hash lógico e historial sin cambios; en replay, mismo recibo y cardinalidad. F=F1.3; R=reservas futuras; I=F1.4/F2; D=carga de datos; T=calendario; A=IA. Estas comprobaciones comunes forman parte de cada escenario.

| # | Situación inicial → acción | Resultado e invariante | Fase |
|---|---|---|---|
|1|Fuente autorizada → abrir presupuesto|A exacto, ninguna caja creada|F|
|2|A vigente → aumentar/reducir|Revisión autorizada, D negativo visible si procede|F|
|3|D suficiente → reservar|R aumenta una vez, sin asiento|R|
|4|Capacidad disponible → confirmar|U aumenta una vez; conversión R/U atómica si existe reserva|F/R|
|5|Reserva activa → liberar dos veces|Solo primera cambia R|R|
|6|Firme parcialmente ejecutado → cancelar|Liberar solo parte válida, conservar deuda/historial|F/I|
|7|Precio y caja suficientes → contado|Un compromiso, adquisición y pago|I|
|8|15 M€ en tres cuotas → formalizar|U=15 M€, caja -5 M€, pendiente10 M€|I|
|9|Obligación10 → pagar3|Pendiente7, sin adquisición nueva|I|
|10|Pendiente7 → pagar7|Pendiente0, un pago|I|
|11|D insuficiente → confirmar|Rechazo salvo autorización explícita|F|
|12|Caja insuficiente → pagar|Sin pago ni negativo|I|
|13|Financiación aprobada → disponer|Caja/pasivo reales, sin ingreso ficticio|Futura financiación|
|14|Operación confirmada → repetir|Recibo idéntico; otro contenido rechazado|F|
|15|Dos compromisos exceden juntos D → competir|Solo combinación autorizada se confirma|F|
|16|Fallo entre registros → commit|Rollback integral, reintento posible|F|
|17|Carrera antigua → migrar copia|Historial intacto, sin inventar autorización|F|
|18|Compromiso plurianual → consultar siguiente periodo|Asignaciones conservadas, caja sin nueva apertura|F/T|
|19|Patrimonio negativo → consultar capacidad|No saneamiento; presupuesto independiente|F/I|
|20|Deuda elevada → evaluar propuesta|Riesgo visible sin borrar deuda|I|
|21|Recién ascendido → revisar plan|Solo autorizaciones con fuente, sin dinero automático|T/A|
|22|Descendido → revisar plan|Compromisos previos intactos, déficit posible|T/A|
|23|Club ilíquido con A positivo → pagar|Caja bloquea pago aunque exista presupuesto|I|
|24|Club saneado con A bajo → comprometer|Caja no amplía A por sí sola|F/I|
|25|IA usa comando → exceder límites|Mismo rechazo que usuario|A/F|
|26|Fotografía conciliada → importar|Fuente, fecha, perímetro y saldos reproducibles|D|
|27|Estimación y observado → consultar|Etiquetas y método preservados, desconocido distinto de cero|D|
|28|Publicación nueva → reabrir carrera vieja|Versión fijada sin cambio retrospectivo|D|
|29|Varias cuotas → reclasificar vencimientos|Mismos IDs y principal, sin deuda nueva|I|
|30|Compromiso ya consumido → pagar/reintentar|U no se descuenta de nuevo; caja y obligación una vez|I|

## 11. Alternativas, decisiones abiertas y riesgos

Recomendar historial inmutable de revisiones más proyecciones reconstruibles frente a un saldo editable: facilita auditoría y cancelaciones. Reusar recibos F1.1 frente a otra infraestructura idempotente. Preferir compromiso íntegro de fichaje y calendario de liquidez separado frente a consumir solo la cuota inicial, que ocultaría capacidad futura comprometida. La alternativa de presupuestos plurianuales de caja exige una base separada y aprobación; no combinar ambas restas.

Antes de programar: aprobar base de cada categoría, política salarial/prorrateo, autoridad y límites de excepciones, periodos, tratamiento de variables condicionadas, regla de liberación y fuente inicial acreditada. Reservas siguen aplazadas; no se activa su conversión por haberla descrito. Desconocidos actuales: cifras verificadas de cada club, calendarios completos, costes empresariales, financiación y reglas de insolvencia. No derivarlos de reputación.

Riesgos: doble autoridad legacy/nueva, remanentes sin origen, periodos incompatibles, dobles imputaciones, cambios retroactivos de fuentes y rendimiento de historiales. Mitigar con corte explícito, validación transaccional, datos versionados y pruebas de carga antes de prometer décadas. No se garantiza integración global por superar pruebas unitarias de F1.3.

## 12. Orden y aceptación propuestos

1. Aprobar políticas pendientes y contrato de procedencia.
2. Dominio de periodos/categorías/autorizaciones y consultas exactas.
3. Compromisos y revisiones, idempotencia compartida y concurrencia.
4. Migración explícita en copias, conciliación presupuestaria, dos periodos y regresión completa.
5. Revisar F1.3 antes de integrar adquisiciones en F1.4; obligaciones/pagos en F2 y carga financiera por entrega propia.

Cierre futuro F1.3: todos los casos F ejecutados, criterios por categoría aprobados, sin cambios a caja/diario por autorizar presupuesto, reintentos/concurrencia/rollback probados, partidas compatibles y desconocidos explícitos. Integraciones I/R/T/D/A se declaran pendientes con su responsable. En esta tarea solo se crea esta especificación, sin código, migración, datos, commit de F1.3 ni push.
