# Ficha compacta de futbolista — rediseño de la Entrega 4

Este encargo modifica exclusivamente presentación y su validación. No inicia otra entrega, cambia estadísticas, esquema, identificadores ni publicaciones. La referencia funcional es una ficha compacta de gestión; la silueta, recursos y estilo gráfico son originales de Furvo.

## Distribución

- Cabecera horizontal con retrato, nombre destacado, club, posición principal, secundarias, nacionalidad, edad y nacimiento. La edad mantiene la fecha de referencia existente del mundo, no el reloj del ordenador.
- Una única zona de atributos con tres columnas: técnicos (17); mentales (16); físicos (8) y, justo debajo, portero (10). Cada bloque conserva su encabezado. No utiliza pestañas ni las tablas grandes.
- Los diez atributos de portero se muestran para cualquier posición, dentro del cuarto bloque de la tercera columna.
- Los 51 atributos públicos se construyen exclusivamente desde PlayerDetails. AttributeSection verifica categoría y código público antes de mostrarlo. No recibe Player, AllValues, atributos ocultos ni una valoración global.
- Botón de regreso existente; se mantiene la selección por ID en la plantilla.

A 1280 × 720 la prueba geométrica de Godot mide 767 píxeles de contenido total y 453 de zona visible: se conserva desplazamiento para acceder a los paneles inferiores. La cabecera y el botón de regreso permanecen fuera de esa zona desplazable. También se comprueba el regreso a 960 × 640 y 1600 × 900. Se conservan fondo azul oscuro, acentos naranjas y texto claro. Los ajustes al marco común solo se activan para la ficha, mediante showHeading=false; los otros recorridos mantienen su código y aspecto anterior.

## Paneles inferiores añadidos

Debajo de los cuatro bloques se incorporan campo de posiciones a la izquierda y fortalezas/debilidades a la derecha. En ventanas estrechas se apilan; cabecera y regreso permanecen fuera del desplazamiento. La selección es descriptiva y usa únicamente atributos relevantes de la posición principal. Detalles, configuración explícita y límites 14/7 en [PLAYER_SHEET_PANELS.md](PLAYER_SHEET_PANELS.md).

## Colores de presentación

AttributeValueStyle centraliza umbrales y paleta, sin modificar valores:

| Valores | Color | Hexadecimal |
| --- | --- | --- |
| 1–5 | Rojo tenue | d99391 |
| 6–10 | Neutro | dce5f0 |
| 11–15 | Verde suave | a8cda7 |
| 16–20 | Verde destacado | 69d7a0 |

Siempre se muestra el número. El color no sustituye la información ni representa un cálculo de rendimiento. Los valores fuera de 1–20 se rechazan; no se recortan ni reescalan.

## Retrato reutilizable

PlayerPortrait mantiene un marco uniforme **78 × 104** (3:4) y centra la textura conservando su aspecto. El avatar provisional es un SVG propio en **src/Furvo.Godot/Assets/Portraits/avatar.svg**, sin fotos, fuentes ni referencias externas. No se descarga ni genera ninguna cara individual.

Para futuras imágenes autorizadas, se buscarán únicamente recursos locales:

~~~text
res://Assets/Portraits/Players/<PlayerId en formato N>.png
res://Assets/Portraits/Players/<PlayerId en formato N>.webp
~~~

Formato N: 32 caracteres hexadecimales minúsculos, sin guiones. El PlayerId conserva su valor original; solo se formatea para el nombre del archivo. No se usan nombres de jugadores como claves ni se aceptan rutas libres desde la base. No hacen falta nuevas columnas SQLite.

La búsqueda comprueba existencia y tipo Texture2D, captura errores de carga y rechaza texturas vacías o mayores de 4096 píxeles por lado. Si no hay retrato válido utiliza la silueta; si falta incluso ese recurso instalado, conserva la ficha con un marcador textual en lugar de cerrarla. Solo se contemplan recursos empaquetados de confianza bajo res://, no archivos remotos ni exploración de carpetas del usuario.

Al incorporar imágenes en otra tarea, importarlas en Godot y asegurarse de incluir esa carpeta en la exportación; las rutas dinámicas deben viajar con el paquete. No se han añadido retratos individuales ni probado una exportación Windows en este encargo. Los .uid y avatar.svg.import son metadatos generados por Godot para estos recursos, no IDs de futbolistas.

## Componentes

| Archivo | Responsabilidad |
| --- | --- |
| Scripts/Components/PlayerSheet.cs | Cabecera y cuatro bloques en tres columnas; solo recibe la ficha pública. |
| Scripts/Components/AttributeSection.cs | Filas compactas, alineación, validación pública y colores. |
| Scripts/Components/AttributeValueStyle.cs | Umbrales cromáticos independientes del motor y de la persistencia. |
| Scripts/Components/PlayerPortrait.cs | Marco reutilizable, carga local y fallback. |
| Scripts/Components/PortraitResourcePaths.cs | Convención segura basada en PlayerId. |
| Resources/UiStrings.resx | Textos centralizados en español y mensajes de retrato. |

Rutas relativas a src/Furvo.Godot. Main solo compone PlayerSheet y conserva el botón de regreso y la gestión de errores existentes. DataTable permanece intacta para clubes y plantillas.

## Validación actual

La ampliación conserva los cuatro bloques, retrato y cabecera. El 27/10/2026 pasan **120 pruebas C#**, incluidas las 108 anteriores; Debug y Release sin errores ni advertencias. La prueba de escena Godot comprueba un portero, un jugador con dos secundarias, fichas de otros clubes, 51 atributos, descripciones relevantes y regreso.

Con los paneles añadidos, la zona completa mide 767 píxeles de contenido frente a 453 visibles a 1280 × 720. El desplazamiento es necesario para consultar la parte inferior. También se verifican apilado a 960 × 640, disposición lateral a 1600 × 900 y regreso sin solapamientos. Son comprobaciones de escena y geometría; queda inspección visual de la ampliación. No se prueba exportación Windows.

La UI sigue leyendo v4 sin cambios. No se modifica SQLite, reputación, contratos ni atributos. Más información en [PLAYER_SHEET_PANELS.md](PLAYER_SHEET_PANELS.md), [VALIDATION.md](VALIDATION.md) e [inventario](INVENTORY.md).
