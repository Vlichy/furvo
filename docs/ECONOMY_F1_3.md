# Economía F1.3 — Inspección previa a implementación

**Estado actual (22/11/2026): núcleo F1.3 implementado; validación y revisión independiente según el apartado final.** Las inspecciones siguientes se conservan como historia y no describen ya el estado actual del código. La autorización de implementación es posterior a ellas.

Fecha: 20/11/2026. Estado: **bloqueada por decisiones de diseño**, sin implementación iniciada. Se aplica la instrucción del apartado 2 del encargo de implementación controlada: documentar y detener la programación cuando la especificación conserve decisiones bloqueantes.

## Estado comprobado

Rama main, HEAD `53f74792fde07249884a63ee55ea5f150eb69ecb`. Al comenzar solo estaba sin seguimiento `docs/ECONOMY_F1_3_SPEC.md`; se conserva intacto. F1.2 está confirmada con sus cinco archivos de auditoría. Las 914 pruebas completas y 38 específicas son evidencia de la ejecución anterior, no pruebas ejecutadas en esta inspección.

Se ha leído la especificación completa y contrastado con el informe F1, el cierre F1.2, Staff P5 y SqliteFinancialOperations. Son reutilizables EuroAmount, operaciones idempotentes con recibo y transacción compartida y diario v2 de observación. No existe todavía el registro presupuestario común propuesto. La especificación distingue correctamente presupuesto, caja y diario, pero sus secciones 5 y 11 mantienen políticas pendientes de aprobación.

## Decisiones que impiden fijar el comportamiento

| Decisión | Evidencia y efecto | Propuesta para aprobar |
|---|---|---|
| Reservas en esta entrega | La especificación §§2/5/11 las aplaza conforme a Staff P5; el nuevo encargo las contempla y exige invariantes. No queda inequívoco si amplía ese alcance. | Implementar el ciclo técnico explícito de reservas en F1.3, sin activar reservas automáticas durante negociaciones de Staff/Mercado. Confirmación sustituye reserva por compromiso atómicamente. Alternativa: mantenerlas aplazadas y excluir sus comandos/pruebas ahora. |
| Base de consumo | Especificación §5 recomienda precio fijo íntegro de fichaje; el informe F1 exige aprobación específica de esta base. Salarios carecen de prorrateo aprobado. | Fichajes: consumir íntegro al compromiso en periodo de adquisición. F1.3 recibe asignaciones explícitas por periodo; no calcula prorrateo salarial ni amortización. |
| Excepciones y autoridad | Especificación §5 ofrece revisión del autorizado o permiso extraordinario acotado; son dos mecanismos distintos. | Usar solo revisión autorizada previa, con actor, causa, origen y versión esperada. No crear permisos de exceso paralelos. |
| Liberación y ejecución | Especificación §11 exige aprobar liberación; distinguir ejecución de pago determina cuánto puede cancelarse. | Solo liberar parte no ejecutada mediante resolución explícita; mantener ejecutado dentro del consumo. Pago no libera ni vuelve a consumir. La resolución no extingue obligaciones contables. |
| Periodos y renovación | No hay calendario funcional completo ni política aprobada de arrastre. | Periodos explícitos con fechas inclusivas no solapadas por club/categoría/base; nuevo periodo exige autorización propia, sin arrastre automático. Conservar asignaciones plurianuales. |

Estas recomendaciones no se consideran aprobadas por su inclusión en este documento. La autorización general de implementar está condicionada por el propio encargo a que no existan decisiones bloqueantes; no se interpreta como selección entre alternativas pendientes.

## Elementos que no bloquean esta entrega por pertenecer a fases posteriores

Cifras reales de cada club, préstamos, insolvencia, reglas fiscales, nóminas, obligaciones exigibles, pagos y productores económicos quedan pendientes de sus fases. No se requiere investigarlos ahora. La procedencia prevista admite verificado/derivado/estimado/desconocido y versiones inmutables, sin convertir desconocido en cero.

F1.4 mantiene CommitAcquisition y la transición de autoridad económica. El diario F1.2 continúa en observación; presupuesto no produce asientos automáticamente. La restricción de caja no negativa permanece mientras no exista financiación explícita. No se implementa un segundo modelo monetario ni contabilidad paralela.

## Criterios de aceptación y verificación

