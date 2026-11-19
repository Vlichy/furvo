# Economía F1.2 — Signos, pérdidas y situaciones financieras adversas

Aclaración de diseño registrada el 18/11/2026 sobre `main`, HEAD `fa1ba9a`, que ya incluye F1.1. Recoge las decisiones expresas del usuario y se conserva como base de la implementación F1.2 autorizada posteriormente en el encargo adjunto. La arquitectura y evidencia de esa implementación se añaden desde el apartado 9. Financiación e insolvencia siguen pendientes.

## 1. Criterio de dominio seleccionado

### Revisión previa a la implementación de F1.2

El nuevo encargo autoriza el diario mínimo y su apertura. Antes de programar se contrastaron los diseños con `SqliteEconomyStore`, `SqliteFinancialOperations`, `SqliteCareerStore.Load`, `CareerWorkspace`, Staff y Mercado. Se identificaron estos límites y decisiones de transición.

- La caja antigua pertenece a una fila club/temporada y `CommitAcquisition` todavía la modifica directamente. El diario se instalará explícitamente y se abrirá por club desde un corte identificado. Será autoridad únicamente de su ámbito contable de observación; el juego mantendrá el escritor antiguo hasta F1.4. La diferencia con la caja antigua se informará por separado de la conciliación interna.
- El modelo nuevo tendrá una única proyección por club, continua por fecha y sin otra apertura por temporada. No cambia Load ni Workspace, que siguen limitando la transición completa de carrera. La continuidad del diario se probará con fechas controladas.
- Un saldo de caja no acredita patrimonio completo. La apertura usará una cuenta técnica explícita, sin presentarla como ingreso o patrimonio real investigado. Caja cero abrirá el ámbito sin inventar líneas de importe cero.
- Para impedir cabeceras o líneas persistidas a medias se propone una fila de asiento confirmado con todas sus líneas JSON, restricciones y vista de apuntes. El modelo de dominio mantiene cabecera y líneas. Solo el documento completo se inserta, dentro de la transacción del consumidor.
- El catálogo incluye cuentas de resultado y patrimonio con saldos de signo libre. Las cantidades Debe/Haber serán positivas. No se activan financiación, obligaciones operativas o procedimientos concursales.
- El manifiesto central de extensiones aportaría un índice global, pero requiere adaptar todos los servicios. F1.2 usará versionado local y rechazo de extensiones parciales, reutilizando F1.1. No se añade ese manifiesto por fuera del alcance.

La reconstrucción consultable de caja se derivará del diario. Una discrepancia interna bloqueará nuevos apuntes en ese ámbito, sin ajustar cifras automáticamente. Activación global, saneamiento de proyecciones de usuario, estados patrimoniales iniciales completos y cambios del calendario necesitan sus decisiones e integración posteriores.

Furvo debe admitir pérdidas, patrimonio neto negativo, sobreendeudamiento, problemas de liquidez e impagos. El sistema futuro podrá representar reestructuración e insolvencia cuando se definan sus procedimientos. Los signos se validan según la naturaleza del dato, sin una regla global que exija que todos los indicadores sean no negativos.

| Magnitud | Signo y representación requeridos | Consecuencia para el diseño |
|---|---|---|
| Resultado económico | Positivo, cero o negativo | Ingresos devengados menos gastos reconocidos. Una pérdida válida se conserva y se muestra. |
| Patrimonio neto | Positivo, cero o negativo | Activos menos pasivos, cuando esos datos estén acreditados y completos. El balance sigue cuadrando con patrimonio negativo. |
| Tesorería efectiva | Fondos disponibles; no se autorizan pagos sin cobertura | La financiación o el descubierto necesitan contrato, autorización y registro propios antes de ampliar la capacidad de pago. |
| Deuda y obligaciones | Importe adeudado positivo, con acreedor, causa y vencimiento | El pendiente no se vuelve negativo para simular falta de liquidez. Rectificaciones y liquidaciones se enlazan al documento original. |
| Presupuesto y disponible | El déficit, la desviación y el exceso de compromisos pueden ser negativos | La insuficiencia se registra y presenta. La decisión de autorizar gasto adicional se evalúa por una política separada. |
| Contratos | Salarios, precios y primas con el signo válido de su concepto | Los problemas financieros se representan en cuentas, obligaciones y capacidad, sin salarios o precios contractuales negativos ficticios. |

