# Economía F1.1 — Importes exactos y operaciones idempotentes

Esta entrega implementa infraestructura independiente sobre la revisión base `3c9df44`. No está conectada a los flujos funcionales del juego. Reutiliza la conexión/transacción de Persistence, las migraciones embebidas y el patrón de token, revisión y replay ya empleado por Mercado, sin modificar sus repositorios ni los de Staff.

## Representación monetaria

`Furvo.Core.Finance.EuroAmount` es un valor inmutable expresado en céntimos `long`. `FromEuros` exige conversión exacta y `Euros` devuelve el decimal exacto. `FromCents` admite únicamente importes no negativos por defecto. `MoneySign.Signed` permite explícitamente signos negativos para futuros ajustes o saldos que los autoricen.

- 1,23 EUR y 1,230 EUR representan exactamente 123 céntimos.
- 1,239 EUR se rechaza, sin truncamiento o redondeo implícito.
- El rango técnico de almacenamiento es el del entero de 64 bits. Un importe positivo admite como máximo 92.233.720.368.547.758,07 EUR; el extremo negativo solo se admite en contexto Signed.
- La conversión comprueba multiplicación y cast con overflow. No aporta operadores aritméticos que puedan sumar o multiplicar sin política; las futuras agregaciones deberán comprobar su propio rango.
- El límite de representación no constituye un presupuesto o autorización de gasto. Las políticas existentes de contratos, Staff y Mercado mantienen sus límites propios.

El tipo es exclusivamente EUR. No sustituye las propiedades decimal existentes, el conversor histórico `SqliteEconomyStore.Cents` ni cantidades guardadas. La adopción por cada operación se hará en su entrega autorizada. No se añaden reglas de cálculo salarial ni de redondeo de fracciones de céntimo.

## Identidad y términos de una operación

`FinancialOperationRequest` contiene token Guid, SaveId, tipo versionado, origen de negocio y términos. Valida identidades y campos no vacíos. Su huella SHA-256 delimita carrera, tipo, origen y términos con una serialización de array independiente de la cultura.

Los términos son una representación canónica preparada por el futuro servicio de negocio. Deben incluir todos los datos que cambien el efecto, por ejemplo club, moneda, fecha efectiva, cantidades y revisión esperada. El registro no deduce ni valida reglas de contratación o pagos. Distingue los términos exactamente, incluyendo diferencias textuales; no equipara JSON con espacios u orden distintos. El servicio debe versionar y conservar su serialización para que un reintento use el mismo contenido.

La clave de negocio `(Kind, Origin)` impide registrar otra operación con un token distinto para el mismo hecho. Origin debe identificar el hecho completo dentro de la carrera, con ámbito suficiente, por ejemplo club/pedido o contrato/concepto/periodo. Los ID se generan en el límite del comando y se conservan al reintentar.

## Persistencia compartida

`SqliteFinancialOperations` recibe una conexión abierta y una transacción del llamador. Verifica que ambas correspondan y que la base sea la partida indicada. No abre conexiones ni confirma transacciones.

| API | Comportamiento |
|---|---|
| `Migrate(connection, transaction, career)` | Instalación explícita, aditiva y repetible de la extensión. Rechaza publicaciones iniciales y otras carreras. |
| `Find(connection, transaction, request)` | Recupera resultado o devuelve null. Una partida sin extensión se consulta sin instalar nada. Conflictos de identidad/origen/términos producen error. |
| `Execute(connection, transaction, request, execute)` | Devuelve replay si existe, o ejecuta el delegado y registra su resultado JSON en la misma transacción. Requiere extensión previamente instalada. |

El delegado representa la futura operación transaccional, por ejemplo apertura o adquisición. Solo puede realizar escrituras SQLite mediante la conexión y transacción recibidas, y debe devolver un resultado JSON serializable y versionado por su consumidor. No puede confirmar, cerrar o sustituir la transacción ni producir efectos externos como archivos, correos o pagos de red. Esos efectos no pueden protegerse con un rollback SQLite.

Antes del delegado se crea un SAVEPOINT. Un fallo del delegado, resultado JSON inválido o fallo de inserción revierte las escrituras de esa ejecución hasta el punto de restauración. El llamador puede continuar o deshacer su transacción. Si falla el propio acceso SQLite de modo que no pueda completarse esa recuperación, el llamador debe descartar la unidad de trabajo; nunca confirmar a ciegas después de un error de almacenamiento.

