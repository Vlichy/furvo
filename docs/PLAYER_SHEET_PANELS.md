# Paneles inferiores de la ficha — ampliación visual de Entrega 4

La cabecera, retrato, cuatro bloques de atributos y navegación existentes se conservan. Se añaden dos paneles debajo, dentro del mismo desplazamiento: **izquierda, campo de posiciones; derecha, fortalezas y debilidades**. No se modifica Core, Persistence, SQLite, los 480 futbolistas, los 61 atributos, contratos ni reputaciones. No se genera otra publicación ni se implementa otra entrega.

## Campo reutilizable

PositionPitch es un Control de dibujo vectorial nativo de Godot. Dibuja líneas exteriores, medio campo, círculo central, áreas y porterías en vista superior. Conserva proporción 105:68 dentro del espacio asignado. La orientación de ataque es **izquierda → derecha**; el lateral derecho aparece en la parte inferior y el izquierdo en la superior, según la orientación del futbolista al atacar.

Solo dibuja la posición principal y las secundarias que recibe de PlayerDetails. No muestra marcas grises que puedan confundirse con posiciones adicionales. Principal naranja, secundarias azul claro; leyenda textual y abreviaturas españolas centralizadas. Si no existen secundarias, no se crean. Sin posición detallada, el campo indica que no consta una posición, sin inferirla del grupo general.

PitchPositionLayout declara coordenadas normalizadas para las trece posiciones existentes y reutiliza las validaciones de PlayerPositions. Ordena las secundarias de forma estable. PositionPitch recibe posiciones, no entidades editables ni estado de alineación; ignora entradas de ratón y no selecciona, arrastra ni asigna jugadores. Puede reutilizarse después como representación visual, sin implementar hoy pantallas tácticas.

## Descripción deportiva

PositionInsightRules es una política pura de **presentación**, no un servicio de valoración interna. No tiene pesos, medias, puntuación global ni relación con valor de mercado. No persiste resultados y no modifica atributos. Su única entrada deportiva son posición principal detallada y grupos públicos de atributos.

Primero limita los candidatos a los códigos relevantes de la posición. Después aplica límites descriptivos centralizados:

- **Fortalezas:** valores ≥ 14; mayor valor primero.
- **Debilidades:** valores ≤ 7; menor valor primero.
- Hasta tres por apartado. Un empate se resuelve por código estable, comparación ordinal.
- Si faltan candidatos que cumplan los límites, muestra menos de tres o un mensaje de ausencia; no etiqueta artificialmente una puntuación neutra como carencia o fortaleza.
- Se excluyen categorías ocultas, códigos ocultos y categorías mal etiquetadas, incluso si llegasen mediante un DTO erróneo. Sin posición detallada no se inventa una selección. Se rechazan duplicados públicos relevantes o valores fuera de 1–20.

Los límites 14/7 son criterios descriptivos de esta ampliación y pueden ajustarse en una única clase. No son pesos ni umbrales de la futura valoración interna por posición. El orden de los códigos de configuración no añade prioridad deportiva; los empates se resuelven ordinalmente.

| Posición | Códigos relevantes explícitos |
| --- | --- |
| Portero | reflexes, handling, punching, one_on_ones, aerial_claims, rushing_out, goalkeeping_positioning, command_of_area, hand_distribution, defensive_communication |
| Central | marking, tackling, anticipation, defensive_positioning, heading, strength, concentration, jumping, bravery, decisions |
| Lateral derecho / izquierdo | tackling, marking, defensive_positioning, anticipation, pace, acceleration, stamina, crossing, short_passing, decisions |
| Mediocentro defensivo | defensive_positioning, anticipation, tackling, marking, short_passing, decisions, teamwork, tactical_discipline, concentration, stamina |
| Centrocampista central | short_passing, long_passing, vision, decisions, ball_control, first_touch, teamwork, stamina, tactical_understanding |
| Mediapunta | vision, short_passing, creativity, decisions, technique, first_touch, ball_control, dribbling, off_the_ball, long_shots, composure |
| Banda derecha / izquierda | crossing, short_passing, dribbling, pace, acceleration, stamina, work_rate, off_the_ball, ball_control |
| Delantero centro | finishing, off_the_ball, composure, heading, anticipation, first_touch, shot_power, strength |
| Extremo derecho / izquierdo | dribbling, pace, acceleration, off_the_ball, crossing, technique, first_touch, finishing, creativity |
| Segundo delantero | off_the_ball, finishing, creativity, short_passing, vision, first_touch, dribbling, composure, decisions |

