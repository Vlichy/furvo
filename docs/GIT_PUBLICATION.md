# Preparación inicial de GitHub — Furvo

## Remoto y conservación del historial

Remoto origin: https://github.com/Vlichy/furvo.git. La consulta autenticada confirma repositorio **privado**, rama predeterminada **main**, no archivado y permiso de escritura disponible. La lectura inicial contiene un único commit **725e06f46ef3a564cba72d3e2fc8ce871cfc9227**, cuyo único archivo es README.md.

La carpeta local no era un repositorio. Se inicializa main y se utiliza aquel commit como base antes de importar el proyecto. No se sobrescribe ningún archivo local al adoptar el historial: el README remoto breve se conserva en su commit y el README actual amplía la explicación del mismo proyecto. No se crean historiales desconectados, no se hace reset destructivo, no se reescribe historial ni se utiliza push --force.

Origin no existía localmente antes de configurarlo. Se conserva la configuración global de Git; el commit usa la identidad ya configurada. Su nombre/correo forman parte de los metadatos del commit, no de un archivo de credenciales. No se ha añadido una licencia nueva.

## Archivos incluidos y excluidos

Se incluyen solución/configuración .NET, cuatro proyectos activos, código, SQL de migraciones, pruebas, escenas, recursos y metadatos .uid/.svg.import de Godot, tareas portables de VS Code y documentación. Los documentos de desarrollo incluyen correspondencias internas con clubes reales, informes técnicos y propuestas históricas **no aprobadas**; su inclusión en Git no las convierte en datos publicados del juego.

Se excluyen bin/obj, .godot, registros, cachés, exportaciones, resultados de pruebas, backups (.old/.bak), la copia obsoleta src/furvo-(4.5), partidas/bases de usuario y archivos de credenciales. No se eliminan esos archivos locales. .gitignore permite expresamente solo estas cuatro bases de distribución:

| Publicación | SHA-256 local preservado |
| --- | --- |
| data/initial-world-2026-27.db | d4949e610ba88f4f80864c9a4637412063e411a61a9f6068e9902f9d176a2a0e |
| data/initial-world-2026-27.v2.db | d1c9a4905a49e1ea36c383d460f07739c34e4ee785699f84533c1b797a45c597 |
| data/initial-world-2026-27.v3.db | 4666ae27c4804abc130f6d13fce9b7cfc57cf8b96844309886b3b263a859991a |
| data/initial-world-2026-27.v4.db | 0f179b93b178f6db1891a5520c0dab900a7fe08edcf7a2635147a506610f8275 |

Se abrieron en modo de solo lectura y se comprobaron kind=initial y cero filas en save_metadata. v1/v2 contienen veinte clubes sin jugadores; v3/v4 contienen veinte clubes y 480 jugadores/contratos. .gitattributes evita conversiones de fin de línea en SQLite y normaliza texto a LF según .editorconfig.

## Revisión de privacidad

Revisión de rutas, extensiones, configuración y patrones habituales de claves/tokens/credenciales en los archivos candidatos. No se detectaron indicios de secretos. Los únicos rastros de rutas personales encontrados en la primera revisión estaban en logs de artifacts, excluidos. Se comprobaron además las exclusiones con rutas de ejemplo. Esta revisión por patrones no sustituye auditorías futuras ni autoriza incorporar secretos después.

## Publicación pendiente de confirmación

Se prepara un commit local descriptivo en main como descendiente del commit remoto. **No se ejecuta el primer push sin confirmación explícita del usuario.** Antes de publicarlo, refrescar origin y comprobar que no ha avanzado; si aparecen commits nuevos, integrarlos sin perder historial. Nunca usar force.

El comando preparado es:

~~~powershell
git push --set-upstream origin main
~~~

Mientras no se confirme, el remoto mantiene únicamente su commit inicial. Para revisar localmente:

~~~powershell
git status --short --branch
git log --oneline --graph --decorate --all
git show --stat --oneline HEAD
git diff origin/main..main --stat
~~~

## Validación del contenido preparado

Se extrajo el índice Git a artifacts/github-initial-check, una carpeta excluida, sin copiar bin/obj ni cachés. Sobre esa copia se ejecutaron dotnet build Furvo.sln (correcto, cero errores y cero advertencias) y dotnet test Furvo.sln --no-build --no-restore (129 superadas, cero fallidas y cero omitidas). Se comprobó también que los blobs SQLite del índice coinciden con los archivos locales y git diff --cached --check no informa problemas. No se modificó código funcional ni las bases de datos.

