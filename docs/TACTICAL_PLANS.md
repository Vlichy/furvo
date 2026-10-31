# Planes tácticos y formaciones personalizadas

## Alcance
Equipo → Táctica mantiene Alineación, Estilo de juego, Transiciones y Balón parado. Se añaden Inicial, Ganando y Perdiendo, cada uno con Con balón y Sin balón. El selector indica contexto de edición, no activa automáticamente un plan durante partidos. Formación permanece por encima de Mentalidad.

Los controles se disponen horizontalmente en pantallas anchas para conservar altura de campo a 720p. Las herramientas de copiar, restablecer y comparar están en el panel lateral desplazable de Alineación. Las ventanas de confirmación preservan el estado si se cancelan.

## Una composición, dos disposiciones
TacticalPlanBook contiene tres TacticalPlan con identificadores estables derivados de la carrera. Cada plan guarda:
- Un PlanRoster: semilla, límite de suplentes y 24 miembros con once titulares, suplentes y reservas.
- Dos TacticalDisposition: referencia, posiciones normalizadas, demarcaciones y bandera personalizada.
- Mentalidad e instrucciones propias (Con balón, Sin balón y Transiciones).

El mapa lógico puesto → PlayerId pertenece al roster del plan. Las fases referencian esos puestos y no almacenan dos listas independientes de futbolistas. Así se garantiza que compartan exactamente el mismo once. View(club, revisión, fase) devuelve la vista preparada para consumidores, incluido el futuro motor.

Cada disposición conserva su propia referencia de formación para permitir, por ejemplo, 4-4-2 sin balón y 3-5-2 con balón. La referencia es una instantánea para restablecer; las coordenadas activas son Positions. Las once plantillas anteriores siguen disponibles. La personalización no cambia automáticamente el nombre base.

## Editor
El arrastre sobre césped libre conserva la identidad y guarda coordenadas relativas 0–1. El origen debe ser titular, el destino estar dentro del campo y separado del resto de centros al menos 0,055 unidades normalizadas. Es una protección contra superposición completa, no una restricción táctica ni una penalización deportiva.

El círculo de destino provisional y la previsualización acompañan al puntero. Soltar fuera o cancelar no escribe. La demarcación se determina automáticamente por la zona al soltar. Coordenadas y demarcación se guardan juntas en una única transacción, sin popup ni editor manual de demarcación. Incluso un cruce pequeño de zona actualiza el rol; no cambia la ficha original. Véase [ALIGNMENT_EDITOR_FIX.md](ALIGNMENT_EDITOR_FIX.md).

Las posiciones naturales y secundarias no cambian. Se mantienen los trece roles de dominio. CAD y CAI son especializaciones tácticas opcionales del lateral derecho/izquierdo, serializadas como TacticalCode, no nuevas posiciones naturales ni atributos. Se validan con su rol base y comparten su clasificación de adecuación. No incorporan fórmulas de rendimiento.

Verde, ámbar y rojo siguen indicando natural, secundaria y fuera de posición. Se conserva el portero y su demarcación; no se permiten intercambios portero/jugador de campo que invaliden el once.

## Movimiento e intercambio
- Césped vacío: solo coordenadas/demarcación de la fase activa.
- Titular sobre titular: se permutan sus ubicaciones/demarcaciones SOLO en esa fase. El mapa compartido de identidades permanece estable y la otra fase no cambia.
- Titular ↔ suplente/reserva: cambia la composición del plan y el nuevo jugador ocupa el ancla correspondiente en ambas fases. Otros planes no se modifican.

El panel de banquillo permanece próximo al campo con desplazamiento propio. No se duplica la lista de titulares. Plantilla general conserva la vista principal Inicial/Sin balón a través del adaptador; seleccionar un plan alternativo para editarlo no cambia otras secciones.

## Copia, referencia y restablecimiento
Copiar plan clona roster, ambas fases, referencias, mentalidad e instrucciones, manteniendo el identificador del destino. No copia balón parado. Se advierte antes de sustituir el destino y se rechazan revisiones obsoletas.

Copiar disposición copia geometría, roles, código táctico, referencia y bandera de personalización. Mantiene el propio once del destino; los puestos lógicos proporcionan una correspondencia estable incluso entre planes con distintos futbolistas.

Restablecer recupera la instantánea de la referencia de esa fase, sin tocar la otra ni el once. Elegir otra plantilla sobre una disposición personalizada requiere confirmación. Mostrar referencia dibuja once marcadores tenues de la fase opuesta del mismo plan, sin líneas añadidas.

Los cambios válidos se guardan inmediatamente. Guardar planes confirma también la estructura completa, incluida su conversión inicial. No hay un borrador silencioso que se pierda al navegar.

## Estilo, transiciones y balón parado
Mentalidad y los dieciséis códigos de instrucción pertenecen al plan seleccionado. Con balón y Sin balón de Estilo muestran la disposición correspondiente; Transiciones usa la fase seleccionada, sin una formación adicional. Las vistas toman las posiciones exactas guardadas y conservan pases a símbolos existentes, movimientos a objetivos marcados y zonas explicativas.

Balón parado y el orden de la tanda siguen siendo comunes, en las tablas tácticas existentes. Se comprueba la disponibilidad de sus PlayerId frente al once editado sin borrar preferencias cuando un futbolista queda fuera. Copiar planes no duplica ni modifica esas configuraciones.

