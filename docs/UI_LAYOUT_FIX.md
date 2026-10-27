# Corrección de maquetación y elección provisional — 27/10/2026

## Alcance y diagnóstico

Solo presentación. La exploración sigue siendo anterior a la elección del equipo y al inicio de una partida. Se conservan los cambios locales anteriores y no se hace commit ni push.

- Plantilla usaba pesos 4/2/1/2 y mínimos sin proporción con 35/30/10/25. El texto informativo era un Label de una sola línea sin autowrap. Se explicitan márgenes y tamaño horizontal; se habilita salto de línea y se conservan tablas y servicios existentes.
- Los campos HistoryCoachAppearances y HistoryAdditionalRecords se añadían al cuerpo de Historia después de construir HistoryRecordsPanel: por eso quedaban fuera del borde de la tarjeta.
- ClubUi.Field repartía todo el ancho entre dos etiquetas ExpandFill. En una ventana grande el valor comenzaba cerca del centro de la ventana. Ahora el ancho de la etiqueta depende de su propia fila, con un máximo de 240 px y separación de 12 px.
- Las tarjetas se estiraban verticalmente dentro de la fila hasta la altura de la más alta. Ahora se alinean al inicio y conservan su altura de contenido.
- La instrumentación previa no reprodujo el recorte lateral a 1280×720: la escena permanecía dentro del viewport en ese diagnóstico. No se atribuye ese recorte a una causa no demostrada ni se confunde el encuadre parcial de una captura con una medición del motor.

## Correcciones y nodos

Plantilla mantiene DataTable sin duplicarla ni modificar sus reglas. Columnas configuradas con pesos 35/30/10/25 y mínimos proporcionales 245/210/70/175. Nombre y Posición alinean título/celda a izquierda; Edad al centro; Nacionalidad a izquierda de forma consistente. SquadDescription tiene nombre de nodo, autowrap y expansión horizontal. No se añaden columnas, búsqueda o filtros inexistentes: actualmente esos controles no estaban implementados.

Historia usa dos VBoxContainer dentro de ClubHistoryColumns adaptable. Cronología, trayectoria y palmarés a izquierda; clasificaciones, evolución y récords a derecha. En ancho reducido se apilan. La tabla histórica vacía ajusta su altura al encabezado y el gráfico vacío reduce su mínimo a 150 px, sin rellenar datos.

Los doce campos de Récords están dentro del VBox de HistoryRecordsPanel, tanto en Información como en Historia. Las etiquetas/valores usan el ancho de su fila; no se emplean posiciones absolutas para alinearlos.

Información conserva identidad, estadio y los dos paneles de filosofía/expectativas. Su fila inferior comienza alineada y muestra trayectoria, leyendas y récords. **Palmarés aparece exclusivamente en Historia; Leyendas exclusivamente en Información**, con el detalle disponible y sin convertir personajes de archivo en jugadores.

ClubFooter es un HFlowContainer fuera de ClubTabs y de los ScrollContainer. Incluye Volver a lista de clubes, Ver futbolista (solo en Plantilla) y Elegir este club. Permite envolver botones si falta ancho. No se solapan con la zona desplazable.

ChooseClub abre el AcceptDialog ClubChoiceNotice con texto exacto:

> La selección de club estará disponible cuando se implemente el inicio de partida

Cerrar el aviso conserva pestaña, ClubId y selección. No selecciona realmente un club, asigna entrenador ni usa servicios de escritura. El diálogo se centra mediante el método nativo de Godot; no usa posiciones absolutas para sus filas de contenido.

## Archivos de este encargo

