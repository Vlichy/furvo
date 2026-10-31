# Registro de validación

## Estado vigente — desarrollo deportivo integrado, 31/10/2026

- Debug y Release: cuatro proyectos compilados, **0 errores y 0 advertencias**.
- **403/403 pruebas en Debug y 403/403 en Release**, 0 fallos y 0 omitidas. Se conservan las 366 anteriores y se añaden 37 casos. El cambio de dos capitanes actualiza la expectativa del dato efectivo sin destruir prioridades legadas.
- TRX: `artifacts/test-results/sports-debug.trx` y `sports-release.trx`.
- Godot 4.7.2 .NET/OpenGL, recorrido completo con **salida 0**, incluidos `FURVO_SPORTS_OK`, `FURVO_ADVANCED_TACTICS_OK`, `FURVO_ACADEMY_OK`, `FURVO_DRAG_OK` y `FURVO_UI_SMOKE_OK`. Se comprueban izquierda/campo/derecha, dos capitanes, seis disposiciones, edición de sesiones, grupos, seis áreas, delegación parcial, informes actuales, tecnología pendiente y reapertura; además, ficha de cantera, bloqueo por licencia desconocida y registro de propuesta sin mover al futbolista.
- Capturas reales revisadas en 1280×720, 1600×900 y 1920×1080: `artifacts/career-visual/career-individual-*.png`, `career-training-*.png`, `career-sports-technology-*.png`. Se corrigieron encabezados internos/repetidos y variaciones de ancho que afectaban al arrastre.
- **Cinco publicaciones SQLite iniciales sin cambios por SHA-256** frente al registro previo de este encargo. No se genera una publicación nueva ni se sobrescriben carreras personales. Pruebas nativas en carreras aisladas bajo `artifacts`.
- Migraciones explícitas de carrera: `Sports001_Initial.sql` y `SportsTactics002_Assignment.sql`. Consulta antigua sin escritura y rollback transaccional cubiertos por pruebas.
- Corregida la ambigüedad SQL `current_date`: la consulta usa `s.current_date`, con regresión contra la fecha persistida.
- El banco de desarrollo abarca 30×365 días sintéticos y comprueba determinismo, límites, envejecimiento, dosis excesiva y recuperación gradual; **no equivale a validar treinta temporadas del juego completo ni una calibración comercial**.
- [Informe e inventario](SPORTS_DELIVERY.md), [matriz de 50 requisitos](SPORTS_VALIDATION_MATRIX.md), [fuentes y lagunas](SPORTS_RESEARCH.md).
- Pendientes: reloj/calendario, elegibilidad federativa completa, capacidades del personal, IA de planificación/recomendaciones, medicina y aprobación/precios de instalaciones. La interfaz no simula que esos proveedores existan. Revisión humana prolongada, teclado y escalados de Windows complementan el recorrido automático.


## Registro anterior — roles, instrucciones individuales y preparación táctica

- Compilación Debug y Release: cuatro proyectos, 0 errores y 0 advertencias. SDK .NET 10.0.402.
- Suite final: **366/366 pruebas en Debug y 366/366 en Release**, 0 fallos, 0 omitidas. Referencia previa: 343/343; se añaden 23 casos, sin eliminar ni desactivar pruebas anteriores.
- Resultados: `artifacts/test-results/advanced-debug.trx` y `advanced-release.trx`.
- Godot 4.7.2 .NET con renderizado nativo: runner completo finalizado con `FURVO_ADVANCED_TACTICS_OK` y `FURVO_UI_SMOKE_OK`, código 0. Se comprueban roles de portero, personalización, cancelación segura, conservación del once, cuatro diagramas defensivos, capitanes tras reapertura, Cantera y regresiones de arrastre/planes/balón parado.
- Capturas reales e inspección en 1280×720, 1600×900 y 1920×1080: `artifacts/career-visual/career-individual-{resolución}.png` y `career-advanced-defence-{resolución}.png`. Los paneles conservan desplazamiento; a 720p algunas instrucciones requieren desplazar el panel lateral. No se afirma una revisión humana exhaustiva de todas las combinaciones.
- Integridad: las cinco publicaciones `data/initial-world-2026-27*.db` coinciden byte a byte con el contenido versionado, comprobado con `git ls-files --stage` y `git hash-object`. No hay publicación nueva del mundo ni cambios en esas bases. Las carreras de QA se crean únicamente bajo `artifacts`.
- Migraciones nuevas: `TacticalBehaviour001_Initial.sql` y `Tactics004_Personnel.sql`; se aplican solo a guardados explícitos de carreras de prueba. Lectura antigua sin escritura cubierta por pruebas. No se migran automáticamente las carreras personales.
- Pendiente: comprobación humana con distintos escalados de Windows y uso prolongado; efectos del motor, contexto real de rival y entrenamiento todavía no operativos.
- Catálogo completo, predeterminados, jerarquía, archivos y comandos Windows: [Informe táctico](TACTICAL_BEHAVIOURS.md).