## Inventario exacto preparado

**136 archivos**: 8 en raíz, 3 de VS Code, 4 bases, 20 documentos/informes, 80 archivos de src, 17 de pruebas y 4 utilidades. Tamaño local aproximado antes de compresión: 8.76 MiB. Ningún archivo supera 5 MiB.

```text
.editorconfig
.gitattributes
.gitignore
.vscode/extensions.json
.vscode/launch.json
.vscode/tasks.json
Directory.Build.props
Furvo.sln
NuGet.Config
README.md
data/initial-world-2026-27.db
data/initial-world-2026-27.v2.db
data/initial-world-2026-27.v3.db
data/initial-world-2026-27.v4.db
docs/ARCHITECTURE.md
docs/CLUBS_2026_27.md
docs/CLUB_HISTORY_PROPOSAL.md
docs/CLUB_HISTORY_SCHEMA_PROPOSAL.md
docs/CLUB_SCREEN.md
docs/DECISIONS.md
docs/GIT_PUBLICATION.md
docs/INVENTORY.md
docs/PLAYER_SHEET.md
docs/PLAYER_SHEET_PANELS.md
docs/SQUADS_2026_27.md
docs/V1_CONTRACT.md
docs/VALIDATION.md
docs/WORLD_MODEL.md
docs/decisions/club-reputations-2026-27.json
docs/development/CLUB_CORRESPONDENCE_2026_27.md
docs/development/DELIVERY4_VALIDATION.json
docs/development/SQUAD_REPORT_2026_27.json
docs/development/club-reference-2026-27.json
docs/development/source-documentation-review.json
global.json
src/Furvo.Core/Domain/ClubId.cs
src/Furvo.Core/Domain/ClubProfile.cs
src/Furvo.Core/Domain/CoachId.cs
src/Furvo.Core/Domain/CompetitionId.cs
src/Furvo.Core/Domain/ContractId.cs
src/Furvo.Core/Domain/DomainRules.cs
src/Furvo.Core/Domain/Entities.cs
src/Furvo.Core/Domain/PlayerAttributes.cs
src/Furvo.Core/Domain/PlayerId.cs
src/Furvo.Core/Domain/PlayerPositions.cs
src/Furvo.Core/Domain/SaveId.cs
src/Furvo.Core/Domain/SeasonId.cs
src/Furvo.Core/Domain/WorldId.cs
src/Furvo.Core/Furvo.Core.csproj
src/Furvo.Core/Generation/DeterministicRandom.cs
src/Furvo.Core/Generation/InitialSquadGenerator.cs
src/Furvo.Core/Generation/SquadGenerationSettings.cs
src/Furvo.Core/Queries/ClubDetails.cs
src/Furvo.Core/Queries/WorldQueries.cs
src/Furvo.Core/Saves/ISaveStore.cs
src/Furvo.Core/Worlds/IWorldStore.cs
src/Furvo.Godot/Assets/Portraits/avatar.svg
src/Furvo.Godot/Assets/Portraits/avatar.svg.import
src/Furvo.Godot/Furvo.Godot.csproj
src/Furvo.Godot/Resources/UiStrings.resx
src/Furvo.Godot/Scenes/Main.tscn
src/Furvo.Godot/Scripts/Components/AttributeSection.cs
src/Furvo.Godot/Scripts/Components/AttributeSection.cs.uid
src/Furvo.Godot/Scripts/Components/AttributeValueStyle.cs
src/Furvo.Godot/Scripts/Components/AttributeValueStyle.cs.uid
src/Furvo.Godot/Scripts/Components/ClubEconomyPage.cs
src/Furvo.Godot/Scripts/Components/ClubEmblem.cs
src/Furvo.Godot/Scripts/Components/ClubHeader.cs
src/Furvo.Godot/Scripts/Components/ClubHistoryChart.cs
src/Furvo.Godot/Scripts/Components/ClubHistoryPage.cs
src/Furvo.Godot/Scripts/Components/ClubHistoryPlot.cs
src/Furvo.Godot/Scripts/Components/ClubInformationPage.cs
src/Furvo.Godot/Scripts/Components/ClubUi.cs
src/Furvo.Godot/Scripts/Components/DataTable.cs
src/Furvo.Godot/Scripts/Components/DataTable.cs.uid
src/Furvo.Godot/Scripts/Components/PitchPositionLayout.cs
src/Furvo.Godot/Scripts/Components/PitchPositionLayout.cs.uid
src/Furvo.Godot/Scripts/Components/PitchPositionsPanel.cs
src/Furvo.Godot/Scripts/Components/PitchPositionsPanel.cs.uid
src/Furvo.Godot/Scripts/Components/PlayerInsightsPanel.cs
src/Furvo.Godot/Scripts/Components/PlayerInsightsPanel.cs.uid
src/Furvo.Godot/Scripts/Components/PlayerPortrait.cs
src/Furvo.Godot/Scripts/Components/PlayerPortrait.cs.uid
src/Furvo.Godot/Scripts/Components/PlayerSheet.cs
src/Furvo.Godot/Scripts/Components/PlayerSheet.cs.uid
src/Furvo.Godot/Scripts/Components/PortraitResourcePaths.cs
src/Furvo.Godot/Scripts/Components/PortraitResourcePaths.cs.uid
src/Furvo.Godot/Scripts/Components/PositionInsightRules.cs
src/Furvo.Godot/Scripts/Components/PositionInsightRules.cs.uid
src/Furvo.Godot/Scripts/Components/PositionPitch.cs
src/Furvo.Godot/Scripts/Components/PositionPitch.cs.uid
src/Furvo.Godot/Scripts/Components/StadiumPreview.cs
src/Furvo.Godot/Scripts/Main.cs
src/Furvo.Godot/Scripts/Main.cs.uid
src/Furvo.Godot/Scripts/UiText.cs
src/Furvo.Godot/Scripts/UiText.cs.uid
src/Furvo.Godot/project.godot
src/Furvo.Persistence/AdjustedInitialWorld2026.cs
src/Furvo.Persistence/Data/club-reputations-2026-27.v2.json
src/Furvo.Persistence/Data/initial-squads-2026-27.json
src/Furvo.Persistence/Data/initial-world-2026-27.json
src/Furvo.Persistence/Data/initial-world-2026-27.v2.json
src/Furvo.Persistence/Furvo.Persistence.csproj
src/Furvo.Persistence/InitialSquads2026.cs
src/Furvo.Persistence/InitialWorld2026.cs
src/Furvo.Persistence/Migrations/001_Initial.sql
src/Furvo.Persistence/Migrations/002_World.sql
src/Furvo.Persistence/Migrations/003_ClubUniverseAndScales.sql
src/Furvo.Persistence/Migrations/004_PlayerPositionsAndGeneration.sql
src/Furvo.Persistence/PublishedWorldQueries.cs
src/Furvo.Persistence/SchemaCompatibilityException.cs
src/Furvo.Persistence/SqliteSaveStore.cs
src/Furvo.Persistence/SqliteWorldFiles.cs
src/Furvo.Persistence/SqliteWorldStore.cs
src/Furvo.Persistence/WorldDatabase.cs
tests/Furvo.Tests/AdjustedWorldTests.cs
tests/Furvo.Tests/ClubDetailsQueryTests.cs
tests/Furvo.Tests/ClubHistoryPlotTests.cs
tests/Furvo.Tests/ClubReferenceTests.cs
tests/Furvo.Tests/ClubUniverseTests.cs
tests/Furvo.Tests/Furvo.Tests.csproj
tests/Furvo.Tests/PitchPositionTests.cs
tests/Furvo.Tests/PlayerAttributeTests.cs
tests/Furvo.Tests/PlayerSheetPresentationTests.cs
tests/Furvo.Tests/PositionInsightsTests.cs
tests/Furvo.Tests/ReputationDecisionTests.cs
tests/Furvo.Tests/SaveStoreTests.cs
tests/Furvo.Tests/ScaleMigrationTests.cs
tests/Furvo.Tests/SquadGenerationTests.cs
tests/Furvo.Tests/SquadPersistenceTests.cs
tests/Furvo.Tests/WorldQueryTests.cs
tests/Furvo.Tests/WorldStoreTests.cs
tools/BuildAdjustedWorld.cs
tools/BuildInitialSquads.cs
tools/BuildInitialWorld.cs
tools/GodotUiSmoke.gd
```