| Archivo | Cambio |
| --- | --- |
| src/Furvo.Godot/Scripts/Main.cs | SquadDescription, proporciones y alineaciones, pie adaptable, botón y aviso. |
| src/Furvo.Godot/Scripts/Components/ClubUi.cs | Altura de tarjetas ajustada a contenido y separación acotada de campos. |
| src/Furvo.Godot/Scripts/Components/ClubHistorySections.cs | Los dos campos finales se construyen dentro de Récords. |
| src/Furvo.Godot/Scripts/Components/ClubHistoryPage.cs | Dos columnas compactas, sin Leyendas ni campos fuera de tarjeta. |
| src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs | Trayectoria/Leyendas/Récords; retira Palmarés y evolución de esta pestaña. |
| src/Furvo.Godot/Resources/UiStrings.resx | Texto del botón, aviso y cierre centralizados. |
| tools/GodotUiSmoke.gd | Aserciones de proporciones, alineación, altura de filas, contención, pie, aviso y exclusividad de secciones. |
| docs/UI_LAYOUT_FIX.md | Este informe. |
| docs/VALIDATION.md, docs/INVENTORY.md, docs/CLUB_SCREEN.md, README.md | Estado vigente y límites de validación. |

artifacts/layout-baseline.gd y artifacts/layout-visual-review.gd son instrumentos locales temporales excluidos de Git; no forman parte del juego ni de los datos publicados.

## Comprobaciones ejecutadas

- dotnet build Furvo.sln: correcto, 0 errores y 0 advertencias (Debug), también después de la compactación final.
- dotnet test Furvo.sln --no-build --no-restore: **150 superadas, 0 fallidas, 0 omitidas**. Se mantienen todas las pruebas anteriores; las regresiones de interfaz se amplían en el smoke real de Godot. No se repite Release para este encargo.
- Godot 4.7.2 .NET, tools/GodotUiSmoke.gd: salida 0 y FURVO_UI_SMOKE_OK en la versión final.
- Geometría automatizada a 960×640, 1280×720, 1600×900 y 1920×1080: proporciones con tolerancia inferior a 0,6 puntos porcentuales; alineación, filas uniformes, encabezado completo, botones contenidos, doce récords dentro de tarjeta, ausencia de solapamiento y apertura/cierre del aviso desde las cuatro pestañas. Se conservan 24 jugadores, ordenación, selección, 51 atributos públicos y regreso desde futbolista.
- Inspección visual real parcial: se observó Plantilla de Mosaico en ventana ampliada, con texto informativo completo, títulos alineados y botones visibles; también se observó Historia en dos columnas durante el ajuste. Se lanzaron bancos visuales solicitando 1280×720 y 1600×900, pero las capturas observadas correspondían a ventanas ya ampliadas y con navegación distinta a la etapa inicial. No se declaran por ello inspeccionadas visualmente esas dos resoluciones exactas. Se observó además Plantilla en la instancia a pantalla completa de 1920×1080: encabezado, alineación de columnas, filas y pie visibles. Queda inspección visual final de Récords y del diálogo, y recorrido visual completo a 1280×720 y 1600×900. Las pruebas de geometría no sustituyen esa inspección.
- Se ejecuta una instancia normal de la versión final al terminar. No se afirma aprobada una exportación Windows.
- Cuatro bases comparadas por SHA-256 con las huellas previas a esta tarea: idénticas. No se modifica Persistence, datos de jugadores, contratos, atributos, reputaciones, historia ni motor.

## Comprobación local pendiente

~~~powershell
dotnet build Furvo.sln
dotnet test Furvo.sln --no-build --no-restore
& $env:GODOT4 --headless --path .\src\Furvo.Godot --script "$PWD\tools\GodotUiSmoke.gd"
& $env:GODOT4 --windowed --path .\src\Furvo.Godot --resolution 1280x720
& $env:GODOT4 --windowed --path .\src\Furvo.Godot --resolution 1600x900
& $env:GODOT4 --windowed --path .\src\Furvo.Godot --resolution 1920x1080
~~~

GODOT4 debe apuntar al ejecutable .NET instalado. Mantener el tamaño solicitado sin maximizar para contrastar resoluciones exactas. Revisar Plantilla, descender al último récord en Historia/Información, abrir y cerrar Elegir este club en cada pestaña y regresar desde un futbolista. Ninguna pulsación debe crear una partida.
