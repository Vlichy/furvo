# Mercado — Recompra, tanteo, incorporación y validez

Fecha: 06/11/2026. Rediseño de presentación dentro de Hacer oferta, conservando la pantalla única y sus secciones plegables.

## Cambios visuales

Recompra y tanteo utilizan tarjetas oscuras con título, descripción breve y activación visible. Las condiciones y ayudas se ocultan cuando la cláusula está desactivada; Read devuelve null y no se registra esa cláusula. Reactivar recupera los valores introducidos. Recompra reutiliza TransferMoneyField y el límite técnico existente; el precio debe seguir siendo positivo conforme a Core. Resúmenes dinámicos con formato es-ES. Años y días usan singular/plural correcto.

Selecciones nuevas de recompra: 1/2/3/5/10 años. Tanteo: 1/2/3/5 años y respuestas de 3/7/14 días. Son catálogos de presentación centralizados; no se restringen contratos históricos admitidos por Core. El valor anterior se añade como opción identificada cuando sea distinto, incluidos tanteo de 10 años, respuesta de 30 días y validez de 60 días. Las cláusulas personalizadas anteriores se conservan hasta reemplazo expreso; el historial no se reescribe.

Incorporación ofrece tres tarjetas seleccionables: Lo antes posible, Próxima ventana de fichajes e Incorporación futura. Solo la última muestra selectores de día/mes/año. No se permiten fechas anteriores a la carrera. No se inventa una ventana ni una autorización reglamentaria: se indica la validación pendiente.

La validez dispone de opciones rápidas independientes de 3/7/14/30 días y muestra su fecha calculada desde la carrera. Un borrador recuperado conserva su caducidad y muestra vigencia restante; elegir otro plazo la recalcula explícitamente. En una propuesta nueva se presupone envío en la fecha de carrera mostrada; la validación al enviar permanece en el servicio existente. No se utiliza el reloj del ordenador ni se confunde este plazo con el tanteo.

Condiciones adicionales muestra un resumen compacto con las selecciones reales. El resumen principal evita los textos jurídicos extensos de estas cláusulas. Las ayudas accesibles por botón y tooltip mantienen su explicación; la confirmación final sigue incluyendo las condiciones completas, sus fechas y criterios contractuales.

OfferChoices conserva foco y operación estándar de botones Godot; utiliza marca textual de selección además de borde y color. Los grupos se adaptan mediante HFlowContainer y conservan su estado al plegar o desplazarse. No hay pestañas internas nuevas.

## Arquitectura y archivos

Creados:
- src/Furvo.Godot/Scripts/Components/OfferChoices.cs: selector visual exclusivo, reutilizable para plazos y tarjetas descriptivas.
- src/Furvo.Godot/Scripts/Components/OfferConditionOptions.cs: catálogos de presentación, conservación de opciones previas y claves de singular/plural; no depende del motor Godot.
- src/Furvo.Godot/Scripts/Components/TransferRightsCards.cs: recompra, tanteo, visibilidad, ayudas y resúmenes; conserva objetos anteriores cuando corresponde.
- src/Furvo.Godot/Scripts/Components/TransferArrivalCards.cs: incorporación y validez independientes, fecha de carrera y errores locales.
- tests/Furvo.Tests/OfferConditionCardsTests.cs: 24 casos nuevos de opciones, lenguaje, reglas conservadas y persistencia.
- docs/MARKET_CONDITION_CARDS.md: este informe.

Modificados:
- src/Furvo.Godot/Scripts/Components/TransferOfferEditor.cs: integra las tarjetas y separa resumen compacto de confirmación detallada; elimina controles sustituidos.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados.
- tests/Furvo.Tests/Furvo.Tests.csproj: compila el catálogo puro de presentación para probar la fuente real sin cargar Godot ni añadir paquetes.
- tools/GodotUiSmoke.gd: activación/desactivación, resúmenes, ayudas, fechas, selección persistente y recorrido nativo.
- README.md, docs/INVENTORY.md y docs/VALIDATION.md: estado y evidencias.

Reutilizados: ClubUi, WindowUi, TransferMoneyField, TransferOfferPresets, TransferTerms y SqliteTransferStore. Core, Persistence y sus esquemas no se modifican en esta intervención. Sin dependencias nuevas ni migraciones.

## Validaciones conservadas

Se mantienen máximo monetario configurable, rechazo de negativos y recompra de cero/incompleta, duraciones y respuestas admitidas por Core, fechas de incorporación, caducidad y coherencia del calendario de pagos. Las secciones desactivadas no bloquean por valores ocultos. No se alteran estados de negociación, ofertas históricas ni contratos. La comparación exacta de condiciones estándar versionadas y la comprobación transaccional de participantes permanecen en los servicios previos.

## Pruebas y evidencia

- Compilaciones Debug y Release: cero errores y cero advertencias.
- xUnit Debug: 815 superadas, cero fallidas y cero omitidas.
- xUnit Release: 815 superadas, cero fallidas y cero omitidas.
- Subconjunto nuevo: 24 pruebas superadas. Opciones históricas, 1 año/1 día, resúmenes localizados, duraciones, precio negativo y máximo, 3/7/14/30 días, independencia respecto al tanteo, las tres incorporaciones, fecha pasada, guardado/carga/envío, historial y huella de negocio sin cambios.
- Godot nativo: selección por teclado con foco y evento real de barra espaciadora; activar y desactivar, excluir condiciones inactivas, error monetario local, resúmenes reales, ayuda contractual, fecha pasada, fecha futura, cambio de incorporación, opciones de validez independientes, plegar/reabrir, guardar/reabrir/enviar/retirar y regresión de pagos, selector y Negociaciones.
- Resoluciones 1280×720, 1600×900 y 1920×1080: recorrido y capturas reales. Inspección de recompra, tanteo, incorporación y validez, con desplazamiento y botones principales fijos. Se corrigió el salto de línea innecesario de los selectores cortos.
- Las cinco publicaciones iniciales conservan SHA-256. git diff --check sin incidencias. Carreras de prueba aisladas en artifacts; no se utilizan carreras personales.

Archivos de evidencia: artifacts/test-results/market-condition-cards-debug.trx, market-condition-cards-release.trx, market-condition-cards-focused.trx; artifacts/market-condition-cards-native.log; artifacts/career-visual/career-condition-cards-*.png y career-offer-single-clauses-*.png.

Comandos Windows, por separado: dotnet build Furvo.sln -c Debug; dotnet build Furvo.sln -c Release; dotnet test Furvo.sln -c Debug; dotnet test Furvo.sln -c Release.

## Límites

No se ejecuta recompra, tanteo ni traspaso, ni se descuentan fondos por enviar. No hay agentes, contratos personales o respuestas automáticas nuevas. La validación reglamentaria completa continúa pendiente. Las fechas de derechos usan los orígenes contractuales ya disponibles y no afirman una formalización efectiva futura. No se modifican buscador, columnas, atributos, plantillas, cantera, presupuestos ni derechos ajenos. Sin commits ni push.
