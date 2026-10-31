## Estado vigente — planes tácticos y seis disposiciones

Inicial, Ganando y Perdiendo tienen dos disposiciones propias y un único once por plan; mentalidad e instrucciones independientes y balón parado común. Se conserva una fuente canónica de composición, con lectura compatible y conversión transaccional al guardar. [Modelo, migración, archivos y comprobación](TACTICAL_PLANS.md). Las descripciones anteriores de una táctica única se conservan como historial.

## Segunda iteración de Táctica

Arrastre nativo, formación reubicada, selector Con balón/Sin balón, diagramas enlazados y nuevas asignaciones de balón parado. Extensión táctica versión 2 bajo guardado explícito, compatible con lecturas v1. [Informe e inventario de esta iteración](TACTICS_ITERATION_2.md). Este apartado actualiza el alcance descrito a continuación, que se conserva como historial.

## Estado vigente — Equipo → Táctica

La alineación existente se integra en Táctica junto con Estilo de juego, Transiciones y Balón parado. Mentalidad común y configuración estructurada persistente por carrera. Extensión SQLite independiente versión 1, creada al guardar; lecturas sin migraciones. [Decisiones, archivos y comprobación](TACTICS.md). Los apartados siguientes conservan el historial; las referencias a Equipo → Alineación se entienden ahora como Táctica → Alineación.

## Estado vigente — plantilla de seguimiento e Inicio simplificado (28/10/2026)

Equipo > Plantilla tiene doce columnas: Rol, Posición, Jugador, Calidad, Estado físico, Ritmo, Moral, Forma (últimos 5), PJ, G, A y Media. Indicadores reutilizables sin valoraciones inventadas; los atributos individuales siguen en la ficha. En el estado inicial confirmado sin partidos: PJ/G/A=0, Media — y cinco círculos de forma inactivos. Calidad, condición, ritmo y moral continúan sin datos. La distribución inicial de roles está aprobada pero pendiente de reglas y persistencia: se mantiene Sin asignar. Inicio abre directamente el escritorio; Bandeja se abre desde la cabecera y tiene regreso al escritorio, sin submenú redundante.

[Componentes, contratos de datos, archivos, validación y dependencias](SQUAD_INDICATORS.md). Debug/Release: 265 pruebas superadas y compilaciones sin errores ni advertencias. Recorrido Godot y salida en pantalla completa superados; capturas revisadas a tres resoluciones. No hay cambios SQLite. Los apartados anteriores sobre ocho columnas y submenú de Inicio quedan como historial y son sustituidos por este estado.

## Corrección vigente — menú, Club propio y plantilla deportiva (28/10/2026)

La cabecera de carrera tiene solo Menú de partida como acceso al menú; Guardar/Cargar siguen deshabilitados. Club abre el empleador contratado y en paro muestra Sin club, sin selector público ni Elegir club. Equipo > Plantilla es una pantalla independiente con ocho columnas: Rol, Posición, Jugador, Calidad, Condición y tres atributos reales por demarcación. Roles Sin asignar y calidad/condición — hasta disponer de sistemas persistidos; sin atributos ocultos ni valoraciones inventadas. El explorador inicial previo a contratar se conserva.

[Informe, correspondencias por posición, archivos y validación](NAVIGATION_SQUAD_CORRECTION.md). Debug/Release sin errores ni advertencias; 249 pruebas superadas por configuración y regresión Godot superada, incluida salida en pantalla completa. Catorce bases preexistentes intactas; no hay migraciones. Las descripciones anteriores de un selector público dentro de carrera quedan sustituidas por esta corrección.

# Interfaz principal de carrera y navegación

Entrega de 28/10/2026. Se mantiene la arquitectura Core/Persistence/Godot, la publicación económica v5 y todos los datos anteriores. No hay migraciones, tablas ni nuevas mecánicas deportivas.

## Entrada y datos

Tras confirmar firma, rechazo o paro directo, Main.EnterCareer reabre la carrera con SqliteCareerStore.Load y carga su mundo mediante SqliteWorldStore y SqliteEconomyStore. WorldQueries recibe esa instantánea y la fecha persistida. La cabecera, el perfil y el contrato proceden de CareerSummary; el club explorado no es el empleador. Las consultas del catálogo inicial quedan separadas de las de carrera y se recuperan al volver al menú principal.

CareerWorkspace en Core mantiene la ruta y los permisos de esa carrera. Valida identidad y coherencia laboral; CanManageClub/RequireManagement solo admiten su ClubId contratado. No depende de visibilidad de botones ni del último club consultado. Es estado de navegación no persistido: no cambia la fecha, reputación ni las condiciones firmadas.

## Mapa vigente

