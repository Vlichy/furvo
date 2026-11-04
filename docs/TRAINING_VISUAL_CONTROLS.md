# Entrenamiento: pestañas, horarios y participantes — 03/11/2026

Cambios exclusivamente de presentación en Godot. Se conservan la agenda global, los registros, la fecha y semana seleccionadas, la especialización de los formularios y todas las validaciones deportivas. No hay cambios de Core, persistencia, esquemas ni dependencias.

## Archivos

- src/Furvo.Godot/Scripts/Components/TrainingUi.cs (nuevo): estilo de pestañas, fuente destacada mediante FontVariation sobre la fuente existente, tamaño de menús y etiquetas públicas de participantes.
- src/Furvo.Godot/Scripts/Main.Training.cs: estilo común para las siete pestañas y nombres/posiciones en miembros de grupos.
- src/Furvo.Godot/Scripts/Main.TrainingCalendar.cs: controles horarios contiguos de 160 píxeles, menús limitados y nombres/posiciones en participantes individuales y excepciones.
- tools/GodotUiSmoke.gd: verificaciones de pestañas, menús abiertos/cerrados, identidad, selección múltiple y geometría en tres resoluciones.
- docs/TRAINING_VISUAL_CONTROLS.md, INVENTORY.md y VALIDATION.md: inventario y evidencia.

## Decisiones

La pestaña activa utiliza fondo diferenciado, texto naranja con negrita sintética y borde inferior naranja de 3 píxeles. Todas reservan la misma altura y ancho suficiente para la fuente destacada, evitando movimientos al alternar. Las inactivas mantienen el azul.

Se conserva OptionButton y su PopupMenu: la documentación XML del GodotSharp 4.7.2 instalado indica que Window.MaxSize limita el menú y permite desplazamiento interno. La prueba aislada en el motor instalado confirmó que se requieren límites no nulos tanto de anchura como de altura; con anchura cero Godot ignoraba el límite vertical. Se mantiene WrapControls activo y se fijan ambos límites (160×216 para horas). No se reemplaza el selector ni se recrean los valores al abrirlo. Se conserva la selección de cuarto de hora, las horas antiguas no normalizadas, el cálculo de duración y las comprobaciones existentes.

Los participantes utilizan TrainingCandidate.Positions.Primary, procedente de la plantilla de la carrera, y los textos de posición centralizados existentes. Ante una posición ausente se muestra Desconocido, sin inferirla. El selector individual dispone de una fila completa para evitar recortar la posición; su menú se limita a 720 píxeles de anchura y unas siete filas. El texto es Nombre completo — Posición principal; el identificador estable se conserva en metadatos y en el enlace existente a los candidatos. No se consultan atributos. ItemList no proporciona columnas de nombre/posición por fila, por lo que grupos y excepciones usan la etiqueta compuesta, mantienen selección múltiple y altura acotada con desplazamiento interno.

## Validación

Antes: 515 pruebas Release correctas. Después: 515 Debug correctas. También pasan las 515 pruebas Release, sin fallos ni omisiones. Compilaciones Debug y Release sin errores ni advertencias. El recorrido nativo confirma el estilo de las siete pestañas, dimensiones 160×216 del menú horario, nombre/posición e identidad en todos los selectores, conservación de selección individual y múltiple, y regresiones de agenda, navegación y persistencia. Godot termina con código 0 y las capturas se han revisado en 1280×720, 1600×900 y 1920×1080. El WARNING de solapamiento es un caso negativo esperado. Furvo se ha abierto normalmente y permanece ejecutándose; artifacts/training-visual-final-run.log. No se añaden pruebas xUnit que reproduzcan estilos; se amplía el recorrido real en Godot para comprobar geometría, popup y selección.

Evidencia: artifacts/test-results/training-visual-{before,debug,release}.trx; artifacts/training-visual-native.log; artifacts/career-visual/career-training-visual-*.png.

~~~powershell
dotnet build Furvo.sln -c Debug --no-restore
dotnet test Furvo.sln -c Debug --no-build --no-restore
dotnet build Furvo.sln -c Release --no-restore
dotnet test Furvo.sln -c Release --no-build --no-restore
~~~


## Corrección de Porteros — 03/11/2026

El selector de participantes de Porteros ofrece exclusivamente Individual y Por demarcación. Los candidatos individuales se obtienen de la plantilla, con posición principal Portero; la lista de demarcaciones solo contiene Portero. Las otras áreas mantienen sus cuatro modalidades. La selección se enlaza explícitamente con TrainingScope, sin inferir el enum a partir del índice visual. Los ejercicios nuevos proponen Por demarcación/Portero. Los ejercicios antiguos con Equipo o Grupo no se convierten al consultarlos: antes de guardar una edición se exige elegir una modalidad admitida. No se modifican bases de datos automáticamente.

Archivos: Main.TrainingCalendar.cs, Resources/UiStrings.resx y tools/GodotUiSmoke.gd. Compilaciones Debug y Release correctas, sin advertencias; 515 pruebas Debug correctas. Godot nativo termina con código 0 y prueba el guardado de ambas modalidades conservando ID. Furvo abierto normalmente y dejado ejecutándose. Evidencia en artifacts/training-keeper-scope-*.
