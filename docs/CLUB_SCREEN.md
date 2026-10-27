# Ficha de club — identidad, historia y economía

## Recorrido actual

Inicio → Clubes → seleccionar club → **Ver club** o doble clic. La ficha abre siempre **Información**. Pestañas superiores, en orden: **Información, Plantilla, Historia, Economía**. Cabecera y Volver a clubes son comunes. Al volver de un futbolista se abre Plantilla con el mismo PlayerId, orden y selección.

La cabecera muestra emblema provisional original de Furvo, nombre, ciudad/región/país, competición vigente y reputación visible. El emblema y la imagen de estadio son dibujos vectoriales nativos, sin imágenes de terceros ni escudos reales.

## Datos mostrados

GetClubDetails proyecta la instantánea cargada por Persistence: nombre/ciudad/país/reputación de Club, región/fundación/colores/tradición/estadio/capacidad de ClubProfile y competición/temporada de inscripciones vigentes en ReferenceDate. No hay SQL en componentes Godot. Las fechas de fundación existentes se conservan, sin derivarlas del club real ni del reloj.

Apodo, nombre de afición, rivalidades, abonados, ciudad del estadio, asistencia media, récord y ocupación no existen todavía como datos aprobados en el esquema. Sus campos opcionales son null y se muestran como **Pendiente de definir**. No se asume que ciudad del estadio y del club sean iguales. Las cifras desconocidas no se representan como cero.

Plantilla reutiliza DataTable y GetSquad; conserva 24 jugadores, sort, doble clic/Enter, selección e ingreso a PlayerSheet. No se duplican tablas ni catálogos de jugadores. La ficha de jugador y sus 51 atributos y paneles inferiores se conservan.

Historia prepara trayectoria, tabla de temporadas, gráfico, palmarés, personajes y récords. GetClubHistory devuelve explícitamente ausencia de datos aprobados; no convierte temporadas/inscripciones en resultados. Tabla y gráfico están vacíos, sin las temporadas de ejemplo del encargo. No se importan las propuestas. El gráfico recibe colecciones ordenadas sin límite fijo de tres temporadas: primero arriba, bandas separadas de Primera/Segunda, líneas solo entre temporadas consecutivas conocidas y flechas de cambio de categoría. No se ejecutan ascensos ni simulación.

Economía muestra nueve campos en estado **No disponible**: tesorería, presupuestos de fichajes/salarios, salarios comprometidos, ingresos, gastos, balance, deuda y situación. Una explicación distingue saldo disponible, límites planificados y balance del período. No existen importes, fórmulas aplicadas, operaciones o reglas económicas.

## Historia propuesta y persistencia pendiente

[CLUB_HISTORY_PROPOSAL.md](CLUB_HISTORY_PROPOSAL.md) contiene veinte propuestas y sesenta personajes ficticios de archivo, solo para revisión editorial. No es una publicación aprobada ni crea PlayerId, CoachId o plantillas históricas. Conserva los datos institucionales aprobados y propone cronologías, resultados, rivalidades y apodos. No define clubes futuros de Segunda.

[CLUB_HISTORY_SCHEMA_PROPOSAL.md](CLUB_HISTORY_SCHEMA_PROPOSAL.md) separa historia editorial previa a 2026/27 y resultados generados en cada partida; describe tablas/campos futuros, identidades y migración segura. **No se aplica ningún esquema nuevo**. La clasificación histórica acumulada propuesta usa puntos normalizados de Primera (3 por victoria y 1 por empate), con desempates documentados, no reputación. Falta una fuente aprobada de estadísticas completas, por lo que la posición acumulada sigue pendiente.

## Componentes

- ClubHeader, ClubEmblem y StadiumPreview: cabecera e imágenes provisionales originales.
- ClubInformationPage: identidad/estadio con datos existentes y pendientes claros.
- ClubHistoryPage, ClubHistoryChart y ClubHistoryPlot: estructura de historia y gráfico genérico sin resultados inventados.
- ClubEconomyPage: estructura visual sin sistema financiero.
- ClubUi: paneles, campos y desplazamiento compartidos.
- ClubDetails.cs y WorldQueries: contratos/proyecciones públicos, sin jugadores ocultos ni SQL en presentación.

## Validación actual

El 27/10/2026 pasan **129 pruebas**, incluidas las 120 anteriores. Debug y Release: cuatro proyectos, cero errores y advertencias. La prueba de escena de Godot 4.7.2 .NET pasa comprobando Información inicial, cuatro pestañas, ClubId constante, campos conocidos/pendientes, historia vacía, importes ausentes, sort de plantilla, ficha de futbolista y regreso. Se comprueban tamaños 960×640, 1280×720 y 1600×900 y la posición del botón común sin solapamientos.

Se contrastan por hash las publicaciones v1–v4, Persistence, entidades existentes, DataTable y PlayerSheet sin cambios. No se abren ni modifican partidas reales. La aritmética editorial de la propuesta se revisa por separado; no es validación de resultados deportivos reales.

La prueba sin ventana valida comportamiento y geometría, no la apariencia. Queda inspeccionar manualmente escudo/estadio, espacios, textos largos y scroll en cada pestaña. La versión actual se ejecuta al finalizar el encargo para facilitar esa revisión. No se ha probado una exportación Windows.

## Rediseño vigente — 27/10/2026

La composición anterior se amplía conforme a la captura recibida: tres equipaciones y emblemas provisionales por ClubId, identidad institucional completa, estadio con campo horizontal corregido y dos ilustraciones originales, síntesis histórica inferior y cinco bloques económicos. Los campos ausentes permanecen pendientes o no disponibles. La categoría del estadio es opcional 1–5, independiente de reputación; veinte candidatos se documentan sin cargar.

[Composición, validación, diferencias y componentes actuales](CLUB_PRESENTATION_REDESIGN.md). Estado: 138 pruebas Debug/Release y smoke Godot correcto, incluida geometría a 1920×1080; inspección visual real parcial a 1280×720. Las publicaciones no cambian.
