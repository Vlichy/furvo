# Simplificación del contrato inicial y antecedentes — 27/10/2026

## Estado vigente

La oferta inicial fija incorporación 01/07/2026 y finalización 30/06/2027. La interfaz muestra «30/06/2027 (1 año)»: la anotación se deriva de ambas fechas, no es un campo persistido. No hay fila de duración en meses ni temporadas. Las fechas históricas se conservan: solo se anota un año cuando ambas fechas efectivamente cubren un año inclusivo. El método de cálculo de meses permanece en Core para compatibilidad y validación; no se muestra ni se serializa como dato contractual independiente. Futuras renovaciones deberán producir nuevas condiciones explícitas, sin modificar contratos aceptados. No se implementan renovaciones.

## País, moneda y regla salarial

ContractCountryPolicies centraliza España → EUR. Se utiliza el país persistido del club; no la nacionalidad del entrenador. Un país sin configuración genera ContractCountryConfigurationException y un mensaje específico de configuración, sin recurrir a una moneda por defecto ni convertir importes.

Referencia legal a jornada completa: [Real Decreto 126/2026, artículo 3.1](https://www.boe.es/eli/es/rd/2026/02/18/126/con), mínimo bruto anual 17.094 EUR para 2026 (1.221 × 14), consultado el 27/10/2026. Se comparan importes anuales brutos, no mensualidades. Es el suelo general publicado, no una cifra atribuida a un entrenador o club real ni una determinación de complementos o convenios específicos.

Regla configurable initial-salary-v1: salario bruto anual = mínimo bruto anual configurado + margen anual aprobado estrictamente positivo. Su fundamento es garantizar un importe estrictamente superior al suelo, sin introducir diferencias por antecedentes ni inventar datos históricos. La fórmula está preparada; el margen económico real permanece null y pendiente de aprobación. No se ofrece cero como salario. Un margen nulo, cero, negativo o una versión vacía no producen salario. Los márgenes empleados en pruebas son exclusivamente fixtures sintéticos.

La firma valida el importe contra el suelo, la moneda y el resultado de la regla. La oferta conserva una instantánea inmutable de país, referencia legal y regla: cargar un contrato no consulta leyes actuales ni recalcula su sueldo. Las ofertas iniciales reales solo muestran el error de salario pendiente; finalización y EUR están resueltos. Preferencias orientativas no bloquean; obligaciones exigibles siguen requiriendo contenido/prioridad.

Pendiente antes de habilitar firma real: aprobar el margen/importe salarial inicial y revisar los mínimos o complementos específicamente aplicables. No se extrapola la referencia 2026 a 2027: la actualización legal de ese ejercicio deberá configurarse/revisarse antes de atribuir cumplimiento para todo el periodo contractual. No se han aprobado nuevos presupuestos ni condiciones económicas de rescisión.

## Antecedentes y recursos

Nuevas altas: únicamente Novice (Entrenador debutante) y FormerPlayer (Exfutbolista). NewCoachBackgrounds es la lista común del formulario y del almacén de perfiles. Ambos antecedentes son descriptivos, sin efectos deportivos, de reputación o salario. Sus posibles efectos futuros deberán diseñarse y aprobarse. La experiencia profesional se adquirirá en las carreras, sin implementar aún esa progresión.

Experienced=3 permanece como valor histórico para poder leer/reutilizar perfiles y snapshots anteriores sin convertirlos silenciosamente. No está disponible para nuevas altas. Reintentar un perfil existente idéntico continúa siendo idempotente; no se sobrescriben identidades.

La oferta mantiene fichajes inicial, presupuesto salarial, margen salarial restante, staff y cantera. Se elimina exclusivamente su fila de fichajes restante; Economía conserva esa consulta para gestión posterior. No se cambian importes. Filosofía y expectativas se mantienen. Rescisión conserva sus tres estados: definida, ausencia expresa de cláusula económica y pendiente; no se infiere ausencia a partir de null.

## Persistencia y compatibilidad

No se cambia ningún esquema ni migración: perfiles 1, carreras 1/2 compatibles, mundo 4. El nuevo metadato salarial opcional viaja en el JSON de la oferta. Su ausencia en contratos antiguos no implica aplicar retroactivamente una regla inicial nueva. Las fechas, salario, moneda y antecedentes de contratos/perfiles históricos no se reescriben. Ninguna publicación ni partida personal se migra automáticamente. Los contratos históricos sintéticos con salarios inferiores a la nueva referencia permanecen como fixtures de compatibilidad, no como ofertas iniciales reales.

## Inventario de esta corrección

Creado:
- src/Furvo.Core/Careers/InitialManagerContractPolicy.cs: país/moneda, suelo legal, regla configurable y fechas iniciales.
- tests/Furvo.Tests/InitialManagerContractPolicyTests.cs: casos de política, compatibilidad y persistencia.
- docs/INITIAL_CONTRACT_SIMPLIFICATION.md: este informe.

Modificado:
- src/Furvo.Core/Careers/ManagerOffers.cs: metadatos salariales y validaciones específicas.
- src/Furvo.Core/Careers/CoachProfiles.cs: catálogo de antecedentes para nuevas altas, preservando el histórico.
- src/Furvo.Persistence/SqliteCoachProfileStore.cs: rechaza nuevas altas del antecedente retirado, conserva lectura y reutilización.
- src/Furvo.Godot/Scripts/Components/InitialManagerOfferSource.cs: fechas y moneda resueltas, salario mediante configuración.
- src/Furvo.Godot/Scripts/Components/CoachProfileForm.cs: solo dos opciones.
- src/Furvo.Godot/Scripts/Components/ManagerOfferDocument.cs: fecha anotada, sin fila de duración ni fichajes restante.
- src/Furvo.Godot/Scripts/Main.Careers.cs: error de configuración específico y retirada de la fila de meses del resumen.
- src/Furvo.Godot/Resources/UiStrings.resx: textos centralizados y errores reales.
- tests/Furvo.Tests/CareerCreationTests.cs: expectativas de las veinte ofertas actualizadas.
- tools/GodotUiSmoke.gd: formulario, fecha, moneda y ausencia de filas/avisos obsoletos.
- README.md y docs/MANAGER_CONTRACTS.md, CAREER_CREATION.md, COACH_PROFILES.md, ARCHITECTURE.md, VALIDATION.md, INVENTORY.md: referencia al estado vigente.

No hay nuevas dependencias, cambios de arquitectura, commit ni push. Se conservan los cambios anteriores del proyecto.

## Validación ejecutada

- Debug y Release: cuatro proyectos compilan con 0 errores y 0 advertencias.
- Debug y Release: 222 pruebas superadas, 0 fallidas, 0 omitidas (209 anteriores y 13 casos adicionales).
- Godot 4.7.2 .NET sin ventana, tools/GodotUiSmoke.gd: salida 0, FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK. Comprueba dos opciones de antecedentes, oferta, un solo bloqueo salarial, fechas/EUR, eliminación de filas, navegación y regresión de clubes/plantillas/atributos.
- Comparación SHA-256 de bases preexistentes: 9 archivos idénticos, incluidas las cuatro publicaciones y las bases preexistentes de usuario; las pruebas usan almacenamiento aislado.
- Estas comprobaciones automatizadas no equivalen a inspección visual manual.

## Comprobación local

Desde PowerShell en la raíz del proyecto:

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
~~~

GODOT4 debe contener la ruta del ejecutable Godot .NET instalado. Abrir src/Furvo.Godot/project.godot y ejecutar F6/F5 según escena/proyecto. Revisar manualmente legibilidad a 1280×720, desplazamiento, regreso, texto final «30/06/2027 (1 año)», EUR, ausencia de fila de duración y fichajes restante, y el único bloqueo salarial. No intentar habilitar la firma real con importes sin aprobación.
