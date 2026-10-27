# Validación — ficha institucional de club, Entrega 4

Fecha: **27/10/2026**. Estado actual; las revisiones previas se conservan como histórico.

| Comprobación ejecutada | Resultado |
| --- | --- |
| dotnet build Furvo.sln --no-restore | Debug, cuatro proyectos: 0 errores y 0 advertencias |
| dotnet test Furvo.sln --no-restore | 129 aprobadas durante integración |
| dotnet test Furvo.sln --no-build --no-restore | Final: **129 aprobadas, 0 fallidas, 0 omitidas** |
| dotnet build Furvo.sln -c Release --no-restore | Release, cuatro proyectos: 0 errores y 0 advertencias |
| Godot 4.7.2 .NET, prueba de escena sin ventana | Código 0, FURVO_UI_SMOKE_OK |
| Entrada al club | Abre Información; pestañas Información/Plantilla/Historia/Economía en orden |
| Identidad | ClubId estable al cambiar de pestaña; datos conocidos proceden del mundo y campos futuros permanecen pendientes |
| Historia/economía | Tabla/gráfico sin resultados; sin títulos/personas/contadores ficticios ni importes económicos |
| Regresión | 24 jugadores por club, sort, selección, ficha con 51 atributos, protección de ocultos, regreso a Plantilla |
| Geometría de club | Cuatro pestañas a 960×640, 1280×720 y 1600×900, con navegación común accesible |

Nueve casos nuevos contrastan la ficha con SQLite para los veinte clubes, nulabilidad de campos no disponibles, historial vacío, errores de IDs, ausencia de cambios de bytes y gráfico genérico. Los resultados del gráfico usados en pruebas son fixtures, no datos del juego. Se conservan las 120 pruebas anteriores.

## Conservación y propuestas

No se modifican Persistence, v1/v2/v3/v4, sus datos o migraciones, los 480 futbolistas, contratos ni reputaciones. DataTable y PlayerSheet mantienen sus hashes. El esquema sigue siendo 4. No se han abierto ni modificado partidas reales. No se añaden paquetes.

CLUB_HISTORY_PROPOSAL.md contiene veinte borradores y sesenta personajes de archivo propuestos. Se verificaron cronologías, fundaciones, recuentos, puestos, títulos no duplicados, ascensos de campeones de Segunda y rivalidades simétricas. No está aprobado ni se embebe en el juego. No se presenta como historia real ni como fuente de cifras de UI. El diseño de persistencia queda en CLUB_HISTORY_SCHEMA_PROPOSAL.md, pendiente de aprobación y sin scripts SQL.

La clasificación acumulada tiene criterio documental explícito (3 puntos por victoria y 1 por empate en Primera, con desempates definidos), pero no se calcula sin estadísticas completas aprobadas. No se usa reputación como sustituto. Economía no tiene importes, cálculos ni consecuencias financieras.

## Ejecución y revisión local

Al finalizar el encargo se abre la versión actual con Godot a 1280×720, conforme a la instrucción del usuario de ejecutar cada tarea terminada. La ejecución no acredita por sí sola una inspección visual. Queda revisar manualmente:

1. Inicio → Clubes → Ver club / doble clic: debe abrir Información, con emblema y cabecera correctos.
2. Fundación, colores, tradición, nombre/capacidad del estadio deben coincidir con el catálogo. Abonados, apodos y métricas pendientes no deben mostrar cifras.
3. Cambiar por las cuatro pestañas y confirmar nombre/ClubId; Plantilla debe mantener sort y selección. Abrir un futbolista y volver: debe regresar a Plantilla.
4. Historia: bloques, tabla vacía y gráfico sin puntos ficticios. Desplazar hasta palmarés/leyendas/records. Economía: No disponible y conceptos diferenciados.
5. Probar 960×640, 1280×720 y 1600×900: legibilidad, escudo/estadio, textos largos, scroll y botones sin solapamientos. La prueba sin ventana verifica geometría y comportamiento, no apariencia.

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

