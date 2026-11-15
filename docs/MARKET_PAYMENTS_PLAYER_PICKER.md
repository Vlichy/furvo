# Mercado — Pagos editables y selector visual

Fecha: 06/11/2026. Amplía la pantalla única de Hacer oferta; sustituye los calendarios nuevos de 2/6/10 años. Los documentos MARKET_OFFER_REDESIGN.md y MARKET_CLUB_NEGOTIATIONS.md conservan el historial de entregas anteriores.

## Calendarios

Pago único al formalizar y plantillas de 2, 3 o 4 pagos: 50/50; 50/25/25; 40/20/20/20. Son valores iniciales editables, no una regla universal del mercado. Plan personalizado reutiliza el mismo editor de porcentajes y meses. El último porcentaje es residual y los céntimos sobrantes se ajustan en el último vencimiento. Se pueden editar los primeros porcentajes y todos los meses posteriores al inicial. El editor admite de 1 a 4 pagos, anticipo porcentual positivo y máximo ordinario de 36 meses, con orden estrictamente creciente. No se habilitan calendarios excepcionales nuevos.

La fecha proviene exclusivamente de la carrera o de la incorporación futura solicitada. Las filas muestran el hito relativo a formalización, fecha estimada, importe y porcentaje efectivo, además de total, anticipo y saldo pendiente. El plan versionado formalization-shares-v1 conserva meses y porcentajes para que un futuro cierre pueda resolver la fecha efectiva. No existe ejecución de cobros. Una ventana de incorporación aún desconocida no se presenta como fecha real confirmada.

Importes decimal con dos decimales, límite técnico configurado y sin precio mínimo universal. Para importes de pocos céntimos, se omiten cuotas redondeadas a cero y el residuo se conserva íntegro en el vencimiento final; se muestra el número efectivo de pagos. La plantilla porcentual nominal permanece disponible. Core comprueba que el calendario coincida exactamente con el plan, su referencia y el importe. También rechaza cualquier vencimiento anterior a una incorporación futura explícita al guardar nuevas revisiones, incluso si el calendario procede de una oferta antigua. Las lecturas históricas no ejecutan esta validación ni reescriben datos. Persistence utiliza esa misma validación dentro de la transacción existente.

## Viabilidad y contraofertas

TransferFinancialReview compara anticipo con tesorería conocida e importe fijo con presupuesto de fichajes restante, solo si la moneda coincide. Es información, no aprobación financiera, reserva de fondos ni bloqueo arbitrario. La interfaz distingue importes excedidos, compatibles con el dato conocido y datos pendientes. Siguen pendientes compromisos futuros completos, situación financiera integral, preferencias del vendedor y aceptación definitiva. No se estiman intereses ni se inventan datos. El modelo ClubDecision/CounterTerms existente puede transportar otras distribuciones, vencimientos e importes; no se genera IA negociadora.

## Selector de jugadores

Ventana modal centrada y acotada al área disponible. Nombre destacado; posición, edad y equipo real secundarios. Tarjetas seleccionadas diferenciadas por borde, fondo y checkbox. Búsqueda instantánea compartida con MarketQueries.NormalizeName: tildes, mayúsculas, espacios y fragmentos. Filtros combinables por grupo deportivo y categoría real de carrera: First, Senior (filiales, incluidos segundos filiales) y Juvenil. Las categorías desconocidas solo aparecen en Todos; no se deducen por nombre.

Selección transitoria por identificador estable, sin duplicados; confirmar aplica y cancelar/Escape descarta. Cambiar filtros no pierde los IDs provisionalmente seleccionados. Reabrir parte de los confirmados. Las tarjetas de la oferta permiten retirar jugadores. El tooltip muestra vencimiento contractual público cuando está disponible. No se muestran atributos ocultos ni valoraciones globales; no existen valores estimados en el modelo consultado y no se inventan. No se implementa una vista rápida adicional de atributos.

El orden prioriza únicamente intereses suministrados por un registro real. Como ese registro persistente no existe actualmente, la interfaz permanece sin etiquetas de interés. Se conserva la comprobación de pertenencia actual al comprador al guardar/enviar. No existe un sistema completo de elegibilidad competitiva para cantera: se muestra explícitamente como pendiente, no se da por transferible un futbolista solo por estar en el selector.

## Compatibilidad

