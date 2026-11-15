# Mercado — Propuestas de compra y negociación entre clubes
Fecha: 05/11/2026.

## Resultado y alcance por fases
Se implementa una base persistente de negociación entre clubes sobre el buscador existente. Una propuesta enviada, contraoferta o acuerdo nunca ejecuta un fichaje ni modifica un contrato deportivo, una plantilla o un saldo.

| Fase | Estado real |
|---|---|
| A — Fundamentos | Modelo, reglas, referencias, transacciones, revisión optimista, historial inmutable y migración por carrera implementados. |
| B — Ofertas | Formulario, importe fijo, plazos, intercambios, variables y cláusulas opcionales, resumen, borrador, envío confirmado, reapertura y retirada implementados. |
| C — Negociaciones | Historial, estados, versiones y punto de integración de respuestas verificado mediante fixtures. No existe proveedor de respuestas en el juego; ninguna respuesta automática se simula. |
| D — Derechos económicos | Cláusulas propuestas persistidas con sus versiones. Modelo preparatorio de derechos de cobro, propuestas/acuerdos de modificación y reducción parcial/total probado en dominio. No se crean derechos vigentes ni se habilita su adquisición en la carrera. |
| E — Economía e IA | Bloqueada por dependencias: ejecución financiera transaccional, operaciones de origen, normativa de inscripción/ventanas, evaluación fundamentada y negociación personal. |

No se declara completa la compra de un futbolista ni la adquisición operativa de derechos. El contrato con jugadores, salarios, primas y representantes queda fuera del alcance.

## Auditoría y dependencias encontradas
Existían contratos de jugador con club/fechas, identidades estables, pertenencias deportivas por carrera y cantera, 51 atributos públicos, buscador externo y preferencias de columnas por posición. La economía contiene tesorería, presupuestos y previsiones aprobadas, con compromiso específico del entrenador. No es un libro mayor de obligaciones de traspasos.

No existen valoración o precio solicitado de jugadores, fichajes completados, derechos retenidos de operaciones anteriores, ventanas verificadas, contadores deportivos evaluables, proveedor de decisiones negociadoras ni cierre de contratos de futbolistas. No se inventan esos datos.

La interfaz muestra los recursos conocidos y advierte que no validan la viabilidad financiera completa. No reserva presupuesto ni descuenta el importe de una oferta. Si una carrera antigua no tiene extensión económica, la moneda procede de ContractCountryPolicies, según el país del club, y los recursos permanecen No disponible. No se inventa un presupuesto ni se convierte moneda.

## Interfaz implementada
Mercado mantiene Buscar jugadores y añade Mis ofertas, Negociaciones y Derechos económicos. Hacer oferta se integra en la ficha pública reutilizada; no se reconstruye el buscador ni se altera su configuración de columnas. El club propio continúa excluido de la búsqueda.

El formulario tiene nueve pestañas con desplazamiento:
1. Futbolista y recursos: identidad actual, posición, edad, club y fechas contractuales conocidas; valor estimado y precio solicitado no disponibles.
2. Importe fijo: decimal monetario y declaración expresa de propuesta gratuita cuando proceda.
3. Pagos: uno o varios importes/vencimientos, orden de pago, estado Propuesto sin ejecutar y comprobación de suma.
4. Jugadores incluidos: selección múltiple de jugadores propios de primer equipo y cantera, con ID estable, nombre, posición, edad, equipo y contrato conocido.
5. Variables: objetivos tipados, umbral, importe, periodo, máximo de activaciones, acumulación y competición existente de referencia.
6. Futura venta: varias cláusulas con porcentaje, base definida por texto explícito, activación y vigencia.
7. Recompra y tanteo: opciones independientes, plazos, precios y condiciones expresas.
8. Incorporación: intención inmediata, próxima ventana no resuelta o fecha futura solicitada; plazo de propuesta.
9. Resumen: dinero inmediato/aplazado, fijo propuesto, máximo monetario de variables y condiciones contingentes diferenciadas.

