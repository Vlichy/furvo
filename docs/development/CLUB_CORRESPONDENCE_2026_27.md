# Correspondencia real-ficticia — corrección de la Entrega 2

**Documento de desarrollo**, excluido del juego. Lista obligatoria aprobada por el usuario y contrastada el **21 de octubre de 2026** con [LaLiga: clubes de Primera 2026/27](https://www.laliga.com/laliga-easports/clubes). Se normalizan las etiquetas de la web a las denominaciones indicadas en el encargo.

Málaga, Racing y Deportivo son los tres recién ascendidos, según la [bienvenida oficial de LaLiga](https://www.laliga.com/noticias/laliga-da-la-bienvenida-a-los-siete-clubes-ascendidos-para-la-temporada-2026-27). Ese antecedente se refleja en la descripción ficticia, sin implementar ascensos ni descensos.

## Correspondencia exacta

| Referencia real | Equivalente ficticio | Ciudad / área | Rep. | Estadio ficticio | Capacidad |
| --- | --- | --- | ---: | --- | ---: |
| Athletic Club | Club Deportivo Ferrería de Bilbao | Bilbao | 4 | Estadio de la Fragua | 50.600 |
| Atlético de Madrid | Club Atlético Pedernal de Madrid | Madrid | 5 | Estadio de la Meseta Roja | 68.400 |
| CA Osasuna | Club Deportivo Senda de Pamplona | Pamplona | 3 | Estadio del Camino del Norte | 21.800 |
| RC Celta de Vigo | Unión Deportiva Ensenada de Vigo | Vigo | 3 | Estadio de las Rías del Sur | 25.800 |
| Deportivo Alavés | Club Deportivo Robledal de Vitoria | Vitoria-Gasteiz | 2 | Estadio del Anillo Verde | 19.800 |
| Elche CF | Unión Deportiva Palmeral de Elche | Elche | 2 | Estadio del Camino del Palmeral | 29.300 |
| FC Barcelona | Unión Deportiva Brisa de Barcelona | Barcelona | 5 | Estadio del Mirador Azul | 87.200 |
| Getafe CF | Club Deportivo Fragua del Sur | Getafe | 1 | Estadio de los Talleres | 16.800 |
| Levante UD | Unión Deportiva Acequia de Valencia | Valencia | 2 | Estadio de la Huerta del Norte | 24.700 |
| Málaga CF | Atlético Mosaico de Málaga | Málaga | 2 | Estadio del Patio Abierto | 32.200 |
| Racing de Santander | Unión Deportiva Abra de Santander | Santander | 2 | Estadio de la Bahía Abierta | 22.400 |
| Rayo Vallecano | Club Deportivo Umbral de Vallecas | Madrid (Vallecas) | 2 | Estadio de las Azoteas | 13.700 |
| Deportivo de La Coruña | Unión del Faro de A Coruña | A Coruña | 3 | Estadio de Punta Clara | 28.600 |
| RCD Espanyol | Unión Deportiva Litoral de Cornellà | Cornellà de Llobregat | 3 | Estadio del Arco Metropolitano | 38.200 |
| Real Betis | Club Atlético Azahar de Sevilla | Sevilla | 4 | Estadio de la Huerta Clara | 57.500 |
| Real Madrid | Club Deportivo Horizonte de Madrid | Madrid | 5 | Estadio de las Siete Puertas | 81.200 |
| Real Sociedad | Club Deportivo Mareal de Donostia | Donostia-San Sebastián | 4 | Estadio de las Tres Bahías | 37.600 |
| Sevilla FC | Unión Deportiva Aljarafe de Sevilla | Sevilla | 4 | Estadio de la Forja del Sur | 42.100 |
| Valencia CF | Club Deportivo Lumbre de Valencia | Valencia | 4 | Estadio de la Alameda del Sol | 49.200 |
| Villarreal CF | Club Deportivo Ceramista de Vila-real | Vila-real | 4 | Estadio del Horno Mayor | 22.600 |

La correspondencia legible por pruebas está en club-reference-2026-27.json, en esta misma carpeta. Las pruebas mantienen además una lista independiente de las veinte referencias, de modo que duplicar, omitir o cambiar un equipo no se valida contra la misma lista por accidente. El mapa JSON se copia exclusivamente a la salida de Tests: no se embebe en Persistence ni se exporta desde Godot.

Reputaciones, fundaciones ficticias, colores y capacidades son decisiones editoriales aproximadas, no datos oficiales ni fórmulas de valoración. El área de Cornellà representa el ámbito metropolitano de Barcelona y Vallecas se distingue dentro de Madrid. No se copian nombres oficiales, escudos ni logotipos al catálogo comercial.

## Clubes conservados

Se conservan nombre e identificador de ocho clubes. Se revisan descripción y reputación o capacidad cuando es necesario para representar la referencia correspondiente.

| Referencia | Club conservado | ClubId conservado |
| --- | --- | --- |
| Athletic Club | Club Deportivo Ferrería de Bilbao | 72026027-0001-4000-8000-000000000005 |
| CA Osasuna | Club Deportivo Senda de Pamplona | 72026027-0001-4000-8000-000000000019 |
| FC Barcelona | Unión Deportiva Brisa de Barcelona | 72026027-0001-4000-8000-000000000002 |
| Málaga CF | Atlético Mosaico de Málaga | 72026027-0001-4000-8000-000000000008 |
| Deportivo de La Coruña | Unión del Faro de A Coruña | 72026027-0001-4000-8000-000000000006 |
| Real Betis | Club Atlético Azahar de Sevilla | 72026027-0001-4000-8000-000000000003 |
| Real Madrid | Club Deportivo Horizonte de Madrid | 72026027-0001-4000-8000-000000000001 |
| Valencia CF | Club Deportivo Lumbre de Valencia | 72026027-0001-4000-8000-000000000004 |

Las revisiones relevantes son Senda (club consolidado, reputación 3 y aforo 21.800), Mosaico (recién ascendido, reputación 2), Faro (recién ascendido, reputación 3) y los aforos de Horizonte, Brisa, Ferrería y Azahar. No se confunde la historia de un club ascendido con una reputación forzosamente mínima.

## Clubes retirados de la selección activa

Estos doce registros no corresponden a participantes de la lista aprobada. Se conservan en la publicación v1 y en sus partidas; sus IDs **no se reutilizan**.

| Club anterior | ClubId retirado de la selección activa |
| --- | --- |
| Club Deportivo Cierzo de Zaragoza | 72026027-0001-4000-8000-000000000007 |
| Unión Deportiva Alisio de Las Palmas | 72026027-0001-4000-8000-000000000009 |
| Club Deportivo Cendal de Valladolid | 72026027-0001-4000-8000-000000000010 |
| Club Deportivo Calima de Alicante | 72026027-0001-4000-8000-000000000011 |
| Atlético Ribera de Logroño | 72026027-0001-4000-8000-000000000012 |
| Unión Deportiva Claraboya de Salamanca | 72026027-0001-4000-8000-000000000013 |
| Club Deportivo Castañal de Oviedo | 72026027-0001-4000-8000-000000000014 |
| Unión Deportiva Tramontana de Palma | 72026027-0001-4000-8000-000000000015 |
| Club Deportivo Estuario de Huelva | 72026027-0001-4000-8000-000000000016 |
| Atlético Campiña de Albacete | 72026027-0001-4000-8000-000000000017 |
| Unión Deportiva Cobre de Cartagena | 72026027-0001-4000-8000-000000000018 |
| Unión Deportiva Vértice de Girona | 72026027-0001-4000-8000-000000000020 |

Se incorporan Pedernal, Ensenada, Robledal, Palmeral, Fragua del Sur, Acequia, Abra, Umbral, Litoral, Mareal, Aljarafe y Ceramista. Sus doce GUID nuevos usan el espacio 72026027-0003-4000-8000, con valores explícitos en el JSON v2. No son renombrados de los clubes retirados ni actualizaciones automáticas de sus partidas.

## Publicación y protección

- Fuente activa: src/Furvo.Persistence/Data/initial-world-2026-27.v2.json.
- DatasetVersion: 2026-27.entrega2.v2.
- Nuevo WorldId: 72026027-0000-4000-8000-000000000004. El anterior era 72026027-0000-4000-8000-000000000001.
- Se conservan CompetitionId=72026027-0000-4000-8000-000000000002 y SeasonId=72026027-0000-4000-8000-000000000003.
- SQLite activo: data/initial-world-2026-27.v2.db. El archivo data/initial-world-2026-27.db permanece intacto como publicación anterior.
- Esquema SQLite: 3, sin cambios de estructura ni migraciones de datos sobre las partidas.

Antes de generar la v2 se verificó el archivo anterior con el cargador v1: era una plantilla idéntica a la publicación original, sin SaveId, jugadores, contratos ni entrenadores. Su SHA-256 antes y después es d4949e610ba88f4f80864c9a4637412063e411a61a9f6068e9902f9d176a2a0e. Si ese archivo contuviera una partida o datos diferentes, no se utilizaría como destino: la corrección siempre publica en otro archivo.

El recurso v1 no se incluye en los datos del juego actual; se conserva en el repositorio y se copia solo para pruebas históricas. La carga actual rechaza destinos publicados que no coincidan con v2 y no convierte silenciosamente v1. Las partidas anteriores siguen en sus archivos con sus clubes originales.

## Documentación del código

Se documentan **31 fuentes activas: 28 archivos C# y tres migraciones SQL**. El inventario enumera cada archivo. Se añaden comentarios en español sobre identidades, validaciones, separación de responsabilidades, rangos, ocultos, transacciones, FK, migraciones, errores, idempotencia y propósito de pruebas.

Se utilizan comentarios XML en clases, interfaces, constructores y métodos públicos relevantes, sin comentarios por línea. JSON y SQLite binario no reciben comentarios. La copia src/furvo-(4.5) está fuera de la solución activa y se conserva intacta, al igual que fuentes generadas de bin/obj/.godot.

El informe source-documentation-review.json compara las 30 fuentes preexistentes ignorando comentarios y espacios fuera de literales; preserva íntegros los literales, incluidos los SQL raw. Coinciden salvo tres cambios explícitos de selección de datos: recurso y versión v2 en el cargador, destino v2 en la utilidad y WorldId esperado en una prueba. No se cambian firmas, estructura ni lógica por documentar. Los SQL 001/002/003 solo reciben comentarios y mantienen el mismo código ejecutable.

## Límites

Nombres e identidades ficticios siguen pendientes de revisión de marcas antes de la publicación comercial. No se ha realizado una validación registral. No se generan jugadores, plantillas, contratos ni nuevas mecánicas y no se comienza la Entrega 3.

Huella final v2 tras cargar dos veces: d1c9a4905a49e1ea36c383d460f07739c34e4ee785699f84533c1b797a45c597. El archivo v1 conserva su huella original.
