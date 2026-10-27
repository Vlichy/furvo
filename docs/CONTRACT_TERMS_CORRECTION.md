# Corrección contractual — 27/10/2026

## Contrato de datos y compatibilidad antes del cambio

Se retira DurationSeasons del modelo y de la interfaz actuales. Duración en meses se deriva exclusivamente de StartDate/EndDate, sin columna ni campo JSON derivado. Fechas de cobertura inclusivas para el cálculo: 01/07/2026–30/06/2027 = 12 meses. Se cuentan aniversarios mensuales con DateOnly.AddMonths; los días restantes se expresan como fracción del siguiente intervalo mensual y se redondean a dos decimales. La fecha final debe ser estrictamente posterior al inicio. Se documentan casos parciales y de fin de mes en pruebas, sin aproximar cada mes a 30 días.

Se exigen IDs válidos, fechas, salario anual positivo y moneda del catálogo de códigos admitidos (EUR, USD, GBP, CHF, JPY, CAD, AUD). No son tipos de cambio ni reglas salariales. No se exige aprobación global ni completar preferencias. Obligaciones con Mandatory=true exigen código/contenido y prioridad definidos; null o false no se convierten en obligaciones. La filosofía contractual se modela por separado de los principios institucionales orientativos. Al firmar solo se congelan las obligaciones expresamente exigibles, nunca se promueven borradores institucionales.

Rescisión distingue Unknown, Defined y NoSpecificEconomicClause. Unknown no implica ausencia de cláusula; el tercer estado expresa ausencia económica conocida. No se exige rescisión desconocida para firmar; una cláusula declarada Defined debe tener texto.

Career001_Initial.sql se conserva intacto. Career002_ContractTerms.sql se aplica únicamente a la extensión recién creada y todavía vacía en un temporal, antes de publicar carreras nuevas. Nunca se aplica automáticamente a partidas existentes. La extensión v2 elimina la columna duration_seasons y admite rescisión desconocida o sin cláusula económica, manteniendo FK y estados. El mundo mantiene esquema 4; perfiles mantienen esquema 1. Load admite extensiones 1 y 2 en solo lectura: la duración histórica en temporadas no se usa como condición actual, no se reescribe el JSON antiguo y los compromisos históricos no se reinterpretan.

## Archivos de esta corrección

- Core: Careers/ManagerOffers.cs y Careers/CareerCreation.cs: validaciones específicas, meses derivados, clasificación de rescisión y congelación de compromisos explícitos.
- Persistence: SqliteCareerStore.cs y nueva Migrations/Career002_ContractTerms.sql: contratos v2 y lectura compatible v1 sin reescritura.
- Godot: Components/InitialManagerOfferSource.cs, Components/ManagerOfferDocument.cs, Main.Careers.cs y Resources/UiStrings.resx: campos separados, etiquetas correctas y motivos concretos.
- Pruebas: CareerCreationTests.cs, nuevo CareerContractTermsTests.cs y tools/GodotUiSmoke.gd.
- Documentación: README.md, ARCHITECTURE.md, CAREER_CREATION.md, MANAGER_CONTRACTS.md, VALIDATION.md, INVENTORY.md y este informe.

Las rutas de código parten de src/Furvo.Core, src/Furvo.Persistence y src/Furvo.Godot; las pruebas C# de tests/Furvo.Tests. Se conservan los cambios previos del repositorio, sin commit ni push. No se añaden dependencias.

## Resultados

Debug y Release: cuatro proyectos compilan con cero errores y advertencias; 209 pruebas superadas, cero fallidas u omitidas (179 anteriores y 30 casos adicionales). Incluyen fechas, meses parciales, años bisiestos, salario/moneda, preferencias pendientes, obligaciones incompletas, rescisión, persistencia v2 y lectura v1 con hashes idénticos. GodotUiSmoke.gd termina con código 0 y marcadores FURVO_CAREER_UI_OK y FURVO_UI_SMOKE_OK: oferta, navegación y regresión del explorador. No se declara revisión visual manual por estas pruebas.

## Pendiente de aprobación y comprobación manual

No se fijan salarios, moneda ni fecha de finalización de las veinte ofertas reales. Estos tres motivos mantienen su firma bloqueada. Las preferencias institucionales no son causas de bloqueo. No se aprueban cláusulas económicas, presupuestos ni compromisos por defecto.

En Windows: dotnet build Furvo.sln; dotnet test Furvo.sln; dotnet build Furvo.sln -c Release; dotnet test Furvo.sln -c Release. En Godot revisar oferta a 1280×720, etiquetas, desplazamiento y tres motivos de bloqueo; comprobar que el botón no permite firmar condiciones reales todavía incompletas.
