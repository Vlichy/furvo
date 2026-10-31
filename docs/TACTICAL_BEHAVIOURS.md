# Ampliación táctica: roles e instrucciones

## Alcance y uso

Se amplían los tres planes y sus seis disposiciones, reutilizando la pizarra, el arrastre, el balón parado y la sincronización de Cantera. No se implementan efectos deportivos, partidos ni entrenamiento. No hay dependencias nuevas.

En Alineación, seleccionar un titular muestra nombre, demarcación, adecuación y rol en el panel lateral. El editor individual desplegable se divide en Ataque, Pase y creación, Finalización y Defensa. Las opciones se filtran por posición y fase. Cambiar un rol personalizado requiere confirmación; cancelar conserva su configuración. Editar una orden conserva las demás.

Los porteros no reciben órdenes de remate, centros o desmarques. Su configuración incluye pase con balón y salidas sin balón. El destino de centros se limita a las bandas. No se consultan atributos ocultos ni se añade una valoración global.

Estilo de juego → Sin balón incorpora orientación de presión, presión de salida del portero rival, trampa del fuera de juego y comportamiento al superar la presión. Se mantienen las órdenes anteriores. Las vistas previas usan posiciones reales, zonas y flechas enlazadas. Representan intenciones, no resultados garantizados.

Balón parado incorpora cinco prioridades editables para capitanes, penaltis durante el partido, faltas directas/indirectas y córners de cada lado. La tanda existente de cinco penaltis sigue independiente. No se permiten jugadores ajenos ni repetidos dentro de una jerarquía. Se muestra el primero disponible en el once. La vista de córner existente representa el lado izquierdo; el resolver admite ambos lados explícitamente. Si no hay nueva jerarquía se conserva el especialista legado disponible. Una jerarquía explícita agotada queda sin asignación, sin elegir arbitrariamente un futbolista.

## Modelo y compatibilidad

La clave es plan + fase + puesto estable, no la identidad del titular. Las dos disposiciones conservan los mismos once jugadores. Los roles no alteran la alineación. Cada fase tiene su propio mapa de comportamientos.

Jerarquía: reglas fundamentales → posición geométrica → instrucciones colectivas → rol → personalización compatible. Las órdenes individuales no desplazan las anclas tácticas. Presión alta con línea retrasada o libertad individual con disciplina colectiva generan avisos informativos. El motor futuro deberá respetar estructura y reglas antes de ejecutar preferencias; todavía no hay resolución de acciones.

Valores neutros: mantener posición para movimientos y seguimiento, zonal para marcaje, mixto para destino de centros y opción intermedia para las demás instrucciones. El preset del rol sustituye los neutros y las personalizaciones sustituyen únicamente sus claves explícitas.

Un cambio de demarcación por arrastre conserva el rol compatible; si deja de serlo adopta un preset válido y elimina órdenes inaplicables. No abre confirmaciones de demarcación. Cambiar formación conserva los comportamientos compatibles de los puestos que sobreviven. Copiar entre fases filtra las personalizaciones que no corresponden a la fase de destino. Copiar un plan conserva ambas fases mediante copias independientes.

## Catálogo de roles

Hay 35 presets con diferencias explícitas, definidos en `src/Furvo.Core/Tactics/IndividualTactics.cs`:

- Portero: clásico, líbero, distribuidor.
- Central: defensivo, salida de balón, corrector, cobertura.
- Laterales: defensivo, equilibrado, ofensivo, interior, carrilero.
- Mediocentro defensivo: pivote defensivo, organizador, recuperador, apoyo.
- Centrocampista central: organizador, todoterreno, recuperador, interior de apoyo, llegador.
- Mediapunta: organizador adelantado, llegador, segundo delantero, libre.
- Bandas/extremos: clásico, inverso, interior de banda, apoyo, goleador.
- Delantero centro/segundo delantero: rematador, apoyo, presionante, hombre objetivo, móvil, falso nueve. Segundo delantero también admite su preset específico de mediapunta.

La tabla de valores específicos de cada preset se genera a continuación desde el catálogo. Se aplican únicamente las instrucciones disponibles en cada fase; las restantes toman los valores neutros descritos.

