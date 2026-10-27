## Estado vigente — simplificación del contrato inicial (27/10/2026)

La oferta inicial incorpora fechas 01/07/2026–30/06/2027 y EUR por país del club. Sin filas de duración ni fichajes restante en la oferta. El salario depende de un margen anual aún pendiente de aprobación sobre el suelo legal configurado; es el único bloqueo actual de las veinte ofertas reales. Nuevos perfiles: debutante o exfutbolista; experiencia histórica conservada sin reescritura. No cambian esquemas ni contratos existentes. Compilación Debug/Release sin errores ni advertencias; 222 pruebas superadas por configuración y prueba de escena Godot superada. Los apartados anteriores de entrega que siguen se conservan como historial y quedan subordinados a esta corrección.

[Reglas económicas, compatibilidad, archivos y validación](INITIAL_CONTRACT_SIMPLIFICATION.md).

# Perfiles de entrenador

Perfil reutilizable independiente de partidas. CoachProfileId es estable y diferente del CoachId de cada carrera. Se almacena en user://coach-profiles/profiles.db, no en el mundo inicial. Nombre 1–60, apellidos 1–100, ciudad 1–100; textos recortados sin caracteres de control. Nacionalidad por código de catálogo estable v1; experiencia descriptiva (debutante, exfutbolista, con experiencia), avatar genérico con clave estable. Fecha válida y edad 18–100 a la fecha inicial del mundo (2026/07/01), sin reloj de sistema. El máximo es una validación de formulario, no una capacidad deportiva.

Migración Profiles001_Initial.sql, application_id propio y PRAGMA user_version=1, únicamente para base vacía propia. No modifica perfiles existentes. Altas idempotentes por ProfileId: repetir datos devuelve el mismo perfil; reutilizar ID con datos distintos falla. No existe edición/borrado de perfiles en esta entrega. Cada carrera guarda una copia completa de identidad y antecedente, sin consultas posteriores al perfil.

## Resultado

Se implementan selección y alta, avatar provisional, validación de formulario, persistencia y reutilización. El catálogo inicial tiene 45 códigos de nacionalidad con etiquetas en recursos españoles; es extensible por versión y no se eliminan códigos históricos. DateOnly y SQL coinciden en el aniversario de nacimientos 29/02 y los límites 18–100. Los reintentos conservan ProfileId y una doble pulsación no duplica el perfil. El perfil no contiene atributos deportivos ni bonificaciones.

No se ofrece edición o borrado. Una prueba modifica un perfil en un almacén temporal para comprobar que una carrera anterior conserva su copia original. [Almacenamiento, archivos y validación](CAREER_CREATION.md).