| Criterio | Estado |
|---|---|
| Inspección de Git, especificación y dependencias | Realizada |
| Cierre previo F1.2 | Commit existente comprobado; resultados históricos documentados |
| Políticas presupuestarias definitivas | Pendientes de las decisiones anteriores |
| Entidades, servicios y persistencia F1.3 | No implementados |
| Migraciones, reservas, compromisos y consultas | No implementados |
| Pruebas F1.3 e invariantes de integración | Diseñadas en SPEC, no ejecutadas |
| Compilación/suite/Godot en esta tarea | No ejecutados: no hay cambios de código |
| Partidas y publicaciones | Sin modificaciones |

No se han detectado ni corregido regresiones de ejecución en esta inspección. No se crean commits ni push. Único archivo creado en esta tarea: este informe; la especificación previamente pendiente permanece intacta. Próximo paso: resolver las cinco decisiones, actualizar la especificación con su estado aprobado y entonces implementar y ejecutar la matriz correspondiente.

## Auditoría del estado real — 21/11/2026

La sección anterior se conserva como evidencia histórica. Nueva inspección en main, HEAD `25042cb`, árbol inicialmente limpio. Se ha leído el encargo de auditoría, la especificación y los componentes económicos reales. El «ejecútalo» posterior a la propuesta se aclaró expresamente como «Abrir Furvo»; no aprobó las cinco decisiones ni inició F1.3. El commit actual incorpora documentación e interfaz, no una implementación presupuestaria nueva.

### Clasificación previa a cambios

| Funcionalidad | Estado | Evidencia | Fase responsable |
|---|---|---|---|
| Recursos y planes iniciales por club | A: implementada y ejecutable | SqliteEconomyStore.ReadResources, Economics001_Initial.sql, EconomicCareerTests | Base anterior |
| Compromiso particular del mánager | A: implementada y ejecutable, alcance limitado | CommitManager registra manager_cost_cents una vez sin pago; no es un registro general de compromisos | Base anterior |
| Precisión monetaria exacta | A: implementada y ejecutable | EuroAmount exige céntimos exactos, controla signo y overflow | F1.1 |
| Recibos idempotentes y transacción compartida | A: implementada y ejecutable | SqliteFinancialOperations, FinancialOperationTests | F1.1 |
| Diario v2, partida doble y conciliación | A: implementada y ejecutable dentro del corte | SqliteJournal, Journal001/002, JournalTests y JournalAuditTests | F1.2 |
| Uso de esa infraestructura por operaciones completas | B: pendiente de integración | Existen componentes y escritores anteriores; no hay autoridad global unificada | F1.4/F2 |
| Autorizaciones/revisiones por periodo y capacidad común | C: futura, requerida para cerrar F1.3 | Solo SPEC; sin entidades, servicios, migración o pruebas específicas | F1.3 |
| Reservas, conversión y liberación | C: futura; alcance pendiente de decisión | Solo descripción; no operación ejecutable | Decisión F1.3 |
| Compromisos generales, modificaciones y cancelaciones | C: futura, requerida para cerrar F1.3 | manager_cost_cents no cubre identidad/revisión/asignaciones generales | F1.3 |
| Obligaciones, pagos y deuda operativa | C: futura | No ejecutor común de vencimientos | F2 y posteriores |
| IA financiera, traspaso completo y cambio integral de temporada | C: futura, fuera de esta auditoría | Dependencias pendientes; continuidad de fechas contables no equivale a rollover | Entregas propias |

La clasificación C no convierte requisitos de F1.3 ausentes en exclusiones aceptadas. La fase está incompleta por carecer de su núcleo, aunque las integraciones futuras no sean requisito de cierre.

### Cobertura real e invariantes

Las pruebas existentes comprueban únicamente sus operaciones reales. FinancialOperationTests verifica replay tras reapertura, conflicto por términos/origen, rollback interior/exterior y escritores concurrentes. JournalTests y JournalAuditTests comprueban partida doble, inmutabilidad, contraasientos completos, proyección conciliada, restricciones de caja, saldos con signo, concurrencia, migración v1/v2 y rechazo de historial ambiguo. ConfirmedJournalHasValidSqliteReferencesAfterReopening y FinancialOperationExtensionPreservesExistingCareerAndEconomicContracts ejecutan PRAGMA integrity_check y foreign_key_check sobre copias temporales.

EconomicCareerTests comprueba planes iniciales, independencia entre carreras, compromiso particular del mánager sin pago y rollback. Ninguno de esos casos demuestra reserva de 3 millones sobre presupuesto de 10, confirmación sin doble descuento o concurrencia sobre disponibilidad presupuestaria. Esos casos no son ejecutables porque no hay comandos F1.3. No se añade infraestructura ni pruebas artificiales para simularlos.

