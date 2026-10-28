# Corrección de navegación, menú y plantilla — 28/10/2026

## Cambios funcionales

La cabecera de carrera conserva únicamente Menú de partida como acceso a pausa/salida; se retira el botón directo Menú principal. El menú y Esc usan el mismo ExplorationMenu en contexto de carrera: Continuar partida cierra la ventana sin reconstruir la pantalla, Guardar/Cargar se muestran deshabilitados con explicación, Ajustes abre las opciones existentes, Volver al menú principal regresa al inicio y Salir termina Godot. No se simula un guardado.

La carrera actual solo permite consultas: no existe un borrador editable ni comandos de alineación, roles, condición o finanzas. Ordenar/seleccionar/navegar no produce cambios sin guardar. La creación contractual ya fue confirmada transaccionalmente antes de entrar. Por tanto no se solicita una confirmación ficticia de cambios inexistentes. Cuando exista edición pendiente real, las salidas deberán consultar su estado y confirmar descarte/guardado antes de abandonar. No se añade un indicador arbitrario de suciedad ni un guardado placebo.

## Separación de ámbitos

Antes de iniciar carrera se conserva el explorador de los veinte clubes con información, plantillas, economía, historia, elección y contratación. Ninguna de esas pantallas se elimina del flujo inicial.

En carrera, Club abre directamente Información y estadio del club contratado, con acceso a Economía e Historia y récords ya existentes. Directiva, filosofía, expectativas, palmarés y estadio se mantienen en sus componentes informativos; no se aprueban nuevos contenidos. No hay selector de los veinte equipos, Elegir club ni contratación inicial dentro de carrera.

PublicClubs permanece como identificador histórico de ruta no persistida, pero CareerWorkspace.CanOpen lo rechaza en todas las carreras, incluso cuando se intenta desde código. ShowClubs también impide ese acceso si existe una carrera activa. Un futuro explorador público necesitará su propio contexto explícito: no se vuelve a integrar en Club como selector de empleo.

En paro, Club conduce a NoClub, muestra Sin club y explica que no hay gestión disponible. Equipo permanece deshabilitado y RequireManagement rechaza clubes ajenos. La futura movilidad exige ofertas/vacantes y contratación reales, no navegación.

## Equipo > Plantilla

Pantalla específica Main.Squad.cs: cabecera compacta con club, recuento y temporada; una única DataTable de ocho columnas. No contiene ClubHeader, equipaciones, ClubTabs, paneles institucionales ni separación en tres tablas o grupos.

Orden exacto: Rol | Posición | Jugador | Calidad | Condición | Atributo 1 | Atributo 2 | Atributo 3.

Las 24 filas usan PlayerId estable y las proyecciones públicas del mundo de la carrera. La ficha completa se abre por activación/doble clic de la fila/nombre o por Ver futbolista; Volver mantiene la selección y orden. Los tres atributos ocupan tres celdas independientes con nombre y puntuación 1–20. Las celdas usan AttributeValueStyle, la misma escala cromática de la ficha; el número siempre está escrito, sin depender del color.

Rol: todos Sin asignar porque no hay alineación persistida. SquadAssignment prepara Titular, Suplente, Reserva y Sin asignar, sin generar ni persistir asignaciones. Calidad y Condición muestran —; no se promedian atributos ni se infiere condición de resistencia. La futura columna Ritmo será independiente y requerirá su propio dato persistido. No hay gestión de alineaciones ni efectos deportivos nuevos.

Orden inicial: Titular, Suplente, Reserva, Sin asignar; dentro de cada grupo demarcación futbolística, nombre ordinal sin distinción de mayúsculas y PlayerId como desempate. En los datos actuales todos están sin asignar. El orden de posiciones se declara explícitamente: POR, DFC, LD, LI, MCD, MC, MP, MD, MI, ED, EI, SD, DC; no es orden alfabético ni el entero bruto de PlayerRole (Striker=10).

Cabeceras alternativas habilitadas: Rol, Posición, Jugador y los tres atributos (orden numérico ascendente/descendente). Calidad/Condición no ofrecen una ordenación engañosa de datos inexistentes. Ordenar una columna de atributo compara sus valores, no calcula calidad ni combina las tres puntuaciones.

