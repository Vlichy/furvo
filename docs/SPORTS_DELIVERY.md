# Desarrollo deportivo integrado — entrega y límites

Fecha: 31/10/2026. Proyecto existente, .NET 10 / Godot 4.7.2 .NET / SQLite. Sin paquetes nuevos, sin reconstrucción de arquitectura, sin publicación nueva del mundo.

## Estado inicial

La [auditoría previa](SPORTS_AUDIT.md) detalla los sistemas encontrados. Existían Táctica y Cantera, con 366 pruebas. No existían entrenamiento, movilidad, calendario/avance temporal, analistas, motor médico ni adquisición de tecnología. Había cambios sin commit de entregas previas; no se han eliminado ni atribuido íntegramente a este encargo.

## Estado funcional por bloque

| Bloque | Implementado y utilizable | Dependencia o limitación real |
|---|---|---|
| Táctica | Rol/instrucciones a la izquierda, campo central, candidatos/acciones a la derecha; ancho estable, seis disposiciones y arrastre. | A 720p hay desplazamiento en los laterales. |
| Capitanes | Dos puestos independientes, nulos admitidos, sin duplicados; primer disponible del once. Se guardan por equipo/carrera y no se sincronizan con Cantera. | Sin ambos disponibles se devuelve ausencia; el capitán provisional requiere el futuro motor. |
| Movimientos | Servicio transaccional entre cualquier par de equipos propios, validación y reconstrucción de planes; entrada desde ficha. | La política de producción bloquea cuando faltan normas/licencias/inscripción. Los casos autorizados se ejecutan y prueban con políticas sintéticas. No se afirma que todos los movimientos comerciales estén habilitados. |
| Participación temporal | Modelo distinto del cambio permanente, explicación específica. | Sin encuentro/calendario e inscripción no se autoriza una convocatoria temporal. |
| Propuestas | Registro por responsable existente, jugador/origen/destino/motivo/fecha, edad y evidencia visible, estado pendiente, aceptación/rechazo exclusivos del principal y revalidación al aceptar. | Registro supervisado; no existe IA autónoma que observe partidos o genere recomendaciones. Preparación y rendimiento quedan sin evaluar si no hay datos. Invalidación/caducidad automática avanzada pendiente. |
| Entrenamiento | Planificación semanal por todos los equipos, horarios, crear/editar/eliminar sesiones, carga, individuo/posición/grupo/equipo, excepciones, objetivo, responsable y frecuencia prevista. | La frecuencia no replica sesiones automáticamente; no hay calendario de partidos conectado. Una semana vacía no equivale a descanso confirmado. |
| Modalidades | Campo, diez ejercicios de gimnasio, diez de porteros, jugadas diseñadas en Táctica, recuperación y ejercicios específicos Footbonaut. | No son animaciones ni sesiones disputadas al pulsar guardar. |
| Carga | Modelo compartido de planificación, sumas por jugador/día, solapamientos y políticas de juveniles; reservas secuenciales. Ejecutor con evidencia y observación física explícitas. | Modelo experimental, sin motor de lesiones ni autorizaciones médicas. No convierte un índice de riesgo en diagnóstico o lesión real. |
| Delegación | Control manual y configuración por nueve áreas; modo manual/mixto/delegado derivado. En Cantera se puede asignar su responsable real a áreas compatibles. | No se crean empleados. Sin preparador, médico, entrenador de porteros o analista disponible, su delegación no se habilita. No existe generación automática de microciclos. |
| Desarrollo | Estímulos fraccionarios, fatiga/preparación, estancamiento y declive; persistencia, consulta de atributos efectivos y banco determinista de 30 años sintéticos. | No hay proveedor de reloj/asistencia en producción. No se ejecuta desde la interfaz ni al guardar. No está calibrado como simulación comercial definitiva. |
| Análisis | Equipo/Jugadores/Rival/Informes; carga prevista separada de observaciones; informes técnico/resumen/individual de datos existentes, prioridades y solicitudes. | Sin partidos no hay posesión, xG, mapas, rendimiento o conocimiento perfecto del rival. Sin analista las solicitudes quedan pendientes. |
| Tecnología | Club → Instalaciones, Footbonaut, disponibilidad desconocida/ausente/instalada/pedida, solicitudes de presupuesto y reservas compartidas. | Ningún club recibe una máquina sin evidencia. No hay precios ni autorización de directiva aprobados en producción. |
| Adquisiciones | Puerto de autorización, validación de presupuesto/espacio/plazo/moneda, tesorería y pago/pedido atómicos. | Disponible para integrar un proveedor real; la interfaz solo registra solicitudes sin gasto. Pedido no equivale a puesta en servicio. |

