# Equipo → Táctica — segunda iteración

## Cambios funcionales
Se conserva la pantalla existente y las cuatro pestañas. Cabecera: club, submenú, Formación y Mentalidad. Formación se edita solo en Alineación; el resto de pestañas lee el mismo CareerLineup persistido. Estilo de juego incorpora Con balón / Sin balón: solo se muestran controles de la fase activa y ninguna opción se reinicia al alternar.

Sin configurar significa ausencia de instrucción específica, no un error. Las instrucciones elegidas se distinguen del estado neutro mediante texto y color; no hay efectos deportivos ni valores predeterminados inventados.

## Arrastrar y soltar
LineupDragButton utiliza _GetDragData, _CanDropData y _DropData de Godot. El payload identifica PlayerId, carrera y revisión; no posiciones de listas. Un preview semitransparente acompaña al puntero. El destino resalta en verde/rojo y Godot muestra su cursor de aceptación/rechazo. Escape cancela el arrastre sin abrir el menú; soltar fuera no escribe.

LineupSelection.Exchange intercambia ambos miembros completos (rol y puesto), conservando los 24 identificadores, once titulares y límites de banquillo. Admite ambos sentidos titular/suplente, titular/reserva y titular/titular. No incorpora un movimiento nuevo entre suplentes y reservas: anteriormente no existía. Se rechazan jugadores ajenos, misma identidad, destinos sin titular y portero/campo incompatible.

Se permiten intercambios de jugadores de campo fuera de sus posiciones registradas: se muestran en rojo y no se aplican penalizaciones. Esta ampliación no modifica sus posiciones naturales/secundarias ni el método Replace anterior. El clic y selector de intercambio usan el mismo Exchange que el drop.

Solo un drop válido o confirmación por clic guarda mediante SqliteLineupStore; iniciar, sobrevolar o cancelar no escribe. Si una revisión cambió se rechaza el guardado sin pisar la vigente. La reconstrucción del control se difiere hasta después de la entrega nativa del drop.

Campo y banquillo comparten un bloque centrado cuyo ancho se calcula por la altura/proporción del césped; no hay columna de titulares ni una gran separación vacía. Suplentes y Reservas tienen scroll interno independiente. Filas: nombre, posición natural y secundarias; adecuación únicamente cuando existe un puesto de destino seleccionado. Tooltip completo y nombres abreviados en campo.

## Diagramas explicativos
TacticDiagram es un grafo puro y comprobable: símbolos con identidad, zonas y aristas que referencian símbolos existentes. TacticalPreview calcula cada extremo en el perímetro del círculo y dibuja la punta hacia dentro del segmento, sin sobrepasar el destino.
- Continua: pase/trayectoria del balón.
- Discontinua: movimiento del jugador hacia un objetivo marcado.
- Sombreado/contorno: zona de ocupación, cobertura o presión.
- Borde rosa: función individual asignada a un jugador respecto a R1, R2 o R3.
- Círculo rival hueco: referencia rival esquemática, sin identidad nueva ni oponente de un partido generado.

Estilo y Transiciones conservan la formación real; la recuperación y pérdida se presentan por separado. Anchuras/línea alteran los puntos y las flechas se recalculan desde ellos. Progresión y presión utilizan zonas. Las instrucciones sin representación espacial clara tienen explicación en lugar de animación arbitraria. El marcaje colectivo al hombre en Estilo no inventa asignaciones individuales.

Balón parado utiliza zoom contextual sobre el mismo campo. Las funciones concretas muestran PlayerId y nombre abreviado del futbolista elegido. ? identifica funciones pendientes; * identifica una preferencia guardada cuyo jugador está fuera del once actual. No se reasignan funciones automáticamente cuando cambia el once: se conserva la preferencia y se señala pendiente de revisión.

## Defensa a balón parado
Al hombre: Marker1–3 frente a referencias R1–3, con borde solo si el marcador está asignado y pertenece al once. Zonal: Zone1–3 y áreas sombreadas. Mixto: dos zonas y dos marcas individuales, con funciones diferenciadas.

Faltas defensivas: barrera sin configurar, sin barrera o de 2/3/4 jugadores. Hombre tumbado Sí/No es contextual a una barrera activa. Wall1–4 y Prone son asignaciones explícitas, distintas de los marcadores y entre sí. En dibujo B1–B4 se corresponden con los selectores de barrera. El tumbado usa un símbolo horizontal y no otro círculo de pie. Si se desactiva la barrera, sus preferencias se conservan inactivas; no se dibujan ni actúan como funciones simultáneas.

## Ataque a balón parado
Faltas directas e indirectas: disparo directo, pase corto, centro al área y jugada ensayada. El centro admite primer/segundo palo, centro o mixto, y ataque en carrera/llegada de segunda línea/combinado. Las opciones legadas área/mixto permanecen interpretables. La variante predefinida es pase previo y desmarque; no se añade un editor libre.

Taker, Receiver, Finisher, Header y StayBack son funciones explícitas según la jugada. No se asignan por calidad. Las opciones de destino/movimiento sin elegir se conservan pendientes; no se convierten en instrucciones guardadas por dibujar una plantilla orientativa.

