# Mánager integral

La integración actual de Club → Personal → Delegaciones se describe en [Delegaciones del club](CLUB_DELEGATIONS.md).

Cada carrera comienza con el usuario al frente de la gestión integral del club. La delegación se configura por responsabilidad en Club → Personal → Delegaciones. El cargo operativo, la negociación, la aprobación y la firma son facultades distintas. Las decisiones societarias corresponden al órgano competente según la forma jurídica del club.

## Base disponible

TrainingStaff registra identidad, equipo, cargo y competencias específicas de entrenamiento. AcademyStaff registra responsable, equipo, cargo y estado de vinculación. AnalysisStaff identifica al autor de informes. Son fuentes de sus módulos respectivos. Falta un registro común capaz de acreditar disponibilidad, club, vigencia contractual y competencias para todas las responsabilidades. Las delegaciones guardadas en TrainingPlan y la gestión de cantera siguen siendo las fuentes operativas de esos módulos.

Core incorpora una definición extensible por código y departamento, tres niveles de autonomía y una regla de validación que consulta la aptitud en la fuente del módulo. Un empleado no disponible devuelve el control efectivo al mánager. La asignación original queda disponible para que una futura persistencia pueda registrar el cambio.

## Integración prevista

| Departamento | Responsabilidades iniciales | Dependencias |
|---|---|---|
| Primer equipo | Entrenamiento, táctica, preparación, cargas e informes | Separar preparación técnica y funciones médicas. Adaptar las delegaciones existentes por equipo. |
| Cantera | Entrenamiento, seguimiento y promociones | Validar vínculo y equipo con la fuente de cantera. |
| Mercado | Negociación con clubes, venta y planificación | Mantener separados fichajes, cesiones y acuerdos personales. |
| Mercado | Negociación con representantes y renovaciones | Contratos personales, representación y consentimiento. |
| Ojeo | Búsqueda, seguimiento e informes | Personal y competencias de ojeo. |
| Economía y comercial | Tesorería, presupuestos, financiación y patrocinios | Libro mayor, contratos y facultades de aprobación. |
| Instalaciones | Mantenimiento, mejoras y obras | Inversiones, permisos y compromisos. |

La pantalla tendrá una fila por tarea con responsable y autonomía. Yo (mánager) solo ofrece gestión personal. Con empleado apto se podrá elegir apoyo personal, supervisada o completa. Los límites serán específicos de cada tarea. Una operación fuera de límites requerirá aprobación. La futura bandeja guardará responsable, operación y revisión de origen, condiciones, impacto, motivo, fecha y estado. Antes de ejecutar se comprobarán la autorización y la información vigente. Una vacante no transfiere autorizaciones ni elimina operaciones pendientes.

La futura escritura SQLite será por carrera y club, con historial de cambios y referencias a identidades verificadas. Una carrera sin configuración resolverá a gestión personal. La integración de entrenamiento y cantera deberá conservar sus asignaciones históricas y mantener una única fuente de autoridad por tarea. Esta entrega aporta las reglas de Core. La pantalla, las migraciones, los límites persistentes y la bandeja quedan pendientes de esa integración.

## Economía y planificación

La tesorería mide fondos disponibles. El presupuesto expresa autorización para gastar. Margen salarial, compromisos y deuda requieren registros separados. El libro mayor previsto utilizará asientos equilibrados y referencias a operaciones para evitar duplicados. Los ingresos dependerán de contratos, asistencia y resultados, entre otros datos del club. Los patrocinios se liquidarán conforme a sus contratos y los préstamos según su amortización.

Propiedad, órganos, participaciones y competencias requieren un modelo propio. Una futura emisión de acciones, venta de acciones existentes y cotización tienen efectos distintos sobre el club. Las reglas jurídicas y deportivas se verificarán antes de habilitar estas operaciones.

La hoja de ruta general parte de la auditoría de Mercado y continúa con auditoría económica, economía básica, flujos, finanzas, estructura societaria, integración con Mercado, simulación de 30 temporadas, agentes y auditoría final. La infraestructura de responsabilidades puede avanzar en paralelo. El orden de las entregas de Mercado se detalla en MARKET_ROADMAP.md.
