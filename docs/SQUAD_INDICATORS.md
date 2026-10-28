# Plantilla e indicadores deportivos — 28/10/2026

## Alcance vigente

Equipo > Plantilla mantiene una sola tabla de 24 jugadores con cabecera compacta de club, temporada y recuento. Columnas: Rol | Posición | Jugador | Calidad | Estado físico | Ritmo | Moral | Forma (últimos 5) | PJ | G | A | Media. Los tres atributos por demarcación se retiran de la tabla principal; siguen consultables en las fichas de 51 atributos públicos. CareerSquadRules se conserva como utilidad descriptiva para futuros análisis, sin crear otra pantalla ni calcular calidad.

Inicio abre directamente el escritorio. No se crea la barra contextual Escritorio/Bandeja para el área Inicio, tampoco en Bandeja. Solo el botón Bandeja de la cabecera la abre; desde ella Volver al escritorio retorna al panel inicial. La tarjeta de bandeja del escritorio conserva el estado informativo vacío, pero se elimina su botón duplicado. Las otras áreas y el menú de partida aprobado conservan su comportamiento.

## Datos actuales y procedencia

La identidad y demarcación se leen de IWorldQueries sobre el mundo de la carrera, con PlayerId estable. No se consulta la puntuación de atributos para rellenar calidad, estado físico, ritmo o moral.

No existen aún resultados, actuaciones, condición, ritmo, moral ni calidad persistidos. La carrera nueva conserva la fecha inicial de la temporada y no hay infraestructura de partidos: en ese estado inicial confirmado se muestran PJ=0, G=0, A=0, Media=— y cinco círculos de forma inactivos. En una carrera de fecha posterior sin fuente estadística no se certifican ceros: sus estadísticas quedan desconocidas. Este adaptador inicial deberá sustituirse por consultas de actuaciones cuando exista esa fuente; la fecha por sí sola no será una prueba suficiente después de implementar partidos.

Los indicadores Calidad, Estado físico, Ritmo y Moral reciben null en el juego actual. No se generan porcentajes, notas ni estados Feliz/Normal de relleno. Un indicador gris con guion significa sin datos, no nivel cero, mala calidad, condición 0 % o moral normal. Los tooltips explican esa diferencia.

La distribución provisional de titulares/suplentes/reservas está autorizada, pero no hay reglas de asignación ni tabla persistida de roles en la versión actual. Por la condición explícita del encargo, se mantiene Sin asignar y se documenta la dependencia: definir criterios y persistencia versionada antes de generar una alineación. No se eligen once jugadores por puntuaciones no aprobadas y no se escribe ni sobrescribe ningún rol existente.

## Componentes reutilizables

