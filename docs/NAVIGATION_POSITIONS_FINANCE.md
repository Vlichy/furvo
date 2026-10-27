# Posiciones, navegación y terminología financiera

## Correspondencia exacta sin cambios persistentes

PlayerRole.RightMidfielder=8 y LeftMidfielder=9 pertenecen a PlayerPosition.Midfielder. Son diferentes de RightWinger=11 y LeftWinger=12, que pertenecen a Forward. La correspondencia de presentación pasa de Banda derecha/izquierda a **Centrocampista derecho/izquierdo**. No hay ambigüedad en los códigos tipados; no se cambian valores, datos ni secundarias. No se convierten en extremos ni se añade una función táctica.

La apertura inicial de cada club usa orden por posición: porteros, defensas, centrocampistas y delanteros; dentro, orden estable por códigos específicos existentes, nombre e ID. El usuario puede cambiar el orden y conservarlo al volver desde una ficha o cerrar Esc. El valor por defecto de la API GetSquad se conserva para consumidores anteriores; cambia el criterio inicial de la pantalla.

## Navegación de esta fase

Inicio: Nueva partida (solo abre explorador), Cargar partida (deshabilitado), Opciones y Salir del juego. Esc en exploración abre un único menú Continuar/Menú principal/Opciones/Salir; volver mediante Continuar conserva los nodos de pantalla y su selección. Esc no es un atajo de salida. Los diálogos usan cierre local y no apilan menús. Salir llama a SceneTree.Quit y cancela actualizaciones de cargas pendientes. No hay entrenador ni partidas.

Las opciones cambian solo la ventana del proceso actual después de Aplicar; Cancelar o Esc no aplican cambios. No se escribe configuración, registros del sistema ni preferencias permanentes. La resolución de ventana se limita al espacio disponible del monitor; en pantalla completa se usa la resolución del monitor y el selector queda deshabilitado.

## Contrato económico conceptual, sin contabilidad implementada

- Ingresos por abonos: importes monetarios, separados del número de abonados en Información.
- Compras/ventas de jugadores: separar pagos/cobros de reconocimiento contable cuando se implemente; no son ahora operaciones.
- Intereses devengados: gasto financiero, mostrado una sola vez en Gastos financieros, dentro de Gastos.
- Devolución o amortización del principal: disminuye deuda y tesorería, **no** es gasto de resultados. Se muestra como obligación/pago informativo, sin contabilizar.
- Deuda corta/larga, tipos de interés informativos, intereses devengados, amortización de principal, vencimientos y pagos pendientes tienen conceptos distintos. No se duplica el importe de intereses en Deuda.
- Presupuesto de fichajes no equivale a tesorería; fichajes restante no equivale a margen salarial restante. No se calculan importes ni fórmulas en esta fase.
- Estado financiero futuro (Saneada/Ajustada/En riesgo) deberá derivarse de indicadores económicos definidos y aprobados. Hasta entonces **No evaluado**, nunca un estado elegido por reputación o por ausencia de datos.
- El selector usa SeasonId y fechas/nombres de temporadas realmente existentes mediante IWorldQueries.GetSeasons. No inventa histórico financiero: la disponibilidad de una temporada del mundo no implica datos económicos disponibles. Si solo hay una temporada, muestra esa y deshabilita el cambio.

La cabecera financiera muestra Situación financiera, Presupuesto de fichajes, Margen salarial restante y Deuda total. Debajo: tesorería/presupuestos, ingresos, gastos, deuda/obligaciones y evolución financiera. Los importes siguen No disponible. No hay nuevas tablas, migraciones, publicaciones o dependencias.

## Archivos de este encargo

7 archivos creados y 9 modificados. Se mantienen los cambios locales acumulados de encargos anteriores; no se reconstruye el proyecto ni se añade una dependencia.

