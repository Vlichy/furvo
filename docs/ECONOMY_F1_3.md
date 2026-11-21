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
