## Estado vigente — planes tácticos y seis disposiciones

Inicial, Ganando y Perdiendo tienen dos disposiciones propias y un único once por plan; mentalidad e instrucciones independientes y balón parado común. Se conserva una fuente canónica de composición, con lectura compatible y conversión transaccional al guardar. [Modelo, migración, archivos y comprobación](TACTICAL_PLANS.md). Las descripciones anteriores de una táctica única se conservan como historial.

## Segunda iteración de Táctica

Arrastre nativo, formación reubicada, selector Con balón/Sin balón, diagramas enlazados y nuevas asignaciones de balón parado. Extensión táctica versión 2 bajo guardado explícito, compatible con lecturas v1. [Informe e inventario de esta iteración](TACTICS_ITERATION_2.md). Este apartado actualiza el alcance descrito a continuación, que se conserva como historial.

# Equipo → Táctica

## Alcance implementado
Una táctica activa por club dirigido y carrera. La ruta anterior Equipo → Alineación se integra en Equipo → Táctica; el identificador interno CareerPage.Lineup se conserva por compatibilidad, sin duplicar pantallas. Submenú: Alineación, Estilo de juego, Transiciones y Balón parado. Mentalidad común bajo ese submenú: Muy defensiva, Defensiva, Equilibrada, Ofensiva y Muy ofensiva.

Sin preferencias inventadas: mentalidad, instrucciones, rutinas y lanzadores comienzan Sin definir / Sin asignar si no existe una configuración guardada. La primera elección crea la extensión y guarda esa elección; el resto permanece null. La mentalidad representa riesgo general y no cambia presión, línea ni pases.

## Datos estructurados de Core
TacticalSettings contiene ClubId, Revision, TeamMentality nullable, diccionario de dieciséis instrucciones con códigos estables y siete SetPiecePlan. TacticalCatalog valida el conjunto exacto de códigos, opciones admitidas y pertenencia de lanzadores. El formato de datos es versión 1.

| Grupo | Instrucciones |
|---|---|
| Con balón | passing, tempo, attack_width, freedom, movement, progression |
| Sin balón | pressing, defensive_line, marking, defend_width, organization, tackling |
| Transiciones | recovery, loss, keeper, keeper_target |

Los valores son identificadores de configuración, no textos traducidos ni fórmulas. Los textos se encuentran en UiStrings.resx. No se calculan ventajas, multiplicadores, química, calidad posicional ni estadísticas.

## Alineación reutilizada
Se mantienen catálogo de once formaciones, selección inicial, semilla, once/suplentes/reservas y persistencia de la entrega anterior. Véase LINEUPS.md. Se reutiliza FootballPitch con orientación vertical opcional; los usos horizontales del explorador permanecen intactos.

Once círculos, códigos y nombres abreviados blancos. Relleno según posiciones reales: verde natural, ámbar secundaria, rojo fuera de posición. El borde indica selección/foco sin reemplazar ese relleno. Tooltip con nombre completo y demarcación; el panel añade clasificación textual. El ancho de los nombres se adapta a la separación entre puestos, con elipsis y tooltip completo, para evitar colisiones; el campo mantiene proporción 68 × 105.

El panel derecho contiene suplentes y reservas, con orden de elegibilidad y criterio provisional anterior. Sin columna de titulares. Seleccionar un puesto permite cambiarlo por un jugador elegible del banquillo/reservas. El selector contextual Intercambiar con otro titular permite intercambios si ambos quedan en puestos compatibles. Las asignaciones incompatibles se rechazan y conservan el once anterior. No se inventan posiciones secundarias. Cada operación válida guarda inmediatamente y mantiene once jugadores únicos.

Los paneles permiten desplazamiento cuando falta espacio. El campo y controles se adaptan a las resoluciones objetivo; se conserva una altura mínima para mantener códigos legibles.

## Estilo y transiciones
Con balón y Sin balón conviven en el mismo panel desplazable. Campo vertical explicativo a la izquierda. Al enfocar o modificar una instrucción se actualizan contexto y vista previa:
- Anchuras: separación horizontal; línea: altura orientativa de defensas.
- Presión: zona destacada. Progresión: flechas por bandas/centro. Pases: trayectoria según longitud.
- Ritmo, libertad, movimientos, marcaje, organización e intensidad: explicación contextual, sin animación arbitraria.
- Recuperación, pérdida y distribución del portero se representan de forma separada, según el control enfocado. No se superponen acciones de recuperación y pérdida.
- Destino preferente del portero y tipo de salida son preferencias independientes; seleccionar uno no modifica el otro.

Las vistas son esquemas declarativos y no resultados de un motor.