No se prueba exportación Windows ni una migración histórica inexistente. No se inicia otra entrega. Guía de pantalla: [CLUB_SCREEN.md](CLUB_SCREEN.md); inventario: [INVENTORY.md](INVENTORY.md).

## Registro histórico anterior a esta ampliación


Fecha: **27/10/2026**. Este es el estado actual; las revisiones anteriores permanecen al final como registro histórico.

| Comprobación ejecutada | Resultado |
| --- | --- |
| dotnet build Furvo.sln --no-restore | Debug, cuatro proyectos: 0 errores, 0 advertencias |
| dotnet test Furvo.sln --no-restore | 120 aprobadas en la primera integración |
| dotnet test Furvo.sln --no-build --no-restore | Comprobación final: **120 aprobadas, 0 fallidas, 0 omitidas** |
| dotnet build Furvo.sln -c Release --no-restore | Release, cuatro proyectos: 0 errores, 0 advertencias |
| Prueba de escena Godot 4.7.2 .NET sin ventana | Código 0; FURVO_UI_SMOKE_OK |
| Posiciones | Portero con una marca principal y jugador con exactamente sus dos secundarias; comparación de las 480 fichas |
| Descripción | Solo códigos públicos relevantes; máximo tres por apartado; límites 14/7 y desempates ordinales |
| Regresión de ficha | Los mismos 51 atributos, cuatro bloques, avatar e identidad seleccionada |
| Geometría | Paneles debajo de los atributos, sin solaparse; laterales a 1280/1600 y apilados a 960; regreso siempre fuera del scroll |
| Protección de datos | Core, Persistence, publicaciones v1/v2/v3/v4, Main, DataTable y AttributeSection conservan sus hashes |
| Cabecera | Método BuildHeader idéntico a la versión anterior |

Se conservan las **108 pruebas anteriores** y se añaden doce casos de PositionInsightsTests y PitchPositionTests. Incluyen límites, empate estable con orden invertido y cultura tr-TR, ausencia de candidatos, exclusión de ocultos/categorías falsas, posiciones no inventadas, distancias suficientes entre marcas y contraste de los 480 jugadores con la publicación v4. No se elimina ni desactiva ninguna prueba.

## Escena real sin ventana

La prueba abre cuatro futbolistas de tres clubes: un portero, Adrián Alcendor Vallescuro (Mosaico) con principal 6 y secundarias 5/7 registradas y dos fichas adicionales. Recorre los 51 atributos públicos y verifica código/valor; comprueba las marcas, distinción principal/secundarias, descripciones solo de códigos visibles y relevantes para el portero, límites descriptivos y regreso con el mismo PlayerId.

A 1280 × 720 el contenido mide **767 píxeles**, con **453 visibles**. Se desplaza hasta el final para verificar acceso a ambos paneles sin cubrir el botón. También se comprueba adaptación a 960 × 640 y 1600 × 900. La configuración del campo deja espacio entre marcas cercanas; sus etiquetas de 28 px no se solapan en el tamaño mínimo dibujado.

Estas comprobaciones son funcionales/geométricas, **no inspección visual**. No se ha revisado visualmente esta ampliación ni una exportación Windows.

## Datos y alcance

No se modifica SQLite, ningún atributo, contrato, reputación ni identidad. No hay nueva publicación, migración, dependencia, simulación, alineación, táctica, valoración interna o sistema económico. La selección descriptiva es una política de presentación separada; no sustituye ni calcula una valoración general. La navegación existente no cambia.

## Comprobación local pendiente

Recompilar C# y abrir la versión actual en Godot .NET; cerrar ventanas de versiones anteriores para no inspeccionar otra compilación.

1. Abrir un portero: comprobar marca junto a su portería, naranja, leyenda y ausencia de posiciones secundarias inventadas.
2. Abrir un jugador con secundarias: comprobar todas las marcas y su distinción azul/naranja respecto a la cabecera. El ataque está indicado hacia la derecha.
3. Recorrer la zona hasta los nuevos paneles; comprobar líneas del campo, abreviaturas, lectura de fortalezas/debilidades, números y mensajes vacíos.
4. Confirmar los cuatro bloques y 51 atributos anteriores. No debe haber ocultos ni valoración global.
5. A 1280 × 720 y resoluciones superiores, revisar ambos paneles a izquierda/derecha y el regreso fijo. A 960 × 640 revisar apilado, textos y desplazamiento sin recortes.
6. Volver a plantilla y comprobar que sigue seleccionado el mismo jugador.

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release --no-restore
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

