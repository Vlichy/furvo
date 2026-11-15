# Mercado — Entrega 1: búsqueda y consulta
Regla vigente: [exclusión obligatoria del club dirigido](MARKET_EXTERNAL_PLAYERS.md), incluidos filiales y juveniles. Los recuentos históricos de la primera entrega describen el universo previo a esta corrección.

Ampliación posterior: [filtros editables y columnas por posición](MARKET_SEARCH_COLUMNS.md).

Fecha: 04/11/2026. Alcance: lectura de la carrera activa, sin operaciones deportivas ni económicas.

## Funciones y navegación
Mercado → Buscar jugadores conserva la cabecera de carrera y habilita búsqueda para entrenadores con club. Entrenadores en paro mantienen las restricciones actuales. Equipo → Plantilla conserva la gestión contractual. No se crean páginas vacías de seguimiento, ojeo o negociaciones.

Filtros combinables: nombre parcial sin distinguir mayúsculas ni tildes y con espacios normalizados, club actual registrado, posición principal, edad mínima/máxima y un atributo público con rango inclusivo 1–20. Se aplican con Buscar o Intro; los campos vacíos no restringen. Un filtro inválido muestra un mensaje y conserva los resultados de la última búsqueda válida. Limpiar restablece filtros, selección y orden. Ordenación ascendente/descendente por nombre, edad, club, posición (orden canónico de demarcaciones) y fecha contractual; desempate estable por nombre e identificador. Los datos desconocidos quedan al final.

La ficha reutiliza PlayerDetails y PlayerSheet: 51 atributos visibles, sin valoración global ni atributos ocultos. El desplegable contractual muestra identificador, club del contrato, fechas y vigencia registrada. Una fecha o contrato desconocidos nunca se interpretan como libertad contractual. Salarios, cláusulas, primas, valor e interés no disponibles se declaran expresamente. No hay botones de operaciones ni permisos sobre clubes externos.

Las columnas se conservan por usuario y posición en un archivo de preferencias independiente de SQLite. El contexto en memoria se identifica por carrera: borrador y filtros aplicados, orden, selección estable y desplazamiento de tabla. Volver a la búsqueda relee la carrera; una selección que ya no cumple los filtros se descarta. Entrar en otra carrera restablece el contexto.

## Universo y coherencia de lectura
SqliteMarketQueries abre una única conexión y transacción de lectura sobre la carrera. Combina jugadores fundacionales, todos los equipos inferiores ya almacenados, contratos, asignaciones deportivas y cambios visibles de desarrollo existentes. No inicializa cantera, crea jugadores, migra esquemas ni modifica presupuestos.

La carrera de referencia contiene 1.894 jugadores: 480 fundacionales y 1.414 de cantera, distribuidos en los 68 equipos inferiores existentes. Todos se incluyen, una vez por identificador. No se promete un universo externo de jugadores todavía inexistentes. Una carrera antigua sin extensión de cantera ofrece sus 480 jugadores; abrir Mercado no añade esa extensión.

Pertenencia actual y contrato registrado son conceptos distintos. Se respetan las asignaciones deportivas persistidas; si no existe pertenencia vigente ni asignación explícita, el club actual figura como No disponible. Puede consultarse el último contrato registrado sin inferir pertenencia actual. Categoría y equipo proceden de datos existentes; los equipos inferiores no se convierten en clubes independientes.

## Inventario de esta entrega
Nuevos:
- src/Furvo.Core/Queries/MarketQueries.cs: DTO públicos, filtros, validación, orden y selección.
- src/Furvo.Persistence/SqliteMarketQueries.cs: consulta global por lotes, exclusivamente de lectura.
- src/Furvo.Godot/Scripts/Main.Market.cs: buscador, navegación y ficha pública reutilizada.
- src/Furvo.Godot/Scripts/Components/MarketSearchContext.cs: contexto visual independiente por carrera.
- tests/Furvo.Tests/MarketTests.cs: 35 casos automatizados.
- docs/MARKET_DELIVERY_1.md: informe y límites.

