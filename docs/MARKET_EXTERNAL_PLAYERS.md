# Mercado — Exclusión del club dirigido
Fecha: 04/11/2026. Corrección de alcance del buscador existente.

## Regla y servicio
Mercado solo consulta jugadores con pertenencia actual verificada a un club distinto del dirigido. La exclusión no es una opción del usuario y se aplica antes de construir fichas públicas.

SqliteMarketQueries.Read obtiene el identificador estable del club desde career_state, mediante SqliteLineupStore.Owner, dentro de la misma transacción de lectura que el resto de la instantánea. La pertenencia de cada jugador procede del contrato vigente y de las asignaciones deportivas ya persistidas. Los equipos inferiores utilizan el identificador de su club matriz, por lo que la misma comparación excluye primer equipo, filiales, juveniles y cualquier categoría almacenada.

El lector descarta al jugador propio antes de WorldQueries.ProjectPlayer y devuelve el club dirigido como ManagedClub en MarketSnapshot. MarketQueries.Search vuelve a imponer la exclusión independientemente de los filtros. MarketQueries.Player también rechaza la consulta directa de un jugador propio por ID. No se utilizan nombres de clubes ni un club fijo. Las pruebas emplean deliberadamente dos clubes con el mismo nombre para comprobarlo.

Un jugador sin pertenencia actual verificable tampoco se presenta como perteneciente a otro club. Un contrato caducado no demuestra pertenencia actual ni libertad contractual. No se modifican esos registros: simplemente no forman parte del universo externo consultable.

## Filtros, recuentos y fichas
El desplegable contiene Todos los clubes y los 19 clubes externos. La lista conocida de clubes de la instantánea conserva los 20 identificadores para validar referencias; un filtro programático por el club propio devuelve cero jugadores, nunca permite recuperarlos.

Nombre completo o parcial, apellidos, posición, edades, atributos y sus combinaciones operan sobre jugadores externos. El recuento total disponible utiliza la lista ya excluida. La ordenación y la selección por ID reciben esa misma lista. No existen paginación ni exportación en esta versión.

Al abrir ficha se relee el servicio: no se utiliza una ficha anterior si han cambiado el contexto o la pertenencia. Se conserva la ficha común de 51 atributos visibles para jugadores externos. Las preferencias de columnas, su archivo, predeterminados, selección y orden no se modifican.

## Cambio de club o carrera
El contexto temporal se identifica por carrera y club dirigido. Cada entrada o regreso al buscador relee el propietario persistido. Si cambia, se invalidan filtros, selección y resultados anteriores. Las preferencias visuales por usuario permanecen intactas. Entrar en otra carrera sigue restableciendo el contexto; nunca se acepta un identificador de club procedente del filtro como club dirigido.

Limitación del sistema actual: el empleo inicial de una carrera es inmutable, protegido por un disparador SQLite. No se elimina ni modifica esa protección y no se implementa un cambio de empleo. Se prueba el cambio de ámbito en Core con instantáneas y contextos distintos, además de carreras válidas con clubes diferentes. La prueba de persistencia comprueba que Mercado respeta el propietario almacenado y que el disparador sigue bloqueando una actualización arbitraria. La adaptación al club que declare un futuro flujo de empleo queda preparada; no se presenta dicho flujo como disponible.

No hay sondeo en segundo plano. Los puntos actuales de actualización son entrar, regresar y abrir ficha; un futuro cambio de empleo deberá actualizar el contexto de carrera y navegar nuevamente a la pantalla.

## Plantilla, Cantera e integridad
La restricción pertenece exclusivamente a Mercado. SqliteClubSquadStore, SqliteAcademyStore y sus consultas de ficha no se cambian. Los jugadores propios siguen accesibles desde Equipo → Plantilla y Equipo → Cantera.

La carrera nativa de Mosaico conserva los 1.894 jugadores almacenados. Mercado muestra 1.786 externos: se excluyen 108 propios, de los cuales 24 pertenecen al primer equipo y 84 a la cantera. No se eliminan jugadores ni se cambian contratos, atributos, asignaciones deportivas, presupuestos, publicaciones iniciales o datos personales.

