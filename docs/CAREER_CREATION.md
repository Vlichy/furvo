## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](INITIAL_CONTRACT_SIMPLIFICATION.md).

## Corrección contractual vigente — 27/10/2026

La validación actual sustituye las reglas de bloqueo descritas en los apartados históricos: solo exige identidad válida, fechas válidas, salario positivo, moneda admitida y contenido/prioridad de obligaciones expresamente exigibles. Las preferencias orientativas pendientes no bloquean ni se guardan como compromisos aceptados. La duración se calcula en meses desde las fechas. Extensión de carrera 2 para nuevas carreras; carga de extensión 1 en solo lectura. Las veinte ofertas reales siguen pendientes de fecha final, salario anual y moneda.

Validación: Debug y Release, 209 pruebas superadas; compilación de cuatro proyectos sin errores ni advertencias. Prueba de escena Godot sin ventana superada; no equivale a revisión visual manual. Véase [informe de corrección](CONTRACT_TERMS_CORRECTION.md).

# Creación de carreras

Antes de implementar se documenta el contrato: perfil → explorador → oferta → confirmación → publicación atómica de una partida nueva. También se permite paro directo. Perfil, entrenador de carrera (CoachId) y partida (SaveId) tienen identidades distintas. No se usa el perfil como entrenador ni se crea un club para el paro.

Se reutilizan WorldDatabase, SqliteWorldFiles.Publish y backup SQLite de solo lectura. El mundo conserva user_version=4. Carrera añade Career001_Initial.sql y career_schema_version=1 solo al archivo temporal nuevo; perfiles tienen esquema independiente. Las publicaciones v1–v4 y partidas existentes no se migran automáticamente. La extensión no altera tablas anteriores ni convierte coaches.club_id a NULL: el entrenador de carrera vive en career_coaches, con identidad inicial congelada.

Nuevas tablas: career_coaches, career_state, manager_contracts, declined_manager_offers y career_schema_version. FKs a clubes/temporadas/metadatos y restricciones aseguran contratado ⇔ club y contrato activo presentes; paro ⇔ ambos ausentes. Las dos partes de la relación carrera/contrato se validan en una única transacción. Se publica mediante rename sin sobrescritura. Si falla antes del rename, solo se limpia el temporal propio.

Solicitud de creación con SaveId estable permite reintento/doble confirmación sin otra partida: misma identidad y contenido devuelve el resultado existente; contenido distinto falla. Cada nuevo inicio explícito genera otra solicitud y CoachId. El rechazo conserva una instantánea de la oferta rechazada; Volver no la registra. Al finalizar se muestra resumen sin avanzar fecha ni simular. Cargar carreras desde menú queda pendiente; el almacén sí recupera una carrera por ID y pruebas de reapertura.

## Archivos de esta entrega

**18 archivos creados y 13 modificados.** No se añaden proyectos, herramientas ni paquetes NuGet.