## Registro anterior — alineación automática sin popup y separación de controles


- Debug y Release: **cuatro proyectos compilados, 0 errores y 0 advertencias**.
- Debug y Release: **317 pruebas superadas por configuración, 0 fallidas y 0 omitidas**. Se mantienen las 311 anteriores y se añaden seis casos de asignación automática, persistencia/independencia, cruce pequeño de zona, portero y adecuación natural/secundaria.
- Godot 4.7.2 .NET con ventana/OpenGL: **recorrido completo, salida 0**, con FURVO_AUTO_POSITION_OK, FURVO_PLANS_OK, FURVO_INSTRUCTIONS_OK y FURVO_UI_SMOKE_OK.
- Aceptación comprobada: Inicial/Sin balón → arrastrar MC a MP → ningún diálogo → abreviatura MP → color derivado de natural/secundarias → una sola revisión → alternar fases → guardar → recargar la carrera aislada desde SQLite con reconstrucción del espacio de trabajo → recuperar coordenadas y demarcación. No hay selector manual de demarcación ni selector de intercambio con otro titular.
- Verificados márgenes entre niveles y límites de los grupos de configuración a **1280 × 720, 1600 × 900, 1920 × 1080 y retorno a 1280**. Capturas revisadas en las tres resoluciones; los recorridos anteriores conservan sus comprobaciones de once visible, navegación, instrucciones y balón parado.
- Se corrigieron durante la validación dos problemas de composición: etiqueta de formación que se partía verticalmente y herramientas cuya búsqueda en el árbol ocurría antes de insertar su contenedor. Ahora se pasa el contenedor explícitamente y la formación informativa no se envuelve.
- Integridad: SHA-256 sin cambios en las **cinco publicaciones SQLite del mundo inicial**. No hay migración de esquema en esta corrección.
- QA: la reapertura usa un punto exclusivo de Debug y almacenamiento aislado; llama al flujo real EnterCareer/SqliteCareerStore.Load. No añade carga de partidas al menú del juego ni demuestra un cierre/reinicio completo del proceso. Quedan pendientes esa comprobación desde un futuro flujo de carga, otros DPI y resoluciones menores.
- TRX: artifacts/test-results/alignment-auto-{debug,release}.trx. Log: artifacts/alignment-auto-native.log. Capturas: artifacts/career-visual/career-auto-position-{resolución}.png.
- Inventario y explicación del código eliminado: [ALIGNMENT_EDITOR_FIX.md](ALIGNMENT_EDITOR_FIX.md).

### Registro anterior

## Estado vigente — ampliación de instrucciones y pizarra contextual

- Debug y Release: **compilación correcta de cuatro proyectos, 0 errores y 0 advertencias**.
- Debug y Release: **311 pruebas superadas en cada configuración, 0 fallidas y 0 omitidas**. Se mantienen los 301 casos anteriores y se añaden diez de instrucciones, grafos, ubicaciones y migración. El caso del catálogo anterior se actualiza a las 17 instrucciones aprobadas.
- La primera ejecución dentro del aislamiento no conectó con testhost y fue anulada. Las ejecuciones completas fuera de ese aislamiento terminaron correctamente; archivos TRX en artifacts/test-results/tactics-expansion-{debug,release}.trx.
- Godot 4.7.2 .NET con ventana/OpenGL: recorrido completo con **salida 0**, FURVO_INSTRUCTIONS_OK, FURVO_PLANS_OK y FURVO_UI_SMOKE_OK. Incluye cinco vías de contraataque, visibilidad dependiente, ocho contextos ofensivos/defensivos, asignaciones compactas, barrera/tumbado y navegación anterior.
- Resoluciones comprobadas para los paneles: **1280 × 720, 1600 × 900 y 1920 × 1080**, incluido regreso a 1280. Comprobados límites de las asignaciones tras desplazarse hasta el final. Capturas revisadas de la falta frontal defensiva, la falta lateral ofensiva, el portero mixto y las asignaciones inferiores.
- Persistencia: pruebas de reapertura independiente por ubicación, instrucciones por plan, rechazo de duplicados sin escritura y migración explícita desde v2 manteniendo la jugada legada; se mantienen las pruebas anteriores de v1, rollback y disposiciones personalizadas.
- Integridad SHA-256: las **cinco publicaciones SQLite del mundo inicial** dentro del proyecto permanecen idénticas. Las pruebas de interfaz usan almacenamiento aislado en artifacts, sin editar carreras personales.
- Pendiente manual: DPI, ventanas menores y revisión exhaustiva de todas las combinaciones. Las trayectorias y rivales son referencias descriptivas; no hay simulación ni cálculos de eficacia.
- Inventario, compatibilidad y comandos: [TACTICAL_INSTRUCTIONS.md](TACTICAL_INSTRUCTIONS.md).