Esta entrega no habilita el botón Continuar ni inventa partidos, asistentes, licencias o altas médicas para simular una integración completa.

## Capitanes y compatibilidad anterior

La fuente sigue siendo `TeamPersonnel`, compartida por todos los planes del equipo. `CaptainSlots` contiene exactamente dos puestos, que pueden estar pendientes. En datos anteriores se interpretan las dos primeras prioridades como primer/segundo capitán y se conservan las prioridades antiguas en su campo legado; no se destruye el JSON histórico. Las nuevas selecciones no compactan un primer puesto vacío ni convierten al segundo en otro futbolista.

## Movilidad e integridad

`SqliteClubSquadStore` es la consulta común de pertenencia deportiva. El club propietario no cambia al cambiar de equipo. `sport_assignments` tiene una única fila por futbolista; los contratos no se terminan ni se sustituyen.

Un jugador original que baja permanece almacenado una sola vez en las entidades generales y se consulta por su asignación a Cantera. Un jugador complementario que asciende al primer equipo se traslada de su fila complementaria a la entidad general dentro de la misma transacción, conservando identificador y los 61 atributos, sin crear un contrato ficticio. Los cambios posteriores usan su identidad existente. Las publicaciones iniciales no intervienen.

Los tres planes del primer equipo y de los equipos afectados se reconcilian con sus propios jugadores. La geometría/roles permanecen; se conservan asignaciones posibles y se mantienen once cuando hay disponibilidad. Un movimiento que dejaría al origen con menos de once se rechaza. Se retiran de preferencias personales del origen las referencias a quien salió. La configuración de Cantera y sus entrenadores no se sustituyen.

La transacción contiene asignación, traslado de almacenamiento cuando procede, planes, limpieza de referencias e historial y decisión. Un fallo revierte el conjunto. Las planificaciones afectadas se conservan marcadas para revisión, sin generar asistencia ni progreso. El estado de desarrollo se vincula al identificador del jugador, no se pierde al cambiar de equipo. Moral e historial inexistentes no se fabrican.

La normativa tiene cinco ámbitos y un puerto de verificación de licencias/periodos. La política conservadora de producción no sustituye esa evidencia. [Fuentes y pendientes reglamentarios](SPORTS_RESEARCH.md).

## Planificación y personal

Cada carrera guarda un plan por equipo con revisión optimista, sesiones, participantes confirmados, grupos, exclusiones y delegaciones. Los grupos se pueden crear y modificar con responsable y objetivo. Las sesiones se pueden editar manteniendo su identidad; si cambian los participantes se revalida la planificación. No se duplica la carga por pertenecer a varios grupos.

Se comprueban fecha, hora, duración, ejercicio, objetivo, frecuencia, pertenencia, porteros, responsable, coincidencia de sesión/ejercicio de balón parado, disponibilidad de instalación y solapamientos de personas/responsables/recursos entre equipos. Las sesiones realizadas tienen instantáneas de evidencia inmutables y no pueden eliminarse ni reescribirse; las sesiones históricas se conservan cuando se planifican fechas posteriores.

El gimnasio utiliza el mismo calendario y carga que el campo. Los porteros pueden participar también en sesiones colectivas. Jugadas ensayadas referencia la configuración de Táctica y no crea otro diseñador. La recuperación no borra automáticamente la fatiga ni autoriza rehabilitación clínica.

Los responsables conocidos son el principal de carrera y los entrenadores institucionales ya existentes en Cantera. Las responsabilidades asignables describen funciones, no una capacidad o valoración inventada. Las sesiones sin responsable especializado siguen siendo planificación; no se afirma que una plantilla de personal incompleta pueda ejecutarlas profesionalmente.

## Carga y desarrollo experimental v0

Los siguientes números son parámetros de juego experimentales, no datos de clubes reales ni prescripciones médicas. La versión está identificada en `TrainingCatalog.Version` y `DevelopmentEngine.Version`.

| Parámetro | Valor actual |
|---|---|
| Intensidades descanso/muy baja/baja/media/alta/muy alta | 0 / 0,25 / 0,5 / 1 / 1,4 / 1,8 unidades por minuto antes del factor de ejercicio |
| Factor por ejercicio | Declarado individualmente en el catálogo; no todas las sesiones de gimnasio son equivalentes. |
| Límite de planificación diario | 180 unidades en adultos; 90 en menores o equipos juveniles. No son límites federativos universales. |
| Carga máxima por sesión juvenil | Media; política editorial provisional adaptada a categoría y edad. |
| Capacidad Footbonaut | Un participante simultáneo; los grupos rotan y reparten duración/carga. Mínimo cinco minutos planificados por participante. |
| Carga competitiva futura | Minutos observados × 1,2, solo cuando un proveedor real aporte esos minutos. |
| Cambio de fatiga por día procesado | Carga × 0,18 menos recuperación de 8; recuperación explícita, 12. Acotado 0–100. |
| Estímulo por atributo/sesión | Duración/3000 × dosis relativa del ejercicio (máximo 1,2), con máximo 0,03, modulado por fatiga, exceso de carga, edad, aprendizaje existente y atributo actual. |
| Cambio entero | Al alcanzar un punto acumulado; valores finales entre 1 y 20, sin varios puntos por una sesión ordinaria. |
| Declive físico | Desde 32 años: pequeño débito por día realmente procesado, no por abrir una pantalla. |