La continuidad contable entre 30/06 y 01/07 conserva caja sin nueva apertura; no acredita cambio de temporada funcional. El diario sigue en observación y no sustituye el escritor antiguo. El conversor legacy SqliteEconomyStore.Cents no equivale a la garantía exacta de EuroAmount; futuras integraciones deben usar la política F1.1, sin reemplazo masivo en esta auditoría.

### Hallazgos y decisiones

Hallazgo de cierre: implementación F1.3 ausente. No hay defecto ejecutable de reserva/compromiso que reproducir y corregir. Las cinco decisiones iniciales siguen pendientes. No se modifica la especificación ni se implementan funciones. No se alteran partidas, publicaciones, esquemas ni código. No se crean pruebas nuevas: la regresión existente cubre la base disponible, no las operaciones ausentes.

### Verificación de esta ejecución

Compilación Debug ejecutada: cero errores y cero advertencias. Nueva suite completa ejecutada: **914 superadas, 0 fallidas, 0 omitidas**, duración 5 min 31 s, código 0. Pase específico de JournalTests, JournalOpeningPreserves y FinancialOperationTests: **48 superadas, 0 fallidas, 0 omitidas**. Las pruebas económicas de CareerCreationTests se incluyen en la suite completa. Pruebas específicas F1.3: no aplicables, no existen. Integridad, FK, reapertura, rollback, migraciones y concurrencia se evalúan mediante las pruebas existentes de la base, sin afirmar cobertura F1.3. Godot headless no se ejecuta: no hay componente nuevo ni cambio de presentación en esta auditoría.

Veredicto: **F1.3 incompleta o bloqueada**. Próximo paso: resolver el alcance presupuestario y autorizar su implementación; después auditar sus invariantes reales. No hay commits ni push.

Comandos de esta auditoría:

```powershell
dotnet build Furvo.sln --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test Furvo.sln --no-build --no-restore -m:1
dotnet test Furvo.sln --no-build --no-restore -m:1 --filter "FullyQualifiedName~JournalTests|FullyQualifiedName~JournalOpeningPreserves|FullyQualifiedName~FinancialOperationTests|FullyQualifiedName~EconomicCareerTests"
git diff --check
```

El filtro EconomicCareerTests no corresponde al nombre de clase (el archivo contiene CareerCreationTests); no se atribuye su cobertura al pase de 48. La suite completa sí las ejecuta. PRAGMA de integridad y FK, migraciones/reapertura y rollback/concurrencia existentes pasan dentro de esa suite. No hay pruebas fallidas ni correcciones funcionales. git diff --check sin incidencias; estado final: únicamente docs/ECONOMY_F1_3.md modificado. No se ha ejecutado una auditoría directa sobre partidas personales.

## Implementación controlada — 22/11/2026

F1.2 cerrada documentalmente en `a776f58` (único commit creado por esta intervención, sin push), sobre `c9acce1`. Se implementa el núcleo común solicitado después de revisar la especificación. Las cinco recomendaciones quedan concretadas en la actualización de SPEC; no se vuelven a auditar como sustituto de programarlas.

### Arquitectura y operaciones disponibles

Core/Finance/Budget.cs contiene BudgetDefinition (club, categoría, base, periodo, EUR y fuente), BudgetSource (procedencia/version/fecha/naturaleza/método), BudgetSnapshot, BudgetAllocation, BudgetChange, BudgetAction e IBudgetAuthorization. No depende de SQLite ni Godot. Los importes de entrada usan EuroAmount; los documentos contienen céntimos enteros, no un nuevo tipo monetario. Sumas y conversiones son comprobadas para detectar overflow.

SqliteBudgetStore ofrece Migrate, Create, Read, List, History y Apply. Los comandos reciben conexión y transacción del consumidor y utilizan SqliteFinancialOperations. Apply puede modificar varios presupuestos/periodos atómicamente, con una modificación por partida en cada solicitud. El consumidor debe abrir una transacción de escritura, confirmar y reintentar conservando IDs/términos; un bloqueo SQLite obliga a reintentar toda la unidad. Ninguna API confirma una transacción por su cuenta.

| Acción | Efecto real |
|---|---|
| Create | Registra definición, fuente y autorización inicial; no modifica caja |
| Authorize | Nueva cifra autorizada absoluta, con permiso y revisión esperada; puede revelar déficit |
| Reserve | Retiene capacidad; ID y origen únicos dentro de la partida |
| Commit | Compromiso directo; no se utiliza para confirmar una reserva existente |
| Confirm | Cambia reserva a compromiso conservando ID, origen y neto; disponibilidad idéntica |
| Amend | Revisa el neto de asignación, preservando liberaciones y lo ejecutado; ampliación exige capacidad |
| Release | Libera importe no ejecutado; cancelación total queda registrada sin borrar la asignación |
| Execute | Registra ejecución presupuestaria de compromiso; no es pago ni devengo contable automático |