### Registro anterior

## Estado vigente — planes tácticos y formaciones personalizadas

- Debug y Release: cuatro proyectos compilados, **0 errores y 0 advertencias**.
- Debug y Release: **301 pruebas superadas por configuración, 0 fallidas y 0 omitidas**. Los diez casos nuevos cubren planes, seis disposiciones, independencia, coordenadas, roles, copias, persistencia, compatibilidad y rollback.
- Godot 4.7.2 .NET con ventana/OpenGL: recorrido automatizado completo terminado con FURVO_PLANS_OK y FURVO_UI_SMOKE_OK. Verificados arrastre nativo, confirmación de demarcación, cancelaciones, copia, restablecimiento, referencias y navegación anterior.
- Capturas y comprobaciones de límites a 1280 × 720, 1600 × 900 y 1920 × 1080, incluido reducir la ventana tras ampliarla. Diálogo de demarcación dentro del área visible.
- Integridad: SHA-256 de las **21 bases de datos preexistentes sin cambios**. Las pruebas usan almacenamiento aislado en artifacts; no se modifica el mundo inicial ni las carreras personales.
- Pendiente de comprobación manual exhaustiva: escalado DPI, navegación completa por teclado y combinaciones de edición fuera del recorrido automatizado. No se implementa simulación ni activación automática de planes.
- Arquitectura, migración, inventario y comandos: [TACTICAL_PLANS.md](TACTICAL_PLANS.md).

### Registro anterior

## Estado vigente — segunda iteración de Táctica

- Debug y Release: **compilación de cuatro proyectos, 0 errores y 0 advertencias**, con MSBuild secuencial (-m:1).
- Debug y Release: **291 pruebas superadas por configuración, 0 fallidas y 0 omitidas**. Se conservan las 282 anteriores y se añaden nueve casos: intercambios con suplentes/reservas, destinos inválidos, adecuación tras intercambio de campo, detalles/tanda persistidos, funciones incompatibles, lectura v1 sin escritura y migración explícita conservando datos, grafos/variantes, marcas explícitas y pertenencia de cada símbolo zonal a su área sombreada.
- Godot 4.7.2 .NET con ventana/OpenGL: **GodotUiSmoke.gd, salida 0**. Los arrastres recorren el sistema nativo _GetDragData/_CanDropData/_DropData mediante InputEventMouseButton y MouseMotion: titular ↔ suplente, titular ↔ reserva y titular ↔ titular. Se verifican cancelación fuera y Escape, revisión persistida, selección y ausencia de pérdida/duplicados.
- Recorrido de las cuatro pestañas, formación informativa/no editable fuera de Alineación, selector Con balón/Sin balón y conservación de valores; barrera/tumbado con jugadores asignados, zonas y marcajes al hombre/mixto, faltas ofensivas, tanda de cinco y las seis estrategias de banda. Recuperación al reabrir la pantalla y pruebas SQLite al reabrir archivos. Se conservan las comprobaciones anteriores de perfiles, carrera en paro/contratada, consulta y fichas de 51 atributos.
- Las once formaciones vuelven a recorrerse en la interfaz, comprobando once identificadores únicos.
- Capturas renderizadas a **1280×720, 1600×900 y 1920×1080** en artifacts/career-visual. Inspeccionadas alineación, fases, barrera/tumbado, marcajes zonal/mixto, centro al área, tanda ordenada y banda ensayada. Se ajustaron la separación del banquillo y el dibujo de barrera/marcadores; las puntas de las flechas se calculan sobre los círculos/objetivos referenciados.
- **18 de 18 bases preexistentes idénticas por SHA-256**, incluidas cinco publicaciones iniciales y datos personales. Pruebas en archivos aislados. No se crea ni sobrescribe una publicación del mundo.
- Tactics002_SetPieceAssignments.sql amplía a versión táctica 2 únicamente al guardar. Lecturas v1/v2 sin migración; el lanzador de penalti legado se conserva y no se copia a la nueva tanda.
- Sin dependencias nuevas. git diff --check sin incidencias.