Se puede añadir, editar o retirar condiciones antes de enviar. No hay un límite de tres/cinco/diez cláusulas. El límite monetario técnico corresponde a importes representables como céntimos Int64, compatible con el almacenamiento económico existente. Se usa decimal y dos decimales, nunca float/double. La entrada no admite separadores de miles: «1.000» se rechaza para evitar interpretar mil como uno; debe escribirse «1000» o «1000,00».

La confirmación utiliza una ventana propia con resumen desplazable y botones fijos. Se ajusta al tamaño de la ventana; no oculta condiciones o acciones al reducir resolución. Cancelar no envía la oferta; el borrador ya guardado se conserva. Volver al buscador con cambios sin guardar exige confirmar descarte. Los borradores no se autoguardan al abandonar mediante la navegación principal.

## Reglas monetarias y de participantes
Se rechazan negativos, formatos no numéricos, desbordamientos, más de dos decimales, plazos no positivos, vencimientos anteriores a la propuesta y órdenes duplicados o incoherentes. Los pagos deben sumar exactamente el fijo. El calendario guarda un estado explícito: una propuesta solo admite Propuesto; no puede declararse Pagado ni Pendiente de ejecución antes de un cierre validado.

Cero sin dinero se admite con intercambio o declaración expresa de propuesta gratuita; no demuestra que la operación sea reglamentariamente ejecutable. No se usa el valor de los jugadores como dinero.

La moneda debe coincidir con la configuración del comprador. Las referencias se releen dentro de la transacción de escritura. Se rechazan jugadores duplicados, ajenos, el propio objetivo incluido en intercambio y participantes que han dejado de pertenecer al club correspondiente.

Al reabrir, los jugadores ofrecidos que ya no están disponibles se señalan y no se eliminan silenciosamente: el usuario debe excluirlos expresamente al revisar la propuesta. Un objetivo que ya no pertenece al vendedor no puede reutilizarse para enviar; retirar/cancelar sigue disponible.

## Variables y cláusulas
Las variables se registran como condiciones pendientes de evaluación, sin estadísticas o pagos inventados. Se pueden expresar los contadores individuales de clubes y objetivos de liga referidos a una competición existente; no se ejecutan. Internacionalidades, clasificación europea, copas y títulos internacionales no son seleccionables/registrables porque no hay referencias suficientes. Porterías a cero requiere portero.

Futura venta y plusvalía son bases distintas. La definición concreta de cálculo y deducciones es obligatoria; no se asume una fórmula universal. No se suman porcentajes de bases distintas como un supuesto porcentaje de propiedad del club. Los porcentajes de idéntica base/definición no pueden superar 100.

El beneficiario de una nueva cláusula propuesta es el vendedor y se vincula al jugador solicitado y negociación de origen. Recompra y tanteo también benefician al vendedor dentro de esa propuesta. Nada se activa al guardar, enviar o aceptar.

## Estados, respuestas e historial
Estados modelados: Borrador, Enviada, Pendiente de respuesta, Contraoferta recibida, Aceptada entre clubes, Rechazada, Retirada, Caducada, Pendiente de negociación con jugadores y Cancelada.

Las transiciones son explícitas. El comprador puede guardar/revisar borradores, enviarlos, retirar propuestas y cancelar según estado. Enviada es un registro interno del juego, no una comunicación externa ni una respuesta de IA. Las pantallas explican que las respuestas están desactivadas.

IClubNegotiationDecisions es un punto de integración sin implementación en producción. El almacén verifica negociación, revisión, club vendedor y condiciones vigentes de cualquier respuesta suministrada. Los fixtures sintéticos prueban contraoferta, rechazo y aceptación. Aceptar añade una versión Aceptada entre clubes y otra Pendiente de negociación con jugadores, en una misma transacción, sin tocar el estado deportivo/económico.