Detalles de configuración y criterios descriptivos en [PLAYER_SHEET_PANELS.md](PLAYER_SHEET_PANELS.md). Inventario actual en [INVENTORY.md](INVENTORY.md).

## Registro histórico anterior a esta ampliación


Fecha: **27/10/2026**. Estado actual; las revisiones anteriores se conservan más abajo como histórico.

| Comprobación ejecutada | Resultado |
| --- | --- |
| dotnet build Furvo.sln | Debug, cuatro proyectos: 0 errores, 0 advertencias |
| dotnet test Furvo.sln | **108 aprobadas, 0 fallidas, 0 omitidas**; conserva las 102 anteriores |
| dotnet build Furvo.sln -c Release --no-restore | Release, cuatro proyectos: 0 errores, 0 advertencias |
| Godot 4.7.2 .NET, prueba de escena sin ventana | FURVO_UI_SMOKE_OK, código 0 |
| Tabla de clubes | Anchos medidos 44,99 % / 29,99 % / 25,02 %; Club a la izquierda, Ciudad y Reputación centradas en encabezados/celdas |
| Ficha de tres clubes | Tres columnas y cuatro bloques; Físicos seguido de Portero; 51 códigos públicos únicos, todos 1–20; sin pestañas ni ocultos |
| Navegación | Selección, activación equivalente a doble clic/Enter, orden y regreso con el mismo PlayerId |
| Ventanas | Zona desplazable: 488 píxeles de contenido / 453 visibles a 1280 × 720; regreso accesible a 960 × 640 y 1600 × 900 |
| SQLite v4 | 20 clubes, 480 jugadores, 480 contratos, 29.280 valores de atributos; integrity_check=ok y sin FK huérfanas |
| Diferencias v3→v4 | Solo Club.Reputation de Fragua del Sur cambia 1→2; comparado el mundo completo tras restaurar ese único campo |
| Catálogo interno | Fragua 32/100; otros diecinueve valores exactos intactos; derivación aprobada comprobada para 0–100 |
| Publicaciones anteriores y migraciones | v1/v2/v3 y 001–004 conservan sus hashes |
| Repetir publicación real v4 | Misma huella y mismo hash físico, sin reescritura |

Las seis pruebas nuevas se encuentran en AdjustedWorldTests: comparación completa de entidades y atributos, tabla interna exacta, tramos/validación, publicación segura e idempotente, consultas v4 e integridad SQL. Los casos anteriores siguen comprobando sus publicaciones históricas; conservar una prueba que espera Fragua=1 en v3 no significa que la publicación activa v4 mantenga ese dato.

## Publicación nueva

Archivo: **data/initial-world-2026-27.v4.db**. WorldId: **72026027-0000-4000-8000-000000000005**. Esquema **4**, sin migración nueva. Huella canónica: **1d1e410db42f063891814f2599beb2e90dfdfd43aedb7a972d1f2e633f657b68**. SHA-256 físico: **0f179b93b178f6db1891a5520c0dab900a7fe08edcf7a2635147a506610f8275**.

El catálogo de reputación interna es src/Furvo.Persistence/Data/club-reputations-2026-27.v2.json; SQLite almacena la reputación visible derivada. No se añade una columna interna ni se implementa evolución temporal. Godot embebe v4 y usa la caché SHA-256 existente; las cachés anteriores no se borran. No se han abierto ni modificado partidas reales ni se aplica la corrección automáticamente a ellas.

## Repetir comprobaciones

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet run --file tools/BuildAdjustedWorld.cs -- data/initial-world-2026-27.v4.db data/initial-world-2026-27.v3.db
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

No se añade ninguna dependencia. La auditoría NuGet histórica no se ha repetido para estos ajustes; no se declara una auditoría nueva.