## SQLite y compatibilidad
Nueva extensión TacticalPlans001_Initial.sql, versión 1:
- tactical_plans_schema: versión.
- career_plan_books: club propietario y revisión global.
- career_tactical_plans: id, tipo, semilla, banquillo, mentalidad e instrucciones.
- career_plan_members: única composición por plan, con FK a jugadores, unicidad y pertenencia contractual.
- career_plan_dispositions: dos fases por plan, referencia, posiciones y personalización.

Las lecturas de carreras antiguas proyectan tres copias independientes de la alineación/táctica existente sin escribir. Los IDs son estables entre lecturas. La conversión se confirma en la primera edición o Guardar planes, dentro de una transacción. La táctica anterior pasa a Inicial; Ganando y Perdiendo parten de esa misma configuración, sin mentalidades impuestas.

Si la formación anterior ya tenía coordenadas personalizadas, se conservan en ambas fases y se identifican como personalizadas; si existe plantilla conocida se conserva como referencia para restablecer. Una referencia legada desconocida sigue siendo legible y no se sustituye al abrir.

Tras convertir, career_plan_members es la fuente canónica. Se retiran las filas de composición anteriores de career_lineup_members/career_lineups dentro de la misma transacción, después de validar la propuesta; no quedan dos onces activos. Las columnas de mentalidad/instrucciones del antiguo encabezado común quedan neutras: las elecciones se leen exclusivamente de los planes.

SqliteLineupStore y SqliteTacticsStore conservan sus APIs como adaptadores de Inicial/Sin balón e Inicial respectivamente. SqlitePlanLineupStore adapta el editor al plan/fase elegidos. Balón parado se guarda una sola vez mediante WriteRaw en la transacción común. Una lectura no migra, regenera ni sobrescribe datos. Cualquier fallo revierte extensión, composición y configuración; las revisiones impiden pisar cambios concurrentes.

No se modifican el esquema fundacional del mundo, perfiles, contratos, jugadores, clubes o publicaciones. La extensión nueva no implica compatibilidad de lectura con ejecutables antiguos.

## Inventario de esta entrega
Nuevos:
- src/Furvo.Core/Tactics/TacticalPlans.cs: modelo, reglas, copia y propuestas.
- src/Furvo.Persistence/SqliteTacticalPlanStore.cs: repositorio y adaptador del editor.
- src/Furvo.Persistence/Migrations/TacticalPlans001_Initial.sql: extensión versionada.
- src/Furvo.Godot/Scripts/Main.Plans.cs: controles, acciones y diálogos.
- tests/Furvo.Tests/TacticalPlanTests.cs: persistencia, independencia, migración y validaciones.
- docs/TACTICAL_PLANS.md: este informe.

Modificados:
- Core/Lineups/LineupModels.cs: TacticalCode opcional compatible con instantáneas anteriores.
- Persistence/SqliteLineupStore.cs y SqliteTacticsStore.cs: adaptadores y escritura transaccional compartida.
- Godot/Main.Tactics.cs, Main.Lineups.cs y Main.Navigation.cs: contexto de plan/fase y navegación.
- Godot/Components/InteractiveLineupPitch.cs: movimiento libre, vista previa y referencias.
- Godot/Components/TacticalPreview.cs y TacticDiagram.cs: posiciones exactas del plan seleccionado.
- Godot/Resources/UiStrings.resx: textos españoles.
- tools/GodotUiSmoke.gd: recorrido de seis disposiciones y confirmaciones.
- README.md y documentación de arquitectura, inventario, navegación y validación.

## Comprobación en Windows
Desde la raíz:

```powershell
dotnet build Furvo.sln --no-restore -m:1
dotnet test Furvo.sln --no-build -m:1
dotnet build Furvo.sln --no-restore -m:1 -c Release
dotnet test Furvo.sln --no-build -m:1 -c Release
```

Abrir src/Furvo.Godot/project.godot en Godot .NET y F5. Crear carrera de prueba → Equipo → Táctica.
1. Alternar los tres planes y las dos fases; editar mentalidad e instrucciones y comprobar independencia.
2. Arrastrar un titular al césped libre, comprobar la demarcación automática sin confirmación y observar la marca personalizada.
3. Intercambiar titulares y comprobar que la otra fase conserva sus posiciones. Sustituir desde banquillo y comprobar el mismo once en ambas fases.
4. Activar referencia; copiar disposición y plan, cancelar y confirmar; restablecer una fase.
5. Revisar Estilo, Transiciones y balón parado común.
6. Redimensionar entre 1280×720, 1600×900 y 1920×1080, también reduciendo después de ampliar.
7. Salir/reentrar a Táctica. La reapertura del archivo se valida por los servicios SQLite; no se habilita un menú general nuevo de cargar carreras.

Las pruebas nativas sincronizan el cursor antes del arrastre libre porque el sistema de drag de Godot consulta también la posición del cursor de Windows. Headless no requiere esa sincronización.

## Límites
No hay activación automática por marcador/minuto, motor de partidos, eficacia, penalizaciones, química, IA rival ni ampliación de balón parado. Las referencias de fase son una ayuda visual, no una simulación. La comprobación automática y las capturas no sustituyen una campaña manual de DPI, teclado y todas las combinaciones posibles.

## Registro histórico de validación de la entrega original

Debug y Release: 301 pruebas superadas en cada configuración, sin fallos ni omisiones; compilaciones sin errores ni advertencias. Recorrido nativo completo de Godot superado en tres resoluciones, incluyendo retorno a 1280 × 720 y límites del diálogo de demarcación. Las 21 bases de datos preexistentes conservan su SHA-256. Véase VALIDATION.md para los límites de la comprobación.
