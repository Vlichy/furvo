# Club → Personal → Delegaciones

Abre una carrera con club, entra en Club y pulsa Personal. Selecciona el equipo, el responsable y el tipo de gestión. Guarda con el botón de esa fila. Los errores se muestran junto a la tabla y una confirmación aparece después de guardar.

| Responsabilidad | Configuración | Fuente SQLite |
|---|---|---|
| Entrenamiento por equipo | Plan semanal, colectivo, individual, por demarcación, grupos, gimnasio, porteros, balón parado y recuperación. Personal, Supervisada o Completa en las tareas con personal apto. | `training_plans`. Supervisada reutiliza Mixta y Completa reutiliza Delegada. Personal con apoyo se guarda en la misma asignación. |
| Gestión de cada equipo inferior | El mánager dirige o delega en el técnico institucional asignado. | `academy_teams`. Gestión directa o delegada, independiente de la táctica. |

La pantalla usa el personal que suministra la carrera. El primer equipo suele disponer únicamente del propio mánager. Los equipos inferiores pueden ofrecer su entrenador institucional. El modelo actual acredita la vinculación al equipo. La contratación general, su vencimiento y el historial de movimientos de personal siguen pendientes.

Cada escritura comprueba la revisión y conserva el resto de la planificación. La disponibilidad se vuelve a consultar al asignar. Si la vinculación deja de ser válida, el control efectivo se muestra como personal. Cantera también usa ese control efectivo para permitir o denegar editar alineaciones. La asignación persistida se conserva hasta una edición explícita.

Se reutiliza el esquema existente. Una carrera sin planificación resuelve sus responsabilidades de entrenamiento como personales. Una carrera sin cantera inicializada muestra los equipos disponibles sin crear personal. La primera escritura en entrenamiento utiliza la migración deportiva existente cuando corresponde.

Mercado, ojeo, economía, comercial e instalaciones quedan pendientes de un catálogo de competencias y servicios propios. La autonomía de entrenamiento es configuración, con ejecución autónoma pendiente. Los límites, aprobaciones, historial y gestión supervisada de cada equipo inferior quedan pendientes.

## Inventario de cambios

Archivos nuevos: `Main.ClubDelegations.cs`, `ClubDelegationPersistenceTests.cs` y este documento.

Archivos modificados:

- Core: `CareerWorkspace.cs`, `Training.cs` y `AcademyModels.cs`.
- SQLite: `SqliteSportsStore.cs` y `SqliteAcademyStore.cs`.
- Godot: `Main.Navigation.cs`, `Main.TrainingDelegation.cs`, `Main.Academy.cs` y `UiStrings.resx`.
- Documentación y validación: `INTEGRAL_MANAGER.md` y `GodotUiSmoke.gd`.

Para repetir la prueba gráfica en una carrera de pruebas, ejecutar Godot .NET con `--path src/Furvo.Godot --script res://../../tools/GodotUiSmoke.gd -- --club-delegations-only --capture`. El recorrido comprueba el acceso desde Club, el control personal en el primer equipo, la selección de un entrenador de cantera, el guardado y la recuperación de la asignación al renderizar la pantalla de nuevo. Comprueba las coordenadas de los selectores y del botón en 1280×720, 1600×900 y 1920×1080.