## Inspección visual pendiente

No se ha realizado una revisión visual de esta compilación final. En Godot .NET, recompilar C# y F5 (cerrar cualquier ventana anterior para evitar revisar una compilación antigua):

1. Clubes: comprobar 45/30/25, encabezados, valores centrados y los nombres largos a la izquierda; revisar salto de línea, desplazamiento, selección y doble clic.
2. Confirmar Fragua del Sur en 2/5 y que los otros diecinueve clubes no cambian.
3. Abrir fichas de distintos clubes: comprobar cuatro encabezados, Técnicos/Mentales en las primeras columnas y Físicos/Portero en la tercera, sin pestañas; recorrer hasta el último atributo.
4. Verificar lectura, colores, avatar y regreso correcto a plantilla a 1280 × 720; repetir a 960 × 640 y 1600 × 900. La prueba sin ventana comprueba geometría y funciones, no apariencia final.

No se probó exportación Windows. No se inicia otra entrega ni se añaden mecánicas. Inventario actual en [INVENTORY.md](INVENTORY.md).

## Registro histórico anterior a estos ajustes


## Estado actual: 27/10/2026

| Comprobación ejecutada | Resultado |
| --- | --- |
| dotnet build Furvo.sln --no-restore | Debug, cuatro proyectos: 0 errores, 0 advertencias |
| dotnet test Furvo.sln --no-restore | **102 aprobadas, 0 fallidas, 0 omitidas**; conserva las 90 anteriores |
| dotnet build Furvo.sln -c Release --no-restore | Release, cuatro proyectos: 0 errores, 0 advertencias |
| Importación de recursos con Godot 4.7.2 .NET | SVG provisional importado correctamente |
| tools/GodotUiSmoke.gd con Godot sin ventana | Código 0; FURVO_UI_SMOKE_OK |
| Fichas de tres clubes en escena real | 51 atributos públicos únicos, 1–20, sin ocultos, identidad y regreso correctos |
| Avatar y pestañas en escena real | Textura SVG cargada; Portero muestra diez atributos y permite volver a Atributos |
| Geometría a 1280 × 720 | Los 41 atributos generales caben; contenido/página 415/415 en las tres fichas |
| Geometría a 960 × 640 y 1600 × 900 | Botón de regreso dentro de la ventana |
| Hashes protegidos | Core, Persistence, sus recursos/migraciones, bases v1/v2/v3 y DataTable intactos |
| Inicio, clubes y plantilla | Métodos idénticos a la versión previa al rediseño |

Doce casos nuevos comprueban límites cromáticos, rechazo de números fuera de 1–20, rutas locales por identidad estable y avatar SVG sin imágenes externas. No se añade ningún paquete, migración ni mecánica. La auditoría NuGet que figura más abajo pertenece a la revisión anterior; no se afirma haberla repetido para este ajuste visual.

### Revisión visual y límites

Se observó en Godot una ficha compacta de **Adrián Alcendor Vallescuro (Mosaico)** con silueta, cabecera completa, posiciones secundarias, tres columnas y colores numéricos. Tras esa observación se ajustó el espacio vertical y se aprobó la prueba de escena final. El usuario detuvo Computer Use mediante Escape antes de otra inspección; no se realizaron más interacciones.

**Pendiente visualmente:** abrir la compilación final a 1280 × 720; revisar legibilidad, color y alineación; consultar Portero y regresar; repetir con jugadores de otros clubes y redimensionar a 960 × 640 y 1600 × 900. Estas operaciones se comprueban funcional/geométricamente en la escena sin ventana, pero eso no acredita inspección visual. No se probó una exportación Windows.

La ficha compacta sustituye las tablas grandes y la antigua organización de cuatro paneles con barra exterior. Las descripciones de ese diseño que aparecen en el registro histórico siguiente no representan el diseño actual. Los informes DELIVERY4_VALIDATION.json y la auditoría anterior se conservan como evidencias de su revisión, no como resultados del rediseño.

Detalles: [PLAYER_SHEET.md](PLAYER_SHEET.md), [inventario](INVENTORY.md). No se comienza otra entrega.

