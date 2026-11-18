# Perfiles profesionales del Staff

## Datos de origen

Antes de esta entrega, `academy_staff` guardaba ID, nombre, equipo, cargo y vínculo institucional. `StaffProfile` lo proyectaba para Personal y Mercado. Los técnicos carecían de fecha de nacimiento, capacidades, idiomas y condiciones laborales. El mánager del usuario ya tenía una identidad y un contrato propios. Las competencias de Delegaciones proceden de `SqliteSportsStore.Staff` y las reglas de Cantera.

## Generación y persistencia

La creación transaccional de una carrera añade la extensión `StaffProfession001_Initial.sql` después de inicializar Cantera. El perfil y el contrato usan la identidad existente del técnico. Cada fila tiene una referencia al registro institucional. La versión `staff-profession-v1` deriva su flujo determinista del identificador estable y la fecha de inicio de la carrera. Las carreras existentes se consultan sin migrarlas ni atribuirles contratos retrospectivos. Los mundos iniciales y el catálogo de equipos no se editan.

Se crean perfiles para los puestos que ya genera Cantera, con el mismo número de técnicos y equipos por club. Los perfiles incluyen fecha de nacimiento, nacionalidad, especialidad, experiencia, reputación y niveles de idioma. La edad se calcula a partir de la fecha de juego. La cohorte ficticia española se genera con español nativo y algunos técnicos conocen inglés a un nivel variable. El historial admite etapas, fechas, logros y formación. La semilla inicial conserva vacío el historial de etapas anteriores.

La especialidad de entrenador de cantera tiene diez atributos. Los conocimientos tácticos, entrenamiento técnico, ofensivo y defensivo, desarrollo juvenil, preparación física, motivación, disciplina, gestión de grupo y adaptabilidad táctica se calculan con una base ligada a experiencia y categoría, variación por capacidad y un sesgo hacia desarrollo juvenil. La política semilla limita valores a 3–18 dentro de la escala oficial 1–20. La reputación usa experiencia y nivel del puesto y se limita a 1–4 dentro de 1–5. Fortalezas y debilidades se derivan de los atributos pertinentes, hasta tres por grupo. Los límites son 13 o superior y 8 o inferior, con desempate por código.

El catálogo también define especialidades para porteros, preparación física, ojeo, dirección deportiva, dirección de cantera y medicina. Son definiciones de dominio para futuras altas. El catálogo inicial todavía carece de esos puestos y la generación se limita a los técnicos institucionales existentes.

## Contratos y economía

La regla inicial crea un contrato desde el inicio de la carrera hasta el fin de temporada si existe un plan económico compatible. La remuneración se obtiene de una proporción del salario de referencia del mánager, diferenciada por categoría, experiencia, reputación y desarrollo juvenil. Se utiliza la referencia salarial congelada de la temporada del proyecto y el límite de remuneración por técnico se calcula a partir del gasto previsto de personal, descontando el coste planificado del mánager y aplicando el factor de coste patronal del plan. El grupo de técnicos queda dentro de esa envolvente de diseño. Cuando la publicación histórica carece de esos datos o no admite el mínimo según esa regla, se conserva el estado desconocido. La regla es de generación del juego y no contabilidad.

`StaffEmployment` guarda identidad, empleado, club, fechas, salario anual, moneda y cláusulas estructuradas. La duración se deriva. El modelo valida reducciones por descenso, aumentos o restablecimiento por ascenso, primas por objetivo, indemnización y rescisión. Los contratos iniciales se generan sin cláusulas adicionales y carecen de ejecución de pagos. El contrato del mánager conserva su fuente existente. Su situación en paro puede acreditarse sin contrato.

Mercado filtra por reputación guardada, cargo, situación y vencimiento. Plantilla y Delegaciones conservan las mismas identidades de SQLite y las competencias funcionales por tarea. Los atributos de la ficha no amplían por sí mismos las autorizaciones.

## Alcance de la ficha y pendientes

La ficha reúne identidad y contrato en paralelo y agrupa atributos, puntos fuertes y débiles e idiomas. Una ficha anterior sin ampliación usa un único aviso para los datos ausentes. El alcance todavía excluye contratación o negociación laboral, pago y ejecución de cláusulas, entrenamiento lingüístico, progresión, nuevas categorías de empleados y generación de candidatos libres externos. Las fichas de ojeador y director deportivo quedan pendientes de identidades persistidas de esos puestos.

Archivos: `StaffProfession.cs`, `StaffProfile.cs`, `StaffProfession001_Initial.sql`, `SqliteInitialStaffProfession.cs`, `SqliteCareerStore.cs`, `SqliteStaffDirectory.cs`, `Main.Staff.cs`, `UiStrings.resx`, `StaffDirectoryTests.cs`, `GodotUiSmoke.gd` y este documento.

Las fixtures históricas de `AcademyTests.cs` y `MarketTests.cs` retiran primero las tablas que referencian al personal para recrear bases anteriores al módulo. Sus comprobaciones de carga y lectura se mantienen.

En la carrera de prueba, SQLite devolvió 68 perfiles distintos y 68 contratos. El intervalo anual observado fue 29.400–787.500 EUR. Los límites de coste de la envolvente se respetaron en los 20 clubes. `integrity_check` devolvió `ok` y `foreign_key_check` no encontró infracciones.