El exceso de carga reduce el estímulo útil. Los atributos cerca del máximo progresan más despacio. No se crea un potencial global. El aprendizaje existente se utiliza solo internamente; las consultas de Godot no incluyen los diez atributos ocultos ni el acumulador exacto de desarrollo. Se expone únicamente la observación física cuando existe.

La ejecución requiere `ITrainingExecutionClock`, fecha de carrera, fin de sesión, asistencia verificada y observación física; registra evidencia y evita procesar una sesión dos veces. En producción todavía no existe ese proveedor de calendario/asistencia. El banco automatizado procesa 30 × 365 días sintéticos para comprobar límites y reproducibilidad; **no equivale a validar treinta temporadas completas del juego ni una calibración económica/deportiva**. Calidad del personal, experiencia competitiva real, lesiones y equilibrio comercial requieren sistemas y validaciones posteriores.

## Análisis e instalaciones

Los informes actuales son instantáneas de plantillas, sesiones programadas o los 51 atributos visibles. Sus fuentes/fechas se conservan y las prioridades no cambian la táctica. El rival queda pendiente de calendario/datos y responsable; no se genera xG ni se extrapola rendimiento de los atributos.

Footbonaut se planifica con los mismos tipos de alcance, intensidad, responsables y calendario. Las reservas de todos los equipos comparten la instalación y se rechazan solapamientos. La ausencia de evidencia inicial se muestra como desconocida, no como ausencia. [Inventario de los veinte clubes y fuentes](SPORTS_RESEARCH.md).

Una solicitud de compra no altera la tesorería. El puerto de adquisición exige precios completos, moneda del club, mantenimiento/operación, espacio, plazo y aprobación presupuestaria. Solo entonces compromete dinero y pedido en la misma transacción. La maquinaria pedida no puede utilizarse antes de una puesta en servicio verificada. No hay proveedor de aprobación ni precios iniciales aprobados conectados a la interfaz.

## Persistencia y migraciones

- `Sports001_Initial.sql`: extensión deportiva v1 por carrera: asignaciones, planificación/grupos/delegación, desarrollo, evidencia de asistencia, propuestas/historial, informes/solicitudes e instalaciones/adquisiciones.
- `SportsTactics002_Assignment.sql`: adaptación del trigger de pertenencia de los planes a la asignación deportiva explícita, conservando el contrato como relación distinta.
- Capitanes: ampliación compatible del JSON existente, sin otra tabla duplicada.

Consultar una carrera antigua no crea estas tablas. Las migraciones se aplican dentro de acciones explícitas de escritura. No se cambia `user_version` del mundo ni se genera una publicación fundacional nueva. No hay migración automática de carreras personales al abrir el juego.

## Archivos creados

- `src/Furvo.Core/Development/Training.cs`: planificación, candidatos públicos, grupos, responsabilidades, ejercicios, carga prevista y validación.
- `src/Furvo.Core/Development/DevelopmentEngine.cs`: observaciones, estado y ejecutor experimental determinista.
- `src/Furvo.Core/Development/AcademyMobility.cs`: normativa configurable, movimientos y propuestas.
- `src/Furvo.Core/Development/SportsAnalysis.cs`: informes, hallazgos, prioridades y solicitudes.
- `src/Furvo.Core/Development/SportsFacilities.cs`: instalación, adquisición y puerto de autorización.
- `src/Furvo.Persistence/SqliteClubSquadStore.cs`: plantillas y proyecciones públicas por equipo.
- `src/Furvo.Persistence/SqliteSportsStore.cs`: planificación, personal existente, análisis, instalaciones y presupuestos.
- `src/Furvo.Persistence/SqliteAcademyMobilityStore.cs`: movimientos y decisiones transaccionales.
- `src/Furvo.Persistence/SqliteTrainingExecutionStore.cs`: ejecución con evidencia y persistencia de desarrollo.
- Las dos migraciones indicadas.
- `src/Furvo.Godot/Scripts/Main.Training.cs`: planificación, modalidades, grupos y delegaciones.
- `src/Furvo.Godot/Scripts/Main.SportsAnalysis.cs`: Análisis y Tecnología deportiva.
- `src/Furvo.Godot/Scripts/Main.PlayerMovement.cs`: validación, movimientos y propuestas desde la ficha.
- `tests/Furvo.Tests/SportsDevelopmentTests.cs`: cobertura de los nuevos módulos y las dependencias bloqueadas.
- `docs/SPORTS_AUDIT.md`, `docs/SPORTS_RESEARCH.md`, este informe, `docs/SPORTS_VALIDATION_MATRIX.md` y `docs/development/sports-technology-research-2026-27.json`.