| Área fija | Opciones disponibles | En paro |
|---|---|---|
| Inicio | Escritorio, Bandeja de entrada (estado vacío real) | Escritorio propio, sin presupuesto ni empleador |
| Equipo | Plantilla, ordenación y ficha individual | Área deshabilitada; consulta pública desde Club |
| Mercado | Sin opciones operativas | Deshabilitada con explicación |
| Competición | Sin opciones operativas | Deshabilitada con explicación |
| Club | Información y estadio, Economía, Historia y récords, Explorar clubes | Solo explorar clubes y sus fichas públicas |
| Carrera | Perfil y Contrato | Perfil y estado explícito sin contrato activo |

Las seis áreas son visibles. Mercado/Competición permanecen deshabilitadas, sin abrir páginas falsas. La segunda barra solo contiene rutas implementadas y permitidas. El área/opción activa se marca en naranja y metadato active. Cabecera y barras siguen visibles en plantillas, jugadores, clubes y contrato. No hay barra lateral principal.

La cabecera incluye marca, entrenador, club real o «Entrenador libre — Sin club», temporada, fecha, Bandeja, Continuar, menú de partida y menú principal. Contenedores adaptables y ancho mínimo de marca evitan que FURVO se parta en vertical. El menú de partida conserva opciones gráficas, regreso al menú principal y salir; Esc sigue abriéndolo. No se cierra el juego mediante Esc.

## Dos escritorios

Contratado: identidad del club y escudo ficticio reutilizado, próximo partido sin calendario (estado vacío), situación sin clasificación inventada, objetivos contractuales exigibles únicamente si constan en el contrato aceptado, plantilla con recuento real y acceso directo, economía de la copia de carrera y bandeja vacía. No se convierte la propuesta institucional en objetivo aceptado ni se inventan puntos/resultados/confianza.

En paro: identidad y estado profesional, ofertas/vacantes vacías, bandeja y explicación del historial aún no implementado. No hay reputación numérica no persistida, presupuestos propios, plantilla gestionable ni contrato activo. Las listas reutilizables CareerEmptyList reciben entradas reales o colecciones vacías; no se fabrican vacantes ni empleadores.

La fecha y temporada son permanentes en la cabecera. Las tarjetas no duplican todos esos campos. A 1280×720 hay desplazamiento vertical cuando hace falta; nunca es necesario desplazamiento horizontal para la navegación principal.

## Funciones existentes reutilizadas

Main.ShowClubs/ShowClub/ShowPlayer mantienen DataTable, ordenación por nombre/posición/edad, selección estable, ClubInformationPage (incluido estadio), ClubHistoryPage, ClubEconomyPage, PlayerSheet, retrato, campo de posiciones y fortalezas/debilidades. La ficha conserva 51 atributos visibles y no revela los diez ocultos; no hay valoración global visible.

En carrera se retiran Elegir este club, la contratación inicial y Comenzar en paro del explorador público: no son acciones válidas para una carrera ya iniciada. Abrir otro club no cambia el empleador ni da permisos. Volver desde jugador mantiene la plantilla y selección; al pasar de otro club al equipo propio se reinicia la selección ajena. Los cambios de pestaña del club propio sincronizan el área contextual y descartan callbacks diferidos si ya cambió la ruta.

Las fichas se abren a ancho completo dentro del contenido de carrera, preservando el contexto de ruta, club, orden e identidad seleccionada. Esta separación entre marco fijo y contenido deja preparada una presentación lateral opcional cuando haya espacio; no se implementa un panel lateral permanente ni se comprimen las tablas extensas.

Perfil consulta la identidad global congelada de ese entrenador de carrera y su estado profesional actual. Contrato usa la instantánea aceptada, no una oferta reconstruida desde el catálogo. En paro muestra que no hay contrato. El historial profesional completo y las cifras de reputación todavía no existen y no se inventan.

## Permisos y persistencia

Todas las rutas de club propio pasan por CareerWorkspace.Open y RequireManagement, además de adaptar los botones. Un entrenador contratado tampoco puede gestionar el club que consulta públicamente. No se añaden comandos de gestión, solo acceso a las consultas ya existentes. Entrar en una ruta propia prohibida desde código es rechazado por Core. Los ámbitos de navegación de carreras diferentes son instancias separadas.

Navegar consulta instantáneas y no llama a Apply, Create ni a un avance de tiempo. No hay nuevas migraciones ni formatos. Mundo 4 + extensión económica 1, carreras 1/2 y perfiles 1 siguen igual. La carga desde el menú principal permanece fuera de alcance; la reapertura de Persistence se usa al entrar en una carrera recién creada, sin sobrescribirla. No se modifican carreras o perfiles anteriores para añadir reputación o historial.