Cada edición o cambio de estado añade una versión, nunca sobrescribe las anteriores. La identidad de carrera, comprador, vendedor y objetivo permanece fija. Se aplican revisiones optimistas y tokens de comando para evitar repetir escrituras al reintentar. Retirada, cancelación y respuestas conservan sus motivos.

El vencimiento exige que la fecha de carrera supere la caducidad. No se añade un reloj de simulación ni se inventa un paso del tiempo para caducar ofertas.

## Derechos: preparación y bloqueo explícito
ContractualTransferRight representa un cobro entre clubes, no propiedad del futbolista o de su inscripción. Tiene carrera, operación de origen, beneficiario, deudor, jugador, base, porcentaje, vigencia, revisión y estado. PendingActivation es el valor predeterminado.

RightAmendmentProposal y RightAmendmentAgreement preparan la extinción parcial/total. La regla de dominio exige un derecho activo, origen, partes correctas, revisión vigente, acuerdo expreso de ambas partes y porcentaje no superior al existente; conserva el registro anterior y devuelve nueva revisión. Se prueba con datos sintéticos, sin derechos ni pagos en carreras reales.

No hay importación de derechos ficticios ni un método de activación en el almacén. No existen derechos vigentes de traspasos ejecutados, por lo que la pantalla informa de la dependencia y no ofrece una compra simulada. La persistencia operativa de derechos y sus modificaciones, activación, distribución entre beneficiarios y pagos queda pendiente.