## Balón parado mínimo
Siete apartados: córners ofensivos y defensivos, faltas directas e indirectas, organización defensiva en faltas, penaltis y saques de banda. Un selector cambia la situación representada y los controles aplicables.
- Córner ofensivo: primer palo, segundo palo, corto o mixto.
- Córner defensivo / defensa de faltas: zonal, mixto o al hombre.
- Falta directa y penalti: lanzamiento directo.
- Falta indirecta: área, corto o mixto.
- Banda: corto, largo o mixto.

Lanzador opcional en jugadas ofensivas, faltas, penaltis y bandas, elegido entre los 24 futbolistas contratados del club. Sin autoselección por atributos ni histórico inventado. No se exige que el designado esté en el once: es una preferencia previa, no una ejecución de partido. Las jugadas defensivas no admiten lanzador. Una misma persona puede ser designada para tipos diferentes; no se crean duplicados en la alineación. No hay editor de movimientos, rutinas múltiples ni motor de acciones.

## SQLite y compatibilidad
Nueva extensión independiente Tactics001_Initial.sql, versión 1, ejecutada únicamente al guardar una configuración:
- tactics_schema_version: versión de extensión.
- career_tactics: club, revisión, mentalidad, instrucciones JSON y versión de datos.
- career_set_pieces: tipo, rutina y lanzador por club.

FK a club y jugador, unicidad por club/tipo y restricciones de rango. Triggers verifican propietario empleado y pertenencia contractual del lanzador al insertar/actualizar. Core valida opciones antes de escribir. Guardado transaccional de cabecera y siete jugadas; revisión optimista rechaza ediciones obsoletas. Si falla cualquier escritura, se conserva el estado anterior.

SqliteLineupStore.Owner se comparte internamente para comprobar Save, empleo, club y fecha. No cambia el esquema general del mundo, perfiles ni contratos. Load es solo lectura; no migra carreras anteriores ni genera alineaciones. No se modifican publicaciones SQLite ni se añade un flujo general de cargar partidas.

## Archivos
| Estado | Archivo | Función |
|---|---|---|
| Nuevo | src/Furvo.Core/Tactics/TacticalSettings.cs | Modelo, catálogo y validación |
| Nuevo | src/Furvo.Persistence/SqliteTacticsStore.cs | Lectura/guardado transaccional |
| Nuevo | src/Furvo.Persistence/Migrations/Tactics001_Initial.sql | Extensión de carrera versión 1 |
| Modificado | src/Furvo.Persistence/SqliteLineupStore.cs | Reutilización interna de comprobación de propietario |
| Nuevo | src/Furvo.Godot/Scripts/Main.Tactics.cs | Pantalla común, mentalidad e instrucciones |
| Modificado | src/Furvo.Godot/Scripts/Main.Lineups.cs | Alineación integrada, banco/reservas e intercambio |
| Modificado | src/Furvo.Godot/Scripts/Main.Navigation.cs | Entrada Táctica y limpieza de selección entre carreras |
| Modificado | src/Furvo.Godot/Scripts/Components/FootballPitch.cs | Orientación vertical opcional |
| Modificado | src/Furvo.Godot/Scripts/Components/InteractiveLineupPitch.cs | Colores reales y distribución vertical |
| Nuevo | src/Furvo.Godot/Scripts/Components/TacticalPreview.cs | Esquemas reactivos explicativos |
| Modificado | src/Furvo.Godot/Resources/UiStrings.resx | Textos españoles centralizados |
| Nuevo | tests/Furvo.Tests/TacticsTests.cs | Nueve pruebas de modelos/persistencia |
| Modificado | tools/GodotUiSmoke.gd | Regresión y recorrido de las cuatro pestañas |
| Nuevo | docs/TACTICS.md | Decisiones, alcance e inventario |
| Modificados | README.md, docs/ARCHITECTURE.md, docs/INVENTORY.md, docs/VALIDATION.md, docs/CAREER_NAVIGATION.md, docs/LINEUPS.md | Estado y validación vigentes |

Sin dependencias nuevas. Sin motor de partidos, entrenamientos, mercado, química ni IA.

## Comprobación local
Desde la raíz en PowerShell:

```powershell
dotnet build Furvo.sln -m:1
dotnet test Furvo.sln --no-build -m:1
dotnet build Furvo.sln -c Release -m:1
dotnet test Furvo.sln -c Release --no-build -m:1
```

Abrir src/Furvo.Godot/project.godot en Godot .NET y F5. Crear una carrera de prueba con club → Equipo → Táctica. Revisar las cuatro pestañas; cambiar mentalidad e instrucciones, volver a Plantilla y reabrir Táctica; comprobar lo elegido. En Alineación: seleccionar círculo, cambiar jugador y formación, comprobar once y roles. En Balón parado: elegir jugada, rutina y lanzador; cambiar de pestaña y regresar. Revisar desplazamiento, teclado y nombres largos a 1280×720, 1600×900 y 1920×1080.

La recuperación al volver a abrir el archivo de carrera se prueba mediante servicios SQLite: el menú general de Cargar no se habilita en esta entrega.
