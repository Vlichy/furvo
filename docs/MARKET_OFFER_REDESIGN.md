# Mercado — Rediseño de Hacer oferta

Fecha: 05/11/2026. Sustituye la presentación de nueve pestañas descrita en MARKET_CLUB_NEGOTIATIONS.md; conserva su almacén, historial y flujo de negociación.

## Interfaz implementada

Una pantalla con desplazamiento vertical, secciones plegables, cabecera y botones fijos. Conserva la navegación principal de Mercado. Incluye importe y pagos automáticos, selector múltiple buscable, variables editables, futura venta y plusvalía separadas, recompra, tanteo, incorporación y resumen exacto de confirmación. Los mensajes locales se retiran al corregir el error; un formulario incompleto no impide explorar otras secciones.

El selector utiliza identificadores estables, nombre, posición y edad reales. Comprueba pertenencia actual al comprador, excluye el jugador solicitado y evita duplicados. No existe registro persistente de interés: no se muestran etiquetas inventadas. Core admite registros explícitos para una integración futura. No se afirman reglas de inscripción o elegibilidad competitiva todavía inexistentes.

## Reglas compartidas

TransferMoneyPolicy centraliza el máximo individual de 1.000.000.000 EUR; es configurable por inyección en Core y SqliteTransferStore. Es un límite técnico, no el presupuesto disponible. Los importes usan decimal, dos decimales y validación en dominio, almacenamiento e interfaz. Cero requiere intercambio o propuesta gratuita expresa. Negativos, desbordamientos y entradas excesivas se rechazan. La regla también alcanza variables, recompra, pagos y la reducción de derechos económicos preparada en dominio.

Pago único: vencimiento en la fecha de carrera. Pago aplazado: 2, 6 o 10 vencimientos anuales al final de cada año desde esa fecha, sin entrada inicial ni intereses inventados. Se reparte en céntimos, ajustando el último; se omiten pagos de cero en importes mínimos. La suma es exacta y ningún nuevo vencimiento precede a la negociación. La validación económica final y la ejecución de cobros siguen pendientes.

Porcentajes estándar: 10 a 100 en pasos de 10. Base gross-fixed-cash-v1: importe fijo monetario bruto de la próxima transferencia completada, sin deducciones, excluyendo variables y valoración de intercambios. Base positive-fixed-cash-surplus-v1: máximo entre cero y ese importe menos el precio fijo de adquisición de esta oferta, congelado como base contractual. No se ejecutan automáticamente. Su alcance es la primera transferencia futura completada verificable, sin añadir caducidad temporal arbitraria.

Recompra fixed-buyback-v1 y tanteo equal-terms-notice-v1: duraciones de 1/2/3/5/10 años, hasta el aniversario menos un día. Tanteo ofrece 3/7/14/30 días naturales de respuesta desde notificación acreditada y exige igualar las condiciones completas. Son propuestas estructuradas sujetas a consentimiento y reglas aplicables, no garantías de un futuro fichaje.

Variables: objetivo, umbral, importe y periodo; edición y eliminación sin redacción jurídica manual. No se simula cumplimiento. Incorporación futura usa selectores de fecha, no texto ISO. Validez de propuesta: 7/14/30/60 días, conservando duraciones históricas distintas.

## Compatibilidad

No hay migración SQL nueva ni publicaciones del mundo nuevas. Los campos JSON opcionales incorporan versión estándar, duración y base fija sin reescribir ofertas históricas. Calendarios y cláusulas antiguas personalizados permanecen legibles y se conservan; reemplazarlos requiere acción expresa. Una oferta histórica superior al máximo nuevo puede consultarse o retirarse, pero una nueva revisión debe cumplir el límite configurado. No se cambian contratos firmados ni registros de negocio al consultar o enviar.

La salida propia del formulario conserva la confirmación de descarte. No se añade autoguardado al navegar por otras áreas.

## Archivos de esta intervención

Creados:
- src/Furvo.Core/Market/TransferOfferPresets.cs: política monetaria, calendarios, cláusulas estándar y candidatos de intercambio.
- src/Furvo.Godot/Scripts/Components/TransferOfferEditor.cs: pantalla única y constructor de oferta.
- src/Furvo.Godot/Scripts/Components/TransferMoneyField.cs: entrada monetaria y error local.
- src/Furvo.Godot/Scripts/Components/TransferPlayerPicker.cs: búsqueda y selección múltiple por ID.
- tests/Furvo.Tests/TransferOfferPresetTests.cs: 22 casos adicionales de dominio y persistencia.
- docs/MARKET_OFFER_REDESIGN.md: este informe.

Modificados:
- src/Furvo.Core/Market/TransferNegotiation.cs: validaciones compartidas y metadatos opcionales.
- src/Furvo.Persistence/SqliteTransferStore.cs: política monetaria en la validación transaccional.
- src/Furvo.Godot/Scripts/Main.Transfers.cs: integración con los servicios existentes.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados.
- tests/Furvo.Tests/TransferNegotiationTests.cs: cero injustificado rechazado también al guardar.
- tools/GodotUiSmoke.gd: recorrido nativo de oferta y tres resoluciones.
- README.md, docs/INVENTORY.md y docs/VALIDATION.md: estado y evidencia.

## Validación ejecutada

- Compilación de Furvo.sln Debug y Release: cero errores y cero advertencias.
- xUnit Debug: 761 superadas, cero fallidas y cero omitidas.
- xUnit Release: 761 superadas, cero fallidas y cero omitidas.
- Godot nativo: entrada negativa/excesiva, formato monetario, límite, pagos 2/6/10, búsqueda y selección múltiple, eliminar/reañadir, variable editable, porcentajes, recompra/tanteo, guardar/reabrir/enviar/retirar y conservación de plantilla y preferencias.
- Resoluciones 1280×720, 1600×900 y 1920×1080: recorrido automatizado y capturas; cabecera y botones permanecen visibles. Inspección de capturas del inicio 1280, cláusulas 1600 y resumen 1920.
- SHA-256 de las cinco publicaciones iniciales sin cambios. git diff --check sin incidencias.
- Evidencia: artifacts/offer-redesign-native.log, artifacts/test-results/offer-redesign-debug.trx, offer-redesign-release.trx y artifacts/career-visual/career-offer-single-*.png.

Comandos locales: dotnet build Furvo.sln -c Debug; dotnet build Furvo.sln -c Release; dotnet test Furvo.sln -c Debug; dotnet test Furvo.sln -c Release. Ejecutarlos por separado en PowerShell.

## Límites

Enviar registra una propuesta: no transfiere jugadores, paga, altera contratos ni ejecuta derechos. Siguen pendientes respuestas automáticas de clubes, validación competitiva definitiva, cierre de traspaso, contratos personales/agentes y ejecución económica. Las siete capturas de referencia mencionadas en el encargo no estaban adjuntas a este mensaje; no se afirma coincidencia exacta con ellas. No se añaden dependencias, commits ni push.