Sin migraciones, publicaciones nuevas, paquetes nuevos, commits ni push. Se preservan todos los cambios locales anteriores.

## Archivos de esta corrección
Nuevos:
- tests/Furvo.Tests/MarketExclusionTests.cs: exclusión por categorías, nombres, filtros, IDs, recuentos, contexto y aislamiento.
- docs/MARKET_EXTERNAL_PLAYERS.md: informe técnico y limitaciones.

Modificados:
- src/Furvo.Core/Queries/MarketQueries.cs: ámbito ManagedClub, exclusión en búsqueda y protección de consulta directa por ID.
- src/Furvo.Persistence/SqliteMarketQueries.cs: exclusión antes de la proyección pública, con propietario leído de la carrera activa.
- src/Furvo.Godot/Scripts/Components/MarketSearchContext.cs: identidad del club dentro del contexto temporal.
- src/Furvo.Godot/Scripts/Main.Market.cs: filtros de clubes externos, invalidación por cambio de ámbito y revalidación al abrir ficha.
- src/Furvo.Godot/Resources/UiStrings.resx: explicación del universo externo y recuento.
- tests/Furvo.Tests/MarketTests.cs: adaptación de las expectativas del universo anterior, manteniendo integridad, referencias, persistencia y compatibilidad.
- tools/GodotUiSmoke.gd: recorrido --market-exclusion-only y adaptación del recuento de la prueba anterior al universo externo.
- README.md, docs/INVENTORY.md, docs/VALIDATION.md, docs/MARKET_DELIVERY_1.md y docs/MARKET_SEARCH_COLUMNS.md: alcance y resultados actualizados.

No se editan el editor ni el almacén de columnas, DataTable, fichas compartidas, navegación de otros módulos ni la estructura de Cantera.

## Pruebas y evidencias
18 casos nuevos respecto de las 664 pruebas anteriores. Se comprueban:
- Exclusión de primer equipo, filial y juvenil, conservando sus equivalentes externos.
- Nombre exacto propio sin resultados y coincidencias parciales exclusivamente externas.
- Club propio, posición, edad, atributos y filtros combinados sin posibilidad de eludir la regla.
- Protección por ID, clubes con nombres idénticos, pertenencia desconocida y recuento exacto.
- Cambio de ámbito en Core y dos carreras válidas con clubes diferentes.
- Acceso propio desde las consultas de Plantilla y Cantera, incluyendo 51 atributos.
- Integridad del archivo completo antes y después de las consultas, incluidos contratos y asignaciones.
- Conservación del disparador que protege el empleo inicial.

El recorrido nativo aislado verifica ausencia del club en el desplegable, nombre propio sin resultados, nombre parcial, búsqueda de un portero externo con filtro de Reflejos, ficha y regreso, recuento y acceso posterior a la plantilla propia y Cantera. Capturas en 1280×720, 1600×900 y 1920×1080: artifacts/career-visual/career-market-external-*.png.

TRX y resultados finales Debug/Release: apartado correspondiente de VALIDATION.md. Log nativo: artifacts/market-exclusion-native.log. Regresión de columnas: artifacts/market-exclusion-columns-regression.log. Los artefactos de pruebas son locales.

## Comprobación local
Desde la raíz:

```powershell
dotnet build Furvo.sln -c Debug
dotnet test Furvo.sln -c Debug
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
```

En Godot .NET, iniciar una carrera con club. Buscar por nombre completo a un jugador de su plantilla y comprobar que no aparece en Mercado. Buscar un jugador externo y abrir su ficha. Volver a Equipo → Plantilla y comprobar que el jugador propio continúa disponible.

No se inicia la siguiente entrega de Mercado.

Resultados finales ejecutados: compilaciones Debug/Release sin errores ni advertencias; 682 pruebas superadas en cada configuración, cero fallos y omisiones. 87 casos específicos de Mercado superados. Recorrido nativo de exclusión y regresión de columnas superados. Las cinco publicaciones iniciales conservan sus hashes SHA-256.