| Rol | Código | Predeterminados específicos |
|---|---|---|
| Portero clásico | keeper_classic | Salidas del portero: Mantener posición; Distancia preferida: Mixtos |
| Portero líbero | keeper_sweeper | Salidas del portero: Adelantarse; Riesgo de pase: Medio |
| Portero distribuidor | keeper_distributor | Salidas del portero: Equilibradas; Distancia preferida: Corto; Riesgo de pase: Alto |
| Central defensivo | centre_defensive | Incorporaciones: Pocas; Riesgo de pase: Bajo; Seguimiento del rival: Mantener posición |
| Central con salida de balón | centre_playmaker | Distancia preferida: Largo; Riesgo de pase: Alto |
| Central corrector | centre_stopper | Intensidad de presión: Alto; Seguimiento del rival: Seguir al rival |
| Central de cobertura | centre_cover | Intensidad de presión: Bajo; Seguimiento del rival: Mantener posición; Participación en el repliegue: Recuperar posiciones |
| Lateral defensivo | back_defensive | Incorporaciones: Pocas; Participación ofensiva: Conservadora |
| Lateral equilibrado | back_balanced | Incorporaciones: Equilibradas; Anchura individual: Mantener posición |
| Lateral ofensivo | back_attacking | Incorporaciones: Frecuentes; Anchura individual: Abrirse; Centros: Más |
| Lateral interior | back_inverted | Anchura individual: Cerrarse; Distancia preferida: Corto; Riesgo de pase: Medio |
| Carrilero | wingback | Incorporaciones: Frecuentes; Movimientos: Buscar espacios; Conducción: Más; Centros: Más |
| Pivote defensivo | pivot_defensive | Incorporaciones: Pocas; Seguimiento del rival: Mantener posición; Riesgo de pase: Bajo |
| Pivote organizador | pivot_playmaker | Riesgo de pase: Alto; Distancia preferida: Mixtos; Movimientos: Mantener posición |
| Recuperador | ball_winner | Intensidad de presión: Alto; Agresividad en entradas: Agresiva; Seguimiento del rival: Seguir al rival |
| Mediocentro de apoyo | pivot_support | Distancia preferida: Corto; Incorporaciones: Equilibradas |
| Organizador | playmaker | Riesgo de pase: Alto; Libertad creativa: Alto |
| Centrocampista todoterreno | box_to_box | Incorporaciones: Frecuentes; Llegadas al área: Frecuentes; Participación en el repliegue: Recuperar posiciones |
| Interior de apoyo | interior_support | Movimientos: Buscar espacios; Distancia preferida: Corto |
| Centrocampista llegador | midfield_runner | Llegadas al área: Frecuentes; Frecuencia de disparo: Más; Desmarques: Al espacio |
| Organizador adelantado | advanced_playmaker | Riesgo de pase: Alto; Libertad creativa: Alto; Frecuencia de disparo: Menos |
| Mediapunta llegador | attacking_runner | Llegadas al área: Frecuentes; Desmarques: Al espacio |
| Segundo delantero | second_forward | Frecuencia de disparo: Más; Participación ofensiva: Agresiva |
| Mediapunta libre | free_ten | Movimientos: Libertad de movimiento; Libertad creativa: Alto |
| Extremo clásico | winger_classic | Anchura individual: Abrirse; Centros: Más |
| Extremo inverso | winger_inverted | Anchura individual: Cerrarse; Conducción: Más; Frecuencia de disparo: Más |
| Interior de banda | wide_midfielder | Incorporaciones: Pocas; Distancia preferida: Corto; Participación en el repliegue: Recuperar posiciones |
| Extremo de apoyo | winger_support | Desmarques: Al pie; Riesgo de pase: Medio; Frecuencia de disparo: Menos |
| Extremo goleador | winger_scorer | Llegadas al área: Frecuentes; Desmarques: Al espacio; Centros: Menos |
| Delantero rematador | poacher | Frecuencia de disparo: Más; Llegadas al área: Frecuentes; Desmarques: Al espacio |
| Delantero de apoyo | forward_support | Desmarques: Al pie; Distancia preferida: Corto; Frecuencia de disparo: Menos |
| Delantero presionante | pressing_forward | Intensidad de presión: Alto; Seguimiento del rival: Seguir al rival |
| Hombre objetivo | target_forward | Desmarques: Al pie; Movimientos: Mantener posición; Riesgo de pase: Bajo |
| Delantero móvil | mobile_forward | Movimientos: Buscar espacios; Conducción: Más |
| Falso nueve | false_nine | Movimientos: Libertad de movimiento; Libertad creativa: Alto; Riesgo de pase: Alto; Llegadas al área: Pocas |

## Cantera

La relación permanente hereda comportamientos por puesto y fase, presión avanzada y todas las órdenes colectivas, sin copiar futbolistas. Una táctica propia captura una copia independiente. Las personalizaciones del filial no modifican al primer equipo.

Capitanes, especialistas, tanda y familiaridad son propios de cada equipo inferior. Balón parado permite editar sus preferencias personales también estando sincronizado; el guardado rechaza modificar instrucciones heredadas. Gestión Directa/Delegada sigue independiente de Sincronizada/Propia.

## Modelos preparatorios

- `OpponentOrder`: encuentro, rival y jugador obligatorios; presión, marcaje, cierre de recepción, pierna menos hábil y entradas. `SqliteOpponentOrderStore` exige `IMatchPreparationContext`, que debe verificar el encuentro y devolver la plantilla rival. No existe proveedor de calendario ni pantalla funcional de análisis en producción. No se generan oponentes.
- `FamiliarityObservation`: jugador propio, aspecto (formación, rol, disposición colectiva o coordinación), configuración versionada, fecha y evidencia. Valor normalizado 0–1 como contrato de almacenamiento; sin fórmula de ganancia. Sin observación no existe porcentaje. `IFamiliarityObservations` y `SqliteFamiliarityStore` preparan la integración con Entrenamiento. Abrir o guardar una táctica nunca crea observaciones. Se rechazan rangos inválidos, evidencia vacía y fechas futuras. No se muestran porcentajes inventados.
- `TacticalMatchView.Read`: el motor futuro aporta plan activo y posesión real, y obtiene once, disposición, comportamientos, órdenes y especialistas. No hay cambios automáticos basados en marcadores, minutos o eventos ficticios.