Bandas: corto, largo al área, prolongación de cabeza, al espacio, atrás y ensayado. Cada variante usa un grafo distinto por posiciones y/o enlaces. La prolongación enlaza lanzador → cabeceador → receptor. El espacio y el ensayado distinguen pase continuo y desmarque discontinuo. El valor mixto anterior se conserva como configuración legada.

## Tanda de penaltis
Únicamente cinco puestos ordenados, modificables y con jugadores del club sin repetir. Los huecos son explícitos y el campo no muestra estrategias ni probabilidades. Las listas excluyen los ya elegidos en otros puestos.

El antiguo Taker de Penalty de la versión 1 se conserva para compatibilidad, pero no se copia a la tanda ni se presenta como lanzador habitual del partido. El lanzador habitual sigue fuera del alcance.

## Modelo, validación y migración
SetPiecePlan añade Details opcional. SetPieceDetails guarda destino, movimiento, variante, tamaño de barrera, tumbado y asignaciones por SetPieceRole. TacticalSettings añade PenaltyTakers, con cinco huecos cuando no existe dato anterior.

TacticalCatalog valida opciones y pertenencia al club. Solo las funciones activas en la jugada cuentan como simultáneas; no pueden compartir futbolista entre sí ni con el lanzador. Las preferencias inactivas se conservan. Las nuevas asignaciones de funciones se eligen del once actual. Las antiguas que pasan al banquillo quedan señaladas como fuera del once, sin borrado silencioso. Para la tanda se permiten los 24 miembros del club.

Tactics002_SetPieceAssignments.sql amplía la extensión táctica a versión 2: details_json nullable en career_set_pieces y career_penalty_order con ordinal 1–5, FK a jugador y unicidad por club/jugador. El contrato de instrucciones base continúa con data_version 1. La migración se realiza al guardar, dentro de la misma transacción; Load lee versiones 1 y 2 sin migrar. Revisiones optimistas y rollback se mantienen.

No se crea otra fuente de formación/alineación, ni se toca el esquema general del mundo. No se modifican clubes, futbolistas, contratos, históricos o partidas personales.

## Archivos de esta iteración
- Nuevos: LineupDragButton.cs, TacticDiagram.cs, Main.SetPieces.cs, Tactics002_SetPieceAssignments.sql, TacticsIterationTests.cs y este documento.
- Modificados: LineupSelection.cs, TacticalSettings.cs, SqliteTacticsStore.cs, Main.Lineups.cs, Main.Tactics.cs, InteractiveLineupPitch.cs, TacticalPreview.cs, UiStrings.resx, Furvo.Tests.csproj y GodotUiSmoke.gd.
- Documentación actualizada: README.md, TACTICS.md, ARCHITECTURE.md, INVENTORY.md, CAREER_NAVIGATION.md y VALIDATION.md.
Las rutas de código corresponden a sus proyectos Core, Persistence, Godot y Tests. El estado del repositorio incluye también los cambios previos de la primera iteración; no se eliminan ni se publican automáticamente.

## Límites y comprobación manual
Sin motor, resultados, penalizaciones por puesto, química, fórmulas de calidad, IA rival, entrenamiento, mercado ni planes A/B/C. Si un cambio de opción activa preferencias incompatibles del mismo jugador, se rechaza y se conserva lo guardado; hay que desasignar una de esas funciones antes de activar la combinación. Los rivales de los dibujos son referencias, no entidades de la partida. Ninguna asignación incompleta implica un fallo del sistema; se muestra pendiente.

En Godot .NET abrir src/Furvo.Godot/project.godot, F5 y una carrera de prueba con club → Equipo → Táctica:
1. Arrastrar entre campo y banquillo/reservas, y entre dos titulares. Soltar fuera y cancelar con Escape. Confirmar once, colores y roles.
2. Revisar Formación debajo de pestañas y Mentalidad debajo. Cambiar de pestaña: la formación informativa debe coincidir.
3. Alternar Con balón/Sin balón; seleccionar pases, zonas y transiciones. Comprobar puntas y leyenda.
4. En faltas defensivas elegir marcaje, barrera y tumbado; asignar jugadores distintos y revisar borde, zonas y símbolo tumbado.
5. En faltas ofensivas elegir ejecución, destinos, movimientos y funciones reales.
6. En Penaltis configurar los cinco puestos; intentar repetir un jugador y revisar el orden tras salir/reabrir.
7. En Bandas comparar las seis estrategias y sus asignaciones.
8. Repetir a 1280×720, 1600×900 y 1920×1080. Comprobar scroll del panel y tooltips de nombres.

Debug y Release: 291 pruebas superadas por configuración y compilación sin errores ni advertencias. Godot nativo: salida 0, arrastres por eventos de ratón en los cinco sentidos admitidos, cancelaciones y recorrido completo. Capturas revisadas a tres resoluciones. SHA-256: 18 bases preexistentes intactas. Los comandos y resultados detallados figuran en VALIDATION.md. La carga de archivos se valida mediante servicios SQLite; no se añade un menú general nuevo de cargar carreras.
