# Staff de Furvo

## Consolidación vigente

Referencia de trabajo: rama `main`, revisión `b2eeecc`, árbol limpio al comenzar. Los cambios recientes de Hacer oferta están incluidos en esa revisión. La consolidación registra el estado funcional y la auditoría. Las correcciones posteriores y sus pruebas se recogen en un bloque separado.

| Función | Estado | Evidencia y alcance |
|---|---|---|
| Plantilla propia y directorio externo | Implementada y verificada | `SqliteStaffDirectory.Read` y `Main.Staff`. Identidades procedentes de la carrera y exclusión del club propio en Mercado. |
| Filtros, ordenación y ficha | Implementada y verificada | Nombre, cargo cuando es útil, estado, vencimiento y reputación cuando consta. Plantilla filtra por departamento, cargo y equipo. El vencimiento se ordena y se calcula con la fecha de carrera. |
| Perfiles profesionales | Implementada y verificada | `StaffProfession` guarda atributos 1–20, reputación 1–5, idiomas, experiencia y deriva fortalezas y debilidades. Las carreras económicas nuevas generan perfiles y contratos de sus técnicos de cantera. Las anteriores conservan datos ausentes como tales. |
| Contratos de inicio | Implementada y verificada | `SqliteInitialStaffProfession` y extensión StaffProfession001. El mánager conserva su contrato separado. Los contratos de Staff se consultan y validan según sus fechas. |
| Borrador, envío, revisión e historial | Implementada y verificada | `SqliteStaffOffers` conserva versiones por carrera. Staff y Jugadores tienen vistas y registros separados. |
| Negociación y acuerdo condicionado | Implementada parcialmente | El recorrido de un técnico contratado separa club y persona y llega al acuerdo condicionado. Hay decisiones deterministas, contraofertas, retirada y caducidad. La fase personal de un libre se prueba en dominio. El catálogo externo inicial carece de una población de libres acreditados. |
| Delegaciones y Cantera | Implementada parcialmente | Las identidades coinciden con Entrenamiento y Cantera. SQLite conserva el técnico y modalidad. Las tareas de entrenamiento admiten tres modalidades. La gestión de alineaciones inferiores conserva Directa y Delegada. |
| Efecto profesional en entrenamiento | Implementada y verificada | `SqliteTrainingExecutionStore.Process` usa el perfil del responsable apto y guarda su factor con la evidencia. `StaffTrainingEffectTests` compara Desarrollo juvenil 6 y 16. |
| Efecto durante una jornada normal en Godot | No verificable con la evidencia disponible | El reloj sigue deshabilitado. La prueba de impacto usa evidencia controlada y el ejecutor real. |
| Incorporación, liberación del puesto, renovación, despido, formación y progresión | Pendiente | La aceptación contractual se guarda como condicionada. El organigrama actual proyecta los equipos y técnicos existentes. Falta un organigrama laboral por club con puestos, vacantes y estructuras propias. |
| Salarios devengados, primas, compensación, indemnización y cláusulas ejecutadas | Pendiente | Requieren validación de compromisos, obligaciones e integración con Economía. |

El formulario vigente se documenta en [Ofertas y negociación](STAFF_HIRING.md). El origen y compatibilidad de los datos se detallan en [Perfiles profesionales](STAFF_PROFESSIONAL_PROFILES.md). Aptitud y ejecución se detallan en [Entrenamiento](STAFF_TRAINING_INTEGRATION.md), [Delegaciones](CLUB_DELEGATIONS.md) y [Cantera](ACADEMY_DELEGATIONS.md). La dependencia financiera sigue en [Auditoría de Economía](ECONOMY_FINANCE_AUDIT.md).

### Hallazgos de la consolidación y resolución posterior

- **Pendiente tras la consolidación. Fechas de incorporación transcurridas.** Revisar el tratamiento de envío, revisión y evaluación al avanzar la fecha de la carrera.
- **Media. Borradores históricos con compensación diferente del cálculo actual.** `Main.StaffOffers` conserva y bloquea el importe de la revisión previa. Una contraoferta del club puede aportar otro importe. Antes de ampliar el flujo, definir una acción explícita de actualización que conserve el historial y permita revisar casos antiguos sin mutarlos al abrir.
- **Media. Contratación definitiva y delegaciones.** El modelo institucional conserva un técnico por equipo. La incorporación no debe añadirse sin representar vacantes, titularidad del puesto e historial de funciones. Los presupuestos iniciales son previsiones y requieren un registro de obligaciones para autorizar una firma.
- **Pendiente tras la consolidación. Vencimiento contractual.** Unificar la fecha mostrada en Godot con el último día incluido almacenado en SQLite.
- **Pendiente tras la consolidación. Aviso de fecha.** Retirar el aviso obsoleto al corregir la incorporación.

