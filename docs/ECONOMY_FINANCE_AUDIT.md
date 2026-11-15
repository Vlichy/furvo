# Auditoría de Economía y Finanzas

Furvo conserva recursos iniciales por club y temporada. El fichaje inicial del mánager registra un compromiso anual en la misma transacción que crea la carrera. La economía operativa requiere un libro mayor, un registro de obligaciones y servicios que ejecuten movimientos contra estos registros.

## Inventario verificado

| Componente | Estado | Evidencia y alcance |
|---|---|---|
| Saldo inicial | Implementado y verificado | `SqliteEconomyStore.cs`, `EconomicInitialWorld2026.ReadApprovedPlans/LoadOrCreate`. Veinte planes en EUR de `initial-economy-2026-27.v1.json`. Publicación v5 valida el origen v4, el catálogo y las referencias. |
| Tesorería dinámica | Implementado parcialmente | `Economics001_Initial.sql` guarda `cash_cents` no negativo. `ReadResources` lee el saldo. Faltan entradas, salidas, fechas, conciliación y reversión. |
| Presupuesto de fichajes | Implementado parcialmente | `club_economics.transfer_remaining_cents` se inicializa con el autorizado. `ReadResources` comprueba el techo. `TransferFinancialReview.Assess` compara presupuesto e importe fijo y liquidez y anticipo. Faltan reserva, consumo, liberación y reasignación. |
| Presupuestos salarial, staff y cantera | Implementado parcialmente | `ClubEconomicPlan.Validate/Resources` conserva límites y previsiones. Margen salarial comprometido y restante de jugadores desconocidos. La capacidad planificada sin asignar es una previsión. |
| Presupuesto anual y general | Implementado parcialmente | Clave club–temporada y plan inicial inmutable. Falta aprobación, rollover, revisiones y asignación de partidas durante la carrera. |
| Audiovisuales, taquilla, abonos, patrocinio y comercial | Solo modelo de dominio | `EconomicIncome.RecurringTotal` suma previsiones. `ClubEconomyPage.Block` las presenta como previsiones. Faltan cálculo por hechos, contrato, vencimiento, liquidación y asiento. |
| Premios, ventas de jugadores y otros cobros | No implementado | `ClubEconomicPlan.Validate` exige ventas iniciales desconocidas. Falta evento de competición y formalización de venta. |
| Salarios de jugadores, personal, cantera, estadio, operación e intereses | Solo modelo de dominio | `EconomicExpense` representa previsiones agregadas. Falta nómina, facturación, calendario y pago. |
| Coste del mánager | Implementado y verificado | `SqliteCareerStore` llama a `SqliteEconomyStore.CommitManager` dentro de la transacción de creación. Contrasta oferta con el plan y exige un único compromiso. No hay cargo de caja. |
| Primas, comisiones y amortizaciones | No implementado | Faltan contratos económicos, documentos y servicios de liquidación. |
| Contratos de futbolistas | Implementado parcialmente | `Domain/Entities.cs`, `Contract`, conserva vínculo y fechas. `ContractReleaseClause.cs` aporta rescisión informativa según conocimiento. Faltan salario, primas, renovaciones y comisiones con vencimientos. |
| Contratos de otros empleados | Bloqueado por dependencias | `TrainingStaff` y `AcademyStaff` ofrecen datos deportivos de personal. Falta una fuente de contratación laboral y nómina de empleados. |
| Compromisos futuros | Implementado parcialmente | Compromiso inicial del mánager y propuesta de pagos de traspaso. Falta una obligación por acreedor, moneda, vencimiento, cantidad pendiente y pago aplicado. |
| Libro mayor, cuentas, asientos y documentos | No implementado | `Economics001_Initial.sql` crea metadatos de publicación y recursos. No hay registro económico transaccional, claves de idempotencia contable, conciliación ni reversión. |
| Patrocinios | Solo modelo de dominio | Previsión agregada. Sin contratos principal, técnico, secundarios, estadio o acuerdos comerciales con fechas, exclusividad, renovación y bonificaciones. |
| Préstamos y deuda | Solo modelo de dominio | `ClubEconomicPlan` conserva deuda corta, larga, total e interés previsto. Sin banco, principal por contrato, amortización, pagos anticipados, mora o financiación de instalaciones. |
| Propiedad y órganos | Implementado parcialmente | `ClubDetails.cs` define forma societaria y campos de presidencia y propietario mayoritario. Falta registro de socios, acciones, competencias, mandatos y decisiones. |
| Cotización y ampliaciones | No implementado | Requieren capital, participaciones, autorizaciones y reglas por jurisdicción. |
| Simulación de 30 temporadas | Bloqueado por dependencias | `CareerWorkspace.CanAdvanceTime` es falso. Faltan eventos de competición, cierre contable y renovación de presupuestos. |

## Mercado, contratos y delegación

`SqliteTransferStore.Write` guarda revisiones de propuestas y cambios de estado en SQLite. Las respuestas aceptadas pasan a `PendingPlayers`. Los identificadores de operación evitan repetir una revisión de negociación. El registro está separado de cualquier asiento. `TransferTerms` y `TransferPaymentPlans.Generate` representan al contado, plazos y condiciones estimadas. Intercambios, variables, porcentajes brutos y plusvalías, recompra y tanteo son condiciones de propuesta. Su ejecución requiere una operación formalizada o un ejercicio de derecho válido y comprobable. Las cesiones, reparto salarial y opciones u obligaciones de compra carecen de un ciclo operativo.