Las capturas y eventos automatizados no sustituyen una campaña manual exhaustiva con ratón físico, teclado, DPI y todas las combinaciones de asignaciones. La edición con funciones incompatibles se rechaza y conserva lo guardado; deben desasignarse antes de volver a activar una combinación conflictiva. Las referencias rivales son esquemáticas y las funciones no elegidas siguen pendientes, sin identidades automáticas.

[Informe, decisiones, archivos y pasos manuales](TACTICS_ITERATION_2.md). El historial anterior se conserva debajo.

Comandos usados desde la raíz, en PowerShell:

```powershell
dotnet build Furvo.sln --no-restore -m:1
dotnet test Furvo.sln --no-build -m:1
dotnet build Furvo.sln --no-restore -m:1 -c Release
dotnet test Furvo.sln --no-build -m:1 -c Release
```

## Estado vigente — Equipo → Táctica

- **Debug y Release:** cuatro proyectos compilados, 0 errores y 0 advertencias, con -m:1.
- **282 pruebas superadas por configuración, 0 fallidas y 0 omitidas.** Se conservan las 273 anteriores y se añaden nueve de Táctica: lectura sin migración, todas las opciones y mentalidades, siete clases de balón parado, lanzadores válidos/ajenos, aislamiento entre carreras, revisión obsoleta, valores inválidos, rollback, permisos, adecuación posicional e intercambio persistido de titulares.
- **Godot 4.7.2 .NET con ventana/OpenGL:** GodotUiSmoke.gd superado, salida 0. Cuatro pestañas, mentalidad debajo del submenú, Con balón/Sin balón juntos, vistas reactivas, rutinas/lanzador y recuperación al volver a entrar. Se prueban las once formaciones, ausencia de duplicados y de solapamientos entre nombres, y los once titulares dentro del área visible.
- **Godot headless final:** salida 0 y FURVO_TACTICS_OK, FURVO_LINEUP_OK, FURVO_WORKSPACE_OK, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Añade una operación efectiva de intercambio entre dos centrales titulares y verifica nueva revisión/jugador en el campo.
- Capturas renderizadas de Alineación, Estilo, Transiciones y Balón parado a **1280×720, 1600×900 y 1920×1080**, en artifacts/career-visual. Se inspeccionaron capturas de las cuatro pestañas y las tres resoluciones, incluida defensa de cinco a 720p. Se compactó la cabecera y se ajustaron nombres mediante anchura/elipsis para conservar once visibles y evitar colisiones.
- Las instrucciones no espaciales muestran explicación contextual. La vista previa no cambia al pasar accidentalmente sobre otro selector: cambia al enfocar o seleccionar una instrucción.
- **SHA-256: 17 de 17 bases preexistentes idénticas**, incluidas cinco publicaciones del mundo, perfiles, carreras y catálogos de usuario. Pruebas con carreras aisladas. Sin nuevas publicaciones SQLite.
- Extensión Tactics001_Initial.sql, versión 1, solo al guardar una configuración. Sin migración automática por lectura y sin modificación de contratos, economía ni jugadores.
- Sin nuevas dependencias. Git diff --check sin incidencias.

Estas son verificaciones automatizadas más inspección de capturas, no una campaña manual exhaustiva con ratón y teclado. Quedan para revisión del usuario: navegación manual con teclado, escalado de Windows/DPI distinto, nombres personalizados muy largos y errores reales de permisos/disco. La reapertura de archivos se prueba por los servicios; el menú general de cargar partidas continúa fuera del alcance.

[Inventario, arquitectura, limitaciones y comandos de validación](TACTICS.md). Este apartado sustituye las descripciones anteriores de una pantalla Alineación independiente; se conserva el historial a continuación.