Las operaciones sobre asignación cancelada se rechazan. Repetir el mismo comando recupera recibo; otro comando sobre el mismo estado debe presentar su revisión esperada vigente. Un segundo token para el mismo origen de comando se rechaza. El consumidor debe distinguir identidad de comando (origin de Apply), asignación (Allocation/Origin) y contrato/operación del futuro adaptador. No se deduplica una operación por parecerse en importe a otra legítima.

Disponible = autorizado − reservas netas − compromisos netos. Ejecutado permanece incluido en comprometido. Un presupuesto reducido puede mostrar disponible negativo sin borrar contratos. No se admiten nuevas ampliaciones de consumo que empeoren déficit; una revisión autorizada puede restablecer margen. Cancelar capacidad no cancela deuda, pagos ni contratos. Las obligaciones financieras y la caja siguen fuera de esta API.

### Autorizaciones y fuentes

Actor, autoridad y motivo son obligatorios en cada revisión; IBudgetAuthorization debe autorizar acción/carrera/club. El núcleo no confía solo en texto o interfaz. Las pruebas usan un autorizador explícito de fixture; no se instala uno que autorice todo en el juego. Los adaptadores a directiva/delegaciones llegarán con sus flujos autorizados. Las futuras revisiones pueden referenciar la resolución material en authority/reason.

Un presupuesto requiere procedencia y versión. Derivado/estimado requiere método; verificado no se transforma en una afirmación auditada por el software. No existe importación automática de datos financieros externos ni comprobación de veracidad documental. La ausencia de presupuesto devuelve null/lista vacía; no se convierte en disponibilidad cero ni se deduce de reputación, caja o division. Cada club puede utilizar partidas distintas.

### Persistencia y compatibilidad

Budgets001_Initial.sql instala budget_schema v1, budgets y budget_revisions, con FK a clubes y recibos de operaciones (diferida hasta commit), unicidad de revisión/operación por partida, prohibición de solapamiento y triggers contra UPDATE/DELETE. El historial JSON y sus totales tipados se comprueban al leer. No hay saldo editable ni segunda contabilidad: son autorizaciones administrativas. Los permisos de negocio se validan en la API, no contra escritura SQL externa deliberada.

Migración explícita/aditiva/reversible dentro de la transacción. Puede instalar F1.1 si falta; no instala F1.2 ni modifica mundos o carreras personales. Un esquema incompleto/desconocido falla sin reparación silenciosa. Las consultas sin extensión devuelven ausencia sin escribir. No se convierten transfer_remaining_cents o manager_cost_cents en compromisos generales: faltarían referencias de origen. La historia antigua permanece en su modelo.

Las solicitudes utilizan tipos budget-create-v1 y budget-change-v1 con representación explícita de campos primitivos en orden fijo para la huella. La serialización del dominio no define esa huella. Debe conservarse el orden de la lista de cambios al reintentar. La representación almacenada se versiona mediante budget_schema; cualquier evolución requiere migración/lector compatible. Los recibos confirmados se recuperan sin volver a aplicar autorización o efectos.

Los periodos son intervalos financieros, no relojes ni temporadas automáticas. Fecha del comando = fecha persistida de carrera; pueden autorizarse periodos futuros con esa fecha efectiva. No se retrocede el historial. El mismo Allocation ID puede vincular asignaciones del mismo contrato en partidas/periodos diferentes, cada una con su consumo propio. La distribución de importes la entrega el adaptador explícitamente; no se infieren nóminas o amortizaciones.

### Pruebas y criterios

24 pruebas nuevas en BudgetTests, sobre copias temporales con importes sintéticos. El primer pase reveló un bloqueo de la preparación de pruebas (segunda lectura con BEGIN inmediato); corregido usando transacción diferida de lectura, sin cambiar reglas de producción.