Así un portero nunca recibe remate, centros o córneres como debilidades. Los valores mostrados son exactamente sus valores públicos originales, no conversiones ni estadísticas derivadas.

## Distribución y accesibilidad

Los dos paneles comparten la zona desplazable con los atributos; la cabecera y Volver a plantilla permanecen fuera. A partir de 1000 píxeles de ancho disponible los paneles están lado a lado; con menos ancho se apilan. El límite corresponde al ancho del contenido, no al ancho exterior de la ventana.

A **1280 × 720** el contenido completo mide **767 píxeles** frente a una zona visible de **453**: para acceder a los paneles inferiores es necesario desplazarse. Los cuatro bloques anteriores conservan su estilo y sus dimensiones. No se reduce su tipografía para forzar toda la información simultáneamente. En resoluciones mayores se aprovecha la mayor altura disponible; en las pequeñas el contenido sigue accesible verticalmente.

Los números se muestran siempre en 1–20, con la paleta ya aprobada. Los nuevos paneles usan fondo azul oscuro diferenciado, encabezados naranjas y textos claros. Los textos, leyenda, abreviaturas y mensajes vacíos están en UiStrings.resx.

## Archivos principales

| Archivo | Función |
| --- | --- |
| Scripts/Components/PositionPitch.cs | Dibujo del campo y marcas reales, sin interacción táctica. |
| Scripts/Components/PitchPositionLayout.cs | Coordenadas normalizadas y selección exacta de marcas. |
| Scripts/Components/PitchPositionsPanel.cs | Marco y leyenda del campo. |
| Scripts/Components/PositionInsightRules.cs | Configuración y selección descriptiva estable, sin valoración global. |
| Scripts/Components/PlayerInsightsPanel.cs | Presentación de fortalezas/debilidades y mensajes vacíos. |
| Scripts/Components/PlayerSheet.cs | Integra ambos paneles debajo de atributos y adapta su disposición. |

Las rutas anteriores son relativas a src/Furvo.Godot.

## Validación realizada

El 27/10/2026: **120 pruebas aprobadas**, incluidas las 108 anteriores; Debug y Release sin errores ni advertencias. Doce casos nuevos cubren todos los perfiles, límites, empates, exclusión de ocultos, no invención de posiciones y correspondencia de marcas/descripciones con los 480 jugadores publicados.

La prueba de escena Godot 4.7.2 .NET aprobó cuatro fichas de tres clubes: un portero, Adrián Alcendor Vallescuro con posiciones 5/6/7 registradas y fichas adicionales. Comprueba 51 atributos públicos, avatar, marcas principales/secundarias, descripciones solo relevantes, máximo tres elementos por apartado, disposición inferior, ausencia de solapamientos y regreso con selección.

Se comprueba geometría a 1280 × 720, 960 × 640 y 1600 × 900, incluidos el apilado, el desplazamiento hasta el final y el botón de regreso. No se ha realizado inspección visual de esta ampliación ni una exportación Windows: queda revisar dibujo, abreviaturas, colores, lectura y barras en la compilación final. Los hashes de Core/Persistence, las cuatro bases y los componentes anteriores protegidos se verifican sin cambios. Más detalles en [VALIDATION.md](VALIDATION.md).