Referencia consultada el 05/11/2026: [FIFA TMS — Third-party ownership](https://support.fifatms.com/en/support/solutions/articles/7000025372-third-party-ownership). La fuente distingue los participantes y clubes anteriores de terceros ajenos. El diseño restringe estas cláusulas a relaciones entre clubes y no introduce inversores o titularidad compartida de inscripción. No se afirma que exista una validación reglamentaria completa: se deberá verificar normativa aplicable antes de ejecutar operaciones. [Documentos legales oficiales](https://inside.fifa.com/legal/documents).

## Persistencia y compatibilidad
Transfer001_Negotiations.sql se instala únicamente al guardar la primera propuesta válida, dentro de su transacción. Consultar ofertas vacías o una carrera antigua no migra ni escribe. Una operación inválida revierte también la instalación del esquema.

Tablas:
- transfer_schema: versión de formato 1.
- transfer_negotiations: identidad de negociación/carrera/clubes/jugador.
- transfer_versions: revisión, token único y fotografía JSON tipada de condiciones/estado/evento.

Las referencias a clubes tienen claves foráneas. Los jugadores de mundo y cantera se almacenan en las fuentes ya existentes; se valida su identidad/pertenencia dentro de la misma transacción, sin crear una clase de juvenil ni una tabla paralela de jugadores. El JSON se usa como en otras extensiones de carrera, no como un campo libre de interfaz.

Restricciones y disparadores verifican identidad y revisión y prohíben actualizar o borrar encabezados/versiones. No se modifican migraciones publicadas, contratos ni tablas económicas. No se crean obligaciones contables ni tablas de pagos ejecutados; los calendarios pertenecen a propuestas.

La lectura de participantes reutiliza internamente el lector por lotes de Mercado para obtener información pública de intercambio y nombres históricos. Su API pública Read y los filtros/guardas de Core mantienen la exclusión propia. No se filtran los diez atributos ocultos.

Aislamiento por archivo de carrera, ID de carrera y comprador dirigido. No hay operaciones en carreras personales durante las pruebas. No se instalan herramientas, paquetes ni servicios.

## Inventario
Nuevos:
- src/Furvo.Core/Market/TransferNegotiation.cs: términos, pagos, variables, cláusulas, estados, decisiones y modelos preparatorios de derechos.
- src/Furvo.Persistence/SqliteTransferStore.cs: lectura, escritura transaccional, referencias, revisión, tokens, historial y respuestas verificadas.
- src/Furvo.Persistence/Migrations/Transfer001_Negotiations.sql: extensión aditiva y protección del historial.
- src/Furvo.Godot/Scripts/Main.Transfers.cs: formulario, listas, detalle y pantalla de dependencias de derechos.
- src/Furvo.Godot/Scripts/Components/TransferSendWindow.cs: confirmación desplazable.
- tests/Furvo.Tests/TransferNegotiationTests.cs: dominio, SQLite, compatibilidad y regresiones de estado.
- docs/MARKET_CLUB_NEGOTIATIONS.md: informe de entrega.

Modificados:
- src/Furvo.Core/Careers/CareerWorkspace.cs: destinos de navegación/permisos.
- src/Furvo.Persistence/SqliteMarketQueries.cs: reutilización interna transaccional para participantes públicos, sin ampliar la búsqueda externa.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: accesos de Mercado.
- src/Furvo.Godot/Scripts/Main.Market.cs: acción Hacer oferta en la ficha.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados en español.
- tools/GodotUiSmoke.gd: recorrido --transfer-only.
- README.md, docs/INVENTORY.md y docs/VALIDATION.md: alcance y resultados.

Se preservan los cambios locales anteriores. No se hacen commits ni push.

## Validación y problemas corregidos
57 casos nuevos comprueban importes, plazos/sumas/orden/fechas, intercambio de uno o varios jugadores, duplicados y ajenos, disponibilidad al enviar, referencias competitivas, cláusulas, bases separadas, derechos pendientes, reducción parcial/total sintética, transiciones, contraoferta/rechazo/aceptación, token repetido, revisiones, persistencia e historial inmutable.

La prueba de huella de negocio incluye todos los registros de tablas preexistentes antes y después de negociar: jugadores, contratos, asignaciones, tácticas, cantera, economía y carrera. Solo pueden variar las tablas nuevas de propuestas. La compatibilidad incluye una carrera de publicación anterior sin extensión económica; no se inventan sus presupuestos.

El recorrido nativo aislado construye una propuesta con dos plazos, dos propios, una variable y futura venta; consulta resumen, guarda/reabre, cancela confirmación, envía y retira. Comprueba nueve secciones, estados, ausencia de fichaje automático y controles accesibles en 1280×720, 1600×900 y 1920×1080.

Se detectó una confirmación estándar que podía superar la ventana al reducir la resolución; se sustituyó por un componente desplazable con botones fijos. Las comprobaciones se repiten sobre esta corrección. Los resultados finales Debug/Release y logs se registran en VALIDATION.md.

Evidencias locales:
- artifacts/test-results/transfer-focused.trx, transfer-debug.trx y transfer-release.trx.
- artifacts/transfer-native.log.
- artifacts/career-visual/career-transfer-form-*.png.

## Comprobación local y continuación recomendada
```powershell
dotnet build Furvo.sln -c Debug
dotnet test Furvo.sln -c Debug
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
```

Abrir src/Furvo.Godot/project.godot con Godot .NET. Iniciar una carrera con club, buscar un externo, abrir su ficha y pulsar Hacer oferta. Guardar un borrador con suma de plazos correcta, consultarlo en Mis ofertas, editar, confirmar envío y retirar. Las condiciones no deben cambiar el presupuesto ni la plantilla.

Próxima fase recomendada, sin iniciarla: definir y aprobar evaluación negociadora, reglas de inscripción/ventanas y cierre personal/económico transaccional; después habilitar operaciones de origen y persistencia de derechos vigentes. No incorporar un aceptador aleatorio para aparentar negociación automática.

Resultados finales ejecutados: compilaciones Debug/Release sin errores ni advertencias; 739 pruebas superadas en cada configuración, cero fallos y omisiones. Son 57 casos nuevos respecto de la referencia de 682. El recorrido nativo final y la regresión del buscador externo han pasado; se confirma la conservación del archivo de preferencias y de las cinco publicaciones iniciales.
