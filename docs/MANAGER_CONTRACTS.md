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
