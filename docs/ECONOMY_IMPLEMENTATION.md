# Economía inicial y firma — etapa 2 (28/10/2026)

## Aprobación y resultado

El usuario aprobó expresamente la propuesta initial-economy-2026-27-draft-v1 y sus hipótesis mediante «si» el 28/10/2026. Su copia de ejecución aprobada es src/Furvo.Persistence/Data/initial-economy-2026-27.v1.json, versión initial-economy-2026-27-v1, Status=Approved, RuntimeUseAllowed=true, con ApprovedProposal/ApprovedOn y SHA-256 de la publicación v4. Se preserva el JSON de borrador original como historial; su marca AwaitingUserApproval es histórica, no el estado vigente de la copia aprobada.

Se ha creado data/initial-world-2026-27.v5.db desde v4 sin regenerar datos deportivos. Contiene veinte registros económicos con identidades ClubId/SeasonId estables; mantiene los 20 clubes, 480 jugadores, 480 contratos y sus 61 atributos. WorldId nuevo: 72026027-0005-4000-8000-000000000001. SHA-256 v5: 7095c7c84b55dc0a3807271b12b111e420a88e984fb72eef67e3c1d8a2177d08. La reputación de Fragua sigue en 32 interna/2 visible y los otros valores no cambian.

El mundo conserva esquema general 4. Economics001_Initial.sql añade una extensión económica independiente versión 1 (economic_publication y club_economics), con claves foráneas, restricciones, importes de estado en céntimos enteros y plan inicial JSON protegido frente a actualizaciones. Las carreras conservan extensión 2, con lectura de extensión 1; perfiles mantienen esquema 1. No se ejecuta ninguna migración económica sobre archivos existentes: el script solo se aplica al temporal de la nueva publicación v5.

## Modelo y consulta

ClubEconomicPlan contiene tesorería inicial, presupuestos, deuda, sueldo, coste del entrenador y previsiones aprobadas. EconomicIncome/EconomicExpense ofrecen totales recurrentes derivados. El estado de cada club conserva caja, presupuesto restante de fichajes y compromiso anual del entrenador, separados del plan. No hay motor de movimientos, contabilidad de cierre ni simulación de ingresos/gastos.

PublishedWorldQueries carga el mundo y su extensión económica en modo lectura; WorldQueries.GetFinancialResources es la única proyección pública usada por Economía y Contrato. No hay listas duplicadas en Godot. Las publicaciones anteriores sin extensión siguen devolviendo importes desconocidos. Las carreras pueden consultar su propio estado mediante SqliteEconomyStore.ReadResources sin escribir ni migrar.

CommittedPlayerWages y PlayerWageRemaining permanecen null: aún no existen salarios individuales suficientes. La reserva del escenario (15 %) se conserva en el plan y no se convierte en margen contractual real. No se modifica ningún jugador ni contrato para rellenar estos campos.

## Oferta y firma

La oferta usa el sueldo aprobado del ClubId. La regla existente calcula el margen = sueldo aprobado − referencia mínima anual configurada por país, sin ventajas por antecedentes/nacionalidad. Conserva inicio 01/07/2026, final 30/06/2027 (1 año) derivado de fechas y moneda EUR (€). No muestra duración independiente ni fichajes restante.

El botón «Firmar contrato y comenzar carrera» está habilitado para las veinte ofertas aprobadas. Se siguen validando identidades, fechas, salario/moneda y obligaciones expresamente exigibles. Los borradores de filosofía, expectativas y táctica se muestran como tales, no se congelan como obligaciones. La aprobación económica no aprueba esos borradores institucionales ni inventa cláusulas de rescisión. Rescisión pendiente sigue siendo distinta de ausencia expresa de cláusula económica.

Al confirmar: copia SQLite consistente de v5 a temporal exclusivo, creación de entrenador de carrera, validación de recursos/sueldo contra los datos aprobados de la copia, registro del compromiso de personal, contrato, instantánea y estado laboral dentro de la misma transacción; verificación de FK y publicación por rename sin sobrescribir. Un fallo durante el compromiso económico impide publicar cualquier archivo parcial. La solicitud conserva idempotencia y no duplica carreras al confirmar dos veces.

La economía de los veinte clubes se inicializa mediante la copia del estado v5 para cada carrera, también al comenzar en paro. Solo el club elegido recibe el compromiso del entrenador. En rechazo o paro directo no se crea contrato activo, no hay compromiso del entrenador y no se asignan presupuestos de club a su identidad. Volver/cancelar no crea ninguna carrera.

El coste anual del entrenador (bruto × 1,30, provisión ficticia aprobada) se registra como compromiso incluido en la previsión de personal. No se añade por segunda vez a esa previsión, no consume el presupuesto de jugadores y no se descuenta de caja como si se hubiera pagado todo el año al firmar. Las restantes plantillas de staff no tienen contratos inventados.

El resumen inicial consulta los recursos de la partida y muestra sueldo, fecha final, caja, presupuestos y compromiso del entrenador. Contratos firmados antiguos se cargan con su instantánea original: no se recalculan con el catálogo actual. La referencia 2026 no certifica mínimos de 2027 ni convenios específicos; deberán revisarse al implementar ejercicios futuros. No se implementa renovación ni progresión.

## Interfaz y revisión visual

Economía muestra importes aprobados y distingue «Ingresos previstos»/«Gastos previstos» con aviso explícito de planificación, sin presentarlos como cobros, pagos o resultados cerrados. El saldo de caja y presupuestos no se mezclan. El resultado contable y situación evaluada siguen sin datos/No evaluado. Tipo de interés contractual y vencimientos son desconocidos; el 4 % del escenario no se presenta como una condición bancaria firmada.

