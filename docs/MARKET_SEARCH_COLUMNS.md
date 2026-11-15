# Mercado — Corrección del buscador y columnas por posición
Alcance vigente: [solo jugadores de clubes externos](MARKET_EXTERNAL_PLAYERS.md). El club dirigido y su cantera quedan excluidos antes de aplicar filtros; las preferencias descritas aquí no cambian.

Fecha: 04/11/2026. Ampliación de Entrega 1 sobre el buscador existente.

## Búsqueda y rangos
El nombre se consulta por cualquier fragmento contiguo del nombre completo. La clave de comparación descompone Unicode, elimina tildes, convierte a mayúsculas y reduce espacios repetidos (incluidos tabuladores) a uno. Se conservan intactos los nombres originales. «adrian», «Adrián» y «ADRIÁN» producen las mismas coincidencias; también funcionan apellidos y nombres compuestos.

Los límites de atributo son LineEdit editables mediante teclado. Se aceptan enteros 1–20; el mínimo vacío equivale a 1 y el máximo vacío a 20. Ambos vacíos dejan abierta toda la escala. No se redondean decimales ni se recortan entradas fuera de rango. Se rechaza mínimo mayor que máximo. Sin atributo seleccionado no hay filtro por atributo; Limpiar filtros restablece la búsqueda, no las preferencias de columnas.

Buscar o Intro aplica todos los filtros válidos a la vez. Si hay un error, se mantienen los resultados de la última búsqueda válida y el borrador incorrecto, con mensaje explícito que también se conserva al navegar. Cambiar la posición cambia inmediatamente la configuración de columnas; el filtro de resultados se aplica con Buscar. Los filtros no añaden columnas.

## Atributos predeterminados
Se reutiliza el catálogo descriptivo compartido PositionInsightCatalog. No se crean pesos ni fórmulas de habilidad; el orden siguiente es únicamente de presentación.

| Posición | Columnas iniciales, en orden |
|---|---|
| Todas las posiciones | Control del balón, Pase corto, Decisiones, Velocidad, Resistencia |
| Portero | Reflejos, Blocaje, Despejes, Uno contra uno, Salidas aéreas, Salidas a los pies, Colocación en portería, Mando del área, Saque con la mano, Comunicación defensiva |
| Defensa central | Marcaje, Entradas, Anticipación, Colocación defensiva, Cabeceo, Fuerza, Concentración, Salto, Valentía, Decisiones |
| Lateral derecho | Entradas, Marcaje, Colocación defensiva, Anticipación, Velocidad, Aceleración, Resistencia, Centros, Pase corto, Decisiones |
| Lateral izquierdo | Entradas, Marcaje, Colocación defensiva, Anticipación, Velocidad, Aceleración, Resistencia, Centros, Pase corto, Decisiones |
| Mediocentro defensivo | Colocación defensiva, Anticipación, Entradas, Marcaje, Pase corto, Decisiones, Trabajo en equipo, Disciplina táctica, Concentración, Resistencia |
| Centrocampista central | Pase corto, Pase largo, Visión de juego, Decisiones, Control del balón, Primer toque, Trabajo en equipo, Resistencia, Comprensión táctica |
| Mediapunta | Visión de juego, Pase corto, Talento / creatividad, Decisiones, Técnica, Primer toque, Control del balón, Regate, Desmarques, Disparo lejano, Serenidad |
| Centrocampista derecho | Centros, Pase corto, Regate, Velocidad, Aceleración, Resistencia, Esfuerzo, Desmarques, Control del balón |
| Centrocampista izquierdo | Centros, Pase corto, Regate, Velocidad, Aceleración, Resistencia, Esfuerzo, Desmarques, Control del balón |
| Delantero centro | Remate, Desmarques, Serenidad, Cabeceo, Anticipación, Primer toque, Potencia de disparo, Fuerza |
| Extremo derecho | Regate, Velocidad, Aceleración, Desmarques, Centros, Técnica, Primer toque, Remate, Talento / creatividad |
| Extremo izquierdo | Regate, Velocidad, Aceleración, Desmarques, Centros, Técnica, Primer toque, Remate, Talento / creatividad |
| Segundo delantero | Desmarques, Remate, Talento / creatividad, Pase corto, Visión de juego, Primer toque, Regate, Serenidad, Decisiones |