## Registro histórico: revisión previa al rediseño


Fecha de comprobación: **27/10/2026**, Windows, SDK .NET 10.0.402. Evidencia resumida verificable en [DELIVERY4_VALIDATION.json](development/DELIVERY4_VALIDATION.json). El estado diferencia pruebas de servicios, observación visual y comprobaciones pendientes.

## Comprobaciones ejecutadas

| Comprobación | Resultado |
| --- | --- |
| Línea base anterior, dotnet test tests/Furvo.Tests/Furvo.Tests.csproj --no-restore | 74 aprobadas antes de cambios |
| dotnet build Furvo.sln | Cuatro proyectos Debug; 0 errores, 0 advertencias |
| dotnet test Furvo.sln | **90 aprobadas, 0 fallidas, 0 omitidas** |
| dotnet build Furvo.sln -c Release --no-restore | Cuatro proyectos Release; 0 errores, 0 advertencias |
| dotnet list Furvo.sln package --include-transitive --vulnerable | Ninguna vulnerabilidad conocida detectada en los cuatro proyectos |
| SHA-256 de v1/v2/v3 y migraciones 001–004 antes/después | Todos intactos |
| Godot ejecutado | 4.7.2.stable.mono.official.ed1daf0bf, OpenGL Compatibility |

Se conservan sin eliminar ni desactivar las **74 pruebas anteriores**. Se añaden **16 casos** entre WorldQueryTests y ReputationDecisionTests. Ninguna dependencia NuGet ni versión de SDK se cambia. La auditoría describe los avisos conocidos al ejecutarla, no garantiza ausencia de avisos futuros.

## Cobertura de servicios y datos

- Abrir el SQLite publicado real en modo de lectura: veinte clubes, 480 jugadores, 24 por club y tres porteros por plantilla. Comparar hash antes/después.
- Las 480 fichas contienen exactamente 51 valores visibles en grupos 17/16/8/10, todos dentro de 1–20. Comprobar códigos y serialización de DTO para excluir los diez ocultos; ningún DTO devuelve Player, PlayerAttributes o WorldData.
- Ordenación tipada por nombre, posición y edad, en ambos sentidos y sin pérdida de jugadores. Orden estable ante entradas invertidas y cultura tr-TR. Recuperación de la misma ficha por ID tras cada ordenación.
- Afiliación por contratos inclusivos; no aceptar club inexistente, jugador inexistente ni jugador de otro club. Una ficha con atributos incompletos falla expresamente.
- Base ausente: error sin crearla. Base corrupta: error sin cambiarla. Archivo de partida: rechazo como catálogo, conservando bytes.
- Caché repetible y verificada por hash; una copia alterada se rechaza sin reparación ni sobrescritura. No quedan temporales de esa operación.
- Recursos españoles cubren todos los atributos visibles y posiciones; no incluyen etiquetas de ocultos.
- Veinte reputaciones internas exactas asociadas a ClubId en la decisión documental, su reputación visible coincide con la publicación; los tramos cubren 0–100 una vez, incluidos extremos.

## Revisión visual efectivamente realizada

El usuario aportó capturas y confirmó **Inicio → Clubes → Plantilla del Atlético Mosaico de Málaga**. Mediante Computer Use se observaron clubes y plantilla y se hizo una selección real de **Asier Villabrisa Valcárcel**: el botón Ver futbolista se habilitó y abrió su ficha. Se observaron nombre, club, posición, 23 años, España, nacimiento 26/09/2002, secundarias Ninguna, categorías técnicas/mentales y valores visibles. El resaltado inicial era distinto de la selección efectiva: no se constató un fallo de selección.

Se detectó desplazamiento anidado en la ficha; se ajustó la altura de categorías para que el desplazamiento sea exterior. También se reforzaron el error de ficha incompleta, mensajes de consulta y mínimos de ventana. **Los ajustes finales compilan y pasan pruebas de servicios, pero no se han vuelto a comprobar visualmente.**

