# Reorganización de Club y Delegaciones

La navegación de carrera ofrece Información, Estadio, Economía, Historia y récords, Instalaciones y Personal. Información conserva identidad, filosofía, directiva y síntesis histórica. Estadio concentra identidad, terreno, asistencia, los tres dibujos provisionales e instalaciones asociadas. La ficha pública conserva sus pestañas propias y añade Estadio.

La Economía de carrera y la pestaña Economía de la ficha pública comparten `ClubEconomyPage`. La carrera consulta su copia SQLite y la exploración consulta la publicación que tenga cargada. Antes coexistían dos accesos visibles dentro de la ficha de carrera. Ahora la navegación de carrera tiene un solo acceso visible a Economía y la consulta pública conserva el suyo. Los datos y las reglas económicas siguen en los mismos servicios.

Personal agrupa las responsabilidades actuales en Entrenamiento y Cantera. El primero muestra los nueve tipos de responsabilidad del equipo seleccionado. Cantera muestra gestión directa o delegada. Mercado, Economía, Comercial e Instalaciones se incorporarán cuando sus servicios puedan ejecutar responsabilidades. El [manual de Delegaciones](CLUB_DELEGATIONS.md) describe las asignaciones y sus fuentes.

Archivos de la intervención:

- `src/Furvo.Core/Careers/CareerWorkspace.cs` añade la ruta de Estadio y sus permisos de club.
- `src/Furvo.Godot/Scripts/Main.Navigation.cs` y `Main.cs` conectan las rutas y controlan las pestañas de carrera y exploración.
- `src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs` y el nuevo `ClubStadiumPage.cs` separan la presentación.
- `src/Furvo.Godot/Scripts/Main.ClubDelegations.cs` incorpora los departamentos y protege los cambios de equipo y departamento con ediciones pendientes.
- `src/Furvo.Godot/Resources/UiStrings.resx` concentra las nuevas etiquetas.
- `tools/GodotUiSmoke.gd` comprueba navegación, guardado, recarga y tamaño de los controles.

Se utilizan los modelos y tablas SQLite existentes. La reapertura automatizada de carreras usa el acceso aislado de QA. Cargar partida en el menú normal continúa deshabilitado.

El recorrido de Godot guarda capturas a 1280×720, 1600×900 y 1920×1080 en `artifacts/career-visual`. Los prefijos son `career-club-OwnInformation-`, `career-club-OwnStadium-`, `career-club-delegations-` y `career-club-delegations-academy-`. El recorrido confirma el aviso ante ediciones sin guardar, comprueba el guardado en ambos departamentos y vuelve a consultar el plan de Entrenamiento tras recargar la carrera. Un segundo proceso verifica la recuperación desde el archivo SQLite.