Modificados:
- src/Furvo.Core/Careers/CareerWorkspace.cs y src/Furvo.Godot/Scripts/Main.Navigation.cs: destino y permisos de Mercado.
- src/Furvo.Core/Queries/WorldQueries.cs: club opcional en la proyección pública, para no inventar pertenencias.
- src/Furvo.Persistence/SqliteWorldStore.cs: reutilización interna de la carga dentro de una transacción de lectura compartida.
- src/Furvo.Godot/Scripts/Components/PlayerSheet.cs: club desconocido y contenido complementario opcional dentro del desplazamiento existente.
- src/Furvo.Godot/Scripts/Components/DataTable.cs: lectura/restauración opcional del desplazamiento; la selección sigue basada en ID.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados en español.
- src/Furvo.Godot/Scripts/Main.AcademyFollowup.cs y tests/Furvo.Tests/AcademyTests.cs: acceso compatible al club ahora opcional, sin cambiar funciones de Cantera ni relajar la comprobación del club esperado.
- tests/Furvo.Tests/Furvo.Tests.csproj: enlace del contexto visual puro para pruebas sin Godot.
- tools/GodotUiSmoke.gd: permisos actualizados y recorrido --market-only.
- README.md, docs/INVENTORY.md y docs/VALIDATION.md: estado y validación.

Se conservan los cambios previos de Cantera. La integración compartida del club opcional fue explicada antes de editarla. No hay dependencias nuevas, migraciones ni publicaciones SQLite nuevas. No se cambian contratos, jugadores fundacionales ni carreras personales.

## Validación
Referencia anterior: 594 pruebas Debug superadas. Casos nuevos: 35 superados, incluyendo filtros, orden estable, 51 públicos y rechazo de los diez ocultos, consulta por ID, aislamiento, contratos desconocidos/caducados, asignaciones actuales y carreras sin cantera. Las comprobaciones de hash verifican ausencia de escritura por búsqueda y consulta de ficha.

El recorrido nativo aislado de Godot comprueba entrada desde carrera, 1.894 resultados, filtro combinado, orden conservando ID, ficha externa, regreso con filtros/selección/desplazamiento, mensaje de filtro inválido y resultado vacío. Capturas a 1280×720, 1600×900 y 1920×1080 en artifacts/career-visual/career-market-*.png. La inspección de capturas detectó un solapamiento en posiciones largas y se corrigió habilitando salto de línea. El botón de regreso permanece visible y los atributos utilizan el desplazamiento compartido de ficha.

Resultados finales de la batería y compilaciones: véase el apartado Mercado en VALIDATION.md. Evidencias TRX en artifacts/test-results/market-*.trx y log nativo artifacts/market-delivery1-native.log. Los artefactos son locales y no se incorporan al control de versiones.

## Compatibilidad y límites
La barra interna de Tree se usa únicamente para restaurar desplazamiento, comprobada en Godot 4.7.2; deberá revisarse al actualizar Godot. La búsqueda actual normaliza tildes, mayúsculas y espacios; no translitera alfabetos. Los datos se cargan al entrar/regresar al buscador; no hay actualizaciones en segundo plano. El filtro de atributo es uno por búsqueda. El orden de posición usa el catálogo del dominio, no traducciones.

Sin seguimiento, informes nuevos, ojeadores, ofertas, traspasos, cesiones, agentes, renovaciones, ventanas, IA, valoración monetaria ni cambios de presupuesto. No se inicia la Entrega 2. No se realizan commits ni push.

## Comprobación local
Desde la raíz del proyecto, en PowerShell:

```powershell
dotnet build Furvo.sln -c Debug
dotnet test Furvo.sln -c Debug
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
```

Abrir src/Furvo.Godot/project.godot en Godot .NET, iniciar una carrera con club y acceder a Mercado. Buscar un jugador externo, abrir su ficha y volver. Comprobar filtros, selección y ausencia de controles de contratación.

La regresión nativa adicional de Cantera y seguimiento individual también se ejecutó y superó (artifacts/market-academy-regression.log).
