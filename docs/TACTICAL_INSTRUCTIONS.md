# Instrucciones tácticas y pizarra contextual

## Alcance

Ampliación de Estilo de juego, Transiciones y Balón parado. No hay motor de partidos, cálculo de eficacia ni IA rival. Se mantienen los tres planes, las dos disposiciones, las posiciones personalizadas y sus identidades. Los cambios de instrucción no mueven la formación persistida.

## Controles y representaciones

- Movimientos: mantener muestra referencias locales; intercambiar enlaza recíprocamente dos puestos próximos de la línea ofensiva; mixtos combina ambos. Es una explicación, no una orden ejecutada ni una evaluación de compatibilidad deportiva.
- Marcaje: zonal muestra áreas junto a sus responsables; al hombre añade rivales esquemáticos y conexiones; mixto combina dos responsables zonales y dos asignaciones.
- Recuperación: Contraatacar habilita cinco vías (derecha, izquierda, ambas, centro y largo). Conservar muestra apoyos próximos; equilibrio conserva una zona compensada. La vía queda guardada aunque el selector esté oculto al cambiar la recuperación.
- Portero: salida corta/larga/mixta con hasta dos receptores disponibles por distancia. Mixta combina ambos trazos; el destino preferente filtra y destaca las rutas. Si no hay receptores adecuados no se inventan futbolistas.
- Los pases cortos son discontinuos y los largos continuos. La carrera conserva su semántica independiente. Todas las flechas enlazan dos símbolos identificados y terminan antes del círculo objetivo.
- La pizarra de estilo/transición utiliza coordenadas exactas del plan y fase seleccionados. Las marcas de anchura/línea son referencias explicativas, no modificaciones de esas coordenadas.

## Faltas por ubicación

Las categorías ofensivas existentes (directa e indirecta) se conservan por compatibilidad. Cada una dispone de cuatro ubicaciones independientes, al igual que las faltas defensivas:

| Ubicación | Opciones ofensivas |
| --- | --- |
| Lateral izquierda/derecha | Centro, pase corto, ensayada |
| Central alejada | Disparo lejano, pase corto, balón al área, ensayada |
| Frontal | Disparo directo, pase corto, ensayada |

Los centros conservan destino y movimientos. Se mantienen lanzador, apoyo, rematador y jugador atrás. En defensa: marcaje, barrera de 0–4, integrantes explícitos y vigilancia de palos/rechaces. La posición tumbada solo se ofrece en ubicaciones centrales con barrera y nunca se asigna automáticamente. Las funciones activas no pueden compartir jugador, incluido lanzador. Los datos inactivos se conservan para volver a configuraciones anteriores.

Las asignaciones usan dos columnas; los controles dependientes y el desplazamiento permiten consultar el panel completo. Las advertencias identifican funciones pendientes o fuera del once sin inventar identidades. Las abreviaturas de símbolos pendientes (Z, M, P1/P2, R, TB, AP, ATR, REM, LAN) corresponden a las funciones con nombre completo en los controles. Los rivales son esquemáticos.

## Persistencia y compatibilidad

- Nueva instrucción nullable counter_route dentro de cada plan, junto a las instrucciones existentes. No tiene preferencia predeterminada inventada.
- Migración Tactics003_FreeKickLocations.sql: tabla career_free_kicks con clave club/tipo/ubicación, FK al club táctico y JSON validado. La validación de contenido, pertenencia y exclusión de funciones se realiza en Core antes del guardado transaccional.
- Las jugadas por ubicación siguen siendo comunes al equipo. No se copian dentro de cada plan.
- Lectura compatible de versiones 1 y 2 sin escribir ni migrar. Las claves nuevas se completan en memoria con null. Guardar aplica la migración y mantiene la revisión optimista y el rollback.
- Las jugadas legadas permanecen intactas: se muestran en Frontal para directa/defensiva y Central alejada para indirecta. Las otras ubicaciones empiezan pendientes. Un valor legado que no pertenece al nuevo catálogo contextual se conserva; no se sustituye por otra jugada automáticamente.
- No se publican nuevos mundos iniciales ni se modifican atributos, contratos o reputación. Las carreras solo cambian al editar/guardar explícitamente.

## Archivos de esta ampliación

| Archivo | Función |
| --- | --- |
| src/Furvo.Core/Tactics/TacticalSettings.cs | Ubicaciones, instrucciones, funciones y validación compatible |
| src/Furvo.Persistence/SqliteTacticsStore.cs | Lectura y escritura transaccional de ubicaciones |
| src/Furvo.Persistence/SqliteTacticalPlanStore.cs | Completar instrucciones legadas por plan sin escritura |
| src/Furvo.Persistence/Migrations/Tactics003_FreeKickLocations.sql | Migración nueva de carreras |
| src/Furvo.Godot/Scripts/Main.Tactics.cs | Selectores dependientes y contexto de ubicación; un único selector de fase |
| src/Furvo.Godot/Scripts/Main.SetPieces.cs | Catálogos contextuales y asignaciones compactas |
| src/Furvo.Godot/Scripts/Components/TacticDiagram.cs | Grafos explicativos y geometría contextual |
| src/Furvo.Godot/Scripts/Components/TacticalPreview.cs | Trazos, etiquetas compactas y composición |
| src/Furvo.Godot/Resources/UiStrings.resx | Textos españoles y leyenda actualizada |
| tests/Furvo.Tests/TacticalInstructionExpansionTests.cs | Diez casos nuevos de representación, persistencia y migración |
| tests/Furvo.Tests/TacticsTests.cs | Catálogo aprobado actualizado de 16 a 17 instrucciones |
| tools/GodotUiSmoke.gd | Recorrido anterior y nuevos selectores/contextos/capturas |
| docs/TACTICAL_INSTRUCTIONS.md | Informe técnico e inventario de esta ampliación |
| docs/VALIDATION.md, docs/INVENTORY.md, README.md | Estado actual y enlaces |

## Comprobación local Windows

Desde la carpeta del proyecto:

~~~powershell
dotnet build Furvo.sln -m:1
dotnet test Furvo.sln --no-build -m:1
dotnet build Furvo.sln -c Release -m:1
dotnet test Furvo.sln -c Release --no-build -m:1
~~~

En Godot, abrir src/Furvo.Godot/project.godot y ejecutar F5. En una carrera con club, visitar Equipo → Táctica, alternar plan/fase, probar los cinco contraataques y cada ubicación, salir y reabrir. Revisar manualmente DPI y resoluciones pequeñas; el recorrido automatizado no sustituye una revisión exhaustiva de todas las combinaciones.

Resultados ejecutados de esta entrega: véase VALIDATION.md. Capturas y logs de prueba están en artifacts, que no forma parte de la publicación del código.