GodotUiSmoke.gd se ejecutó sin ventana y con renderizado OpenGL real, incluyendo perfil reutilizado, firma, confirmación, resumen, cancelación, paro directo y rechazo. Se crearon tres carreras aisladas en artifacts, sin tocar las personales. Se revisaron las capturas renderizadas de Economía y Contrato (parte superior y panel de recursos) a 1280×720, 1600×900 y 1920×1080: texto legible, scroll accesible y botones fuera del contenido desplazable, sin solapamientos observados. Capturas en artifacts/economy-visual (ignoradas por Git). Esta inspección cubre las capturas concretas de prueba; no implica validación de todos los clubes con interacción manual.

## Verificación

- Debug y Release: cuatro proyectos, 0 errores, 0 advertencias.
- .NET Debug y Release: 234 superadas, 0 fallidas, 0 omitidas (222 anteriores y 12 casos nuevos).
- Los casos nuevos comparan todos los importes con la propuesta aprobada, veinte sueldos, deuda/márgenes, 480 jugadores/51 atributos públicos, firma y reapertura, dos carreras independientes, rechazo/paro, idempotencia, ofertas alteradas, restricciones SQL, fallo transaccional y publicación sin sobrescritura.
- Godot: FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK, salida 0, con firma aprobada y tres carreras independientes, tanto en prueba de escena sin ventana como con renderizado real.
- SHA-256: nueve bases anteriores idénticas, incluidas las cuatro publicaciones. v5 es el único archivo de mundo nuevo.
- BuildEconomicWorld.cs crea o verifica v5 de manera idempotente; rechaza otro origen o destino incompatible. El borrador documental y la copia aprobada están separados y sus magnitudes se comparan en las pruebas.
- Sin paquetes nuevos, sin cambios de arquitectura, sin commit ni push.

## Inventario de etapa 2

Creados:
- src/Furvo.Core/Queries/ClubEconomicPlan.cs: plan aprobado, previsiones, validación y proyección.
- src/Furvo.Persistence/Data/initial-economy-2026-27.v1.json: copia aprobada completa y trazable de las veinte filas.
- src/Furvo.Persistence/Migrations/Economics001_Initial.sql: extensión económica versión 1.
- src/Furvo.Persistence/SqliteEconomyStore.cs: consultas de economía, compromiso contractual transaccional y publicación v5.
- data/initial-world-2026-27.v5.db: nueva publicación distribuida.
- tools/BuildEconomicWorld.cs: reproducción/verificación de publicación desde v4.
- tests/Furvo.Tests/EconomicCareerTests.cs: cobertura económica y de persistencia.
- docs/ECONOMY_IMPLEMENTATION.md: este informe.

Modificados:
- .gitignore: conserva también la publicación v5, sin incluir bases personales.
- src/Furvo.Core/Queries/ClubFinancialResources.cs y WorldQueries.cs: consulta única de estado económico.
- src/Furvo.Persistence/Furvo.Persistence.csproj: recurso de autoría aprobada, sin dependencias nuevas.
- src/Furvo.Persistence/PublishedWorldQueries.cs: carga económica de la publicación.
- src/Furvo.Persistence/SqliteCareerStore.cs: compromiso atómico, validación de instantánea y coherencia al cargar.
- src/Furvo.Godot/Furvo.Godot.csproj: distribución de v5.
- src/Furvo.Godot/Scripts/Components/InitialManagerOfferSource.cs: salario del club aprobado.
- src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs: tesorería, deuda, presupuestos y previsiones.
- src/Furvo.Godot/Scripts/Components/ManagerOfferDocument.cs: moneda presentada EUR (€).
- src/Furvo.Godot/Scripts/Main.Careers.cs: resumen económico de carrera.
- src/Furvo.Godot/Resources/UiStrings.resx: textos de previsiones, compromiso y botón definitivo.
- tests/Furvo.Tests/Furvo.Tests.csproj: publicación v5 y propuesta como fixtures.
- tools/GodotUiSmoke.gd: cobertura de importes, firma y capturas opcionales.
- README.md, docs/ARCHITECTURE.md, CAREER_CREATION.md, MANAGER_CONTRACTS.md, VALIDATION.md, INVENTORY.md e INITIAL_ECONOMY_PROPOSAL.md: estado aprobado y entrega vigente.

Se conservan también los archivos documentales y el validador de la etapa 1, que permanecen en el estado de trabajo sin commit.

## Comandos de Windows

~~~powershell
dotnet run --file tools/BuildEconomicWorld.cs
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd" -- --capture
& $env:GODOT4 --path .\src\Furvo.Godot
~~~

GODOT4 debe apuntar al ejecutable Godot 4.7.2 .NET instalado. La reconstrucción no sobrescribe publicaciones: verifica v5 si ya existe. Las capturas y partidas de prueba van a artifacts. En uso normal, solo la confirmación del usuario crea una carrera personal. La carga desde el menú principal continúa fuera del alcance anterior: existe reapertura validada en Persistence, no un nuevo flujo de carga en interfaz.

## Límites conservados

Sin partidos, fichajes, ventas, pagos, impuestos operativos, cierres, negociación, renovaciones, calendarios ni evolución económica. No se aprueban nuevos compromisos institucionales. No se migra ninguna partida o contrato existente automáticamente. Los compromisos salariales de jugadores y margen real permanecen desconocidos; no son fallos de carga. No se comienza otra entrega.
