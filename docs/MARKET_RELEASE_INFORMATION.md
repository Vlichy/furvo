# Mercado — Información de cláusulas de rescisión

Fecha: 06/11/2026. Entrega informativa: no ejecuta rescisión ni fichajes.

## Modelo y conocimiento

Contract incorpora ContractReleaseClause opcional, vinculada al mismo ContractId. Null histórico significa desconocida. Presence distingue Unknown, Absent y Present; una cláusula presente exige importe positivo de hasta 1.000.000.000, precisión de dos decimales y código de moneda de tres mayúsculas. No se usa cero para ausencia. From/Until son opcionales y deben respetar los límites conocidos del contrato. Scope y condiciones estructuradas permiten registrar alcance doméstico/internacional y referencias a club comprador o competición, sin inventar condiciones para contratos existentes.

La información interna se separa de ReleaseClauseInfo. El propio club puede consultar el registro de su contrato. Para otros clubes solo se admite PublicConfirmed con evidencia no vacía y fecha de confirmación no posterior a la fecha de carrera. Sin ello, la proyección devuelve el mismo Unknown que un contrato sin datos: no transmite importe, moneda, evidencia ni condiciones internas. No hay un registro de scouting por comprador en esta entrega y no se infiere conocimiento.

El estado visible diferencia Desconocida, Sin cláusula confirmada, Conocida y No vigente/no aplicable. Si el importe es conocido pero faltan datos para evaluar condiciones registradas, muestra Aplicabilidad pendiente. La comprobación informativa de fechas y contexto no equivale a autorización jurídica ni a disponibilidad para ejecutar la rescisión. No se presupone normativa de jurisdicción, inscripción o pago.

MonetaryLimits centraliza el máximo predeterminado compartido con TransferMoneyPolicy. La extensión SQLite fija el mismo máximo en céntimos; ampliar su límite requeriría una migración futura compatible. No se aplican conversiones de moneda implícitas.

## Persistencia y migración

ContractRelease001_Information.sql crea una extensión optativa por partida, versión 1. No incrementa el esquema base del mundo (continúa en 4) ni exige sobrescribir publicaciones anteriores. Tablas contract_release_schema y contract_release_terms; la segunda tiene FK al contrato, importe en céntimos INTEGER, restricciones de estado/moneda/fechas y condiciones estructuradas en JSON. Triggers protegen intervalos contractuales y referencias a clubes/competiciones, también frente a SQL directo.

SqliteContractReleaseStore.Migrate instala explícitamente la extensión en una partida, sin insertar valores en contratos anteriores. Record registra información expresamente proporcionada; no existe editor comercial ni generación automática. Las escrituras y migración son transaccionales. Las lecturas no crean tablas, no migran y no modifican datos. Una base sin extensión se interpreta como información desconocida. Se mantiene la política existente de MigrateCopy para formatos antiguos del esquema base.

SqliteWorldStore.Load recupera la información como parte de Contract. Apply admite una cláusula expresamente incluida en el contrato; omitir el campo en una actualización ajena conserva el registro anterior. Cambiar fechas de contrato de forma incompatible se rechaza. Los contratos sin cláusula nueva conservan su serialización anterior mediante JsonIgnore de null. El almacenamiento permanece aislado por archivo/carrera.

La migración se ha aplicado únicamente en fixtures temporales y carreras aisladas de QA. No se migran automáticamente carreras personales. Las cinco publicaciones iniciales permanecen idénticas y no reciben cifras de rescisión.

## Interfaz

PlayerContractPanel es compartido por la ficha de Mercado, la ficha propia de Plantilla y la cabecera compacta de Hacer oferta. Muestra club/fin de contrato, valor o estado de rescisión y origen general de la información, sin publicar la evidencia interna. Reutiliza la ficha y sus 51 atributos públicos, sin añadir atributos ocultos ni una valoración global.

El importe conocido se formatea en español con moneda explícita. La oferta muestra avisos por debajo, igual o por encima de la cláusula conocida. Solo se compara el importe fijo monetario cuando coinciden monedas y la aplicabilidad registrada está comprobada; variables y jugadores no se valoran como pago de rescisión. Alcanzar el importe no acepta la oferta, no cambia contratos ni descuenta fondos. La información desconocida o condicionada mantiene un aviso específico. No hay botón de ejecución.

En los contratos reales actuales del proyecto, los datos sin autoría siguen mostrándose Desconocida: no se han asignado cláusulas ficticias. Los importes y estados de las capturas de QA son exclusivamente fixtures identificadas como sintéticas dentro de carreras aisladas, nunca datos publicados.

## Preparación futura

PlayerAcquisitionProcedure distingue ClubNegotiation y ContractRelease. IReleaseProcedureChecks enumera comprobaciones de cláusula, condiciones, fondos, obligaciones de pago, inscripción, acuerdo del futbolista e historial transaccional. Es un puerto sin implementación de ejecución ni aceptación automática. El procedimiento económico/jurídico y la negociación personal siguen pendientes.

