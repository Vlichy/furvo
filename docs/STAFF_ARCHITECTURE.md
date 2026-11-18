# Staff de Furvo

## Estado de esta entrega

La primera entrega del Staff unifica la **consulta** de identidades laborales existentes. En **Club → Personal** aparecen Plantilla de empleados, Formación y desarrollo y Delegaciones. La plantilla consulta al mánager y a los técnicos institucionales del club activo, con ficha y contrato si consta. **Mercado → Buscar empleados** sigue inmediatamente a Buscar jugadores y consulta el personal externo de esa carrera. Formación muestra el estado pendiente.

El filtro de nombre normaliza espacios, mayúsculas y acentos con las reglas de Mercado. El cargo, la situación contractual y el vencimiento se combinan. El escenario actual solo registra un contrato laboral verificable, el del mánager. Los vínculos institucionales de cantera se listan con contrato desconocido, fuera de los filtros Libres y Con contrato. Con contrato exige vigencia en la fecha interna. Los vencimientos admiten 3, 6 o 12 meses y se pueden ordenar desde el encabezado. El buscador es de consulta y la contratación sigue pendiente.

## Auditoría inicial

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

## Etapas y compatibilidad

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
