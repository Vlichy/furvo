## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](docs/INITIAL_CONTRACT_SIMPLIFICATION.md).

## Corrección contractual vigente — 27/10/2026

La validación actual sustituye las reglas de bloqueo descritas en los apartados históricos: solo exige identidad válida, fechas válidas, salario positivo, moneda admitida y contenido/prioridad de obligaciones expresamente exigibles. Las preferencias orientativas pendientes no bloquean ni se guardan como compromisos aceptados. La duración se calcula en meses desde las fechas. Extensión de carrera 2 para nuevas carreras; carga de extensión 1 en solo lectura. Las veinte ofertas reales siguen pendientes de fecha final, salario anual y moneda.

Validación: Debug y Release, 209 pruebas superadas; compilación de cuatro proyectos sin errores ni advertencias. Prueba de escena Godot sin ventana superada; no equivale a revisión visual manual. Véase [informe de corrección](docs/CONTRACT_TERMS_CORRECTION.md).

# Furvo — perfiles de entrenador e inicio de carrera

Videojuego comercial de gestión futbolística para Windows con C#/.NET 10, Godot 4.7.2 .NET y SQLite. Se conservan Core, Persistence, Godot y Tests. La versión actual conserva la exploración de los veinte clubes y sus 480 futbolistas, y permite crear perfiles de entrenador reutilizables e iniciar carreras persistentes en paro. Las ofertas se consultan, pero su firma real sigue bloqueada por condiciones laborales y compromisos todavía no aprobados. No se implementa simulación deportiva.

V1.0 será la primera versión comercial. Su alcance aprobado está en [V1_CONTRACT.md](docs/V1_CONTRACT.md), separado de las entregas internas.

## Obtener el código