### Evidencia histórica de la consolidación

- Evidencia histórica anterior a las correcciones finales. `dotnet build Furvo.sln --no-restore` en Debug: cero errores y cero advertencias. `dotnet test Furvo.sln --no-build --no-restore` en Debug: 851 ejecutadas, 851 superadas, cero fallidas y cero omitidas. Incluye Staff, traspasos, datos históricos, Entrenamiento y Cantera.
- En Godot, `--staff-hiring --capture` verificó las cuatro duraciones, el recálculo tras cambiar inicio, el bloqueo ante fecha e importe inválidos, la edición y sugerencia en el mismo campo monetario y la compensación de solo lectura en ambas fases. Resoluciones 1280×720, 1600×900 y 1920×1080. Una carrera histórica aislada anterior a StaffProfession superó el recorrido de ficha y conservó el estado desconocido.
- SQLite, en lectura sobre la carrera de Staff usada por Godot: `integrity_check = ok`, cero infracciones de claves foráneas y 68 contratos iniciales. Las siete revisiones guardaron inicio 18/11/2026, último día incluido 17/11/2029 y compensación 78.000 EUR. En aquella comprobación histórica, el formulario mostró 18/11/2029. La última revisión conserva salario 150.000 EUR, prima 5.100,25 EUR y porcentajes 25 de reducción, 20 de incremento y 50 de recuperación.

### Continuidad propuesta

1. Definir y probar el tratamiento de fechas transcurridas, borradores históricos y caducidad antes de habilitar el reloj.
2. Dentro de Staff, modelar puesto, vacante, disponibilidad, historial, baja y retorno de responsabilidades. Mantener la compatibilidad de identidades institucionales.
3. Incorporar renovación y rescisión como propuestas persistentes. Ampliar perfiles y organigramas con reglas acreditadas. Diseñar formación y progresión ligadas a hechos verificables.
4. Una vez disponible Economía, validar compromisos salariales, compensaciones y liquidez, y formalizar altas y bajas en la misma transacción que registra las obligaciones.
5. Verificar expiraciones, cláusulas y pagos con carreras de varias temporadas, reintentos y reapertura.

## Estado histórico de la primera entrega

La primera entrega del Staff unifica la **consulta** de identidades laborales existentes. En **Club → Personal** aparecen Plantilla de empleados, Formación y desarrollo y Delegaciones. La plantilla consulta al mánager y a los técnicos institucionales del club activo, con ficha y contrato si consta. **Mercado → Buscar empleados** sigue inmediatamente a Buscar jugadores y consulta el personal externo de esa carrera. Formación muestra el estado pendiente.

El filtro de nombre normaliza espacios, mayúsculas y acentos con las reglas de Mercado. El cargo, la situación contractual y el vencimiento se combinan. El escenario actual solo registra un contrato laboral verificable, el del mánager. Los vínculos institucionales de cantera se listan con contrato desconocido, fuera de los filtros Libres y Con contrato. Con contrato exige vigencia en la fecha interna. Los vencimientos admiten 3, 6 o 12 meses y se pueden ordenar desde el encabezado. El buscador es de consulta y la contratación sigue pendiente.

## Auditoría inicial histórica

| Fuente | Uso actual | Límites |
|---|---|---|
| `CoachProfile`, `CareerCoach`, `ManagerContract` | Identidad, nacimiento y nacionalidad, contrato inicial con fechas y salario. | Modelo específico de la carrera del usuario. |
| `AcademyStaff`, `academy_staff` y `academy_teams` | Identidad, puesto institucional, equipo y club. | Sin contrato laboral, datos biográficos, atributos, reputación ni licencias acreditadas. El inventario por club sigue en [ACADEMY_RESEARCH.md](ACADEMY_RESEARCH.md). |
| `SqliteSportsStore.Staff`, `TrainingStaff` | Proyección de aptitud por equipo y tarea. | Conservar como fuente de competencias mientras evoluciona Staff. |
| `AnalysisStaff` | Autor de un informe de análisis. | El informe no acredita un contrato ni constituye un registro laboral adicional. |
| `ClubResponsibility`, Cantera y Entrenamiento | Elegibilidad, autonomía y asignaciones por módulo. | Staff deberá aportar identidades y disponibilidades a estas mismas reglas. |
| `TransferNegotiation` | Revisión, historial y reintentos de acuerdos de clubes. | El empleado necesita su propio contrato y flujo. Reutilizar los patrones transaccionales sin tratar personas como futbolistas transferibles. |