| Archivo | Estado | Función |
| --- | --- | --- |
| docs/COACH_PROFILES.md | Creado | Validaciones, catálogo estable, almacenamiento e instantáneas de perfil. |
| docs/CAREER_CREATION.md | Creado | Flujo, extensión versionada, atomicidad, idempotencia y validación. |
| docs/MANAGER_CONTRACTS.md | Creado | Modelo contractual, compromisos, recursos y bloqueo por reglas pendientes. |
| src/Furvo.Core/Careers/CoachProfiles.cs | Creado | CoachProfileId, perfil validado, experiencia descriptiva y puerto de perfiles. |
| src/Furvo.Core/Careers/ManagerOffers.cs | Creado | Oferta, condiciones, compromisos/táctica y bloqueo de firma. |
| src/Furvo.Core/Careers/CareerCreation.cs | Creado | Identidad de entrenador de carrera, estados, contrato, puerto y flujo temporal. |
| src/Furvo.Core/Queries/ClubFinancialResources.cs | Creado | Única proyección de recursos de club/temporada para Economía y contrato. |
| src/Furvo.Persistence/CareerJson.cs | Creado | Serialización de instantáneas y GUID tipados, y carga de migraciones separadas. |
| src/Furvo.Persistence/SqliteCoachProfileStore.cs | Creado | Altas y lectura de perfiles sin sobrescritura ni duplicación de solicitud. |
| src/Furvo.Persistence/SqliteCareerStore.cs | Creado | Copia SQLite independiente, transacción de inicio, publicación atómica y reapertura. |
| src/Furvo.Persistence/Migrations/Profiles001_Initial.sql | Creado | Esquema de perfiles 1, nacionalidades, nulabilidad, longitud y edad. |
| src/Furvo.Persistence/Migrations/Career001_Initial.sql | Creado | Extensión de carrera 1 con FKs, estados y congelación de instantáneas. |
| src/Furvo.Godot/Scripts/Main.Careers.cs | Creado | Composición de selección/creación de entrenador, oferta, confirmación y bienvenida. |
| src/Furvo.Godot/Scripts/Components/CoachAvatar.cs | Creado | Avatar genérico seguro reutilizable sin PlayerId ni rutas del perfil. |
| src/Furvo.Godot/Scripts/Components/CoachProfileForm.cs | Creado | Formulario con validación, reintento y protección de doble pulsación. |
| src/Furvo.Godot/Scripts/Components/InitialManagerOfferSource.cs | Creado | Adapta los mismos borradores y proyección financiera del explorador. |
| src/Furvo.Godot/Scripts/Components/ManagerOfferDocument.cs | Creado | Documento desplazable de condiciones, compromisos, táctica y recursos pendientes. |
| tests/Furvo.Tests/CareerCreationTests.cs | Creado | 24 casos de perfiles, carreras, firma sintética, rechazo, aislamiento, snapshots y fallos. |
| src/Furvo.Core/Queries/WorldQueries.cs | Modificado | GetFinancialResources valida club/temporada y devuelve recursos desconocidos sin inventarlos. |
| src/Furvo.Persistence/SqliteWorldFiles.cs | Modificado | Publicación existente accesible internamente para reutilizar el mecanismo atómico. |
| src/Furvo.Godot/Scripts/Main.cs | Modificado | Nueva partida abre perfiles, conserva ruta de catálogo e integra las nuevas acciones. |
| src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs | Modificado | Recibe recursos desde la misma consulta del contrato; staff/jugadores separados. |
| src/Furvo.Godot/Resources/UiStrings.resx | Modificado | Textos españoles de perfiles, nacionalidades, oferta y carrera; aviso de inicio actualizado. |
| tests/Furvo.Tests/Furvo.Tests.csproj | Modificado | Enlace del adaptador puro real de ofertas para pruebas sin cargar Godot. |
| tools/GodotUiSmoke.gd | Modificado | Recorrido aislado completo, formularios, firma bloqueada, paro directo y rechazo. |
| tools/GodotApplicationSmoke.gd | Modificado | Nuevo paso por perfiles en la prueba nativa, almacenamiento aislado en artifacts. |
| docs/ARCHITECTURE.md | Modificado | Separación de perfil/carrera, extensión, fuentes compartidas y compatibilidad. |
| docs/INVENTORY.md | Modificado | Inventario actual y conservación de inventarios históricos. |
| docs/VALIDATION.md | Modificado | Resultados Debug/Release y límites de pruebas de Godot. |
| docs/CLUB_SCREEN.md | Modificado | Integración del explorador cerrado con perfiles y ofertas. |
| README.md | Modificado | Estado actual, recorridos, almacenamiento local y bloqueo de firma. |

## Resultado y comprobaciones — 27/10/2026

- Debug y Release: cuatro proyectos compilados, **0 errores y 0 advertencias**.
- **179 pruebas .NET superadas en cada configuración**, 0 fallidas y 0 omitidas. Se mantienen las 155 anteriores y se añaden 24 casos. La firma se valida con ofertas sintéticas aprobadas **solo en pruebas**, nunca incluidas en el juego.
- Godot 4.7.2 .NET sin ventana: código 0, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Se crea el primer perfil, se rechaza un formulario incompleto, se prueba doble pulsación, se reutiliza el perfil, se consulta una oferta bloqueada y se vuelve/cancela sin crear partida. Después se confirman paro directo y rechazo, se verifican dos archivos y dos CoachId distintos y se vuelve al menú por Esc.
- Se conserva la regresión de cuatro pestañas, 20 clubes, 24 jugadores por club, ordenación manual/inicial, fichas de 51 atributos visibles y ausencia de los ocultos.
- Geometría de selección, formulario, oferta, confirmación y resumen comprobada a 1280×720, 1600×900 y 1920×1080. El explorador conserva además la comprobación a 960×640. No equivale a inspección visual humana.
- GodotApplicationSmoke.gd con OpenGL y ventanas reales: salida 0 en windowed y fullscreen; aplica resolución, conserva cancelación y verifica Esc/salida tras seleccionar un perfil de prueba. No acredita una revisión manual de apariencia.
- Pruebas de fallo real durante publicación: no queda archivo parcial/temporal; reintento con la misma solicitud publica una sola carrera. FKs impiden contratos activos en paro y la eliminación del contrato requerido por una carrera contratada.
- Se contrastan por SHA-256 las cuatro publicaciones v1–v4, sin cambios. No se alteran jugadores, contratos de futbolistas, reputaciones, posiciones ni atributos. No se abren ni migran partidas personales antiguas.
- Los smokes escriben solo datos sintéticos en directorios únicos bajo artifacts y las pruebas .NET usan temporales propios. La instancia normal usa el directorio de usuario de Godot.

