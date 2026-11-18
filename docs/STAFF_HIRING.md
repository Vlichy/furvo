# Ofertas y negociación de Staff

En Mercado → Buscar empleados, la ficha externa permite hacer una oferta si la situación laboral está acreditada. Los vínculos desconocidos conservan la explicación y la acción deshabilitada. La plantilla propia conserva la consulta contractual. Renovaciones y modificación de contratos ya firmados quedan pendientes.

El formulario recoge cargo de entrenador de cantera, equipo inferior propio, incorporación, vencimiento, salario anual, prima de fichaje, compensación y cláusulas. Las duraciones ofrecidas son 1, 2, 3 y 5 años. Un borrador anterior puede conservar su plazo original. El vencimiento mostrado y guardado en SQLite es el último día incluido. Para un inicio el 18/11/2026 y tres años, es el 17/11/2029.

Si un borrador sigue vigente y su incorporación ya pasó según la fecha de la carrera, hay que corregirla antes de enviarlo. La corrección explícita crea una revisión y conserva los términos históricos y la fecha de caducidad de la oferta. Una oferta caducada sigue disponible para consulta.

Los importes editables combinan texto y sugerencias en el mismo control. La compensación aparece en euros y en solo lectura desde la propuesta inicial al club hasta la negociación personal. Su importe proviene del cálculo de Furvo o de la revisión que se está editando. Se conservan los desplegables de reducción, incremento y recuperación salarial. Una recuperación antigua sin porcentaje se interpreta como el 100 % de la reducción. Salida, indemnización y prima por ascenso conservan importes editables.

Se reutilizan la validación monetaria de Mercado y las cláusulas estructuradas de `StaffEmployment`. El límite técnico de Staff es 100.000.000 EUR por importe y dos decimales. El salario también se valida frente a los parámetros vigentes del club. La liquidez y los compromisos requieren validación financiera al formalizar.

Un trabajador libre acreditado inicia la fase personal. Un contrato vigente requiere primero una propuesta al club. Un vencimiento próximo no habilita un precontrato por sí mismo. La fase personal conserva la compensación pactada con el club. Enviar guarda una propuesta pendiente. Evaluar respuesta aplica una decisión determinista y guarda la revisión. Una contraoferta puede revisarse y reenviarse. La vigencia inicial de 14 días se comprueba al pasar de estado. Se admiten hasta tres contraofertas por fase. La generación actual carece de un catálogo de técnicos libres externos.

## Criterio de decisión

- Club de origen: umbral sobre salario por tiempo de contrato restante, ajustado por desarrollo juvenil y reputación profesional. Si falta vigencia, rechaza. Una compensación positiva inferior a un cuarto del umbral se rechaza. Una consulta inicial a cero o una compensación inferior al umbral pide ese importe. Al alcanzarlo, concede conformidad condicionada. La baja del puesto depende de la formalización.
- Empleado: referencia del salario que cobraba o, si carece de referencia individual, una fracción del salario de mánager del club comprador corregida por reputación y el mínimo de la temporada. La diferencia de reputación de clubes modifica la referencia un 2,5 % por punto. Una reducción por descenso resta un cuarto de su porcentaje como coste esperado en la evaluación. Una oferta ajustada inferior al 65 % del mínimo se rechaza. Por debajo del mínimo o con menor estabilidad, se pide salario o plazo. El resto queda en acuerdo condicionado.
- Los perfiles actuales se limitan a técnicos de cantera. Las aptitudes de otros cargos, ambición, preferencias geográficas, precontratos, autorizaciones especiales de salida y negociación de renovación requieren reglas y datos propios.

## Persistencia y límites

`StaffOffers001_Initial.sql` se ejecuta al guardar la primera propuesta válida en la carrera. Las lecturas anteriores a la extensión devuelven un historial vacío. `SqliteStaffOffers` guarda cada revisión y comprueba la revisión esperada antes de escribir, el propietario de la carrera y la fecha. Las ofertas y la respuesta se recuperan de SQLite, sin reevaluar al abrir. Se bloquean borradores activos duplicados para el mismo técnico. Ofertas muestra también borradores. Negociaciones muestra el último estado de las propuestas que tienen una revisión enviada.

Se mantiene la separación con las operaciones de futbolistas. Las pantallas comparten estilos de tablas y acciones de navegación. Ninguna conformidad cambia contratos, tesorería, personal o delegaciones. Para formalizar se requiere una operación única y transaccional que valide margen salarial comprometido, liquidez, obligaciones de prima, bonus y compensación, libere el puesto anterior, registre el alta, mantenga trayectoria y preserve o devuelva las responsabilidades afectadas. La aceptación actual muestra ese bloqueo.

Archivos: `StaffNegotiation.cs`, `SqliteStaffOffers.cs`, `StaffOffers001_Initial.sql`, `Main.Staff.cs`, `Main.StaffOffers.cs`, `Main.Transfers.cs`, `UiStrings.resx`, `StaffOfferTests.cs` y el recorrido de `GodotUiSmoke.gd`.

En la carrera aislada de Godot se guardaron nueve revisiones de una propuesta, con 68 técnicos y 68 contratos. SQLite confirmó integridad y claves foráneas. El recorrido `--staff-hiring --capture` comprobó el formulario a 1280×720, 1600×900 y 1920×1080, el acuerdo condicionado y la presencia en Mis ofertas y Negociaciones. El recorrido `--transfer-only` verificó el flujo de futbolistas.