No se identificó un directorio laboral operativo de ojeadores, médicos, directivos, técnicos comerciales o de instalaciones. Tampoco una fuente de agentes, historial de formación, atributos profesionales o personalidades. Los campos desconocidos de las fichas se muestran como tales.

## Modelo previsto

- **Identidad profesional** estable, separada de contrato, cargo, responsabilidad, especialidades y asignación por equipo. Cada club mantiene un organigrama propio. Un empleado podrá ocupar varios puestos cuando cualificación, horarios, disponibilidad, conflictos y carga lo permitan. Una salida liberará puestos y devolverá el control efectivo al mánager, conservando historial.
- **Atributos 1–20 y reputación 1–5.** Catálogo por perfiles: enseñanza técnica y táctica, preparación física, porteros, captación, planificación deportiva, cantera, medicina, psicología, administración y finanzas, comercial e infraestructuras. Emplear solo capacidades relevantes a cada puesto. Fortalezas y debilidades derivadas de los datos conocidos, experiencia y exigencias del rol, con resultados reproducibles y sin puntuación global.
- **Personalidad y criterio** separados de competencia. La ambición, estilo, profesionalidad y tolerancia al riesgo orientarán propuestas reales con razones observables. Registrar experiencia, licencias e historial conforme se produzcan.
- **Idiomas** con idioma y nivel explícitos, compatibles con jugadores. La comunicación depende de idiomas compartidos. La nacionalidad no inicializa conocimientos. Cursos avanzados y efectos deportivos requieren un modelo de comunicación y entrenamiento validado.
- **Contratos independientes** con fechas, periodicidad, salario bruto, objetivos, primas, indemnizaciones, renovación y salida. Negociar salario, duración y cláusulas. Para descenso, aplicar el porcentaje firmado una vez por evento confirmado, según la fecha de efecto. En ascenso, recuperar salario, incrementarlo o devengar prima solo cuando esté pactado. Mantener revisiones históricas y vincular obligaciones al identificador del evento.
- **Mercado laboral** con Libres y Con contrato como estados verificados. El vencimiento es un filtro independiente y no habilita por sí solo contacto anticipado. Añadir filtros por especialidad, reputación, atributo mínimo, edad, salario, nacionalidad, idiomas, experiencia, licencias y club cuando existan datos. La negociación deberá admitir ofertas, contraofertas, retirada, compensación e incorporación transaccional.
- **Formación** con curso, requisito, coste, especialidad, fechas y estado. Registrar asistencia, rendimiento y terminación antes de evolucionar atributos. Respetar el límite 20 y justificar el efecto con nivel de partida, dedicación, aprendizaje y dificultad. La experiencia laboral exige actividad real.
- **Economía** gestiona tesorería y obligaciones por salario, primas, indemnizaciones, compensaciones y formación. Staff aporta contratos y eventos identificables. La contabilidad seguirá en el módulo económico.

## Etapas de la primera entrega

| Etapa | Estado y siguiente comprobación |
|---|---|
| S1 | Auditoría y directorio de lectura disponibles. |
| S2 | Fichas básicas disponibles. Falta catálogo de atributos y derivación de fortalezas y debilidades. |
| S3 | Directorio por club y equipo disponible. Faltan organigramas editables, perfiles transversales y roles múltiples. |
| S4 | Consulta del contrato de mánager disponible. Contratos del resto del personal y cláusulas pendientes. |
| S5 | Navegación y filtros básicos disponibles. Falta ampliar perfiles y filtros. |
| S6 | Pendiente de contratos de empleados, permisos, compensación y circuito económico. |
| S7 | Pendiente de catálogo de cursos, calendario y datos de formación. |
| S8 | Pendiente de rasgos, criterios profesionales y decisiones fundadas. |
| S9 | Consulta de las mismas identidades que usan Entrenamiento y Cantera. Las asignaciones siguen en esos módulos. Falta integrar altas, salidas y competencias nuevas. |
| S10 | SQLite ya aporta las identidades y contratos conocidos. Esta edición agrega una consulta, sin nueva migración. El futuro modelo deberá ampliar los registros por ID sin regenerar nombres ni sobrescribir acuerdos. |
| S11 | Pruebas de filtros, lectura SQLite y navegación de las funciones incorporadas. |

Archivos nuevos: `StaffProfile.cs`, `SqliteStaffDirectory.cs`, `Main.Staff.cs`, `StaffDirectoryTests.cs` y este documento. Archivos modificados: `CareerWorkspace.cs`, `Main.Navigation.cs`, `Main.ClubDelegations.cs`, `UiStrings.resx` y `tools/GodotUiSmoke.gd`.