| Criterio | Evidencia |
|---|---|
| Creación exacta/procedencia/caja intacta | CreationPreservesCashAndDoesNotInstallOrWriteJournal, SourceAndCurrencyAreRequiredWithoutGuessingClubFinances |
| Reserva/confirmación/ejecución/liberación sin doble consumo | ReserveConfirmExecuteAndReleaseNeverDoubleConsume |
| Cancelación única e identidad | CancelledReservationCannotBeReleasedOrRecreatedAgain |
| Autorización, déficit y modificaciones | AuthorizationReductionExposesDeficitWithoutErasingCommitment, AmendmentRetainsExecutedPartAndRequiresCapacity, AuthorityCannotBeInventedByAnActorLabel |
| Replay, conflictos y concurrencia | ReplayAfterReopeningAndConflictingTermsAreSafe, ConcurrentOperationsCannotSpendTheSameCapacity |
| Atomicidad y errores SQLite | MultiPeriodFailureRollsBackEarlierRevisionAndReceipt, OuterRollbackRestoresBudget, SqlFailureCannotLeavePartialMultiBudgetOperation |
| Migración, compatibilidad e integridad | MissingExtensionReadIsNonMutatingAndMigrationRollbackPreservesFile, SqliteHistoryIsImmutableAndReferencesRemainValid, InstalledExtensionWithoutDataPreservesOldSaveAndRejectsUnknownVersion |
| Periodos, separación por club y ausencia de arrastre | PeriodsDoNotOverlapOrAutomaticallyCarryCapacity, ExplicitMultiPeriodCommitmentPreservesIdentityWithoutCarryingCash, SeparateClubsHaveSeparateCapacity |
| Límites, signos, moneda y reloj | MoneyUsesExactCentsAndTechnicalLimits, DomainRejectsInvalidConfirmationAndNegativeSignedMoney, FutureOperationDateAndForeignCareerAreRejected |
| Diario y caja legacy intactos | ExistingJournalAndLegacyResourcesRemainUnchanged |

Compilación Debug: cero errores y cero advertencias. Pase específico: **24 superadas, cero fallidas y cero omitidas**. Suite completa: **947 superadas, cero fallidas y cero omitidas**, duración **3 min 43 s**, código 0. No se implementaron pantallas; Godot headless no valida funciones presupuestarias sin consumidor y no se ejecuta en esta entrega.

### Límites y preparación de auditoría independiente

Código disponible en el árbol local, sin commit/push de F1.3. Revisar Budget.cs, SqliteBudgetStore.cs, Budgets001_Initial.sql y BudgetTests.cs contra esta especificación. La revisión independiente no se considera realizada por estas pruebas ni por este documento. No se ha enviado código a terceros automáticamente.

Especial atención para Claude: base de consumo y déficit; autorización de revisiones; identidad de asignaciones repartidas en periodos; atomicidad del recibo; conservación de cancelados; compatibilidad del formato v1; discrepancias entre JSON/totales; coste de lecturas del historial. Read actualmente carga las revisiones de una partida, sin caché; medir antes de grandes cargas.

Quedan fuera: política automática de salarios/prorrateos, obligaciones exigibles y pagos, reservas automáticas durante negociación, financiación, fiscalidad, insolvencia, importación de fotografías de clubes, avance temporal, IA, pantallas, adaptadores Staff/Mercado y CommitAcquisition F1.4. No se declara economía completa ni integración operativa del juego por pasar la suite. La regla de cada club con su situación individual queda preservada mediante fuentes y autorizaciones explícitas; no se generan cifras reales ni genéricas.

### Resultado de entrega

Núcleo F1.3 implementado y validado en su alcance técnico, listo para auditoría independiente; no se presenta esa auditoría como realizada. Debug sin errores/advertencias, 24 pruebas específicas y 947 de suite completa superadas. Integridad/FK, migración/rollback, reapertura, idempotencia y concurrencia cubiertas por los casos anteriores; regresión F1.0–F1.2 incluida en la suite. Sin defectos pendientes detectados en estos pases. La validación no demuestra simulación económica integral ni treinta temporadas.

Archivos F1.3: src/Furvo.Core/Finance/Budget.cs; src/Furvo.Persistence/SqliteBudgetStore.cs; src/Furvo.Persistence/Migrations/Budgets001_Initial.sql; tests/Furvo.Tests/BudgetTests.cs; docs/ECONOMY_F1_3_SPEC.md; docs/ECONOMY_F1_3.md. Permanecen sin confirmar para revisión. Único commit creado: a776f58, cierre documental F1.2. No push. git diff --check y comprobación de archivos nuevos sin incidencias. No partidas personales modificadas.

Comandos reproducibles desde la raíz:

```powershell
dotnet build Furvo.sln --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test Furvo.sln --no-build --no-restore -m:1 --filter "FullyQualifiedName~BudgetTests"
dotnet test Furvo.sln --no-build --no-restore -m:1
git diff --check
```