## Arquitectura prevista, no implementada

Inicio: noticias y avisos se integrarán cuando exista una fuente real de comunicaciones. Bandeja está preparada para mensajes/decisiones persistidos, sin generar ejemplos en la partida.

Equipo: pizarra, entrenamiento, vestuario, salud y cantera se incorporarán como rutas al disponer de sistemas reales. Pizarra futura: izquierda plantilla/suplentes/reservas; centro campo grande con once; derecha instrucciones/ficha contextual; abajo banquillo, validaciones y guardar alineación. Subsecciones: Alineación, Táctica, Roles, Balón parado y Análisis del rival. Reutilizar FootballPitchGeometry/FootballPitch respetando proporciones y marcas, sin áreas inexistentes en bandas. No se implementan arrastre, roles ni instrucciones tácticas.

Mercado: búsqueda, fichajes/ventas, cesiones, contratos de jugadores, ojeadores y libres esperan a servicios y modelos de mercado. No se presenta como operativa una búsqueda que no existe.

Competición: clasificación, calendario, resultados, estadísticas e historial esperarán a modelos deportivos persistidos. No se fabrican tablas o próximos partidos.

Club: Directiva, Staff, Instalaciones y Afición tendrán rutas propias cuando exista funcionalidad además de los datos informativos existentes. Staff prevé segundo entrenador, preparador físico, fisioterapeuta y ojeador; dependerá de entrenamiento, lesiones, scouting y economía. Su presupuesto existente no implica empleados ni bonificaciones; sin generación, contratación ni despido.

Carrera: reputación, historial profesional, ofertas y guardar/cargar se incorporarán al existir modelos y comandos completos. No se habilita un botón vacío de guardado. El estado actual ya se conserva mediante la operación de creación de carrera; esta entrega no crea otro flujo de persistencia.

Continuar: diseño híbrido futuro, avanzar hasta siguiente evento relevante y consultar fechas desde calendario; eventual avance a fecha concreta respetando decisiones obligatorias. Separar eventos informativos de los obligatorios. Requiere calendario, cola de eventos, política de detención y avance transaccional validado antes de modificar la fecha. Por ahora CanAdvanceTime=false y botón deshabilitado con explicación; no finge un día avanzado.

## Reputación y mercado de entrenadores: decisiones para después

Reputación propia de cada carrera, interna 0–100 y visible 1–5, independiente de la del club. No se hereda reputación por entrenar a un club. Debutante y exfutbolista sin trayectoria de entrenador partirán del nivel visible 1; sin diferencias por antecedentes en esta etapa. El valor interno inicial exacto y su persistencia se resolverán al implementar el modelo; no se añade una cifra arbitraria ahora.

Regla propuesta de elegibilidad futura: reputación visible del club <= reputación visible del entrenador + 1. Nivel 1: clubes hasta 2; 2: hasta 3; 3: hasta 4; 4–5: hasta 5. Elegibilidad no garantiza ofertas; dependerán de vacantes reales, presupuesto, objetivos e IA. Esta regla no se implementa ni modifica la selección inicial aprobada. Sin excepciones por crisis, despidos, dimisiones, ofertas automáticas ni renovaciones. Futuras ofertas comenzarán en fecha efectiva; contratos iniciales actuales siguen siendo de un año y el paro no tiene contrato activo.

## Inventario de esta entrega

Creados:
- src/Furvo.Core/Careers/CareerWorkspace.cs: rutas, áreas y autorización por estado laboral.
- src/Furvo.Godot/Scripts/Main.Navigation.cs: marco fijo, dos escritorios, bandeja, perfil, contrato y consultas de carrera.
- src/Furvo.Godot/Scripts/Components/CareerEmptyList.cs: comunicaciones, ofertas y vacantes sin entradas artificiales.
- tests/Furvo.Tests/CareerNavigationTests.cs: rutas, permisos, identidad, fecha, economía e independencia.
- docs/CAREER_NAVIGATION.md: este mapa, decisiones y guía.

Modificados:
- src/Furvo.Godot/Scripts/Main.cs: integración del marco de carrera, separación del catálogo, exploración pública y contexto de fichas.
- src/Furvo.Godot/Scripts/Main.Careers.cs: entrada al escritorio tras confirmar y bloqueo de contratación inicial dentro de carrera.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados de áreas, paneles y estados.
- tools/GodotUiSmoke.gd: recorridos contratado/en paro, seis áreas, navegación, fichas, permisos, fecha y capturas.
- tools/GodotApplicationSmoke.gd: escritorio y salida desde pantalla completa.
- docs/ARCHITECTURE.md, CAREER_CREATION.md, INVENTORY.md, VALIDATION.md y README.md: estado actual y resultados.