El usuario detuvo Computer Use mediante Escape; se detuvieron las interacciones. No se completó el recorrido visual de regreso, la revisión de otras resoluciones ni la inspección visual de los 51 atributos. La prueba adicional headless no se ejecutó porque se rechazó su solicitud. No se declara aprobada por estar incluida. No se ha probado una exportación Windows.

## Correcciones de esta revisión

- Categorías con altura ajustada y barra exterior para evitar barras verticales anidadas. La selección restaurada se desplaza a una posición visible.
- Consulta de ficha incompleta produce error explícito; no muestra silenciosamente menos de 51 atributos.
- Mensajes contextualizados para clubes, plantillas y futbolistas, con reintento/regreso sin cerrar el juego.
- Datos de nacimiento/secundarias con ajuste de línea y tamaño mínimo 960 × 640; resolución inicial 1280 × 720.
- Prueba adicional que verifica identidad seleccionada tras ordenar y rechazo de atributos incompletos.

## Migraciones y preservación

**Ninguna migración nueva. Esquema actual 4.** La reputación interna 0–100 está consolidada como decisión, no aplicada al almacenamiento actual 1–5. No se crea un mundo v4 ni se inyectan datos en partidas antiguas.

| Archivo | SHA-256 intacto |
| --- | --- |
| data/initial-world-2026-27.db | d4949e610ba88f4f80864c9a4637412063e411a61a9f6068e9902f9d176a2a0e |
| data/initial-world-2026-27.v2.db | d1c9a4905a49e1ea36c383d460f07739c34e4ee785699f84533c1b797a45c597 |
| data/initial-world-2026-27.v3.db | 4666ae27c4804abc130f6d13fce9b7cfc57cf8b96844309886b3b263a859991a |

Los cuatro hashes SQL están en el informe JSON. No se abrieron ni modificaron partidas reales. La UI únicamente materializa la publicación embebida en una caché identificada por hash y la consulta con SqliteWorldStore de lectura. No crea SaveId, entrenador ni nueva partida. La API de guardado anterior sigue cubierta por sus pruebas, sin integrarse en la interfaz.

## Repetir en Windows / VS Code

Desde la raíz:

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release --no-restore
dotnet list Furvo.sln package --include-transitive --vulnerable
~~~

Importar src/Furvo.Godot/project.godot en Godot .NET, compilar C# y F5. Debe usarse la compilación actualizada, no la ventana que permaneciera abierta de una anterior.

1. En 1280 × 720, abrir clubes, seleccionar Mosaico y comprobar que Ver plantilla se habilita. Desplazar hasta el final de la lista.
2. Seleccionar una fila de plantilla, anotar nombre y abrir ficha. Comprobar identidad, nacimiento/edad, club, nacionalidad y posiciones. Probar también un jugador con secundarias.
3. Recorrer **los cuatro grupos completos**: 17 técnicos, 16 mentales, 8 físicos y 10 de portero, con valores 1–20. Confirmar ausencia de ocultos y de valoración global. Verificar el ajuste final a una sola barra exterior.
4. Volver a plantilla; comprobar selección por nombre/ID al ordenar Nombre, Posición y Edad en ambos sentidos. Abrir de nuevo el registro seleccionado, incluso después de desplazarse.
5. Volver Plantilla → Clubes → Inicio y repetir el recorrido. No deben quedar pantallas vacías ni botones apuntando a otra fila.
6. Redimensionar a 960 × 640 y 1600 × 900; comprobar alineación, lectura, botones, ajuste de texto y desplazamiento. Probar ratón y teclado (flechas/Enter/Tab).
7. Los errores de archivo y entidades se cubren por servicios. La inspección visual de sus mensajes está pendiente; cualquier ensayo manual debe utilizar únicamente copias temporales o una instalación de prueba. No borrar, renombrar ni corromper publicaciones o partidas reales para probarlos.

Prueba opcional de escena, pendiente de ejecutar después de compilar, con GODOT4 apuntando a Godot .NET:

~~~powershell
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

Su salida esperada es FURVO_UI_SMOKE_OK y código 0. Automatiza inicio, carga, selección, orden, conteos de atributos, ausencia de códigos ocultos y regreso; no sustituye inspección de legibilidad. El script está incluido sin afirmar que haya pasado.