## Estado vigente — alineaciones y formaciones iniciales (28/10/2026)

- Debug y Release: compilación de cuatro proyectos, 0 errores y 0 advertencias. Se utilizó MSBuild secuencial (-m:1); la invocación paralela devolvió fallo sin diagnóstico en este entorno.
- Debug y Release: **273 pruebas superadas, 0 fallidas y 0 omitidas**. Incluyen las 265 anteriores y ocho nuevas pruebas: catálogo, 220 combinaciones de club/esquema, reproducibilidad con entrada reordenada, cambios y persistencia, aislamiento, ampliación explícita de carreras anteriores, veinte firmas y reaperturas, variación por semilla, exclusión de ocultos y rechazo de plantillas inválidas.
- Godot 4.7.2 .NET con ventana y renderizado OpenGL: GodotUiSmoke.gd superado. Comprueba hover, clic fijado, candidatos naturales/secundarios, sustitución, cambio de formación, revisión persistida, coherencia 11 titulares/7 suplentes/6 reservas y navegación. Conserva el recorrido anterior de perfiles, oferta, paro y fichas de 51 atributos.
- Capturas reales a 1280×720, 1600×900 y 1920×1080 en artifacts/career-visual/career-lineup-*.png y career-squad-*.png. Inspección de distribución, legibilidad, panel derecho y límites; cubre las capturas concretas, no una sesión manual exhaustiva de todos los clubes/formaciones.
- SHA-256: **16 de 16 bases preexistentes idénticas**, incluidas cinco publicaciones iniciales, carreras, perfiles y catálogos de usuario. No se crea publicación del mundo. Las pruebas de interfaz usan almacenamiento aislado en artifacts.
- Extensión versionada Lineups001_Initial.sql solo en carreras nuevas o por inicialización explícita. Ninguna migración ni generación al cargar o abrir carreras antiguas.
- Calidad, condición, ritmo y moral siguen sin valores ficticios. Criterio de selección provisional no visible como calidad oficial.

Véanse [reglas, persistencia e inventario](LINEUPS.md) y [tabla editorial de los veinte clubes](INITIAL_FORMATIONS.md). Los estados anteriores sobre roles sin asignar y ausencia de alineaciones quedan como historial; no describen carreras nuevas.

Comprobación local en PowerShell desde la raíz:

```powershell
dotnet build Furvo.sln -m:1
dotnet test Furvo.sln --no-build -m:1
dotnet build Furvo.sln -c Release -m:1
dotnet test Furvo.sln -c Release --no-build -m:1
```

Abrir src/Furvo.Godot/project.godot y pulsar F6/F5 según escena/proyecto. Crear una carrera de prueba con club → Equipo → Plantilla / Alineación. Comprobar puestos, hover/clic, elección, intercambio compatible/rechazado, cambio de esquema y regreso a Plantilla. Revisar también teclado, nombres largos y escenarios de disco inaccesible; estos últimos no se han inspeccionado manualmente. No se habilita un flujo general nuevo de cargar partidas.

## Estado vigente — plantilla de seguimiento e Inicio simplificado (28/10/2026)

Equipo > Plantilla tiene doce columnas: Rol, Posición, Jugador, Calidad, Estado físico, Ritmo, Moral, Forma (últimos 5), PJ, G, A y Media. Indicadores reutilizables sin valoraciones inventadas; los atributos individuales siguen en la ficha. En el estado inicial confirmado sin partidos: PJ/G/A=0, Media — y cinco círculos de forma inactivos. Calidad, condición, ritmo y moral continúan sin datos. La distribución inicial de roles está aprobada pero pendiente de reglas y persistencia: se mantiene Sin asignar. Inicio abre directamente el escritorio; Bandeja se abre desde la cabecera y tiene regreso al escritorio, sin submenú redundante.

[Componentes, contratos de datos, archivos, validación y dependencias](SQUAD_INDICATORS.md). Debug/Release: 265 pruebas superadas y compilaciones sin errores ni advertencias. Recorrido Godot y salida en pantalla completa superados; capturas revisadas a tres resoluciones. No hay cambios SQLite. Los apartados anteriores sobre ocho columnas y submenú de Inicio quedan como historial y son sustituidos por este estado.

## Resultados ejecutados — 28/10/2026

