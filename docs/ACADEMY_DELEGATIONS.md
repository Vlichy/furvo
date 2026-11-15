# Responsabilidades de cantera

En una carrera con club, abre **Club → Personal → Cantera** dentro de Delegaciones. **Gestión deportiva** permite elegir un equipo inferior existente y editar las asignaciones de entrenamiento y de alineaciones. Los otros grupos describen las tareas pendientes y su alcance.

| Grupo | Ámbito previsto | Estado |
|---|---|---|
| Gestión deportiva | Equipo | Alineaciones y selección usa el ajuste Directa/Delegada de Cantera. Planificación semanal, colectivo, individual, demarcación, grupos, gimnasio, porteros, balón parado y recuperación usan las asignaciones de Entrenamiento. Preparación de partidos, convocatorias, minutos y competiciones siguen pendientes. |
| Captación e incorporaciones | Club para la red de captación, equipo para necesidades deportivas | Pendiente. Búsqueda, observación e informes tendrán una ruta distinta de contacto, negociación, inscripción y renovación. Cada incorporación pasará por los servicios comunes de Mercado y contratos. |
| Desarrollo y seguimiento | Equipo y futbolista | Parcial. Se pueden consultar las observaciones de desarrollo existentes en Equipo → Cantera. La delegación de planes, evaluaciones y propuestas de promoción requiere competencias adicionales. Los movimientos manuales existentes conservan sus reglas. |
| Coordinación general | Club | Diseñado. Supervisión, metodología y planificación por categorías tendrán una asignación de ámbito club. El grupo se muestra una sola vez y sin selector de equipo. |

## Organigrama y compatibilidad

El inventario por club, con procedencia y pendientes, se conserva en [Investigación de cantera](ACADEMY_RESEARCH.md) y [fuentes por equipo](development/academy-reference-2026-27.json). El catálogo distingue filiales y juveniles por club. La interfaz consulta los equipos de la carrera mediante `SqliteClubSquadStore.Teams`. El personal institucional existente se consulta por equipo. Su vinculación no acredita puestos de captación, coordinación, medicina ni contratación. Las competencias de entrenamiento provienen de `SqliteSportsStore.Staff`.

Cada fila deportiva usa los guardados originales. `SqliteSportsStore.SaveDelegation` actualiza una tarea y equipo en `training_plans`. La gestión de alineaciones y selección usa `SqliteAcademyStore.SetManagement` y `academy_teams`. Las modalidades de entrenamiento conservan Personal, Supervisada y Completa como configuración. La ejecución autónoma de tareas sigue pendiente. Alineaciones y selección mantiene Directa/Delegada por compatibilidad con el control de plantillas. La regla de disponibilidad del técnico conserva el retorno efectivo al mánager. Las delegaciones genéricas anteriores conservan su valor y no se convierten en asignaciones de entrenamiento.

Esta entrega reutiliza las tablas SQLite existentes. Una carrera sin equipos inferiores muestra el aviso correspondiente. Las futuras responsabilidades de equipo usarán identidad de equipo, y las generales usarán identidad de club. La carrera delimitará la configuración en ambos casos.

En la carrera aislada de validación, SQLite devolvió 68 equipos de 20 clubes y 68 técnicos institucionales. `PRAGMA integrity_check` devolvió `ok` y `PRAGMA foreign_key_check` no devolvió infracciones. Es un recuento del catálogo ya cargado en la carrera de prueba, con inventario y fuentes por club en la documentación de investigación.

## Plan C1–C8

1. **C1 · Organigrama y personal.** Completar la evidencia club a club, conservar aproximaciones como tales e incorporar contratación, bajas y competencias.
2. **C2 · Entrenamiento y desarrollo.** Conectar observaciones y objetivos con actividades evaluables. La progresión futura dependerá de edad, práctica, minutos y condiciones de formación. Los informes conservarán la escala 1–20 y la proyección pública.
3. **C3 · Captación.** Registrar observaciones, necesidades, informes y propuestas verificables. Vincular al empleado y su ámbito.
4. **C4 · Contratación y renovación.** Integrar Mercado, contratos, presupuesto e inscripción. Antes de activar incorporaciones, verificar reglas de edad, protección de menores y movimientos nacionales e internacionales por competición y jurisdicción.
5. **C5 · Promoción.** Separar recomendación, decisión y traslado. Conservar identidad, contrato e historial al usar la movilidad existente.
6. **C6 · Competiciones.** Integrar convocatorias, minutos y objetivos cuando existan calendario, reglas y resultados verificables.
7. **C7 · Autonomía.** Añadir autorizaciones supervisadas y límites a la ejecución completa. Mantener la decisión personal como opción efectiva cuando no haya técnico apto.
8. **C8 · Economía y equilibrio.** Integrar salarios, compensaciones, captación, personal e instalaciones tras disponer de tesorería, obligaciones y libro mayor. Verificar sostenibilidad y diferencias entre clubes.

Archivos de la entrega: `Main.ClubDelegations.cs`, `UiStrings.resx`, `tools/GodotUiSmoke.gd` y este documento.