Se mantienen los cambios anteriores de economía/contratación presentes en el repositorio. No hay nuevas dependencias, cambios SQLite ni commit/push.

## Resultados ejecutados — 28/10/2026

- Compilación Debug y Release: cuatro proyectos, 0 errores y 0 advertencias.
- Pruebas .NET: 244 superadas, 0 fallidas y 0 omitidas por configuración (234 anteriores + 10 casos de navegación/permisos). Incluyen denegación de rutas propias desde código, club ajeno, identidad, fecha sin avance, hashes y economía de cada copia.
- GodotUiSmoke.gd: salida 0, FURVO_WORKSPACE_OK en paro y contratado, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Se mantienen formulario, firma/rechazo/paro, tres carreras aisladas, tablas y 51 atributos públicos.
- Prueba con renderizado OpenGL real y capturas de ambos escritorios a 1280×720, 1600×900 y 1920×1080: recorridos completos superados. Se revisaron las capturas; se corrigieron anchura/altura de la cabecera y se añadieron comprobaciones de límites/solapamiento.
- GodotApplicationSmoke.gd con modo fullscreen real: salida 0 y FURVO_APPLICATION_SMOKE_OK; escritorio, seis áreas, Esc y salida del juego.
- SHA-256: doce bases preexistentes idénticas, incluidas las cinco publicaciones del mundo. Sin migraciones ni nuevas publicaciones.
- git diff --check sin incidencias. No se declara una campaña manual completa de todos los clubes/equipos: la inspección visual cubre las capturas indicadas y los recorridos automatizados descritos.

## Validación e inspección

Los resultados finales se registran en docs/VALIDATION.md, distinguiendo pruebas de revisión de capturas. Los artefactos de prueba y capturas se escriben en artifacts (ignorado por Git), no en los perfiles o carreras personales. Los hashes de las publicaciones y bases preexistentes se comparan al cierre.

Capturas reproducibles: artifacts/career-visual/career-employed-1280x720.png y career-unemployed-1280x720.png; variantes 1600x900 y 1920x1080. Usan perfiles sintéticos de prueba.

## Comprobación manual en Windows

1. Compilar y ejecutar Godot .NET. Nueva partida: crear/reutilizar un perfil, elegir club, firmar y confirmar. Debe abrir el escritorio de ese club.
2. Ir a Equipo → Plantilla; ordenar, seleccionar y abrir futbolista; comprobar los 51 atributos públicos, campo y fortalezas, y volver conservando selección.
3. Club → Economía/Información y estadio/Historia. Explorar otro club y verificar que el empleador de cabecera no cambia. Volver a Equipo.
4. Carrera → Perfil y Contrato. El sueldo/fechas deben ser los de la instantánea firmada.
5. Menú principal → Nueva partida con el mismo perfil → Comenzar en paro, o rechazar oferta. Debe mostrar Entrenador libre — Sin club, listas vacías, Equipo deshabilitado y contrato inexistente. Club permite consulta pública sin Elegir este club.
6. Bandeja muestra vacío real; Continuar explica por qué no avanza. Fecha estable al navegar.
7. Revisar 1280×720, 1600×900 y 1920×1080; menú de partida/Esc, opciones gráficas, volver al menú y salir en pantalla completa.

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd" -- --capture
& $env:GODOT4 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- fullscreen
& $env:GODOT4 --path .\src\Furvo.Godot
~~~

GODOT4 debe contener la ruta del ejecutable Godot 4.7.2 .NET instalado. No se comienza otra entrega.


## Alineaciones — 28/10/2026

La entrega de alineaciones añade Equipo → Plantilla / Alineación, selección inicial reproducible y persistencia independiente por carrera. La apertura de carreras antiguas no migra ni genera alineaciones automáticamente. Véanse [arquitectura, reglas e inventario de archivos](LINEUPS.md) y [formaciones iniciales de los veinte clubes](INITIAL_FORMATIONS.md). Esta sección actualiza las referencias anteriores a roles sin asignar y al orden inicial por rol: ahora las carreras nuevas tienen once y se ordenan por posición futbolística.


## Desarrollo deportivo integrado

Equipo incorpora Plantilla, Táctica, Entrenamiento, Análisis y Cantera; Club añade Instalaciones y Tecnología deportiva. Hay planificación persistente, grupos, configuración de delegación, dos capitanes, movilidad con normativa explícita e informes de datos existentes. La ejecución temporal, normativa incompleta, personal especializado y compras sin aprobación permanecen bloqueados o preparados, sin simular su disponibilidad.

[Informe, archivos, migraciones y límites](SPORTS_DELIVERY.md) · [Fuentes](SPORTS_RESEARCH.md).