## Editor y tabla
Configurar columnas abre un diálogo con los 51 atributos públicos agrupados por categoría y una lista ordenada. Se pueden activar todos, ninguno o cualquier subconjunto, incluidos atributos ajenos a los habituales de la posición. Subir/bajar cambia el orden. Cancelar descarta el borrador; Aplicar y guardar lo conserva. Restablecer predeterminados modifica el borrador de la posición activa y requiere aplicar; no altera otras posiciones.

Cada atributo ocupa una columna propia, con cabecera y valor real 1–20. Nunca se concatenan atributos en una celda. Primero se muestran nombre, edad, club y posición; después los atributos elegidos, nacionalidad y fecha contractual. Las anchuras mínimas evitan comprimir cabeceras y los desplazamientos horizontal/vertical permiten recorrer la tabla completa. Un resumen de selección fuera de la tabla conserva la identidad del jugador seleccionado al desplazarse horizontalmente.

El Tree reutilizado no ofrece columnas congeladas sin modificar sustancialmente el componente. En esta entrega no se congelan: para consultar otra fila con su identidad hay que volver al extremo izquierdo; el resumen mantiene la identidad de la selección actual. Los botones de consulta y configuración permanecen fuera de la zona desplazable.

Cualquier atributo mostrado se ordena numéricamente, primero ascendente y después descendente. Se conservan nombre, edad, club, posición y fecha contractual como criterios. Desempates por nombre y ID estable; desconocidos al final. Quitar el atributo utilizado para ordenar vuelve a Nombre ascendente. Reordenar sin quitarlo conserva su criterio.

## Persistencia y aislamiento
No existía un almacén persistente de preferencias de interfaz: las opciones de pantalla actuales son de sesión. Se añade un archivo propio, versionado:
- Producción: user://ui/market-columns.v1.json.
- Windows habitual: %APPDATA%/Godot/app_userdata/Furvo/ui/market-columns.v1.json.
- QA: artifacts/career-ui-*/ui/market-columns.v1.json, dentro del almacenamiento aislado.

Alcance por usuario: las 13 posiciones y Todas las posiciones tienen selección/orden independientes, compartidos entre sus carreras. Solo se guarda presentación; no se persisten filtros ni selecciones de jugadores entre procesos. Durante la sesión cada carrera tiene su contexto de búsqueda; cambiar de carrera lo restablece y conserva las preferencias visuales del usuario.

Formato JSON v1, validado antes de aplicarse. Se rechazan códigos ocultos, desconocidos, duplicados y versiones o posiciones no admitidas. Un archivo inválido muestra un aviso y utiliza predeterminados; no se reescribe automáticamente. Un fallo al guardar se anuncia y mantiene el cambio solo en memoria. La escritura usa un temporal y sustitución en el mismo directorio, sin tocar SQLite.

No se modifican jugadores, contratos, presupuestos, publicaciones iniciales ni esquemas de carrera. Sin dependencias nuevas. El lector SqliteMarketQueries no cambia. No se alteran las fichas de Plantilla o Cantera. El componente DataTable recibe opciones de anchura y desplazamiento horizontal cuyo comportamiento por defecto permanece igual para las otras pantallas.

## Inventario
Nuevos:
- src/Furvo.Godot/Scripts/Components/MarketColumnPreferences.cs: validación, predeterminados compartidos, orden y archivo de preferencias.
- src/Furvo.Godot/Scripts/Components/MarketColumnsWindow.cs: editor visual con borrador, selección pública, subir/bajar, restaurar, aplicar y cancelar.
- tests/Furvo.Tests/MarketColumnTests.cs: búsqueda, rangos, columnas, persistencia, orden y ausencia de escritura en la carrera.
- docs/MARKET_SEARCH_COLUMNS.md: informe, inventario y límites.