- Compilaciones Debug y Release: cuatro proyectos, 0 errores y 0 advertencias.
- .NET Debug y Release: 265 superadas, 0 fallidas y 0 omitidas por configuración (249 anteriores y 16 casos nuevos). Se prueban rangos de cinco segmentos/porcentajes, datos desconocidos, ceros iniciales confirmados, cinco actuaciones cronológicas, exclusión de no participantes, notas no agregadas y ordenación sin mutar filas.
- GodotUiSmoke.gd con renderizado real: recorrido completo superado, FURVO_WORKSPACE_OK para contratado/en paro, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Comprueba Inicio sin submenú, Bandeja y regreso, 24 jugadores, doce columnas, metadatos de cinco segmentos/círculos, estadísticas iniciales, media desconocida, ordenación/selección por ID y fichas de 51 atributos públicos.
- Revisión visual de capturas renderizadas de la tabla a 1280×720, 1600×900 y 1920×1080 y del escritorio sin submenú. Sin columnas ocultas ni scroll horizontal observado en esas resoluciones; barras/círculos sin datos y textos legibles. Se corrigió la superposición del texto nativo en las celdas gráficas y se conservaron sus descripciones para tecnologías de asistencia. Esta revisión cubre las capturas concretas, no una campaña de interacción manual con todos los clubes.
- GodotApplicationSmoke.gd: salida 0 en pantalla completa real, escritorio, seis áreas, Esc y salida del juego.
- SHA-256: 15 bases preexistentes idénticas, incluidas las cinco publicaciones del mundo. Sin migraciones ni publicación nueva.
- No se simulan valores para colorear la pantalla: en las capturas de partida los indicadores pendientes están inactivos. Los ejemplos de pruebas unitarias no se importan a las carreras.

Capturas: artifacts/career-visual/career-squad-1280x720.png (variantes 1600x900/1920x1080) y career-employed-1280x720.png. Artefactos ignorados por Git y reproducibles con el guion --capture.

## Corrección vigente — menú, Club propio y plantilla deportiva (28/10/2026)

La cabecera de carrera tiene solo Menú de partida como acceso al menú; Guardar/Cargar siguen deshabilitados. Club abre el empleador contratado y en paro muestra Sin club, sin selector público ni Elegir club. Equipo > Plantilla es una pantalla independiente con ocho columnas: Rol, Posición, Jugador, Calidad, Condición y tres atributos reales por demarcación. Roles Sin asignar y calidad/condición — hasta disponer de sistemas persistidos; sin atributos ocultos ni valoraciones inventadas. El explorador inicial previo a contratar se conserva.

[Informe, correspondencias por posición, archivos y validación](NAVIGATION_SQUAD_CORRECTION.md). Debug/Release sin errores ni advertencias; 249 pruebas superadas por configuración y regresión Godot superada, incluida salida en pantalla completa. Catorce bases preexistentes intactas; no hay migraciones. Las descripciones anteriores de un selector público dentro de carrera quedan sustituidas por esta corrección.

## Resultados ejecutados — 28/10/2026

- Debug y Release: cuatro proyectos compilan con 0 errores y 0 advertencias.
- Pruebas .NET: 249 superadas, 0 fallidas, 0 omitidas por configuración (244 previas y 5 casos nuevos). Se actualiza la prueba de la antigua ruta pública para exigir su denegación, sin eliminar cobertura. Los nuevos casos verifican trece demarcaciones, valores persistidos de los 480 jugadores, roles no inventados, orden determinista y cambio contextual sin mutar atributos.
- GodotUiSmoke.gd: recorrido completo sin ventana y con renderizado real, salida 0 y marcadores FURVO_WORKSPACE_OK para contratado/en paro, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Comprueba menú único, Continuar partida, regreso al inicio, selector inicial preservado, Club propio/Sin club, ocho columnas, 24 filas, altura uniforme, selección por ID después de ordenar en ambos sentidos, apertura por botón y activación de fila, 51 atributos públicos y regreso.
- GodotApplicationSmoke.gd: salida 0 en pantalla completa real; Esc abre el menú y Salir del juego cierra Godot correctamente.
- Revisión visual de capturas renderizadas de plantilla a 1280×720, 1600×900 y 1920×1080, menú de partida, Club propio y Sin club. Se revisaron alineación, textos, colores, scroll y botones; se redujo espaciado y se fijó altura uniforme. Esta inspección cubre las capturas indicadas, no una campaña manual exhaustiva de todos los clubes/equipos.
- SHA-256: catorce bases preexistentes idénticas, incluidas las cinco publicaciones del mundo. Sin migraciones ni modificaciones de jugadores, contratos, reputaciones, perfiles o partidas existentes.
- git diff --check sin incidencias. Sin dependencias nuevas, commit ni push.