Repositorio: [Vlichy/furvo](https://github.com/Vlichy/furvo). Es un proyecto comercial en desarrollo; este repositorio no equivale a la publicación comercial V1.0.

Con acceso al repositorio privado:

~~~powershell
git clone https://github.com/Vlichy/furvo.git
cd furvo
dotnet restore Furvo.sln
~~~

No se incluyen el motor Godot, SDK, paquetes NuGet restaurados ni cachés. Las cuatro bases iniciales v1–v4 sí están versionadas; no deben eliminarse para una instalación limpia. No se incluyen partidas personales ni credenciales.

Preparación inicial, inventario exacto y política de publicación: [GIT_PUBLICATION.md](docs/GIT_PUBLICATION.md).

## Abrir y ejecutar

1. Usar SDK **.NET 10.0.402 x64**, Godot **4.7.2 .NET x64** y VS Code con C#. Abrir esta carpeta y Furvo.sln en VS Code. global.json conserva .NET 10.
2. Ejecutar los comandos de compilación y pruebas desde la raíz:

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release --no-restore
dotnet list Furvo.sln package --include-transitive --vulnerable
~~~

3. Importar **src/Furvo.Godot/project.godot** en Godot .NET, compilar C# y pulsar F5. La resolución inicial es **1280 × 720**, con mínimo de ventana 960 × 640 y contenedores adaptables. Para depurar desde VS Code, GODOT4 debe apuntar al ejecutable principal Godot .NET; reiniciar VS Code tras cambiar la variable. La configuración existente y los cuatro proyectos se conservan.

## Recorrido de consulta

Inicio → Nueva partida → crear o seleccionar entrenador → explorar clubes → seleccionar club → Ver club → **Información**. La ficha tiene cuatro pestañas: **Información, Plantilla, Historia y Economía**. En Plantilla se conserva seleccionar futbolista → Ver futbolista. También se admite doble clic o activación de teclado en las filas. Los botones de apertura se habilitan al seleccionar realmente una fila; pasar el cursor por encima no es selección.

Las cabeceras **Nombre, Posición y Edad** alternan orden ascendente/descendente. La selección se asocia a PlayerId y se conserva después de ordenar y al volver de la ficha. Las tablas de listas tienen desplazamiento; la ficha muestra cuatro bloques en tres columnas: técnicos (17), mentales (16) y físicos (8) con portero (10) justo debajo. No tiene pestañas; conserva desplazamiento cuando hace falta, y todos los valores siguen en 1–20. No muestra los diez ocultos ni una valoración global.

La ficha incluye nombre, club, edad al inicio de temporada, nacimiento, nacionalidad y posiciones principal/secundarias. Debajo de los atributos incorpora un campo con esas posiciones y un panel descriptivo de fortalezas/debilidades relevantes, sin valoración global. En ventanas pequeñas los paneles se apilan y todo el contenido puede desplazarse. Los botones permiten regresar Futbolista → Plantilla del mismo club → Clubes → Entrenadores → Inicio. La carga y los errores de consulta tienen mensajes en español, recuperación y navegación de regreso.

## Datos y conservación

La publicación activa es **data/initial-world-2026-27.v4.db**, embebida desde esa misma ruta al compilar Godot. No hay listas manuales de clubes/jugadores en la presentación ni se regenera el mundo al iniciar. Al solicitar la consulta, PublishedWorldQueries materializa sus bytes en **user://catalogs**, con nombre SHA-256, y los abre mediante SqliteWorldStore de solo lectura. Esa caché de distribución no es una partida. Si una copia existente tiene otros bytes, se rechaza sin sobrescribirla.

Se conservan veinte clubes, 24 jugadores por club, 480 contratos y 29.280 valores de atributos. Los servicios públicos de Core proyectan únicamente los 51 atributos visibles; Godot no recibe Player ni AllValues. La fecha de referencia procede de la temporada, sin reloj del sistema.

**El esquema del mundo sigue siendo 4.** Esta entrega añade migraciones separadas para perfiles y la extensión de carreras nuevas, sin modificar las bases anteriores. Los SQLite v1/v2/v3 y los scripts 001–004 mantienen sus hashes. No se han abierto ni modificado partidas reales. La reputación de la publicación y la interfaz conserva 1–5. El catálogo versionado conserva los veinte valores internos 0–100 y deriva los visibles por los tramos aprobados. Fragua del Sur pasa a interna **32** y visible **2**; los otros diecinueve no cambian. Los valores internos residen en el catálogo, SQLite mantiene su semántica visible 1–5. Véase [DECISIONS.md](docs/DECISIONS.md).

Para crear otra copia o verificar la nueva publicación sin sobrescribir archivos:

~~~powershell
dotnet run --file tools/BuildAdjustedWorld.cs -- data/initial-world-2026-27.v4.db data/initial-world-2026-27.v3.db
~~~

La v3 se abre solo para lectura; v4 tiene otro WorldId y reutiliza exactamente sus futbolistas, contratos y atributos. Los cargadores y herramientas anteriores se conservan para sus publicaciones históricas. Repetir la utilidad verifica v4 sin reescribirla.

## Ficha institucional e historia propuesta

Información muestra datos existentes de SQLite y marca como pendientes apodos, afición, rivalidades, abonados y métricas de estadio sin datos. Historia prepara trayectoria, tabla, gráfico, palmarés y leyendas, sin resultados inventados. Economía distingue tesorería, presupuestos y balance, todos sin cifras. Guía: [CLUB_SCREEN.md](docs/CLUB_SCREEN.md). El [rediseño de club](docs/CLUB_PRESENTATION_REDESIGN.md) añade equipaciones/emblemas provisionales, tres vistas del estadio con campo horizontal corregido, síntesis histórica y cinco bloques financieros, sin modificar SQLite.

La [propuesta histórica de los veinte clubes](docs/CLUB_HISTORY_PROPOSAL.md) es **BORRADOR NO APROBADO**, solo de desarrollo. No se incorpora a SQLite ni a la interfaz. El [esquema histórico propuesto](docs/CLUB_HISTORY_SCHEMA_PROPOSAL.md) requiere aprobación antes de cualquier migración. No se generan clubes de Segunda, personajes jugables ni sistemas deportivos/económicos.

## Estado de validación

El 27/10/2026: **179 pruebas aprobadas, 0 fallidas y 0 omitidas**, incluidas las 155 anteriores. Los cuatro proyectos compilan en Debug y Release sin errores ni advertencias. La prueba de escena Godot 4.7.2 .NET pasa para las cuatro pestañas, datos pendientes, navegación y regresión de futbolistas. Comprueba geometría a 960×640, 1280×720, 1600×900 y 1920×1080.

No se modifica ninguna publicación v1–v4, reputación, jugador, contrato ni atributo; no hay migraciones ni dependencias nuevas. La versión actual se abre al finalizar el encargo. Queda inspección visual manual de las nuevas pestañas y sus dibujos; no se prueba exportación Windows.

~~~powershell
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

Detalle de comprobaciones y pasos manuales: [VALIDATION.md](docs/VALIDATION.md). Inventario y responsabilidades: [INVENTORY.md](docs/INVENTORY.md). Arquitectura: [ARCHITECTURE.md](docs/ARCHITECTURE.md). Catálogo: [WORLD_MODEL.md](docs/WORLD_MODEL.md). Plantillas y algoritmo de generación: [SQUADS_2026_27.md](docs/SQUADS_2026_27.md).

No se implementan entrenador, nueva partida, guardado desde UI, valoración interna, motor, fichajes, economía funcional, IA, calendarios o Segunda. Las decisiones futuras y el plan de probadores se registran sin implementar ni iniciar otra entrega. Los nombres ficticios y el equilibrio deportivo requieren revisión antes de la publicación comercial.

La pestaña Información permite revisar [filosofía y expectativas propuestas para los veinte clubes](docs/CLUB_DIRECTION_PROPOSAL.md), con avisos de borrador. Los objetivos no están aprobados ni incorporados a SQLite; no existe evaluación de directiva.

La exploración incluye «Elegir este club» para abrir la oferta sin crear todavía una carrera. La [corrección de maquetación](docs/UI_LAYOUT_FIX.md) ajusta Plantilla, Récords y navegación; Palmarés se consulta solo en Historia y Leyendas solo en Información.

El inicio actual ofrece Nueva partida (selección de entrenador y explorador), Cargar partida deshabilitado, Opciones de pantalla de sesión y Salir. Esc permite continuar, volver al menú principal, abrir opciones y salir sin iniciar partidas. La plantilla abre por grupos deportivos; Economía muestra temporadas reales y estado No evaluado. [Guía y pruebas de aplicación con ventana](docs/NAVIGATION_POSITIONS_FINANCE.md).

## Flujo actual de nueva carrera

Nueva partida → seleccionar o crear entrenador → explorar clubes → consultar oferta → confirmar inicio. Se puede comenzar directamente en paro o rechazar una oferta y comenzar sin club. Elegir o volver no crea partida. Firmar contrato permanece bloqueado mientras no haya reglas aprobadas; su operación se valida solo con condiciones sintéticas de pruebas. Cada carrera tiene un archivo independiente y su entrenador tiene ID propio, con copia del perfil.

Perfiles y carreras se guardan en user://coach-profiles y user://careers; por defecto en Windows bajo %APPDATA%/Godot/app_userdata/Furvo. Cargar desde el menú queda para una entrega posterior; el almacén ya recupera carreras y se prueba la reapertura. No se modifican las cuatro publicaciones SQLite ni partidas existentes. Debug/Release: 179 pruebas y compilación sin errores/advertencias. [Guía, inventario y validación](docs/CAREER_CREATION.md).