- PlayerSportingStatus (Core/Queries): contrato público de presentación futura, sin atributos ocultos. Agrupa calidad, condición, ritmo, moral, estadísticas de temporada y actuaciones recibidas.
- SegmentedStatus: hasta cinco segmentos, porcentaje opcional 0–100 y tono descriptivo recibido. Valida rango, no convierte porcentajes en niveles ni fija umbrales deportivos.
- SportingIndicator (Godot Control): un componente y un pintor compartidos para controles independientes y celdas de la tabla. Calidad, Estado físico, Ritmo, Moral y Forma son variantes de ese componente; no hay dibujos copiados por jugador.
- DataTable admite visuales de celda genéricos mediante TableCellVisual; sigue siendo la única tabla, con selección y orden existentes. Utiliza el callback custom de TreeItem de Godot, sin otra dependencia. [Referencia de la API](https://docs.godotengine.org/en/stable/classes/class_treeitem.html#class-treeitem-method-set-custom-draw-callback); firma contrastada también con GodotSharp 4.7.2 instalado.
- SquadTrackingRows: filas/orden de seguimiento, sin fórmulas de habilidad ni cálculos de rendimiento.

Los datos sintéticos positivos utilizados en pruebas de componentes no se incorporan a SQLite ni se muestran como datos de una carrera.

## Diseño de cada indicador

Calidad: cinco barras rectangulares en una fila; activas doradas, inactivas gris azulado. El componente puede recibir un nivel real, pero no decide su fórmula ni si será contextual por posición. Sin dato: cinco barras inactivas y guion. Sin estrellas ni media de atributos.

Estado físico: cinco barras y porcentaje numérico cuando se reciba. Colores preparados verde/amarillo-rojo, suministrados como tono por una fuente aprobada. No se definen umbrales definitivos, no se calcula desde Resistencia ni se simulan fatiga/recuperación.

Ritmo: cinco barras con azul claro para valores recibidos, diferenciadas de condición. Sin dato: gris y guion, sin asignación inicial arbitraria.

Moral: círculo y texto (Muy alta, Feliz, Normal, Baja, Muy baja). Positivos verdes, normal neutro, negativos rojos. Sin dato: círculo gris y texto Sin datos con descripción explícita. El color nunca es el único canal informativo.

Forma: cinco círculos independientes. PlayerAppearance identifica encuentro, fecha, minutos, nota opcional y tono de rendimiento recibido. Se excluyen actuaciones con cero minutos; se ordenan fecha/MatchId, se toman las cinco más recientes y se muestran de más antigua a reciente. Los huecos restantes se sitúan a la derecha y quedan grises. No se convierte una valoración global en cinco segmentos ni se considera mala actuación la ausencia. Una actuación sin nota/clasificación también permanece sin evaluar, con fecha/nota disponible en su descripción. No se clasifica una nota mediante umbrales nuevos.

Media: dato de rendimiento de temporada recibido directamente, separado de las cinco actuaciones y de calidad intrínseca. No se calcula una media simple de atributos ni se añade algoritmo de puntuación de partidos.

## Ordenación y adaptación

Se conserva rol → demarcación futbolística → nombre → PlayerId en el orden inicial (incluido Sin asignar al final mientras sea necesario). El orden visual no modifica roles. Nombre y demarcación mantienen alternancia ascendente/descendente y selección por ID.

Condición, ritmo, moral y media solo habilitan su cabecera cuando existe algún dato. PJ/G/A se habilitan con los ceros conocidos del estado inicial. Los desconocidos permanecen al final en ambos sentidos, nunca se convierten en cero. Forma no se ordena como si fuera una cifra agregada. La futura fuente de porcentajes/estados puede conectarse sin cambiar el componente ni las fichas.

Las doce columnas conservan todos sus datos a 1280×720, 1600×900 y 1920×1080; indicadores compactos, nombres con ajuste, números alineados y scroll vertical. No se ocultan columnas en esas resoluciones. A anchos inferiores al mínimo cómodo, la tabla conserva las columnas y permite desplazamiento horizontal antes que eliminar datos silenciosamente. No se añade una cabecera institucional ni pestañas de roles.

## Compatibilidad

Sin nuevas migraciones, publicaciones, estadísticas persistidas, asignaciones o cambios contractuales. Se conserva la separación Core/Persistence/Godot, las cinco publicaciones del mundo, los veinte clubes, 480 jugadores, 61 atributos y diez ocultos, contratos, reputaciones, perfiles y carreras. Los estados sin datos no se guardan como mediciones deportivas reales.

## Inventario

Creados:
- src/Furvo.Core/Queries/PlayerSportingStatus.cs: contratos públicos de datos deportivos futuros y orden cronológico de actuaciones.
- src/Furvo.Godot/Scripts/Components/SportingIndicator.cs: cinco variantes visuales reutilizables, accesibilidad y pintura común.
- src/Furvo.Godot/Scripts/Components/SquadTrackingRows.cs: filas y ordenación de seguimiento.
- tests/Furvo.Tests/SquadTrackingTests.cs: ceros iniciales confirmados, desconocidos, segmentos/rangos, actuaciones y ordenación.
- docs/SQUAD_INDICATORS.md: este informe.

Modificados:
- src/Furvo.Godot/Scripts/Main.Squad.cs: doce columnas, consulta de jugadores y estados disponibles.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: Inicio sin submenú redundante y regreso desde Bandeja.
- src/Furvo.Godot/Scripts/Components/DataTable.cs: callbacks gráficos de celda y metadatos accesibles.
- src/Furvo.Godot/Resources/UiStrings.resx: títulos, descripciones y estados centralizados.
- tests/Furvo.Tests/Furvo.Tests.csproj: reglas puras reales enlazadas sin arrancar Godot.
- tools/GodotUiSmoke.gd: doce columnas, componentes, ceros, media desconocida, navegación de Inicio/Bandeja, fichas y capturas.
- README.md, docs/CAREER_NAVIGATION.md, ARCHITECTURE.md, INVENTORY.md y VALIDATION.md: estado vigente y resultados.

No hay dependencias nuevas, commit ni push. Se conservan los cambios de encargos anteriores del árbol de trabajo.

## Resultados ejecutados — 28/10/2026

- Compilaciones Debug y Release: cuatro proyectos, 0 errores y 0 advertencias.
- .NET Debug y Release: 265 superadas, 0 fallidas y 0 omitidas por configuración (249 anteriores y 16 casos nuevos). Se prueban rangos de cinco segmentos/porcentajes, datos desconocidos, ceros iniciales confirmados, cinco actuaciones cronológicas, exclusión de no participantes, notas no agregadas y ordenación sin mutar filas.
- GodotUiSmoke.gd con renderizado real: recorrido completo superado, FURVO_WORKSPACE_OK para contratado/en paro, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Comprueba Inicio sin submenú, Bandeja y regreso, 24 jugadores, doce columnas, metadatos de cinco segmentos/círculos, estadísticas iniciales, media desconocida, ordenación/selección por ID y fichas de 51 atributos públicos.
- Revisión visual de capturas renderizadas de la tabla a 1280×720, 1600×900 y 1920×1080 y del escritorio sin submenú. Sin columnas ocultas ni scroll horizontal observado en esas resoluciones; barras/círculos sin datos y textos legibles. Se corrigió la superposición del texto nativo en las celdas gráficas y se conservaron sus descripciones para tecnologías de asistencia. Esta revisión cubre las capturas concretas, no una campaña de interacción manual con todos los clubes.
- GodotApplicationSmoke.gd: salida 0 en pantalla completa real, escritorio, seis áreas, Esc y salida del juego.
- SHA-256: 15 bases preexistentes idénticas, incluidas las cinco publicaciones del mundo. Sin migraciones ni publicación nueva.
- No se simulan valores para colorear la pantalla: en las capturas de partida los indicadores pendientes están inactivos. Los ejemplos de pruebas unitarias no se importan a las carreras.

Capturas: artifacts/career-visual/career-squad-1280x720.png (variantes 1600x900/1920x1080) y career-employed-1280x720.png. Artefactos ignorados por Git y reproducibles con el guion --capture.

## Comprobación local

1. Nueva partida con un club. Inicio debe abrir el escritorio sin submenú. Bandeja se abre solo desde la cabecera y ofrece Volver al escritorio.
2. Equipo > Plantilla: una tabla, 24 jugadores, doce columnas en el orden indicado. Sin atributos técnicos en la tabla; los indicadores están sin datos y PJ/G/A son cero al inicio, Media —.
3. Ordenar nombre/rol/posición, seleccionar y abrir un jugador, comprobar 51 atributos públicos y volver conservando su ID. El ordenado no escribe datos.
4. Revisar los tooltips: gris no significa malo ni cero. Comprobar que no hay estados Feliz, porcentajes o notas inventados.
5. Revisar 1280×720, 1600×900 y 1920×1080; comprobar menú de partida y salida en pantalla completa.

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd" -- --capture
& $env:GODOT4 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- fullscreen
~~~

Pendientes: reglas/persistencia de roles iniciales, calidad, umbrales de condición/ritmo, moral, actuaciones y notas, estadísticas y cualquier simulación deportiva. No se inicia otra fase.
