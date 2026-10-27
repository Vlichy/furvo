# Decisiones consolidadas — 27/10/2026

Estas decisiones están aprobadas y distinguen compromisos futuros de la implementación actual. El contrato comercial se recoge en [V1_CONTRACT.md](V1_CONTRACT.md).

## Reputación de clubes

El valor interno de autoría es un entero **0–100**. El visible **1–5** se derivará por tramos: 0–19 → 1; 20–39 → 2; 40–59 → 3; 60–79 → 4; 80–100 → 5. Los extremos 0 y 100 son válidos. No se deduce calidad de plantilla ni se usa aleatoriedad para los valores iniciales. La evolución temporal no se implementa ni se define todavía.

| Club | ClubId estable | Interna aprobada | Visible derivada |
| --- | --- | ---: | ---: |
| Horizonte | 72026027-0001-4000-8000-000000000001 | 98 | 5 |
| Brisa | 72026027-0001-4000-8000-000000000002 | 96 | 5 |
| Pedernal | 72026027-0003-4000-8000-000000000001 | 86 | 5 |
| Aljarafe | 72026027-0003-4000-8000-000000000011 | 76 | 4 |
| Ferrería | 72026027-0001-4000-8000-000000000005 | 74 | 4 |
| Lumbre | 72026027-0001-4000-8000-000000000004 | 72 | 4 |
| Azahar | 72026027-0001-4000-8000-000000000003 | 68 | 4 |
| Mareal | 72026027-0003-4000-8000-000000000010 | 66 | 4 |
| Ceramista | 72026027-0003-4000-8000-000000000012 | 64 | 4 |
| Faro | 72026027-0001-4000-8000-000000000006 | 54 | 3 |
| Ensenada | 72026027-0003-4000-8000-000000000002 | 52 | 3 |
| Litoral | 72026027-0003-4000-8000-000000000009 | 47 | 3 |
| Senda | 72026027-0001-4000-8000-000000000019 | 44 | 3 |
| Mosaico | 72026027-0001-4000-8000-000000000008 | 34 | 2 |
| Abra | 72026027-0003-4000-8000-000000000007 | 32 | 2 |
| Acequia | 72026027-0003-4000-8000-000000000006 | 30 | 2 |
| Umbral | 72026027-0003-4000-8000-000000000008 | 28 | 2 |
| Robledal | 72026027-0003-4000-8000-000000000003 | 26 | 2 |
| Palmeral | 72026027-0003-4000-8000-000000000004 | 24 | 2 |
| Fragua del Sur | 72026027-0003-4000-8000-000000000005 | 32 | 2 |

El catálogo activo es [club-reputations-2026-27.v2.json](../src/Furvo.Persistence/Data/club-reputations-2026-27.v2.json), asociado por ClubId y embebido en Persistence. Fragua del Sur se corrige de 14 a **32**, por lo que su reputación visible es **2**; los otros diecinueve valores internos no cambian. El [manifiesto documental original](decisions/club-reputations-2026-27.json) se conserva como registro histórico, junto a las publicaciones anteriores y sus pruebas.

**Estado implementado:** AdjustedInitialWorld2026 utiliza los veinte valores internos manuales del catálogo para derivar la reputación visible de v4. Club.Reputation y SQLite conservan su semántica 1–5 y el esquema 4; el valor interno se conserva en el catálogo versionado, no en una columna SQLite nueva. No se trata una puntuación visible histórica como si fuera interna. La nueva publicación tiene otro WorldId y mantiene todos los IDs de entidades y los datos deportivos salvo la corrección de Fragua. No se modifica automáticamente ninguna partida ni se implementa evolución temporal.

## Motor determinista futuro

Semilla SHA-256 por encuentro, SplitMix64 inyectado, procesamiento estable y persistencia de resultados/entradas/versiones. No recalcular encuentros disputados. Contrato detallado en [ARCHITECTURE.md](ARCHITECTURE.md). No se implementa motor en esta entrega.

## Valoración interna por posición

Servicio futuro de **Furvo.Core**, resultado **decimal 1–20**, específico por posición. Usará media ponderada de atributos visibles, pesos configurables/versionados y penalización progresiva por carencias graves. Los diez atributos ocultos quedan excluidos incluso del conjunto de entrada del cálculo. No se mostrará ni persistirá el resultado; será una función derivada, separada de valor de mercado, reputación y habilidad efectiva de una acción.

Debe tratar expresamente las posiciones incompatibles; la política numérica, pesos y umbrales se definirán y versionarán al implementar la siguiente entrega, sin inventarlos ahora. Las pruebas cubrirán determinismo, monotonía cuando no cambia la elegibilidad, umbrales de penalización y diferencias entre posiciones. No sustituirá los 61 atributos ni la futura resolución por acciones individuales.

## Interfaz

Componente reutilizable DataTable de Godot con columnas configurables, selección/activación y petición de orden. La lógica de ordenación de plantilla reside en Core. Textos centralizados en recursos .resx, español inicial y claves estables para futuros recursos de idioma, sin implementar otras lenguas. Clubes y jugadores proceden de SQLite mediante servicios existentes, sin catálogos duplicados en código de presentación.

## Probadores privados

1. Técnica durante desarrollo: automatización, pruebas de persistencia, determinismo y estabilidad, registrando versiones y semillas.
2. Alfa privada: 2–3 personas cuando exista una versión jugable.
3. Beta cerrada: al menos cinco personas, cada una con tres temporadas completadas o más.

Las incidencias registrarán versión/build, equipo, pasos, resultado esperado/real, frecuencia y partida/semilla reproducible cuando proceda. Los fallos críticos impedirán publicación. No se contacta ni busca probadores ahora.

## Plan aprobado

1. Finalizar V0.1, entrega 4: consulta visual del mundo y validación de compatibilidad.
2. Implementar valoración interna por posición en una entrega autorizada posterior.
3. Desarrollar **E1 como prototipo experimental independiente del motor de producción**; no integrarlo como mecánica comercial por defecto.
4. Continuar con entregas de mercado, IA, calendario, simulación y demás sistemas; su detalle se concretará antes de cada implementación.
5. Integrar y validar progresivamente el contrato V1.0.

No se comienza la siguiente entrega sin autorización. Consolidar estas decisiones no implementa sus mecánicas.
