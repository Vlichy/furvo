# Mercado — Tabla de Negociaciones

Fecha: 06/11/2026.

## Cambios

Se conserva DataTable/Tree: una única estructura de columnas para cabeceras y filas, sin posiciones horizontales independientes. Modo proporcional opcional 28/27/20/17/8, con reserva de barra vertical y adaptación al área disponible. Las demás tablas no activan este modo. Nombres y clubes a la izquierda, importes a la derecha, versión centrada. Cifras tabulares mediante OpenType tnum sobre la fuente existente; formato monetario es-ES conservado.

Filas compactas con altura mínima 34 y espaciado 4, margen interior entre textos. Recorte visual con el tooltip completo ya disponible en DataTable. Cabeceras fijas del Tree durante el desplazamiento vertical y sin desplazamiento horizontal. Filtro de estados ajustado a su contenido, sin ocupar toda la pantalla.

Se conserva la selección por identificador y el acceso al detalle/historial. El diseño también se aplica a Mis ofertas porque comparte el mismo listado; no se duplica la tabla.

## Archivos modificados en este encargo

- src/Furvo.Godot/Scripts/Main.Transfers.cs: columnas, alineaciones y filtro compacto.
- src/Furvo.Godot/Scripts/Components/DataTable.cs: opciones de cifras tabulares y anchuras proporcionales, desactivadas por defecto.
- src/Furvo.Godot/Resources/UiStrings.resx: cabeceras Jugador e Importe.
- tools/GodotUiSmoke.gd: comprobación nativa de geometría, selección, nombres extensos, cifras grandes, estados y versiones de cinco dígitos.
- docs/MARKET_NEGOTIATIONS_LAYOUT.md, docs/INVENTORY.md y docs/VALIDATION.md: registro y evidencia.

## Validación

Compilaciones Debug y Release sin errores ni advertencias. Pruebas xUnit: 761 superadas en Debug y 761 en Release; cero fallidas y cero omitidas. El último ajuste de margen interior solo afecta al renderizado de Godot y se valida mediante el recorrido nativo.

Godot nativo: recorrido real de ofertas con guardado, reapertura, envío, retirada y acceso al detalle. Prueba visual de la tabla con 1, 8 y 150 filas en 1280×720, 1600×900 y 1920×1080. Los casos extremos se introducen exclusivamente como TreeItems temporales del ensayo: no se guardan ofertas, versiones o estados ficticios. Se comprueban proporciones, ID seleccionado, tooltip completo y desplazamiento de la lista con cabeceras visibles. Capturas inspeccionadas en las tres resoluciones.

Evidencia: artifacts/negotiations-layout-native.log; artifacts/economy-visual/negotiations-table-*.png; artifacts/test-results/negotiations-layout-debug.trx y negotiations-layout-release.trx.

No se modifican Core, Persistence, reglas de negociación, contratos, economía ni bases de datos. Sin migraciones, dependencias, commits ni push. No se recibió una imagen adjunta en este turno: la revisión se basa en los requisitos escritos y las capturas de la aplicación ejecutada.