## Almacenamiento y compatibilidad

En Windows, por defecto: %APPDATA%\Godot\app_userdata\Furvo\coach-profiles\profiles.db y %APPDATA%\Godot\app_userdata\Furvo\careers\<SaveId>.db. Los archivos personales no se versionan. Cargar partida desde el menú continúa deshabilitado: el almacén ya recupera carreras, pero su interfaz de carga corresponde a una entrega posterior, como solicita el apartado de compatibilidad futura.

Versiones independientes: mundo PRAGMA user_version=4 (sin cambiar), perfiles PRAGMA user_version=1 con application_id propio, y extensión career_schema_version=1. Profiles001 no se ejecuta sobre mundos. Career001 solo sobre una copia temporal destinada a una nueva carrera; las migraciones 001–004 no se editan ni se aumenta WorldDatabase.CurrentVersion. La tabla coaches anterior permanece intacta y no se utiliza para representar el paro.

career_state contiene el estado **inicial** congelado en esta versión, no un motor de calendario. El avance, cambios de empleo e historial posteriores requerirán otro contrato/migración explícita que preserve la identidad inicial y los contratos aceptados. No se reescribirá su pasado. Los catálogos de nacionalidad/avatar conservarán sus claves antiguas al evolucionar. Los JSON de instantánea pertenecen al formato de extensión 1; cualquier cambio incompatible requerirá migración/versionado. No se reclama compatibilidad futura automática.

## Límites, riesgos y aprobaciones pendientes

La carrera contratada no es accesible con los datos actuales: faltan fecha final/duración, salario/moneda, rescisión, compromisos aprobados y preferencia táctica con carácter definido. El botón muestra el bloqueo y permanece deshabilitado. No se rellenan con cero ni se convierten borradores en obligaciones. El flujo de paro sí es funcional y persistente.

Los recursos financieros actuales siguen desconocidos; Economía y oferta leen la misma fuente, no dos presupuestos independientes. No se implementa contabilidad, evaluación de objetivos, confianza, sanciones, despidos, búsqueda de empleo, staff, calendario o simulación. Se requiere aprobar reglas laborales, filosofía/expectativas obligatorias u orientativas y preferencias/recursos antes de habilitar ofertas reales.

Queda revisión visual manual exhaustiva de las cinco pantallas y textos largos en las tres resoluciones, además de una exportación Windows. No se ha hecho commit ni push.

## Comprobación manual en Windows

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release --no-build --no-restore
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- windowed
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot --script "$PWD\tools\GodotApplicationSmoke.gd" -- fullscreen
& $env:GODOT4 --windowed --resolution 1280x720 --path .\src\Furvo.Godot
~~~

GODOT4 apunta al ejecutable Godot 4.7.2 .NET instalado. Nueva partida → crear entrenador → guardar y seleccionarlo → explorar. Elegir club abre oferta sin crear partida: confirmar que firma está bloqueada, volver y comprobar que se mantiene el perfil. Crear una carrera en paro directamente y otra rechazando una oferta, con el mismo perfil; confirmar los dos resúmenes y archivos distintos. Cancelar antes de confirmar no debe crear carrera. Probar Esc/Continuar/Opciones/Salir y repetir a 1600×900 y 1920×1080. No borrar archivos personales para probar el estado vacío: usar perfiles nuevos o el smoke aislado.

Nota de estabilidad: la atomicidad se verifica frente a fallos controlados dentro de la publicación. No se ha validado apagado abrupto del equipo; podría dejar un temporal propio no publicado, que no se considera carrera. No se implementa recuperación automática ni eliminación de archivos ajenos.

## Ejecución final e inspección visual parcial

Se ejecutó la compilación final en una instancia normal de Godot 4.7.2 .NET. Se inspeccionó visualmente Crear entrenador en una ventana ampliada: avatar, seis campos, opciones de experiencia y botones Volver/Guardar visibles. No se completó inspección manual de oferta, confirmación y resumen en los tres tamaños exactos. La prueba geométrica y los smokes no se presentan como sustitutos de esa revisión. La aplicación queda abierta para validación del usuario.

Incidencias corregidas durante la validación: el foco del primer campo se difiere a Ready para no pedirlo antes de entrar al árbol; los identificadores del resumen se asocian al nodo desplazable correcto. La última prueba de escena no muestra esos errores.