## Correspondencia por demarcación

Se revisaron PlayerRole (13 valores), PlayerAttributeCatalog y PositionInsightRules. La selección siguiente es descriptiva, fija y versionable por código, independiente de las fortalezas/debilidades y de cualquier futura ponderación posicional. No se seleccionan los tres valores más altos. Los laterales derecho/izquierdo comparten criterios por función; bandas y extremos se distinguen.

| PlayerRole | Código visible | Atributo 1 | Atributo 2 | Atributo 3 |
|---|---|---|---|---|
| Goalkeeper | POR | Reflejos (reflexes) | Uno contra uno (one_on_ones) | Colocación en portería (goalkeeping_positioning) |
| CentreBack | DFC | Marcaje (marking) | Entradas (tackling) | Colocación defensiva (defensive_positioning) |
| RightBack | LD | Entradas (tackling) | Velocidad (pace) | Centros (crossing) |
| LeftBack | LI | Entradas (tackling) | Velocidad (pace) | Centros (crossing) |
| DefensiveMidfielder | MCD | Anticipación (anticipation) | Colocación defensiva (defensive_positioning) | Pase corto (short_passing) |
| CentralMidfielder | MC | Pase corto (short_passing) | Visión de juego (vision) | Decisiones (decisions) |
| AttackingMidfielder | MP | Visión de juego (vision) | Técnica (technique) | Talento/creatividad (creativity) |
| RightMidfielder | MD | Centros (crossing) | Pase corto (short_passing) | Resistencia (stamina) |
| LeftMidfielder | MI | Centros (crossing) | Pase corto (short_passing) | Resistencia (stamina) |
| RightWinger | ED | Regate (dribbling) | Aceleración (acceleration) | Desmarques (off_the_ball) |
| LeftWinger | EI | Regate (dribbling) | Aceleración (acceleration) | Desmarques (off_the_ball) |
| SecondStriker | SD | Primer toque (first_touch) | Pase corto (short_passing) | Remate (finishing) |
| Striker | DC | Remate (finishing) | Desmarques (off_the_ball) | Serenidad (composure) |

CareerSquadRules.Create recibe PlayerDetails y opcionalmente una demarcación contextual; selecciona los mismos códigos y consulta sus valores reales. No cambia la posición ni atributos del jugador. Se filtran definiciones/categorías públicas; faltas, duplicados o valores fuera de rango provocan error comprensible, no sustituciones inventadas. Ninguno de los diez atributos ocultos forma parte de la tabla o de la proyección.

## Compatibilidad

Sin nuevas migraciones, publicaciones, roles en SQLite ni cambios contractuales. Se preservan las publicaciones v1–v5, los veinte clubes, 480 jugadores, sus 61 atributos, reputaciones, perfiles y carreras. Se conservan los cambios de encargos anteriores presentes en el árbol de trabajo; no hay commit ni push.

## Inventario de esta corrección

Creados:
- src/Furvo.Godot/Scripts/Main.Squad.cs: pantalla de plantilla deportiva.
- src/Furvo.Godot/Scripts/Components/CareerSquadRules.cs: correspondencia por demarcación, filas descriptivas y orden estable.
- tests/Furvo.Tests/CareerSquadRulesTests.cs: cobertura de trece posiciones, 480 jugadores, ordenación, contexto y permisos.
- docs/NAVIGATION_SQUAD_CORRECTION.md: este informe y tabla de correspondencias.

