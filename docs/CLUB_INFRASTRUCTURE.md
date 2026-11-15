# Club → Información y Estadio

Información reúne la identidad institucional, la síntesis histórica, la filosofía, las expectativas y el resumen del estadio con sus tres ilustraciones provisionales. En una carrera con club, «Gestionar estadio e infraestructuras» abre Estadio. La ficha pública conserva la consulta de ambos contenidos.

Estadio presenta cinco subapartados. **Estadio actual** permite consultar identidad, terreno, asistencia, ilustraciones e instalaciones asociadas. **Reformas y ampliaciones**, **Nuevo estadio**, **Ciudad deportiva** y **Obras y proyectos** aparecen deshabilitados y señalados como pendientes. La pestaña **Instalaciones** continúa dedicada a equipamiento y tecnología como Footbonaut.

## Datos y arquitectura actuales

`Main.ShowClub` solicita `ClubDetails` a las consultas del contexto activo. Información y Estadio comparten `ClubStadiumPage.AddOverview` para presentar los datos presentes y resolver ausencias como «Pendiente de definir». Conservan los controles `FootballPitch` y `StadiumPreview`. Los campos forman parte de la ficha existente y la navegación funciona sin escrituras.

Se dispone de campos de nombre, ciudad, aforo, dimensiones, césped, categoría, inauguración, asistencia media, récord, ocupación e instalaciones asociadas. Su disponibilidad depende del club. Faltan inventario físico con identidades, propiedad o derecho de uso por instalación, sectores y aforos por clase, accesibilidad, inspección de estado, drenaje, iluminación, servicios, mantenimiento y coste operativo real. La propiedad del club y la del recinto son datos separados.

## Diseño futuro

| Área | Datos y reglas previstos |
|---|---|
| Reformas | Proyecto asociado a una instalación, estado inicial, cambio esperado, requisitos, presupuesto, contratista, autorizaciones, fechas y restricciones temporales de uso. El aumento de capacidad y la asistencia se evaluarán por separado. |
| Nuevo estadio | Terreno, derechos de uso, permisos, capacidad y sectores, cubiertas, servicios, instalaciones anexas, financiación, contratistas, demoras y traslado. Conservar el registro y los derechos del estadio anterior hasta decidir su destino. El modelo admitirá proyectos prediseñados o personalizados una vez elegido el alcance. |
| Ciudad deportiva | Inventario por club, equipo y uso. Campos, gimnasio, recuperación, medicina y análisis para el primer equipo, más formación, vestuarios y posibles residencias para cantera. Incluir oficinas y servicios. La tenencia puede ser propia, alquilada, compartida o de terceros. |
| Obras y proyectos | Registro por carrera de propuesta, instalación, responsable, coste estimado y comprometido, pagos, fuentes de financiación, fechas, retrasos y evidencia de avance. Mantener historial de estados y autorizaciones. |

Flujo previsto: Propuesta → En estudio → Pendiente de autorización → Aprobada → Planificada → En ejecución → Finalizada. Una obra en ejecución puede suspenderse y luego reanudarse. La cancelación requiere registrar el motivo y resolver los compromisos ya adquiridos. La finalización requiere recepción de obra y activa el cambio de inventario en la fecha acordada. Los retrasos y el avance dependerán de hitos del calendario y contratos. Las consecuencias deportivas requerirán un sistema capaz de evaluarlas.

El dominio deberá consultar presupuesto aprobado, tesorería, reservas, obligaciones, deuda, mantenimiento e ingresos por separado. La autorización de un presupuesto, el compromiso contractual y el pago representan pasos distintos. Se prevén recursos propios, crédito, aportaciones de propietarios, acuerdos comerciales y subvenciones cuando procedan. Antes de habilitar operaciones, verificar titularidad, normativa urbanística y deportiva, permisos y competencias de firma y aprobación por jurisdicción.

Delegaciones incorporará mantenimiento, propuestas y seguimiento cuando existan personal elegible y acciones funcionales. Mantendrá Personal, Supervisada y Completa con límites específicos y retorno al mánager en caso de vacante. Cada activo tendrá un registro de mantenimiento y adquisición único, incluso si se muestra en Estadio e Instalaciones.

## Fases propuestas

1. Completar el inventario por club con procedencia y campos desconocidos explícitos. Incorporar propiedad, disponibilidad y calendarios sin extrapolar datos según reputación.
2. Añadir estudios y propuestas de reforma con revisión técnica, autorización y costes estimados. Conectar con el libro mayor y los compromisos cuando estén operativos.
3. Ejecutar proyectos y mantenimiento en el calendario de carrera. Verificar suspensión, retraso, cancelación, recepción y persistencia sin cargos duplicados.
4. Integrar ciudad deportiva y construcción de nuevo estadio. Validar traslado, uso del recinto anterior y efectos deportivos medibles.
5. Activar responsabilidades de infraestructura en Delegaciones y comprobar carreras largas con inversiones y obligaciones recuperables.

Archivos de esta entrega: `ClubInformationPage.cs`, `ClubStadiumPage.cs`, `Main.cs`, `UiStrings.resx`, `tools/GodotUiSmoke.gd` y este documento.
