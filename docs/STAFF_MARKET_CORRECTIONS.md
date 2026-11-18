# Correcciones de Mercado y Plantilla de Staff

## Datos de la carrera

`AcademyGenerator.Coach` genera una identidad determinista por equipo inferior del catálogo aprobado. `SqliteAcademyStore.Initialize` guarda identificador, equipo, nombre, cargo y vínculo institucional en `academy_staff`. El club se obtiene de `academy_teams`. El directorio abre la carrera en lectura y excluye el club propio del listado de contratación.

La carrera de Mosaico de referencia tiene cuatro técnicos institucionales propios y 64 externos. `academy_staff` carece de contrato, salario y vencimiento. Por eso **Todos** muestra los 64, mientras que **Con contrato** devuelve cero y explica la falta de documentos acreditados. **Libres** también devuelve cero en esa carrera. El mánager aparece en la plantilla propia con el contrato firmado de `CareerSummary.Contract`.

Los modelos deportivos aportan competencias por tarea y por equipo, pero no un inventario laboral de segundos entrenadores, preparadores físicos, especialistas de porteros, ojeadores, directores, médicos, psicólogos o administrativos. Los autores de informes tampoco acreditan relaciones laborales adicionales. Los técnicos pueden desempeñar las tareas autorizadas en Entrenamiento y Cantera. Las especialidades, atributos, reputación, idiomas y trayectoria del personal institucional siguen pendientes de datos.

## Interfaz

- Mercado combina nombre, situación, vencimiento y cargo cuando haya más de un cargo disponible. Vencimiento se calcula desde la fecha de carrera y se restablece y deshabilita al elegir Libres. La ordenación alterna ascendente y descendente y deja las fechas desconocidas al final.
- Plantilla de empleados combina nombre, departamento, cargo y equipo. Muestra responsabilidades efectivas de Entrenamiento y Cantera al consultar al personal propio. Salario, fechas y situación siguen disponibles en la ficha.
- Mis ofertas y Negociaciones presentan Jugadores y Staff. Jugadores conserva sus registros y sus acciones. Staff tiene un estado vacío explicativo hasta disponer de ofertas y negociaciones laborales propias, con identidad e historial separados y los patrones de persistencia del Mercado.
- Se usa `DataTable` para alinear cabeceras y celdas. Nombre y equipo van a la izquierda. Cargo, situación y vencimiento van centrados. Los textos recortados se pueden leer con el cursor o al abrir la ficha.

## Archivos y persistencia

Cambios en `Main.Staff.cs`, `Main.Transfers.cs`, `UiStrings.resx` y `tools/GodotUiSmoke.gd`. Se reutilizan los modelos y tablas existentes. Esta corrección no añade migraciones ni datos de personal. El organigrama ampliado respetará la documentación por club de [Cantera](ACADEMY_RESEARCH.md) y el diseño de [Staff](STAFF_ARCHITECTURE.md).

Godot comprobó el recorrido `--staff-only --capture` a 1280×720, 1600×900 y 1920×1080 y separadamente ejecutó `--transfer-only` para validar el flujo de jugadores. La carrera aislada dio `integrity_check = ok`, cero infracciones de claves foráneas, 68 técnicos con IDs únicos de 20 clubes y un contrato de mánager.
