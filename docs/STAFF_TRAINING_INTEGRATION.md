# Staff, ficha y entrenamiento

La ficha conserva dos columnas. Los atributos de la especialidad y sus puntos fuertes y débiles usan la misma barra y paleta que el análisis de jugadores. Los números permanecen visibles. Personal muestra el equipo, departamento y las responsabilidades asignadas con su modalidad. La trayectoria distingue experiencia acumulada de clubes o períodos acreditados.

Para los destacados se exige una diferencia de al menos dos puntos respecto a la expectativa por especialidad y un valor absoluto de al menos 13 o de como máximo 8. En cantera se espera 12 en desarrollo juvenil, técnica y motivación, 8 en preparación física y adaptabilidad táctica y 10 en el resto. Se ordenan las diferencias y se muestran hasta tres por grupo. La consulta deriva los resultados del perfil guardado.

## Efecto que ya usa el ejecutor

`SqliteTrainingExecutionStore.Process` acepta únicamente asistencia verificada en la fecha de carrera. Usa las capacidades del responsable elegible de la tarea que determina `TrainingRules.Responsibility`. En Completa puede utilizar el encargado guardado si el bloque carece de responsable explícito. Supervisada y Personal con apoyo requieren que el bloque indique al encargado. Si el bloque contradice la asignación, el técnico carece de perfil, su contrato conocido está fuera de vigencia o no tiene la competencia de la tarea, el estímulo queda en su valor base.

| Ejercicio | Capacidad observada |
|---|---|
| Técnica y pase en juveniles | Desarrollo juvenil |
| Técnica y pase en otros equipos | Entrenamiento técnico |
| Remate y defensa | Entrenamiento ofensivo y defensivo, respectivamente |
| Trabajo colectivo y balón parado | Conocimientos tácticos |
| Gimnasio | Preparación física |
| Porteros | Técnica de porteros, solo cuando exista la especialidad |

Recuperación, tecnología y métodos sin capacidad aplicable conservan sus cálculos. Se usa una sola capacidad por bloque. El factor experimental es `clamp(1 + (atributo - 11) / 100, 0,95, 1,05)`. Modifica el estímulo previo al tope actual de 0,03. Permanecen los límites y factores del motor por carga, edad, fatiga, aprendizaje y valor actual del futbolista. Un bloque sin estímulo se mantiene en cero. La fatiga, el riesgo y la preparación física observada conservan su cálculo. No hay un efecto adicional de instalaciones fuera de las reglas existentes para sus actividades.

La evidencia de SQLite conserva la sesión, responsable, atributo, valor, factor y versión del modelo. Se utiliza la misma transacción y clave de sesión que protege la ejecución contra duplicados. No requiere migración. El reloj de carrera sigue bloqueado en Godot. Este efecto puede comprobarse con asistencia verificable mediante el ejecutor real y no se activa al guardar una planificación.

## Salario y trayectoria

El caso observado es Amaro Cendal Soutovello, del filial de Umbral. SQLite confirma categoría sénior, reputación 2, diez años de experiencia y desarrollo juvenil 16. La base aprobada del mánager es 420.000 EUR. La regla usa 13 % por categoría, 1,5 puntos por reputación sobre el mínimo, 1 punto por experiencia y 1,2 puntos por desarrollo juvenil. El 16,7 % da 70.140 EUR antes de comprobar los límites. El contrato conserva 01/07/2026–30/06/2027. El vencimiento se consulta con fecha de carrera. El perfil inicial y las condiciones persistidas conservan su versión anterior.

Renovaciones, cambios de puesto o club, eventos de ascenso y descenso, pagos, formación y evolución profesional quedan pendientes de los servicios que produzcan sus hechos. La generación, especialidades y estructura contractual están documentadas en [Perfiles profesionales](STAFF_PROFESSIONAL_PROFILES.md). Tampoco hay datos de roles secundarios ni de carga laboral acumulada para mostrar en la ficha.

Archivos: `TrainingStaffEffect.cs`, `DevelopmentEngine.cs`, `StaffProfession.cs`, `SqliteStaffDirectory.cs`, `SqliteTrainingExecutionStore.cs`, `Main.Staff.cs`, `AnalysisAttributeRow.cs`, `UiStrings.resx`, `StaffTrainingEffectTests.cs` y este documento.