## Estado para cierre

La implementación está compilada, cubierta por 90 casos y **lista para validación final local**. El cierre visual completo queda condicionado al listado anterior. No se comienza valoración interna, motor, mercado, economía, IA, calendarios, Segunda ni otra entrega. Los requisitos futuros se documentan en [DECISIONS.md](DECISIONS.md) y [V1_CONTRACT.md](V1_CONTRACT.md).

## Rediseño definitivo de ficha de club — 27/10/2026

Estado actual posterior a las secciones anteriores: **138 pruebas superadas** (129 anteriores + 9 casos nuevos), tanto Debug como Release, sin errores ni advertencias de compilación. Prueba de escena Godot final con código 0. Se amplía la geometría a 1920×1080 y se protegen identidades provisionales, campo horizontal, categorías independientes y campos desconocidos. Se inspeccionó Información de Mosaico en ventana real a 1280×720; queda revisión visual completa de pestañas y resoluciones, además de exportación Windows.

[Informe detallado, límites y comandos](CLUB_PRESENTATION_REDESIGN.md). [Categorías propuestas, no publicadas](STADIUM_CATEGORY_PROPOSAL.md). Cuatro bases originales idénticas por SHA-256; ningún cambio en Persistence/migraciones ni datos de jugadores, contratos o reputación.

## Filosofía y expectativas — 27/10/2026

Estado posterior: **150 pruebas Debug superadas**, cero fallidas/omitidas, compilación sin errores ni advertencias. Se conservan las 138 anteriores. Smoke Godot final correcto, con paneles de borradores y geometría a 960×640, 1280×720, 1600×900 y 1920×1080; no implica aprobación visual humana. Release no ejecutado para esta ampliación. Cuatro SQLite intactos por SHA-256; sin migración/publicación nueva ni mecánicas de directiva.

[Veinte propuestas, criterios editoriales, fuentes, archivos y comandos](CLUB_DIRECTION_PROPOSAL.md). Los textos mostrados son borradores expresamente identificados, no objetivos vigentes.

Ejecución final confirmada en una nueva ventana de Godot. Inspección visual de ambos paneles de Mosaico en ventana ampliada: principios, tres horizontes, prioridades, plazos y avisos legibles; botón de regreso visible. Pendiente revisión visual exhaustiva de los veinte clubes/tamaños.

## Corrección de maquetación y elección provisional — 27/10/2026

Compilación Debug final sin errores/advertencias; 150 pruebas conservadas y superadas. Smoke final correcto con proporciones 35/30/10/25, doce récords contenidos, exclusividad de Palmarés/Leyendas y aviso exacto desde las cuatro pestañas. Geometría comprobada a 960×640, 1280×720, 1600×900 y 1920×1080. La inspección visual real fue parcial en ventanas ampliadas; no se declara validación visual completa de los tres tamaños exactos. Cuatro SQLite intactos. [Diagnóstico, archivos, límites y comandos](UI_LAYOUT_FIX.md).

Observación adicional: Plantilla comprobada visualmente a pantalla completa de 1920×1080, con cabecera y pie visibles. Permanecen pendientes la inspección visual exacta de 1280×720 y 1600×900 y la revisión final visual de Récords/aviso.

## Posiciones, menús y coherencia económica — 27/10/2026

Estado vigente: **155 pruebas Debug superadas**, cero fallidas/omitidas, compilación sin errores/advertencias. Smoke headless de escena correcto, incluyendo menús, Esc, continuidad, posiciones y Economía. GodotApplicationSmoke.gd pasa con renderizado OpenGL real tanto en ventana como en pantalla completa: Cancelar no aplica cambios, Aplicar cambia resolución y Salir finaliza con código 0 desde menú principal y Esc. No se afirma inspección visual exhaustiva por esos resultados. Release no repetido. Cuatro bases intactas y ningún cambio persistente de posiciones. [Informe, archivos y comandos](NAVIGATION_POSITIONS_FINANCE.md).