## Persistencia

- `TacticalBehaviour001_Initial.sql`: extensión v1, comportamientos por plan/fase con FK al plan y validación del mapa en Core.
- `Tactics004_Personnel.sql`: esquema táctico v4, jerarquías personales, observaciones de familiaridad y órdenes por encuentro.

Las lecturas antiguas proyectan nuevas órdenes colectivas pendientes y roles compatibles sin escribir el archivo. Solo se migra transaccionalmente al guardar explícitamente o registrar una observación real. Se conservan lanzadores/rutinas previos. Cantera guarda los mapas en su JSON existente sin cambiar su esquema.

No se crea una publicación del mundo ni se reescriben contratos, jugadores o carreras personales por abrir el programa. La compatibilidad es hacia lectura de formatos antiguos; el ejecutable antiguo no reconoce el esquema táctico v4. La revisión optimista sigue rechazando guardados obsoletos.

## Inventario de esta ampliación

Nuevos:

- `src/Furvo.Core/Tactics/IndividualTactics.cs`: catálogo y reglas individuales.
- `src/Furvo.Core/Tactics/TacticalPreparation.cs`: especialistas, rival, familiaridad y consulta futura del motor.
- `src/Furvo.Persistence/SqliteFamiliarityStore.cs` y `SqliteOpponentOrderStore.cs`: puntos de persistencia preparatorios.
- Las dos migraciones indicadas arriba.
- `src/Furvo.Godot/Scripts/Main.IndividualTactics.cs`: panel lateral y jerarquías.
- `tests/Furvo.Tests/IndividualTacticsTests.cs`: cobertura de esta ampliación.
- Este documento.

Modificados:

- Core: `Tactics/TacticalPlans.cs`, `Tactics/TacticalSettings.cs`, `Academy/AcademyModels.cs`.
- Persistence: `SqliteTacticalPlanStore.cs`, `SqliteTacticsStore.cs`, `SqliteAcademyStore.cs`.
- Godot: `Scripts/Main.Lineups.cs`, `Scripts/Main.Tactics.cs`, `Scripts/Components/TacticDiagram.cs`, `Resources/UiStrings.resx`.
- Pruebas anteriores: `TacticsTests.cs` refleja las cuatro órdenes aprobadas; `TacticalInstructionExpansionTests.cs` reconstruye el formato v2 sin tablas posteriores y valida su migración hasta v4. No se eliminan ni desactivan pruebas.
- `tools/GodotUiSmoke.gd`: roles, personalización, confirmación, diagramas, capitanes y tres resoluciones. Desplaza candidatos antes de probar arrastres.
- README y documentación de arquitectura, inventario y validación.

Existían cambios de entregas anteriores al comenzar; este inventario no atribuye a la ampliación todos los archivos pendientes de Git.

## Validación local Windows

Desde la raíz:

```powershell
dotnet build Furvo.sln -c Debug
dotnet test Furvo.sln -c Debug --no-build
dotnet build Furvo.sln -c Release
dotnet test Furvo.sln -c Release --no-build
```

Abrir `src/Furvo.Godot/project.godot` con Godot 4.7.2 .NET y ejecutar F5. En una carrera de prueba, Equipo → Táctica: seleccionar titular, editar rol/órdenes, cambiar plan/fase, cancelar sustitución de personalizaciones, comprobar presión y jerarquías. Revisar también Cantera propia y sincronizada. El runner `tools/GodotUiSmoke.gd` usa carreras aisladas en `artifacts`.

Pendiente: efectos de partido, aprendizaje por entrenamientos, contexto real de rival, cambios automáticos de plan y revisión humana prolongada de usabilidad/teclado/escalado de Windows.


## Resultados ejecutados

Compilaciones finales Debug y Release correctas, sin errores ni advertencias. **366 pruebas superadas en cada configuración**, 0 fallos y 0 omitidas. Godot 4.7.2 .NET: recorrido nativo completo correcto, incluidas las regresiones de arrastre, planes, balón parado y Cantera, más `FURVO_ADVANCED_TACTICS_OK`; salida 0. Capturas revisadas de las tres resoluciones en `artifacts/career-visual/career-individual-*.png` y `career-advanced-defence-*.png`. El informe de ejecución está en `artifacts/advanced-native.log`. Las cinco bases iniciales coinciden con sus blobs de Git.

La primera ejecución de pruebas detectó un error transaccional de las jerarquías y supuestos obsoletos del catálogo; se corrigieron y todas las pruebas se repitieron. También se ajustó el desplazamiento de candidatos del runner y su navegación después de reabrir la carrera. No se han eliminado comprobaciones para obtener el resultado final.
