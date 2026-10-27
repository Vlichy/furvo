# Rediseño de ficha de club — contrato de presentación

La última maqueta se recibió durante este encargo y se inspeccionó visualmente. Se usa como referencia de composición; sus fotografías, escudo, camisetas y cifras no se consideran datos aprobados. Información conserva la cabecera, identidad a izquierda, estadio a derecha y síntesis histórica inferior. Plantilla conserva su comportamiento.

Antes de ampliar los contratos de lectura se documentan campos opcionales de propiedad (socios, SAD o privada; independencia o grupo multiclub por separado), presidente, propietario y grupo; colección de peñas; datos opcionales del estadio y registros históricos descriptivos. Null significa pendiente, nunca cero. La categoría de estadio admite exclusivamente 1–5 y no se deriva de otros campos. No hay entidades persistentes nuevas ni migración aplicada: Persistence no tiene fuentes aprobadas para rellenarlos.

Equipaciones y emblemas son ilustraciones vectoriales provisionales originales. Un recurso editorial por ClubId conserva tres combinaciones y patrones sencillos; sus colores base proceden del perfil publicado, sus variantes son propuestas visuales, no nuevos datos deportivos. No son los veinte escudos definitivos. Se podrán sustituir los recursos sin cambiar Core ni SQLite.

El campo reutilizable usa coordenadas en metros (105×68 solo como proporción ilustrativa, no dimensiones atribuidas al estadio), con áreas a izquierda y derecha; no modifica PositionPitch. Exterior e interior son dibujos provisionales identificados, sin fotos ni marcas.

No se importan propuestas históricas, no se generan cifras económicas y no se modifican publicaciones ni partidas.

## Resultado de implementación — 27/10/2026

Se conservan Información/Plantilla/Historia/Economía y la selección estable al volver de un futbolista. La cabecera incluye tres camisetas vectoriales y un emblema diferenciado por ClubId. Información usa proporción 36/64 para identidad y estadio en anchos amplios, tres vistas horizontales con descripciones y dos grupos de datos de estadio. La síntesis inferior comparte componentes de trayectoria, palmarés, evolución, leyendas y diez récords con Historia. Al reducir el ancho se apilan los paneles; el contenido es desplazable y el regreso queda fijo fuera del scroll.

Economía separa tesorería/presupuestos, ingresos, gastos, deuda/obligaciones y situación financiera: treinta campos identificados como «No disponible», incluidos los encabezados de totales. No existen importes ni mecánicas financieras.

### Diferencias conscientes respecto a la maqueta

- Fotografías sustituidas por dos ilustraciones geométricas originales claramente provisionales. El plano usa proporción ilustrativa 105×68; esas medidas no se muestran como dimensiones reales del estadio.
- Escudos y camisetas originales provisionales vinculados a los veinte ClubId; sin copiar insignias, franjas distintivas ni marcas de la imagen. No son diseños definitivos aprobados.
- Se conserva la fundación publicada de Mosaico (1946), sus colores y su perfil existentes; no se adopta 1904 ni el contenido institucional de la captura.
- Peñas, apodos, afición, rivalidades, propiedad, presidente, dueño/grupo, abonados y los datos de estadio sin fuente aprobada permanecen pendientes. No se infiere la ciudad del estadio a partir de la ciudad del club.
- Historia no incorpora nombres, cifras ni temporadas de relleno; tampoco lista competiciones no aprobadas solo porque aparezcan en la captura. Tabla y gráfico permanecen sin resultados aprobados.
- A 1280×720 se utiliza desplazamiento vertical en Información para mantener legibilidad. No se pretende mostrar todos los datos simultáneamente con el tamaño tipográfico diminuto de la imagen.

### Validación ejecutada

- Debug y Release: cuatro proyectos, cero errores y cero advertencias.
- dotnet test: **138 superadas, 0 fallidas, 0 omitidas**, en Debug y Release; se conservan las 129 anteriores.
- Godot 4.7.2 .NET sin ventana: FURVO_UI_SMOKE_OK y salida 0. Verifica las cuatro pestañas, tres vistas y tres camisetas, estados pendientes, cinco bloques económicos/treinta campos, selección/ordenación/regreso, 24 jugadores, 51 atributos públicos y ausencia de ocultos.
- Geometría de escena comprobada a 960×640, 1280×720, 1600×900 y 1920×1080: paneles adaptables, tres vistas sin solapamiento, síntesis inferior alcanzable y botón de regreso accesible. No confundir estas aserciones con inspección visual humana de las cuatro resoluciones.
- Inspección de ventana real a 1280×720: Información de Atlético Mosaico, cabecera, equipaciones, identidad, tres vistas y campo sin áreas en las bandas. También se observó la ficha de Adrián Alcendor con la presentación anterior intacta. No se completó una inspección manual de todas las pestañas y todos los clubes/resoluciones; queda para validación final.
- Cuatro publicaciones SQLite comparadas por SHA-256 antes/después, sin variación; Persistence y sus migraciones no se modifican. No se manipulan partidas reales.

### Aprobaciones pendientes

[STADIUM_CATEGORY_PROPOSAL.md](STADIUM_CATEGORY_PROPOSAL.md) contiene veinte candidatos editoriales manuales, independientes de reputación/capacidad y sin uso en la interfaz. CLUB_HISTORY_PROPOSAL.md sigue siendo propuesta no cargada; también requieren autoría y aprobación los datos institucionales, las métricas del estadio y las identidades gráficas definitivas. Los contratos opcionales de consulta no autorizan poblar tablas ni editar SQLite.

### Comprobación local

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln --no-build --no-restore
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release --no-build --no-restore
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --path .\src\Furvo.Godot --resolution 1280x720
~~~

GODOT4 debe apuntar al ejecutable Godot 4.7.2 .NET instalado. Abrir varios clubes y recorrer las cuatro pestañas; bajar a la síntesis, abrir un futbolista y regresar; repetir manualmente a 1600×900 y 1920×1080. No se ha probado una exportación de Windows. No se ha realizado commit ni push de este rediseño.
