# Economía F1.3 — Inspección previa a implementación

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