| Archivo | Estado | Función |
| --- | --- | --- |
| docs/NAVIGATION_POSITIONS_FINANCE.md | Creado | Correspondencia exacta de códigos, navegación, coherencia financiera y validación. |
| src/Furvo.Godot/Scripts/Components/SeasonSelector.cs | Creado | Selector de temporadas reales por SeasonId, sin inventar ejercicios. |
| src/Furvo.Godot/Scripts/Components/WindowUi.cs | Creado | Contenedores y controles comunes para ventanas modales. |
| src/Furvo.Godot/Scripts/Components/ExplorationMenu.cs | Creado | Menú Esc único con Continuar, volver, opciones y salida. |
| src/Furvo.Godot/Scripts/Components/DisplayOptionsWindow.cs | Creado | Modo y resolución de sesión, Aplicar/Cancelar explícitos, sin persistencia. |
| tests/Furvo.Tests/NavigationFinanceQueryTests.cs | Creado | Cinco casos sobre temporadas, grupos, nomenclatura y conceptos económicos. |
| tools/GodotApplicationSmoke.gd | Creado | Prueba con renderizado real de opciones, resolución, fullscreen y salida. |
| src/Furvo.Core/Queries/WorldQueries.cs | Modificado | GetSeasons de solo lectura y orden de posiciones por grupo, incluidas posiciones genéricas. |
| src/Furvo.Godot/Scripts/Main.cs | Modificado | Menú principal, criterio inicial de plantilla, Esc y composición económica con temporadas reales. |
| src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs | Modificado | Cabecera de cuatro indicadores y bloques contables conceptuales separados. |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado | Nombres concretos, menús, opciones y etiquetas económicas en español. |
| tools/GodotUiSmoke.gd | Modificado | Regresión de menús, Esc, orden inicial, temporadas e indicadores. |
| README.md | Modificado | Estado de desarrollo y comandos nuevos. |
| docs/CLUB_SCREEN.md | Modificado | Recorrido principal/explorador y terminología vigente. |
| docs/VALIDATION.md | Modificado | Resultados reales y límites visuales. |
| docs/INVENTORY.md | Modificado | Inventario de esta ampliación. |

## Validación ejecutada — 27/10/2026

- Compilación Debug de los cuatro proyectos: cero errores y cero advertencias.
- **155 pruebas superadas, 0 fallidas, 0 omitidas**: las 150 anteriores más cinco casos nuevos. No se ha repetido Release para este encargo.
- GodotUiSmoke.gd sin ventana: salida 0, FURVO_UI_SMOKE_OK. Recorre el menú principal, opciones/cancelación, Nueva partida, cuatro pestañas, orden por grupos, ordenación manual, Esc abrir/cerrar sin apilar, Opciones desde Esc, Continuar, volver al menú principal, selector único 2026/27, cuatro indicadores, No evaluado, intereses solo en Gastos financieros, plantillas de 24 y fichas de 51 atributos visibles. Incluye geometría a 960×640, 1280×720, 1600×900 y 1920×1080.
- GodotApplicationSmoke.gd **con OpenGL y ventanas reales**: ambos modos terminan con salida 0. En windowed se comprueba que Cancelar conserva modo/tamaño, Aplicar cambia efectivamente a 960×640 y Salir del menú principal cierra mediante SceneTree.Quit. En fullscreen se verifica el modo real de DisplayServer, Esc y salida desde el menú de exploración. No son simulaciones de DisplayServer ni una prueba headless.
- Las pruebas con renderizado comprueban comportamiento; no equivalen a inspeccionar visualmente todos los textos o todas las resoluciones. Queda revisión visual manual exhaustiva de menús, opciones y Economía en los tres tamaños solicitados.
- Cuatro publicaciones SQLite contrastadas por SHA-256 al principio y al final, sin cambios. No se modifican las entidades de jugadores ni sus atributos, contratos, reputaciones, temporadas persistidas o posiciones secundarias. No hay migraciones ni publicación nueva.

## Comandos locales

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln --no-build --no-restore
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- windowed
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- fullscreen
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot
~~~

GODOT4 apunta al ejecutable Godot 4.7.2 .NET instalado. Los dos smokes de aplicación abren y cierran sus propias instancias; no cierran otras ventanas del usuario. Para revisión manual: menú principal → Nueva partida → club → Plantilla/Economía; Esc → Continuar/Opciones; cambiar modos con Aplicar, salir desde ambos menús. No se ha probado exportación Windows. No se hace commit ni push.
