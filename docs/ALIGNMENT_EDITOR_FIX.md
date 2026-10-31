# Corrección de alineación y editor de posiciones

## Origen y eliminación del popup

WireFreeEditor, en Main.Plans.cs, guardaba primero TacticalPlanRules.Move y después llamaba a RoleDialog cuando Propose detectaba otra zona. El panel contextual también tenía un botón que abría el mismo diálogo. Se han eliminado RoleDialog, RoleChoices, AddRoleEditor, sus invocaciones y sus textos de recursos. No se ha ocultado una ventana existente: ya no se construye.

Soltar sobre césped libre invoca ahora MoveAndAssign de Core. En una sola edición inmutable valida coordenadas y separación, obtiene la demarcación de las zonas existentes y actualiza ambos datos de la disposición activa. La interfaz llama una sola vez a SqliteTacticalPlanStore.Save, con revisión optimista y su transacción existente; después reconstruye los círculos. No hay un estado intermedio persistido con coordenadas nuevas y demarcación antigua.

El arrastre automático aplica la zona aunque el movimiento sea pequeño. Propose mantiene su opción anterior para consumidores legados, pero MoveAndAssign desactiva esa retención. Se conservan las zonas de laterales, carrileros, bandas, extremos, central, MCD, MC, MP y delantero; el portero conserva su demarcación. No se escriben las posiciones naturales o secundarias del futbolista.

Los círculos vuelven a calcular LineupSelection.Compatibility: natural verde, secundaria ámbar y resto rojo. La abreviatura se crea desde la demarcación guardada. Se ha retirado también el selector de intercambio con otro titular; los intercambios por arrastre y las sustituciones siguen disponibles.

## Jerarquía de interfaz

- Navegación Equipo (Plantilla/Táctica) separada del nombre del club.
- Nombre del club → pestañas: 16 px.
- Pestañas → configuración: 20 px.
- Plan, Disposición y Formación son grupos de etiqueta/control con 8 px interiores y 18 px entre grupos; HFlowContainer permite reflujo si falta anchura.
- Configuración → Mentalidad: 16 px.
- Mentalidad → contenido: 24 px.
- El margen de cabecera, junto a la separación del contenedor de carrera, deja al menos 20 px desde la navegación contextual.
- Guardar/copiar plan, restablecer/copiar disposición y referencias están en el panel lateral desplazable. Se pasa su contenedor explícitamente al editor; no depende de buscar un nodo aún no incorporado al árbol.
- La formación informativa no se divide letra por letra cuando se contrae el grupo. La ayuda descriptiva de instrucciones pasa al panel desplazable para conservar altura de pizarra.
- Campo adaptable con mínimo de 260 px y desplazamiento cuando la altura disponible no basta; conserva tamaño mayor en resoluciones superiores.

## Persistencia y alcance

No hay migración ni cambio de esquema: las disposiciones ya almacenaban coordenadas, rol y código táctico. Solo una edición explícita modifica la fase/plan activos. Las otras cinco disposiciones, instrucciones, transiciones y balón parado se conservan. No se altera el mundo inicial, contratos ni atributos.

## Inventario de esta corrección

| Archivo | Cambio |
| --- | --- |
| src/Furvo.Core/Tactics/TacticalPlans.cs | Operación MoveAndAssign atómica y control explícito de retención de zona |
| src/Furvo.Godot/Scripts/Main.Plans.cs | Guardado automático; eliminación del diálogo y herramientas con contenedor explícito |
| src/Furvo.Godot/Scripts/Main.Lineups.cs | Panel lateral de herramientas, retirada de selector de intercambio y tamaño adaptable |
| src/Furvo.Godot/Scripts/Main.Tactics.cs | Márgenes entre niveles, grupos adaptables y anchura de formación |
| src/Furvo.Godot/Scripts/Components/InteractiveLineupPitch.cs | Mínimo de altura y metadatos de QA derivados de las posiciones visibles/color real |
| src/Furvo.Godot/Scripts/Main.Careers.cs | Punto de prueba exclusivo de Debug que recarga una carrera con almacenamiento aislado; no añade carga de partidas a la interfaz |
| src/Furvo.Godot/Resources/UiStrings.resx | Retirada de textos del diálogo y botón manual eliminados |
| tests/Furvo.Tests/AutomaticTacticalRoleTests.cs | Cuatro movimientos de ejemplo, cruce pequeño de zona, portero, independencia y adecuación |
| tools/GodotUiSmoke.gd | Verificación nativa sin confirmación, MP/color, guardado único, fases, reapertura y márgenes |
| docs/ALIGNMENT_EDITOR_FIX.md | Informe e inventario |
| docs/TACTICAL_PLANS.md | Reemplazo de las instrucciones anteriores de confirmación |
| docs/VALIDATION.md | Resultados reales y límites de la comprobación |

## Procedimiento de comprobación

~~~powershell
dotnet build Furvo.sln -m:1
dotnet test Furvo.sln --no-build -m:1
dotnet build Furvo.sln -c Release -m:1
dotnet test Furvo.sln -c Release --no-build -m:1
~~~

El recorrido Godot usa su sistema nativo de arrastre con eventos de ratón sobre círculos reales. En una carrera aislada de Mosaico entra en Inicial/Sin balón, mueve un MC a MP, comprueba que no hay diálogo, verifica texto/color y una sola revisión, alterna ambas fases y guarda. El punto de QA recarga la carrera mediante EnterCareer y SqliteCareerStore.Load, reconstruyendo consultas y espacio de trabajo; regresa al editor y comprueba las coordenadas persistidas. Este punto requiere ConfigureCareerStorage y no se compila en Release.

Se prueban 1280 × 720, 1600 × 900, 1920 × 1080 y retorno a 1280, además de los recorridos anteriores de instrucciones y balón parado. No se ha añadido el flujo comercial de cargar partidas ni se afirma una prueba con cierre/reinicio completo del proceso y carga desde el menú principal. Quedan para revisión manual otros DPI y ventanas más pequeñas.

Los resultados ejecutados se registran en VALIDATION.md. Logs, TRX y capturas están en artifacts.