Un club puede tener caja positiva y patrimonio neto negativo, o beneficio contable y falta de liquidez. Tampoco basta un único saldo o ratio para declarar un procedimiento de insolvencia. El texto concursal distingue el incumplimiento regular de obligaciones exigibles y contempla mecanismos de reestructuración. La traducción a reglas jugables necesita una entrega posterior con versión normativa y alcance definidos. [Texto refundido de la Ley Concursal, artículo 2 y libro segundo](https://www.boe.es/eli/es/rdlg/2020/05/05/1/con).

## 2. Revisión del código actual

### Restricción de caja

`src/Furvo.Persistence/Migrations/Economics001_Initial.sql` declara `cash_cents INTEGER NOT NULL CHECK(cash_cents>=0)`. `SqliteSportsStore.CommitAcquisition` condiciona el cargo a que el saldo cubra compra e instalación. Ambas reglas corresponden a la tesorería operativa actual sin financiación general.

Se conserva el CHECK. No representa resultado, patrimonio, presupuesto neto, deuda o solvencia y no debe utilizarse para validarlos. F1.2 deberá mantener esa separación al definir el diario y la proyección de caja. Quitar el CHECK por sí solo permitiría gastar dinero inexistente sin identificar acreedor, contrato o límite de crédito.

### Otras restricciones que no deben generalizarse

El mismo esquema impide un `transfer_remaining_cents` negativo. `ClubEconomicPlan.Validate` también exige valores no negativos en el catálogo inicial y limita su capacidad salarial de planificación. Esas invariantes validan la publicación inicial aprobada y ciertos recursos históricos; no constituyen el modelo universal de resultado o de presupuesto durante una carrera.

El futuro saldo presupuestario derivado debe admitir un exceso de compromisos y conservar su signo. No se utilizará el campo histórico no negativo como única fuente de ese indicador ni se aplicará `Math.Max(0, disponible)` para ocultar déficit. El diseño definitivo decidirá su proyección y compatibilidad sin reescribir los planes iniciales.

`EuroAmount` de F1.1 permite negativos explícitamente mediante `MoneySign.Signed`. El contexto no negativo por defecto protege cantidades que lo requieren, pero no prohíbe balances o resultados negativos. Antes de reutilizarlo, cada futuro campo debe declarar su contexto. La exactitud en céntimos y el control de overflow se mantienen con ambos signos.

## 3. Integridad del diario en F1.2

Se mantiene la propuesta de diario interno mínimo equilibrado. Las líneas pueden guardar magnitudes positivas y dirección Debe/Haber explícita. El saldo de una cuenta, el resultado, el patrimonio y las variaciones derivadas pueden tener signo. Una corrección se representa mediante documento y contrapartida enlazada, preservando la operación inicial.

Los controles del diario serán equilibrio, identidad de operación, procedencia, moneda, exactitud monetaria, pertenencia a la carrera y ausencia de duplicados. No exigirán que cada saldo de cuenta o indicador resultante sea positivo.

El resultado se derivará de ingresos y gastos reconocidos. El patrimonio se derivará de activos y pasivos con cobertura de datos explícita. La igualdad Activo = Pasivo + Patrimonio neto admite que este último sea negativo. La igualdad contable no se obtiene creando un activo o ingreso ficticio que sanee al club.

Si faltan datos para reconstruir todo el patrimonio inicial, se preservará esa incertidumbre. Una contrapartida técnica de apertura deberá identificarse como tal y no presentarse como capital aportado, beneficio o patrimonio real comprobado. La caja conocida no permite deducir por sí sola la situación patrimonial completa.

F1.2 preparará este comportamiento del núcleo y sus pruebas. La presentación de un resultado o balance completos requerirá las fuentes de apertura y las políticas de reconocimiento de las fases siguientes.

## 4. Descubierto, financiación e impago

La futura financiación deberá identificar entidad acreedora, contrato, límite autorizado, importe dispuesto, moneda, fechas, coste e incumplimientos. Un límite sin disponer no incrementa la caja ni constituye ingreso. Una disposición confirmada genera fondos y deuda enlazados, con operación idempotente y asientos equilibrados.

Como propuesta compatible con la caja actual, un descubierto autorizado podrá representarse mediante fondos dispuestos y pasivo financiero separado. Si una fuente bancaria presenta saldo negativo, se conservará ese dato de origen y su clasificación como deuda en la normalización; no se perderá mediante un recorte a cero. La presentación bancaria y el saldo operativo requieren una regla explícita que evite contar dos veces la disposición.

Esta propuesta no activa descubierto en F1.2. Mientras no exista financiación modelada y autorizada, un pago sin fondos se rechaza y su obligación permanece pendiente o vencida, con el motivo y el vencimiento conservados. Ni el rechazo del pago ni el impago borran el gasto devengado, la deuda o el contrato.

Las quitas, esperas, refinanciaciones, conversiones y resoluciones futuras necesitarán un acto acreditado y efectos trazables sobre cada obligación afectada. No se ejecutarán por saldo negativo, por un flag genérico de riesgo o por una compensación registrada sin causa extintiva. Las consecuencias económicas y deportivas exigirán reglas propias.

## 5. Base documental de los estados iniciales futuros

La situación inicial de cada club se investigará posteriormente con un ejercicio identificado y fecha de cierre, diferenciando cuentas individuales y consolidadas. Las cifras conservarán moneda, unidad de publicación, concepto y tratamiento de comparativas o reformulaciones. Las cantidades en miles o millones deben normalizarse de forma explícita a céntimos sin atribuir una precisión inexistente a la fuente.

Cada dato deberá registrar club ficticio e ID, referencia real interna, entidad y perímetro, ejercicio, documento, URL, página/nota, fecha de consulta y estado de evidencia. Se distinguirán importe comprobado, estimación con método e hipótesis, y dato desconocido. Cero es un valor acreditado, no un sustituto de desconocido. Los supuestos de adaptación al universo Furvo permanecerán diferenciados del importe publicado.

Las cuentas anuales auditadas aportarán el punto de partida por club. Los informes agregados de LaLiga servirán para contexto y contraste, sin repartir automáticamente sus totales entre clubes. Las reglas de control económico se versionarán y se distinguirán del saldo bancario y del resultado contable. Su normativa diferencia presupuestos de ingresos/gastos, inversión, financiación y tesorería. [Normas de Elaboración de Presupuestos, versión 19/11/2025, artículos 14–17](https://assets.laliga.com/assets/2025/11/20/originals/c206fa02a74a29a3ce4b7c21faac3b89.pdf).

Esta tarea no recalcula los veinte estados iniciales ni importa cifras reales en publicaciones o carreras. Su eventual revisión requerirá propuesta de datos, ejercicio de referencia, aprobación y publicación versionada compatible. No se introducirán aportaciones positivas para cuadrar pérdidas o patrimonio negativo.

## 6. Pruebas que deberá cubrir F1.2

| Caso de prueba aislado | Criterio de aceptación |
|---|---|
| Ingresos inferiores a gastos | Resultado negativo exacto y asientos equilibrados, sin rechazo por signo. |
| Pasivos superiores a activos | Patrimonio neto negativo representable, sin activo de saneamiento inventado. |
| Caja positiva y patrimonio negativo | Ambas magnitudes se conservan separadas y conciliadas. |
| Obligación exigible superior a caja | Caja no negativa, pago sin confirmar y deuda íntegra pendiente. El ciclo completo de obligación se verificará cuando exista su servicio. |
| Compromisos superiores al autorizado | Disponible negativo visible, sin recorte a cero ni alteración de contratos. |
| Contrato con importe inválido | Rechazo según el concepto, independientemente de la situación financiera del club. |
| Ajuste o contrapartida | Saldo con signo correcto, origen preservado y operación idempotente. |
| Apertura incompleta | Datos desconocidos y contrapartida técnica claramente identificados; no se declara patrimonio completo. |
| Financiación no modelada | No permite gastar por un límite supuesto. Las disposiciones autorizadas se probarán en su fase posterior. |

Estas son pruebas planificadas. Se aplicarán al alcance efectivamente implementado del núcleo F1.2. No se acreditan como ejecutadas en esta tarea documental.

## 7. Límites y fases posteriores

F1.2 se mantiene centrada en apertura conciliada, diario mínimo y saldo materializado. Debe admitir los signos por concepto y preservar pérdidas o patrimonio negativo acreditados. No exige implementar un balance patrimonial completo para empezar el diario, pero tampoco podrá presentar uno completo con datos insuficientes.

Quedan pendientes las políticas completas de devengo y cierre, nóminas e ingresos recurrentes; cálculo e interpretación regulatoria de indicadores; modelización de financiación/descubierto; clasificación de deuda y vencimientos; procedimientos de reestructuración o insolvencia; decisiones IA y consecuencias económicas o deportivas. La investigación de los estados iniciales por club es otra entrega de datos, no una normalización automática de las partidas existentes.

Las referencias anteriores a caja no negativa continúan limitadas a pagos y cobertura. Cualquier interpretación previa de que resultado, patrimonio o disponible presupuestario deben ser siempre no negativos queda sustituida por esta aclaración.

## 8. Fuentes y alcance de su comprobación

Referencias oficiales consultadas el 18/11/2026. El BOE se usa para distinguir el criterio concursal de la simple existencia de pérdidas y para identificar mecanismos futuros; F1.2 no implementa un procedimiento legal. [Ley Concursal consolidada, artículo 2 y libro segundo](https://www.boe.es/eli/es/rdlg/2020/05/05/1/con).

LaLiga mantiene un índice oficial de normas y publica la versión de presupuestos utilizada como referencia documental. La versión y los artículos aplicables deberán fijarse al concretar reglas del simulador. [Normativa de LaLiga](https://www.laliga.com/transparencia/normativa).

El índice oficial de informes económicos permite identificar el ejercicio y localizar la fuente agregada. No se ha extraído ni validado en esta tarea una base de cifras de clubes. [Informes económico-financieros de LaLiga](https://www.laliga.com/transparencia/gestion-economica/informe-economico-financiero).

El registro oficial de cuentas de un club es un ejemplo de localización de documentación anual y de auditoría; la selección de ejercicio y extracción se realizará para cada entidad. [Registro de cuentas del FC Barcelona](https://www.fcbarcelona.es/es/ficha/941627/fc-barcelona-transparencia-registro-de-cuentas).

La herramienta no pudo abrir íntegramente los PDF consultados de la memoria económica 2024/25 del FC Barcelona y del informe LaLiga 2024/25 por su tamaño. No se atribuye a esta revisión una lectura completa de esos documentos ni verificación de sus cifras. Sí se consultaron el texto consolidado del BOE, la normativa presupuestaria enlazada y los índices oficiales anteriores.

En la aclaración documental previa solo se creó este documento y no se ejecutaron pruebas nuevas. La evidencia histórica de F1.1 fue de 876 superadas, cero fallidas y cero omitidas. Los resultados de la implementación posterior se distinguen a continuación.

## 9. Arquitectura implementada de F1.2

Core añade `JournalEntry`, `JournalLine`, `JournalAccount`, `JournalMovement` y `CashReconciliation`. Un documento exige identidad de asiento y operación, club, fecha económica, referencia, al menos dos líneas no vacías y totales Debe/Haber iguales. Los apuntes usan `EuroAmount` en céntimos, validan el signo por concepto y comprueban overflow de totales. `Balance` expone saldo Debe menos Haber con signo; no impone positividad a resultados o patrimonio.

Persistence añade `SqliteJournal`. Todas sus escrituras reciben conexión y transacción del consumidor. `Open` y `Post` construyen solicitudes canónicas y delegan el replay, conflicto y SAVEPOINT en `SqliteFinancialOperations`. Solo se amplía la visibilidad interna de sus verificaciones de carrera/esquema para reutilizarlas. El diario no crea otro mecanismo de idempotencia ni confirma la transacción exterior.

Godot y los flujos existentes de Staff, Mercado, Carrera y adquisiciones mantienen su implementación. No hay migración al cargar, consultar, crear una carrera por el flujo habitual o arrancar el juego. El punto de entrada nuevo solo se invoca explícitamente; las pruebas usan archivos temporales.

### Catálogo mínimo de cuentas

| ID | Cuenta | Finalidad y semántica |
|---|---|---|
| 1 | Cash | Tesorería del ámbito implantado. Su proyección exige fondos no negativos. |
| 2 | OpeningBalance | Contrapartida técnica de la caja acreditada en el corte. Reservada a la apertura. No es ingreso ni patrimonio real investigado. |
| 3 | Receivables | Futuros derechos de cobro. Su saldo permite distinguir devengo y cobro. |
| 4 | Payables | Futuras obligaciones. Los documentos que las produzcan requerirán su propio servicio. |
| 5 | Income | Ingresos reconocidos cuando existan hechos y reglas. La apertura no utiliza esta cuenta. |
| 6 | Expense | Gastos reconocidos. Permite representar pérdidas con cuentas equilibradas. |
| 7 | Assets | Activos o inversiones cuyo tratamiento se apruebe. |
| 8 | FinancialDebt | Financiación futura, independiente de ingreso o caja disponible supuesta. |
| 9 | Equity | Patrimonio contabilizado, con posibilidad de saldo de naturaleza negativa. |

El catálogo permite probar invariantes sin crear reglas de negocio de nómina, ingreso, préstamo o inversión. Las pruebas con gasto y patrimonio usan documentos sintéticos aislados, no cifras atribuidas a clubes. Un saldo de Equity o de OpeningBalance no certifica por sí solo el patrimonio completo del club.

## 10. Esquema y garantías de persistencia

`Journal001_Initial.sql` instala explícitamente una extensión local de versión 1, aditiva y compatible con el esquema del mundo existente. Reutiliza la extensión de operaciones de F1.1 como dependencia.

| Objeto | Contenido y controles |
|---|---|
| `journal_schema` | Versión independiente. Ausencia reconocida; extensión parcial o versión desconocida rechazadas. |
| `journal_accounts` | Catálogo mínimo cerrado; cuentas existentes inmutables. |
| `journal_scopes` | Un corte por club, temporada de la fila origen, saldo de apertura, caja materializada y referencia a operación/asiento de apertura. Identidad y corte inmutables. |
| `journal_entries` | Cabecera, ID estable, secuencia, club, fecha, operación, referencia, indicador de apertura, contraasiento original opcional y líneas JSON completas. |
| `journal_lines` | Vista de cada apunte, con identidad de asiento y club heredada de su cabecera. No permite editar una línea por separado. |

El documento de asiento es una única fila atómica. No se almacenan borradores en este diario ni cabeceras huérfanas de sus líneas. Las validaciones Core y los triggers SQLite exigen líneas con cuenta válida, cantidades enteras, signo y exclusividad Debe/Haber correctos, y suma equilibrada. SQLite rechaza también sumas fuera del rango entero. Los triggers impiden actualizar o borrar un asiento confirmado.

Las FK enlazan ámbito y club, operación registrada y contraasiento del mismo club. Las referencias a recibos F1.1 son diferidas hasta el commit exterior porque el delegado inserta primero el documento completo y F1.1 registra después su resultado. Confirmar un asiento sin recibo válido falla en el commit y no deja cambios durables. La apertura positiva exige además referencia al asiento de apertura dentro de la misma transacción.

Tras insertar un documento, un trigger actualiza su proyección de caja; un cargo que la haga negativa o desborde su dominio revierte la escritura. Los asientos posteriores mantienen orden económico no decreciente, evitando reescribir retroactivamente los fondos disponibles históricos. La política de apuntes retroactivos requiere una entrega específica.

El manifiesto central de extensiones sigue aplazado. La detección local comprueba versión, objetos del diario y dependencia F1.1. Un manifiesto global requeriría coordinar todos los servicios y no aporta una garantía adicional imprescindible para este alcance.

## 11. Apertura y transición de autoridades

`Open` exige SaveId, ClubId, temporada de la fila económica origen, fecha de corte y token persistente. Lee la caja actual acreditada de esa fila en EUR bajo la misma transacción. Un club sin información económica recibe un error específico y no obtiene saldo de sustitución.

Si la caja es positiva, se registra Debe Cash / Haber OpeningBalance por el importe exacto. Si es cero, se guarda el ámbito y recibo idempotente sin insertar un asiento vacío o líneas de importe cero. La nueva apertura no modifica `club_economics.cash_cents`, planes, presupuestos o contratos. El ID del asiento positivo coincide con el token de su apertura, por lo que permanece estable.

El token repetido con los mismos parámetros recupera el resultado de apertura, incluso tras una variación posterior de la caja antigua. Otro token para ese mismo club produce conflicto de origen y no vuelve a crear dinero. Cambiar fecha o ejercicio origen con el token anterior también es conflicto. La apertura está vinculada al club y no a cada temporada; no hay traslado monetario al comenzar otro ejercicio.

La fase actual es un **ámbito explícito de observación**. La caja del sistema anterior sigue siendo la que usa el juego. El diario es fuente de verdad únicamente para sus propios apuntes desde el corte, y `journal_scopes.cash_cents` es su proyección materializada. Sus consultas y apuntes técnicos no sustituyen las operaciones del juego. Esta distinción impide introducir un escritor financiero paralelo activo antes de F1.4.

Los cargos anteriores al corte quedan incluidos en la caja de apertura y no se reproducen como movimientos nuevos. Los que realice después el escritor antiguo no se importan automáticamente ni se señalan como corrupción interna del diario. Antes de activar este diario como autoridad global habrá que integrar ese escritor y resolver cualquier periodo de observación de forma explícita, sin sumar cargos dos veces.

## 12. Consulta y conciliación

| API interna | Garantía |
|---|---|
| `Migrate` | Instalación explícita en la transacción del consumidor. Rechaza otras carreras/publicaciones mediante F1.1. |
| `Open` | Apertura única e idempotente desde la fila económica identificada. |
| `Post` | Asiento completo, caja conciliada antes de escribir, operación y resultado bajo SAVEPOINT compartido. |
| `Movements` | Histórico estable por fecha económica y secuencia, con filtro opcional de fecha. Sin instalar extensiones. |
| `Balance` | Saldo de cuenta reconstruido desde apuntes hasta una fecha, con signo y control de rango. Fecha anterior al corte desconocida, no cero ficticio. |
| `Reconcile` | Compara caja del diario con su proyección y devuelve por separado la caja antigua y si varió desde la apertura. |
| `SqliteFinancialOperations.Find` | Resultado registrado por token y términos; evita otro API duplicado de recibos. |

`CashReconciliation.Balanced` compara únicamente diario y proyección del ámbito. `LegacyChanged` indica variación del saldo antiguo respecto al importe observado al abrir; no significa que ese movimiento pertenezca al diario ni que deba aplicarse otra vez. La comparación utiliza la fila antigua identificada, sin asumir que todas las temporadas representan cajas distintas.

La reconstrucción devuelve el saldo calculado, no sobrescribe una proyección discrepante. Una discrepancia interna bloquea nuevos apuntes; su saneamiento persistente requerirá una operación explícita y validación del historial y su origen. F1.2 no añade un botón o proceso de reparación automática.

## 13. Transacciones y correcciones

La operación de negocio debe abrir y confirmar una transacción de escritura. El diario participa en ella. F1.1 protege la ejecución con SAVEPOINT y solo devuelve un resultado durable si el consumidor confirma después. Si falla la operación exterior, asiento, proyección y recibo revierten juntos. Tras perder la respuesta de un commit confirmado, el reintento recupera el resultado y no vuelve a registrar el asiento.

`JournalEntry.Reverse` prepara un documento con Debe/Haber invertidos y referencia al original. `Post` y SQLite comprueban que corresponde a un asiento ordinario del mismo club, que no precede a su fecha y que invierte sus líneas completas. Una referencia de contraasiento solo se utiliza una vez. La corrección preserva el original. Corregir la apertura técnica, efectuar rectificaciones parciales, definir permisos o motivos comerciales y realizar ajustes retroactivos quedan para su diseño posterior.

No hay un paso persistente de «primera línea» seguido de otras: las líneas son un documento completo. La prueba de fallo tras evaluar la primera línea inyecta un error de validación antes de aceptar ese documento. Otra prueba falla después de insertarlo. En ambos casos se verifica que no hay estado parcial durable. Esta es la equivalencia con el escenario de fallo intermedio solicitado para una representación normalizada.

## 14. Continuidad y compatibilidad

El diario no tiene temporada como clave de caja. Las pruebas con fecha 30/06 y 01/07 siguientes conservan saldo e historial sin duplicar apertura. Esto no implementa presupuesto, cierre contable comercial ni rollover de la carrera.

`SqliteCareerStore.Load` continúa exigiendo veinte recursos de una temporada y vinculando la oferta inicial a ella; `CareerWorkspace` también conserva su restricción. Se mantienen sus reglas hasta la entrega de calendario/estado vigente. La prueba de reapertura de carrera verifica el caso actual compatible, no una transición de temporada que el resto del sistema aún bloquea.

Partidas anteriores a F1.1 y partidas con operaciones pero sin diario pueden consultarse devolviendo ausencia de ámbito o movimientos. La instalación se realiza en copias aisladas y puede revertirse transaccionalmente. Una dependencia parcial se rechaza con error explícito. Las publicaciones originales y sus datos económicos no se escriben.

## 15. Archivos de esta implementación

- `src/Furvo.Core/Finance/JournalEntry.cs` contiene reglas y contratos de dominio.
- `src/Furvo.Persistence/SqliteJournal.cs` contiene la API transaccional y de consulta.
- `src/Furvo.Persistence/Migrations/Journal001_Initial.sql` contiene la extensión aditiva y sus controles.
- `src/Furvo.Persistence/SqliteFinancialOperations.cs` reutiliza las verificaciones existentes con visibilidad interna, sin cambiar su API pública o flujos.
- `tests/Furvo.Tests/JournalTests.cs` contiene los casos aislados y la regresión de carrera real de prueba.
- Este documento conserva la aclaración previa y registra arquitectura, transición y evidencia de F1.2.

## 16. Resultados de validación

La compilación Debug con `dotnet build Furvo.sln --no-restore -m:1 -p:UseSharedCompilation=false` terminó con cero errores y cero advertencias. El pase específico final de diario y reapertura de carrera superó **27 pruebas, cero fallidas y cero omitidas**. El primer pase de 24 casos también pasó, antes de añadir las tres regresiones finales.

La suite completa Debug ejecutada sin filtros mediante `dotnet test Furvo.sln --no-build --no-restore -m:1` terminó con **903 superadas, cero fallidas y cero omitidas**, duración **3 min 42 s**. Son las 876 anteriores más los 27 casos de F1.2. Las pruebas de integridad y reapertura forman parte de esa ejecución; SQLite devolvió `integrity_check = ok` y cero infracciones de FK en la copia contable comprobada.

El recorrido existente de Godot 4.7.2 .NET se ejecutó con `--headless --path src/Furvo.Godot --script res://../../tools/GodotUiSmoke.gd -- --staff-hiring` y terminó con código 0. Verificó arranque, navegación, ficha, contratación de prueba y carreras aisladas mediante sus aserciones existentes. Es una prueba headless, no una revisión visual nueva ni una pantalla financiera implementada.

## 17. Dependencias y límites restantes

F1.3 debe implementar presupuestos/compromisos por periodo y autorización de capacidad, preservando déficits e información incompleta. F1.4 debe integrar la adquisición existente con el diario y definir la activación de su autoridad global. Los documentos de apertura no acreditan estados patrimoniales investigados de los veinte clubes.

Quedan pendientes calendario y cierres reales, nóminas, obligaciones/pagos operativos, reglas de ingresos, préstamos, descubierto, insolvencia, contratación efectiva de Staff y nuevas infraestructuras. La investigación de fuentes reales y el modelo completo de cuentas anuales continúan fuera del alcance de esta implementación.

## 18. Matriz de cierre

| Requisito | Implementado | Probado | Limitaciones |
|---|---|---|---|
| Partida doble exacta y equilibrada | Sí | Sí, dominio y restricciones SQLite | Catálogo interno mínimo en EUR. |
| Ausencia de líneas parciales persistidas | Sí | Sí, rechazo de documentos incompletos y fallos durante su validación/inserción | Líneas JSON en una fila atómica, con vista de apuntes. |
| Inmutabilidad de asientos | Sí | Sí, intentos de UPDATE/DELETE y edición de la vista rechazados | No sustituye permisos de negocio futuros. |
| Preparación de contraasientos | Sí | Sí, original conservado y caja restablecida | Inversión íntegra de un asiento ordinario; sin corrección comercial completa. |
| Apertura segura e idempotente | Sí | Sí, importe acreditado, repetición y conflictos | Exige fila económica EUR identificada y corte explícito. |
| Caja cero y datos ausentes | Sí | Sí | Cero abre sin asiento vacío; ausencia de datos no inventa saldo. |
| Caja materializada y conciliación | Sí, en el ámbito abierto | Sí, igualdad y discrepancia inducida | Reconstrucción de consulta; sin saneamiento automático. |
| Transacción y recibo F1.1 compartidos | Sí | Sí, rollback, SAVEPOINT, replay y fallo al commit sin recibo | El consumidor confirma; no hay commit independiente. |
| Transición con el sistema anterior | Sí, observación separada | Sí, cargo antiguo detectado sin duplicación | Autoridad global e integración de CommitAcquisition pendientes de F1.4. |
| Pérdidas y patrimonio con signo | Sí, representación contable | Sí, casos sintéticos equilibrados | No acredita un balance patrimonial completo de cada club. |
| Continuidad entre fechas de temporadas | Sí, caja contable continua | Sí, 30/06 y 01/07 sin nueva apertura | No habilita rollover de carrera, presupuestos o calendario. |
| Compatibilidad de extensiones | Sí | Sí, sin F1.1, con F1.1, con diario y extensión parcial | Instalación explícita; no migración de partidas personales. |
| Aislamiento e integridad | Sí | Sí, clubes, reapertura, integridad y FK | Cada carrera conserva su archivo propio. |
| Staff, Mercado y Carrera | Conservados | Sí, regresión específica y suite completa | Las restricciones actuales de temporadas siguen documentadas. |
| Compilación Debug y suite completa | Sí | 903 superadas, 0 fallidas, 0 omitidas; compilación sin errores/advertencias | Release no solicitado en esta entrega. |
| Arranque y recorrido Godot | Conservados | Sí, headless con código 0 | Sin validación visual nueva. |