Capturas locales reproducibles: artifacts/career-visual/career-squad-1280x720.png (también 1600x900/1920x1080), career-game-menu.png, career-own-club.png y career-no-club.png. Se generan con perfiles y carreras de prueba aislados.

## Estado vigente — escritorio y navegación de carrera (28/10/2026)

Tras iniciar una carrera se abre el escritorio contratado o en paro, con seis áreas superiores y subnavegación contextual. La identidad, fecha, club y economía proceden de la copia SQLite de esa carrera. Se reutilizan plantillas, jugadores, información/estadio, economía, historia y contrato. Los permisos de Core no dependen del club consultado. Mercado, Competición y Continuar quedan deshabilitados con explicación; no hay avance temporal, ofertas ficticias ni nuevas mecánicas.

[Mapa, capturas, inventario, decisiones futuras y comprobación manual](CAREER_NAVIGATION.md). Debug/Release: 244 pruebas superadas por configuración y compilaciones sin errores ni advertencias. Pruebas Godot y salida en pantalla completa superadas; capturas de ambos escritorios revisadas a tres resoluciones. Doce bases preexistentes intactas; sin migraciones. Los apartados siguientes conservan el historial de entregas anteriores.

## Resultados ejecutados — 28/10/2026

- Compilación Debug y Release: cuatro proyectos, 0 errores y 0 advertencias.
- Pruebas .NET: 244 superadas, 0 fallidas y 0 omitidas por configuración (234 anteriores + 10 casos de navegación/permisos). Incluyen denegación de rutas propias desde código, club ajeno, identidad, fecha sin avance, hashes y economía de cada copia.
- GodotUiSmoke.gd: salida 0, FURVO_WORKSPACE_OK en paro y contratado, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Se mantienen formulario, firma/rechazo/paro, tres carreras aisladas, tablas y 51 atributos públicos.
- Prueba con renderizado OpenGL real y capturas de ambos escritorios a 1280×720, 1600×900 y 1920×1080: recorridos completos superados. Se revisaron las capturas; se corrigieron anchura/altura de la cabecera y se añadieron comprobaciones de límites/solapamiento.
- GodotApplicationSmoke.gd con modo fullscreen real: salida 0 y FURVO_APPLICATION_SMOKE_OK; escritorio, seis áreas, Esc y salida del juego.
- SHA-256: doce bases preexistentes idénticas, incluidas las cinco publicaciones del mundo. Sin migraciones ni nuevas publicaciones.
- git diff --check sin incidencias. No se declara una campaña manual completa de todos los clubes/equipos: la inspección visual cubre las capturas indicadas y los recorridos automatizados descritos.

## Estado vigente — economía aprobada y firma habilitada (28/10/2026)

El usuario aprobó la propuesta initial-economy-2026-27-draft-v1. La nueva publicación v5 contiene los veinte estados económicos y conserva los 480 jugadores sin cambios. Economía y Contrato consultan los mismos datos; la firma crea una carrera independiente y registra el compromiso del entrenador en una transacción, sin duplicar gastos ni efectuar un pago anual ficticio. Margen salarial real de jugadores: desconocido hasta disponer de contratos salariales. Las publicaciones/partidas antiguas no se modifican.

[Implementación, migración, inventario, pruebas y límites](ECONOMY_IMPLEMENTATION.md). Debug/Release sin errores ni advertencias; 234 pruebas superadas por configuración. Godot con firma y tres carreras aisladas superado; capturas renderizadas de Economía/Contrato revisadas a 1280×720, 1600×900 y 1920×1080. Los apartados siguientes conservan el historial y sus bloqueos anteriores quedan sustituidos por este estado.

## Estado vigente — propuesta económica, etapa 1 (28/10/2026)

Se ha preparado una propuesta ficticia de los veinte clubes para aprobación; no se incorpora a SQLite ni se habilita la firma real. Los presupuestos/previsiones no son saldos ni movimientos. El margen contractual de jugadores permanece desconocido; la reserva de planificación del borrador no lo sustituye. El sueldo propuesto del entrenador está incluido una sola vez en personal. No hay cambios funcionales ni de esquema.

