## Estado vigente — economía aprobada y firma habilitada (28/10/2026)

El usuario aprobó la propuesta initial-economy-2026-27-draft-v1. La nueva publicación v5 contiene los veinte estados económicos y conserva los 480 jugadores sin cambios. Economía y Contrato consultan los mismos datos; la firma crea una carrera independiente y registra el compromiso del entrenador en una transacción, sin duplicar gastos ni efectuar un pago anual ficticio. Margen salarial real de jugadores: desconocido hasta disponer de contratos salariales. Las publicaciones/partidas antiguas no se modifican.

[Implementación, migración, inventario, pruebas y límites](ECONOMY_IMPLEMENTATION.md). Debug/Release sin errores ni advertencias; 234 pruebas superadas por configuración. Godot con firma y tres carreras aisladas superado; capturas renderizadas de Economía/Contrato revisadas a 1280×720, 1600×900 y 1920×1080. Los apartados siguientes conservan el historial y sus bloqueos anteriores quedan sustituidos por este estado.

## Estado vigente — propuesta económica, etapa 1 (28/10/2026)

Se ha preparado una propuesta ficticia de los veinte clubes para aprobación; no se incorpora a SQLite ni se habilita la firma real. Los presupuestos/previsiones no son saldos ni movimientos. El margen contractual de jugadores permanece desconocido; la reserva de planificación del borrador no lo sustituye. El sueldo propuesto del entrenador está incluido una sola vez en personal. No hay cambios funcionales ni de esquema.

[Diagnóstico, veinte filas, fórmulas y aprobación pendiente](INITIAL_ECONOMY_PROPOSAL.md). Datos: docs/proposals/initial-economy-2026-27.draft.json, estado AwaitingUserApproval, sin uso en ejecución. Esta nota sustituye las indicaciones anteriores sobre el estado de aprobación; los apartados siguientes conservan el historial.

## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](INITIAL_CONTRACT_SIMPLIFICATION.md).

# Ofertas y contratos de entrenador

La oferta admite datos laborales incompletos sin inventar condiciones. Firmar requiere identificadores válidos, fecha inicial, fecha final posterior, salario bruto anual positivo y moneda admitida (EUR, USD, GBP, CHF, JPY, CAD, AUD). La interfaz enumera por separado los errores concretos. Las ofertas reales carecen todavía de fecha final, salario y moneda aprobados.

La duración no se almacena: se calcula en meses desde fechas inclusivas. Del 01/07/2026 al 30/06/2027 son 12 meses. No existe duración contractual en temporadas en el nuevo formato.

Principios institucionales, expectativas y preferencias tácticas orientativos pueden estar pendientes sin bloquear. Solo las obligaciones marcadas Mandatory=true requieren contenido y prioridad definidos. La preferencia táctica exigible debe definir formación o estilo, y prioridad. Al firmar se congelan únicamente los compromisos expresamente exigibles; los borradores orientativos no se convierten en compromisos aceptados. No se implementa evaluación, sanción ni simulación.

Rescisión distingue desconocida, texto definido y ausencia expresa de cláusula económica específica. La ausencia expresa no equivale a información desconocida. Un texto declarado definido no puede estar vacío.

Los recursos económicos siguen procediendo de la misma consulta Core que Economía; importes desconocidos no se prometen. No se implementan contabilidad ni reglas salariales.

Career002_ContractTerms.sql solo se aplica a una extensión nueva y vacía, antes de publicar el archivo de carrera. Las carreras v1 se cargan sin migrarlas ni reinterpretar sus compromisos históricos. Las nuevas son v2. El mundo inicial no cambia.

[Resultados, archivos y comprobaciones](CONTRACT_TERMS_CORRECTION.md).