Usar transacción de escritura no diferida para serializar operaciones concurrentes. Con una transacción diferida SQLite puede devolver conflicto al convertir una lectura en escritura; el llamador debe deshacer y reintentar la solicitud original. No se añade un bucle de reintentos automático a esta infraestructura.

| Caso | Resultado |
|---|---|
| Primera ejecución válida | `Replayed=false`, efecto y resultado pendientes del commit exterior. |
| Misma solicitud en la transacción o tras commit | Resultado guardado con `Replayed=true`; no repite el delegado. |
| Token con otros términos, tipo u origen | Conflicto explícito, sin ejecución. |
| Otro token para mismo tipo/origen | Conflicto, sin duplicación del hecho. |
| Fallo antes de registrar el resultado | Rollback al savepoint, sin recibo ni efectos parciales de esa ejecución. |
| Rollback exterior después de Execute | Desaparecen efecto y resultado juntos. |
| Commit confirmado y respuesta perdida | Tras reabrir, Execute/Find recuperan el resultado confirmado. |

`FinancialOperationResult` es un resultado durable únicamente tras commit exterior. La infraestructura no presenta una ejecución sin confirmar como un pago realizado. Tampoco registra como confirmados los intentos fallidos.

## Extensión SQLite y compatibilidad

`FinancialOperations001_Initial.sql` añade únicamente `financial_operation_schema` y `financial_operations`, con versión independiente 1, token único, origen de negocio único, huella y resultado JSON. Los triggers impiden actualizar o eliminar recibos confirmados. El recibo se conserva en el archivo de carrera, con aislamiento comprobado por SaveId.

No altera `PRAGMA user_version`, tablas de mundo, contratos, economía, Staff o Mercado. El SQL ya queda incluido por el patrón `Migrations/*.sql` del proyecto, sin cambiar paquetes o configuración. La instalación utiliza la transacción del llamador y también puede revertirse. Las versiones desconocidas y extensiones incompletas se rechazan.

La extensión no se ejecuta al arrancar, consultar, cargar o crear una carrera mediante los flujos existentes. Esta entrega solo la instala en copias temporales durante pruebas y expone el punto de entrada para una integración futura explícita. La lectura de partidas anteriores devuelve ausencia de operación, sin migrar ni atribuir operaciones retrospectivas.

## Validación

Las pruebas monetarias verifican ida/vuelta exacta, céntimos, precisión adicional convertible, fracciones rechazadas, negativos por contexto y extremos/desbordamientos. Las pruebas SQLite verifican repetición, conflicto, origen duplicado, rollback exterior y local, recuperación tras commit, JSON inválido, migración reversible, versión desconocida, carreras independientes y dos escritores concurrentes.

Otra regresión aplica la extensión a una carrera temporal económica creada con contrato inicial del mánager. Reabre con el cargador existente y comprueba identidad, fecha, temporada, condiciones contractuales, recursos, integridad y FK, además del hash intacto de la publicación de origen.

Compilación Debug ejecutada con `dotnet build Furvo.sln --no-restore -m:1 -p:UseSharedCompilation=false`, completada con **0 errores y 0 advertencias**.

Suite completa Debug ejecutada sin filtros con `dotnet test Furvo.sln --no-build --no-restore -m:1`, completada con **876 superadas, 0 fallidas y 0 omitidas**, duración **5 min 48 s**. Incluye 24 casos nuevos frente a los 852 anteriores, repartidos en 13 casos monetarios y 11 de operaciones/persistencia. El primer pase aislado de 21 casos también pasó antes de incorporar las últimas tres regresiones, que quedan incluidas en la suite completa final.

La migración se probó únicamente en bases temporales. La prueba sobre carrera económica pasó con condiciones contractuales y recursos preservados, `integrity_check = ok` y cero infracciones de FK. La representación Godot no cambia en esta entrega y no se realizó una validación visual nueva.

## Archivos y siguiente entrega

- Core añade `Finance/EuroAmount.cs` y `Finance/FinancialOperationRequest.cs`.
- Persistence añade `SqliteFinancialOperations.cs` y `Migrations/FinancialOperations001_Initial.sql`.
- Pruebas añade `EuroAmountTests.cs` y `FinancialOperationTests.cs`.
- Este documento registra uso, límites y validación de F1.1.

F1.2 sigue necesitando aprobación de apertura, procedencia de saldos, diario y conciliación. Esta base no registra asientos, obligaciones o movimientos de tesorería, no calcula presupuestos disponibles y no modifica `CommitAcquisition`. La integración de una operación económica real continúa pendiente de su entrega. Los documentos de diseño y auditoría anteriores se conservan íntegros.