## Archivos modificados

- Core: `Careers/CareerWorkspace.cs`, `Tactics/TacticalPreparation.cs`.
- Persistence: `SqliteAcademyStore.cs`, `SqliteLineupStore.cs`, `SqliteTacticalPlanStore.cs`.
- Godot: `Main.cs`, `Main.Navigation.cs`, `Main.Squad.cs`, `Main.Academy.cs`, `Main.Lineups.cs`, `Main.IndividualTactics.cs`, `Resources/UiStrings.resx`.
- `tests/Furvo.Tests/IndividualTacticsTests.cs`: comprueba los dos capitanes efectivos conservando el dato legado, conforme a la decisión nueva. Ninguna prueba se elimina o desactiva.
- `tools/GodotUiSmoke.gd`: comprobaciones de distribución, dos capitanes, planificación, grupos, modalidades, delegación, informes, presupuesto pendiente y reapertura. El arrastre nativo mide coordenadas tras estabilizar la selección; se mantiene su comprobación estricta.
- README, arquitectura, navegación, inventario y validación enlazan este estado.

## Problemas encontrados y corregidos

1. `SELECT current_date` en una consulta anterior devolvía el reloj SQL en vez de la fecha guardada. Se sustituyó por `s.current_date`, con prueba de regresión.
2. El lateral individual podía variar de ancho por textos largos. Se estabilizó sin retirar arrastres ni suavizar sus comprobaciones de coordenadas.
3. Las tarjetas nuevas mostraban identificadores internos y encabezados repetidos; se eliminaron durante la inspección de capturas.
4. Las consultas nuevas de interfaz se separaron de entidades internas: candidatos de entrenamiento, ubicaciones públicas y observación física; sin atributos ocultos ni acumuladores exactos.
5. Se protegieron las sesiones realizadas frente a cambios retroactivos y se mantuvo su historial al planificar fechas posteriores.

## Validación y comprobación local

La [matriz de los 50 requisitos](SPORTS_VALIDATION_MATRIX.md) separa las comprobaciones de los puntos preparados o bloqueados. Los resultados finales se registran en [VALIDATION.md](VALIDATION.md), archivos TRX y log nativo. No se infiere que una interfaz funciona por una compilación correcta.

```powershell
Set-Location 'C:\Users\panio\Documents\Getion Furvo'
dotnet build Furvo.sln -c Debug
dotnet test Furvo.sln -c Debug --no-build
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release --no-build
```

Abrir `src/Furvo.Godot/project.godot` en Godot 4.7.2 .NET y ejecutar F5. En una carrera de prueba: comprobar lateral izquierdo y dos capitanes; Entrenamiento en varios equipos, grupos y solapamientos; Análisis sin datos ficticios; ficha de cantera → Mover, con explicación del bloqueo reglamentario; Instalaciones → solicitud de presupuesto sin compra. Cerrar/reabrir y comprobar persistencia. No usar carreras personales para ensayos destructivos.

Pendientes de aceptación humana: sesiones prolongadas, teclado y escalados de Windows. Dependencias funcionales pendientes: reloj/calendario competitivo, normativa y licencias completas, capacidades reales del cuerpo técnico, motor médico, calibración comercial del desarrollo y autorización/precios de instalaciones. No se presentan como sistemas ya operativos.


## Resultados finales ejecutados

Compilaciones Debug y Release: cuatro proyectos, sin errores ni advertencias. **403/403 pruebas en cada configuración**, cero fallos y cero omitidas. Godot 4.7.2 .NET/OpenGL: recorrido nativo completo, salida 0, con `FURVO_SPORTS_OK` y las regresiones anteriores. Se revisaron capturas reales en las tres resoluciones solicitadas. Las cinco publicaciones iniciales permanecen idénticas por SHA-256 respecto del registro previo. `git diff --check` no informa errores. Los archivos TRX y el log nativo están bajo `artifacts`.

El juego se ejecuta al finalizar esta tarea. Las dependencias señaladas en la tabla de estado siguen pendientes: este resultado no declara operativos calendario, entrenamiento temporal en producción, permisos federativos sin verificar, IA autónoma ni compras sin autorización.