Modificados:
- src/Furvo.Core/Careers/CareerWorkspace.cs: denegación del selector y ruta explícita Sin club.
- src/Furvo.Godot/Scripts/Main.cs: separación del explorador inicial, regreso de jugador a la nueva plantilla y pie de Club sin selector.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: menú único, Club propio, NoClub y acceso a plantilla dedicada.
- src/Furvo.Godot/Scripts/Components/DataTable.cs: colores opcionales por celda, altura mínima y espaciado de fila, reutilizados sin duplicar tablas.
- src/Furvo.Godot/Scripts/Components/ExplorationMenu.cs: opciones de partida y guardado/carga claramente no disponibles.
- src/Furvo.Godot/Resources/UiStrings.resx: textos y abreviaturas de las trece demarcaciones.
- tests/Furvo.Tests/Furvo.Tests.csproj: enlace de las reglas puras reales a las pruebas sin cargar Godot.
- tests/Furvo.Tests/CareerNavigationTests.cs: expectativa actualizada de prohibición del selector.
- tools/GodotUiSmoke.gd: menú, Club y plantilla de ocho columnas; recorrido inicial y fichas conservados.
- docs/CAREER_NAVIGATION.md, ARCHITECTURE.md, CAREER_CREATION.md, VALIDATION.md, INVENTORY.md y README.md: estado vigente y resultados.

## Resultados ejecutados — 28/10/2026

- Debug y Release: cuatro proyectos compilan con 0 errores y 0 advertencias.
- Pruebas .NET: 249 superadas, 0 fallidas, 0 omitidas por configuración (244 previas y 5 casos nuevos). Se actualiza la prueba de la antigua ruta pública para exigir su denegación, sin eliminar cobertura. Los nuevos casos verifican trece demarcaciones, valores persistidos de los 480 jugadores, roles no inventados, orden determinista y cambio contextual sin mutar atributos.
- GodotUiSmoke.gd: recorrido completo sin ventana y con renderizado real, salida 0 y marcadores FURVO_WORKSPACE_OK para contratado/en paro, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Comprueba menú único, Continuar partida, regreso al inicio, selector inicial preservado, Club propio/Sin club, ocho columnas, 24 filas, altura uniforme, selección por ID después de ordenar en ambos sentidos, apertura por botón y activación de fila, 51 atributos públicos y regreso.
- GodotApplicationSmoke.gd: salida 0 en pantalla completa real; Esc abre el menú y Salir del juego cierra Godot correctamente.
- Revisión visual de capturas renderizadas de plantilla a 1280×720, 1600×900 y 1920×1080, menú de partida, Club propio y Sin club. Se revisaron alineación, textos, colores, scroll y botones; se redujo espaciado y se fijó altura uniforme. Esta inspección cubre las capturas indicadas, no una campaña manual exhaustiva de todos los clubes/equipos.
- SHA-256: catorce bases preexistentes idénticas, incluidas las cinco publicaciones del mundo. Sin migraciones ni modificaciones de jugadores, contratos, reputaciones, perfiles o partidas existentes.
- git diff --check sin incidencias. Sin dependencias nuevas, commit ni push.

Capturas locales reproducibles: artifacts/career-visual/career-squad-1280x720.png (también 1600x900/1920x1080), career-game-menu.png, career-own-club.png y career-no-club.png. Se generan con perfiles y carreras de prueba aislados.

## Validación

Resultados finales registrados en VALIDATION.md, diferenciando pruebas automáticas y revisión visual. Los perfiles y carreras de pruebas se aíslan en artifacts/temporales y nunca se usan para sobrescribir partidas personales.

## Comprobación manual

1. Nueva partida: revisar el explorador inicial de 20 clubes, elegir uno y firmar; las pantallas anteriores deben seguir disponibles.
2. En cabecera de carrera comprobar que solo está Menú de partida como acceso al menú. Abrir, Continuar partida, Ajustes, Volver al menú principal y Salir. Guardar/Cargar deben estar deshabilitados.
3. Club debe abrir el empleador real, sin selector ni Elegir club. En paro, Club muestra Sin club y Equipo no permite gestionar plantillas.
4. Equipo > Plantilla: ocho columnas, 24 filas, roles Sin asignar, calidad/condición —, tres atributos con valores; ordenar por rol, posición y nombre en ambos sentidos, seleccionar, abrir jugador y volver conservando su ID.
5. Revisar 1280×720, 1600×900 y 1920×1080, desplazamiento vertical y alineación. Comprobar salida en pantalla completa.

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd" -- --capture
& $env:GODOT4 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- fullscreen
~~~

No se implementan tácticas, lesiones, entrenamiento, condición, mercado, ofertas laborales ni nuevas fases de desarrollo.