Un acuerdo de 20 M€ con 5 M€ iniciales, 10 M€ a 12 meses y 5 M€ a 24 meses puede describirse como propuesta. En el modelo futuro, la formalización registraría el compromiso total conforme a la política presupuestaria. La caja reflejaría el importe efectivamente pagado. El libro de obligaciones conservaría 15 M€ pendientes con vencimientos separados. Se requiere un identificador de obligación por pago y una aplicación idempotente del cobro. La auditoría no ejecutó el ejemplo.

El acuerdo entre clubes, el consentimiento y las condiciones personales son pasos separados. Salario, duración, primas, renovación y comisión requieren documentos propios. El paso de una propuesta aprobada a formalización debe volver a validar presupuesto, caja, consentimiento, contrato, inscripción y autoridad.

Las delegaciones de financiación y patrocinio podrían utilizar las mismas reglas de aptitud, supervisión y límites por tarea. La firma requiere competencia vigente del empleado y las aprobaciones del club. Una vacante devuelve la gestión efectiva al mánager y conserva las operaciones pendientes.

## Riesgos y diseño propuesto

- Una carrera v1–v4 puede carecer de extensión económica. En F1, diferenciar valores desconocidos de cero, conservar publicación y ofrecer una inicialización compatible con origen documentado. Probar restauración y migración sobre copias.
- Separar saldos de caja, autorizaciones, reservas y obligaciones. Usar céntimos enteros y claves de club, temporada, carrera y moneda. Mantener un saldo verificable a partir de asientos equilibrados y registrar rectificaciones con contrapartida. La publicación aprobada es el origen del asiento inicial.
- La presentación actual puede leerse como resultados reales si se reutilizan las previsiones en nuevos paneles. Presentar previsto, comprometido, vencido, pagado y disponible como categorías distintas. El coste anual del mánager ya está incluido en la previsión de personal.
- Los servicios de Core validarán documentos, fechas y autoridad. Persistence grabará asientos, obligaciones, consumos y cambios deportivos en transacciones con claves idempotentes y revisiones. Godot mostrará saldos, origen y estado. Conservar vínculos al contrato o la operación original.
- Para préstamos, usar contratos con principal, tasa, plazo, cuotas e identificador de acreedor. Comparar flujo operativo esperado tras gastos y obligaciones con servicio de deuda y reserva de caja. Registrar principal e interés por separado y definir tratamiento explícito de mora y amortización anticipada.
- La futura estructura societaria distingue presidencia, directiva, propiedad, accionistas y órgano competente. Registrar emisiones, transferencias y dilución. Una emisión primaria puede aportar caja al club y una venta secundaria remunera al titular vendedor. Las reglas y autorizaciones requerirán verificación por país y competición antes de su implementación.

## Plan F1–F6

| Fase | Alcance | Criterio de aceptación |
|---|---|---|
| F1 | Tesorería, presupuestos, libro mayor y compromisos. Reutilizar los 20 planes y definir apertura para carreras compatibles. | Asientos cuadrados, saldo conciliado, presupuesto independiente, repetición sin duplicar y recuperación tras rollback y reapertura. |
| F2 | Ingresos y gastos, salarios, contratos comerciales y calendario. | Cada hecho liquidado una sola vez y con fecha de carrera. Previsiones separadas. Renovaciones y bajas dejan trazabilidad. |
| F3 | Presupuesto anual, directiva, préstamos y salud financiera. | Aprobar y revisar presupuestos, amortizar principal e intereses y recuperar compromisos vencidos. Detectar falta de caja y servicio de deuda insuficiente. |
| F4 | Propiedad, participaciones y permisos. Preparación bursátil. | Titularidad y competencias persistentes. Emisión y compraventa de acciones con efectos de caja diferenciados en el diseño. |
| F5 | Integración de fichajes, cesiones y contratos. Empezar por el traspaso simple al contado conforme a la hoja de ruta de Mercado. | La formalización atómica aplica contrato, inscripción y asiento. Ofertas y acuerdos preliminares dejan caja y plantillas intactas. |
| F6 | Simulación económica de 30 temporadas tras disponer de calendario y eventos deportivos. | Medir liquidez, salarios, deuda, ingresos, compromisos vencidos, inflación y desigualdad por categoría. Comparar ascensos y descensos, reconciliar saldos y justificar los umbrales antes de publicar resultados. |

## Delegaciones

`tools/GodotUiSmoke.gd` amplía el recorrido de Delegaciones con salida al menú, reapertura de la misma carrera SQLite y comprobación de la asignación de planificación semanal en Entrenamiento. Un segundo proceso repite la comprobación con el mismo almacenamiento aislado. Se utiliza el punto de entrada de QA `OpenIsolatedMarketForValidation` porque `Main.ShowHome` todavía presenta Cargar partida deshabilitado. El recorrido comercial de carga queda pendiente.

Siguen pendientes la gestión supervisada de Cantera, historial, límites, bandeja, contratación y baja general de empleados y ejecución autónoma.

## Evidencia de ejecución

- `dotnet test Furvo.sln --no-build --no-restore` en Debug. 844 superadas, cero fallidas y cero omitidas. Incluye creación y carga de carreras, persistencia y aislamiento, restricciones económicas y reversión de la creación ante fallo.

- `dotnet build Furvo.sln -c Debug --no-restore` y `dotnet build Furvo.sln -c Release --no-restore` completados sin errores ni advertencias.
- Godot 4.7.2 .NET con `--club-delegations-only --capture` y luego `--club-delegations-reopen`. Ambos procesos terminaron con código 0. El primero comprobó los selectores a 1280×720, 1600×900 y 1920×1080. Ambos comprobaron la correspondencia con Entrenamiento tras recargar SQLite.
- SQLite abierto en modo solo lectura sobre la publicación v5 y las tres carreras de prueba. Los cuatro archivos devolvieron `integrity_check = ok`, cero filas en `foreign_key_check` y 20 filas en `club_economics` cada uno.