[Diagnóstico, veinte filas, fórmulas y aprobación pendiente](INITIAL_ECONOMY_PROPOSAL.md). Datos: docs/proposals/initial-economy-2026-27.draft.json, estado AwaitingUserApproval, sin uso en ejecución. Esta nota sustituye las indicaciones anteriores sobre el estado de aprobación; los apartados siguientes conservan el historial.

### Validación ejecutada el 28/10/2026

- Debug y Release: cuatro proyectos, 0 errores y 0 advertencias.
- .NET Debug/Release: 222 superadas, 0 fallidas, 0 omitidas por configuración; incluyen persistencia y firma con fixtures, sin altas personales.
- Validador documental: FURVO_ECONOMIC_PROPOSAL_OK para veinte clubes.
- GodotUiSmoke.gd sin ventana: salida 0, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK; formulario, firma real bloqueada, rechazo/paro, independencia y regresión de explorador.
- SHA-256: nueve bases preexistentes idénticas, incluidas cuatro publicaciones del mundo.
- No hay inspección visual manual a 1280×720, 1600×900 ni 1920×1080. Las comprobaciones automáticas de geometría no la sustituyen.
- No se valida una economía persistida de producción ni una firma real aprobada: pertenecen a etapa 2.

## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](INITIAL_CONTRACT_SIMPLIFICATION.md).

## Corrección contractual vigente — 27/10/2026

La validación actual sustituye las reglas de bloqueo descritas en los apartados históricos: solo exige identidad válida, fechas válidas, salario positivo, moneda admitida y contenido/prioridad de obligaciones expresamente exigibles. Las preferencias orientativas pendientes no bloquean ni se guardan como compromisos aceptados. La duración se calcula en meses desde las fechas. Extensión de carrera 2 para nuevas carreras; carga de extensión 1 en solo lectura. Las veinte ofertas reales siguen pendientes de fecha final, salario anual y moneda.

Validación: Debug y Release, 209 pruebas superadas; compilación de cuatro proyectos sin errores ni advertencias. Prueba de escena Godot sin ventana superada; no equivale a revisión visual manual. Véase [informe de corrección](CONTRACT_TERMS_CORRECTION.md).

# Validación vigente — perfiles y carreras

27/10/2026: Debug y Release, **179 superadas, 0 fallidas, 0 omitidas**, cuatro proyectos sin errores/advertencias. Se mantienen las 155 anteriores. GodotUiSmoke.gd final código 0 con FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK; cinco pantallas y dos carreras persistentes verificadas de forma aislada. GodotApplicationSmoke.gd pasa con OpenGL real tanto en ventana como en fullscreen. No se declara por ello inspección manual de apariencia. Cuatro publicaciones idénticas por SHA-256. Firma real bloqueada; operación contractual probada con fixtures sintéticos aprobados.

[Archivos, pruebas, riesgos, almacenamiento y comandos](CAREER_CREATION.md).

## Historial de validaciones anteriores

### Validación — ficha institucional de club, Entrega 4

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

Ejecución final confirmada y revisión visual parcial del formulario de entrenador en ventana ampliada, con avatar, campos y pie visibles. Queda la revisión manual completa del resto de pantallas y resoluciones exactas.


## Análisis → Informes — 31/10/2026

Compilaciones Debug y Release correctas, cero errores y cero advertencias. **422 pruebas superadas en Debug y 422 en Release**, sin fallos ni omisiones; resultados en artifacts/test-results/analysis-debug.trx y analysis-release.trx. Se mantienen las 403 pruebas previas y se añaden 19 casos.

Godot 4.7.2 .NET: modo enfocado y recorrido completo final con código 0. Se comprobaron cuatro ámbitos, técnico real, grupos compartidos, alineación, desplazamiento, persistencia y capturas de 1280×720, 1600×900 y 1920×1080; también pasó la regresión nativa de arrastre/tácticas/cantera. Se inspeccionaron capturas de resultados y prioridades. El primer recorrido se detuvo en un arrastre previo y la repetición completa pasó.

Cinco publicaciones SQLite intactas por SHA-256. Sin migraciones SQL nuevas ni dependencias añadidas. Furvo se lanzó de nuevo en una ventana normal de 1280×720 y se dejó en ejecución. [Informe, archivos, decisiones y límites](ANALYSIS_REPORTS.md).