## Archivos creados

- src/Furvo.Core/Domain/ContractReleaseClause.cs: modelo, reglas, máximo compartido y puerto futuro.
- src/Furvo.Core/Queries/ContractInformation.cs: proyección de conocimiento y avisos comparativos.
- src/Furvo.Persistence/SqliteContractReleaseStore.cs: extensión versionada, registro y lectura.
- src/Furvo.Persistence/Migrations/ContractRelease001_Information.sql: tablas, FK y restricciones/triggers.
- src/Furvo.Godot/Scripts/Components/ReleaseClauseText.cs: textos/formato desde DTO seguros.
- src/Furvo.Godot/Scripts/Components/PlayerContractPanel.cs: panel reutilizable.
- src/Furvo.Godot/Scripts/Main.ReleaseInformation.cs: fixtures visuales solo DEBUG y con almacenamiento de QA configurado; ausentes en Release y sin controles en la interfaz comercial.
- tests/Furvo.Tests/ContractReleaseTests.cs: 22 pruebas nuevas.
- docs/MARKET_RELEASE_INFORMATION.md: este informe.

## Archivos modificados

- src/Furvo.Core/Domain/Entities.cs: relación opcional en Contract.
- src/Furvo.Core/Market/TransferOfferPresets.cs: referencia al máximo compartido, sin cambiar su valor.
- src/Furvo.Core/Queries/MarketQueries.cs: campo opcional de información pública, sin cláusula interna.
- src/Furvo.Persistence/SqliteWorldStore.cs: integración con el agregado Contract.
- src/Furvo.Persistence/SqliteMarketQueries.cs: filtro de conocimiento antes de entregar datos a Godot; búsqueda y columnas conservadas.
- src/Furvo.Godot/Scripts/Components/PlayerSheet.cs: panel contractual opcional.
- src/Furvo.Godot/Scripts/Components/TransferOfferEditor.cs: información visible y avisos.
- src/Furvo.Godot/Scripts/Main.Market.cs: entrega del DTO a la ficha existente, sin editar el buscador.
- src/Furvo.Godot/Scripts/Main.cs: consulta de contratos propios al abrir ficha.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados.
- tests/Furvo.Tests/Furvo.Tests.csproj: prueba de la fuente pura de formato, sin cargar Godot.
- tools/GodotUiSmoke.gd: estados, privacidad, umbrales y resoluciones.
- README.md, docs/ARCHITECTURE.md, docs/INVENTORY.md y docs/VALIDATION.md: documentación.

## Validación ejecutada

- Compilaciones Debug y Release: cero errores y cero advertencias.
- xUnit Debug: 837 superadas, cero fallidas y cero omitidas.
- xUnit Release: 837 superadas, cero fallidas y cero omitidas.
- 22 casos nuevos: presencia, ausencia, desconocimiento, caducidad/vigencia futura, precisión/límites, evidencia, privacidad, condiciones, moneda, umbrales, formato, migración, FK/triggers, rollback, persistencia, aislamiento y compatibilidad con ofertas.
- Las pruebas comparan datos de negocio antes/después: añadir información y enviar una oferta no modifica plantillas, contratos básicos ni economía. Las lecturas conservan la huella del archivo y no instalan extensiones.
- Godot nativo ejecutado a 1280×720, 1600×900 y 1920×1080. Estados desconocido, ausencia confirmada, conocida, privada, no vigente y aplicabilidad pendiente en ficha y oferta; ficha propia con información interna; caso real de cantera sin contrato disponible; comparación inferior/igual/superior; 51 atributos públicos y regresión del flujo de oferta completo.
- Capturas inspeccionadas: cabecera conocida en oferta 1280, ficha privada 1600, ausencia confirmada 1920, contrato incompleto y ficha propia. Desplazamiento y botones principales conservados.
- SHA-256 de las cinco publicaciones iniciales sin cambios. git diff --check sin incidencias. Sin paquetes nuevos, commits ni push.

Evidencia: artifacts/test-results/release-information-debug.trx, release-information-release.trx, release-information-focused.trx; artifacts/release-information-native.log; artifacts/career-visual/career-release-*.png.

Comandos Windows por separado: dotnet build Furvo.sln -c Debug; dotnet build Furvo.sln -c Release; dotnet test Furvo.sln -c Debug; dotnet test Furvo.sln -c Release.

## Límites pendientes

No hay importes fundacionales aprobados, scouting contractual granular, ejercicio de cláusulas, jurisdicciones implementadas, fondos reservados, transacciones deportivas, salarios, renovaciones ni agentes. La proyección se limita a información cuya disponibilidad esté documentada; una condición sin datos de evaluación permanece pendiente.