TransferTerms añade PaymentPlan opcional al JSON existente. Sin nueva migración SQL, publicaciones nuevas ni actualización masiva. Las ofertas sin metadatos, incluidos los viejos calendarios de 6/10 años, se conservan como calendario anterior de solo lectura en el editor. Reemplazarlas requiere acción expresa al editar una propuesta permitida; cambiar el importe sin reemplazar un calendario incompatible produce un error local. El límite ordinario de 36 meses se aplica al nuevo formato versionado; las estructuras antiguas siguen admitidas por compatibilidad, sin ofrecer plantillas extraordinarias nuevas en la interfaz. Las ofertas enviadas/históricas no se editan ni se reinterpretan silenciosamente. Las ofertas nuevas con metadatos recuperan su reparto editable y mantienen los IDs de cuotas reutilizables.

## Archivos

Creados:
- src/Furvo.Core/Market/TransferPaymentPlans.cs: planes, generación, validación, revisión financiera informativa, filtros y selección transitoria.
- src/Furvo.Godot/Scripts/Components/TransferPaymentEditor.cs: editor y resumen de pagos.
- tests/Furvo.Tests/TransferPaymentPlanTests.cs: 30 casos nuevos de dominio y persistencia.
- docs/MARKET_PAYMENTS_PLAYER_PICKER.md: este informe.

Modificados:
- src/Furvo.Core/Market/TransferNegotiation.cs: metadatos opcionales y validación del plan.
- src/Furvo.Godot/Scripts/Components/TransferOfferEditor.cs: integración y resumen financiero.
- src/Furvo.Godot/Scripts/Components/TransferPlayerPicker.cs: modal visual, filtros, selección transitoria y tarjetas.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados.
- tests/Furvo.Tests/TransferNegotiationTests.cs: el caso previo de incorporación futura ahora prueba rechazo del calendario anterior y valida uno compatible; se conservan sus comprobaciones opcionales.
- tools/GodotUiSmoke.gd: nuevos escenarios de calendario, filtros, selección, cancelación y recuperación.
- README.md, docs/INVENTORY.md, docs/VALIDATION.md: estado y enlaces.

Reutilizados: TransferMoneyField, ClubUi, WindowUi, MarketQueries, DTO públicos, catálogo de posiciones, SqliteTransferStore, historial y mecanismos transaccionales previos. Sin dependencias nuevas.

## Validación

Compilación Debug y Release sin errores ni advertencias. Pruebas xUnit: 791 superadas en Debug y 791 en Release, cero fallidas/omitidas; 30 casos adicionales. Evidencia: artifacts/test-results/market-payment-picker-debug.trx, market-payment-picker-release.trx y market-payment-picker-focused.trx.

Las pruebas cubren pago único, 2/3/4 pagos, anticipo y meses personalizados, redondeo exacto, extremos monetarios, fechas inválidas, fin de mes/año bisiesto, referencia de incorporación, modificación indebida de cuotas, preservación de calendario antiguo, filtros y normalización, selección sin duplicados y cancelación transitoria. Las pruebas de persistencia guardan/cargan/envían conservando historial, seleccionados y huella de negocio; no hay movimientos de plantilla ni descuentos. La suite anterior conserva sus casos de pertenencia, interés real y exclusión de atributos ocultos.

Comandos Windows (ejecutar por separado): dotnet build Furvo.sln -c Debug; dotnet build Furvo.sln -c Release; dotnet test Furvo.sln -c Debug; dotnet test Furvo.sln -c Release.

Los ensayos nativos usan carreras aisladas en artifacts; no modifican carreras personales. Las cinco publicaciones iniciales conservan su SHA-256. Sin cambios en contratos personales, atributos, economía operativa, agentes, commits ni push.

Validación nativa Godot: pago único y 2/3/4 pagos, modificación del anticipo a 60 %, meses personalizados, rechazo de un vencimiento duplicado, búsqueda, filtros combinados, contador, confirmación, cancelación sin aplicar y reapertura. Recorrido de guardar/reabrir/enviar/retirar y regresión de Negociaciones con 1/8/150 filas. Resoluciones 1280×720, 1600×900 y 1920×1080: lista desplazable, cabecera del modal, contador y botones dentro de la ventana; capturas inspeccionadas. Se sustituyó PopupPanel por Window modal al detectar crecimiento incorrecto del contenedor interior. Evidencia: artifacts/market-payment-picker-native.log y artifacts/career-visual/career-offer-player-picker-*.png, career-offer-single-top-*.png.