Modificados en esta corrección:
- src/Furvo.Core/Queries/MarketQueries.cs: normalización de nombre, límites editables y orden numérico público.
- src/Furvo.Godot/Scripts/Main.Market.cs: controles, columnas dinámicas, ficha reutilizada, persistencia de preferencias y punto DEBUG de reapertura aislada para QA.
- src/Furvo.Godot/Scripts/Components/MarketSearchContext.cs: borradores de rango, atributo de orden, mensaje de error y desplazamiento horizontal.
- src/Furvo.Godot/Scripts/Components/DataTable.cs: anchuras fijas opcionales y restauración horizontal.
- src/Furvo.Godot/Resources/UiStrings.resx: textos del editor y mensajes.
- tests/Furvo.Tests/MarketTests.cs: adaptación del contexto al borrador textual, conservando las comprobaciones existentes.
- tests/Furvo.Tests/Furvo.Tests.csproj: enlace a la clase pura de preferencias para probar su implementación real sin cargar Godot.
- tools/GodotUiSmoke.gd: recorrido de columnas, teclado real, regreso, módulo, restauración individual y reapertura entre procesos; ajuste de la prueba anterior a columnas variables.
- README.md, docs/MARKET_DELIVERY_1.md, docs/INVENTORY.md, docs/VALIDATION.md: documentación actualizada.

Los cambios locales anteriores, incluidos los pendientes de Cantera, se conservan. No se realizan commits ni push.

## Validación
Referencia previa ejecutada: 629 pruebas Debug superadas. Las pruebas nuevas incluyen nombres simples, completos y apellidos, tildes, mayúsculas, espacios, rangos escritos y rechazados, 13 configuraciones más Todas, añadir/quitar/reordenar/restaurar, lectura/escritura del archivo, selección de los 51 atributos y rechazo de los diez ocultos incluso por código o archivo manipulado.

Se comprueba orden numérico con valores 9, 10 y 20, independencia entre filtros y columnas, cambio de criterio al ocultar una columna y conservación de ID. La prueba de integración calcula hashes de la carrera completa y de la publicación inicial antes y después de búsquedas y guardado de preferencias, cubriendo jugadores, contratos y presupuestos.

Recorrido nativo con Godot 4.7.2 .NET en carrera aislada: búsquedas Adrián/adrian/nombre completo, central, añadir Velocidad y Pase corto, subir columna, ordenar Velocidad en ambos sentidos, introducir 13–17 con eventos de teclado, rechazar 21 sin recortar, ficha y regreso con filtros/selección/scroll horizontal, cambio de módulo, configuración independiente de portero, 51 columnas, restaurar solo central y capturas en tres resoluciones. Segundo proceso verifica la recuperación de las configuraciones desde disco.

Resultados finales, TRX y logs: apartado correspondiente en VALIDATION.md. Capturas: artifacts/career-visual/career-market-columns-*.png. Los artefactos permanecen locales.

## Comandos de comprobación
Desde la raíz del proyecto:

```powershell
dotnet build Furvo.sln -c Debug
dotnet test Furvo.sln -c Debug
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release
```

En Godot .NET, abrir src/Furvo.Godot/project.godot e iniciar una carrera con club. Acceder a Mercado, configurar dos posiciones distintas, salir y abrir de nuevo la aplicación para comprobar las preferencias del usuario.

## Límites
Sin columnas congeladas ni reordenación mediante arrastre; se utilizan botones subir/bajar. No se guardan búsquedas entre procesos. Un filtro de atributo por búsqueda, independiente de las columnas. La normalización elimina diacríticos Unicode descomponibles; no translitera alfabetos. No se implementan ojeadores, fichajes, negociaciones, cesiones, agentes, simulación, potencial ni valoraciones globales. No se inicia la siguiente fase.

Resultados ejecutados: compilación Debug/Release sin errores ni advertencias; 664 pruebas superadas en cada configuración, cero fallos y omisiones. 35 casos nuevos respecto de la referencia de 629. Los tres recorridos nativos descritos han superado sus comprobaciones. Las cinco publicaciones iniciales mantienen sus hashes SHA-256.
