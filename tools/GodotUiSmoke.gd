extends SceneTree
# Prueba de integración de la escena C# real, sin depender de coordenadas ni de una ventana visible.
# Ejecutar con Godot .NET después de compilar la solución; no escribe publicaciones ni partidas.

func find_named(node: Node, target: String) -> Node:
    if node.name == target:
        return node
    for child in node.get_children():
        var found = find_named(child, target)
        if found != null:
            return found
    return null

func find_trees(node: Node) -> Array[Tree]:
    var result: Array[Tree] = []
    if node is Tree:
        result.append(node)
    for child in node.get_children():
        result.append_array(find_trees(child))
    return result

func find_attribute_rows(node: Node) -> Array[Node]:
    var result: Array[Node] = []
    if node.has_meta("attribute_code"):
        result.append(node)
    for child in node.get_children():
        result.append_array(find_attribute_rows(child))
    return result

func find_marked_nodes(node: Node, key: String) -> Array[Node]:
    var result: Array[Node] = []
    if node.has_meta(key):
        result.append(node)
    for child in node.get_children():
        result.append_array(find_marked_nodes(child, key))
    return result

func require(condition: bool, message: String) -> bool:
    if not condition:
        push_error(message)
        quit(1)
    return condition

func _initialize() -> void:
    call_deferred("run_test")

func press_escape() -> void:
    var input = InputEventKey.new()
    input.keycode = KEY_ESCAPE
    input.pressed = true
    Input.parse_input_event(input)
    await process_frame
    var release = InputEventKey.new()
    release.keycode = KEY_ESCAPE
    release.pressed = false
    Input.parse_input_event(release)
    await process_frame

func capture_economy_screen(label: String) -> void:
    if not OS.get_cmdline_user_args().has("--capture") or DisplayServer.get_name() == "headless":
        return
    var directory = ProjectSettings.globalize_path("res://../../artifacts/"+("career-visual" if label.begins_with("career-") else "economy-visual"))
    DirAccess.make_dir_recursive_absolute(directory)
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png(directory.path_join(label + ".png"))

func check_career_layout(screen: Node, footer_key: String) -> bool:
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size = resolution
        await process_frame
        await process_frame
        if footer_key == "BackChooseClub":
            await capture_economy_screen("contract-"+str(resolution.x)+"x"+str(resolution.y))
            if OS.get_cmdline_user_args().has("--capture"):
                var scroll = find_named(screen,"ManagerOfferScroll") as ScrollContainer
                scroll.scroll_vertical = int(scroll.get_v_scroll_bar().max_value)
                await process_frame
                await process_frame
                await capture_economy_screen("contract-resources-"+str(resolution.x)+"x"+str(resolution.y))
                scroll.scroll_vertical = 0
                await process_frame
        var footer = find_named(screen,footer_key) as Control
        if not require(footer != null and footer.get_global_rect().position.x >= 0 and footer.get_global_rect().end.x <= root.size.x and footer.get_global_rect().end.y <= root.size.y,"Control inferior de carrera fuera de pantalla"):
            return false
    root.size = Vector2i(1280,720)
    await process_frame
    return true

func check_career_workspace(screen: Node, employed: bool) -> bool:
    find_named(screen,"NavArea_Home").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CareerContextNavigation") == null and find_named(screen,"NavPage_Desktop") == null and find_named(screen,"NavPage_Inbox") == null,"Inicio mantiene el submenú redundante"):
        return false
    var original_employer = find_named(screen,"CareerEmployer").text
    var original_date = find_named(screen,"CareerHeaderDate").text
    if not require(find_named(screen,"Home") == null,"La cabecera duplica el acceso al menú principal"):
        return false
    find_named(screen,"CareerGameMenu").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CareerSaveUnavailable").disabled and find_named(screen,"CareerLoadUnavailable").disabled,"Guardar o cargar se presenta como operativo"):
        return false
    if OS.get_cmdline_user_args().has("--capture") and DisplayServer.get_name() != "headless":
        var directory = ProjectSettings.globalize_path("res://../../artifacts/career-visual")
        DirAccess.make_dir_recursive_absolute(directory)
        await RenderingServer.frame_post_draw
        find_named(screen,"ExplorationMenu").get_texture().get_image().save_png(directory.path_join("career-game-menu.png"))
    find_named(screen,"ContinueExploration").emit_signal("pressed")
    await process_frame
    if not require(not find_named(screen,"ExplorationMenu").visible,"Continuar partida no cierra el menú"):
        return false
    if not require(find_named(screen,"CareerPrimaryNavigation").get_child_count() == 6,"No existen seis áreas de carrera"):
        return false
    if not require(find_named(screen,"CareerContinue").disabled and not find_named(screen,"CareerContinue").tooltip_text.is_empty(),"Continuar simula avance o no explica el bloqueo"):
        return false
    if not require(find_named(screen,"NavArea_Market").disabled and find_named(screen,"NavArea_Competition").disabled and find_named(screen,"NavArea_Team").disabled != employed,"Permisos visuales de carrera incorrectos"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size = resolution
        await process_frame
        await create_timer(0.1).timeout
        for key in ["CareerContinue","CareerHeaderInbox","CareerGameMenu","CareerPrimaryNavigation"]:
            var control = find_named(screen,key) as Control
            if not require(control.get_global_rect().position.x >= 0 and control.get_global_rect().end.x <= root.size.x+1 and control.get_global_rect().end.y <= root.size.y,"Cabecera o navegación fuera de pantalla"):
                return false
        if not require(find_named(screen,"CareerCoachName").get_global_rect().position.y >= 0 and find_named(screen,"CareerEmployer").get_global_rect().end.y <= find_named(screen,"CareerPrimaryNavigation").get_global_rect().position.y,"Identidad de entrenador fuera de la cabecera"):
            return false
        if not require(find_named(screen,"CareerBrand").size.x >= 70 and find_named(screen,"CareerBrand").size.y < 50 and not find_named(screen,"CareerBrand").get_global_rect().intersects(find_named(screen,"CareerCoachName").get_global_rect()),"La identidad de Furvo se parte o solapa la cabecera"):
            return false
        await capture_economy_screen("career-"+("employed-" if employed else "unemployed-")+str(resolution.x)+"x"+str(resolution.y))
    root.size = Vector2i(1280,720)
    find_named(screen,"CareerHeaderInbox").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CareerInbox").get_meta("entry_count") == 0,"Se generaron mensajes ficticios"):
        return false
    find_named(screen,"BackCareerDashboard").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CareerDashboard") != null,"La bandeja no vuelve al escritorio"):
        return false
    find_named(screen,"NavArea_Career").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CareerProfile") != null,"No se abre el perfil de carrera"):
        return false
    find_named(screen,"NavPage_Contract").emit_signal("pressed")
    await process_frame
    if not require((find_named(screen,"ManagerOfferDocument") != null) == employed,"Contrato incompatible con situación laboral"):
        return false
    if employed:
        find_named(screen,"NavArea_Team").emit_signal("pressed")
        await process_frame
        var table = find_trees(find_named(screen,"SquadTable"))[0]
        if not require(table.get_root().get_child_count() == 24,"Se perdió la plantilla de carrera"):
            return false
        if not require(table.columns == 12 and find_named(screen,"ClubTabs") == null and find_named(screen,"ClubHeader") == null,"Plantilla conserva pestañas o cabecera institucional"):
            return false
        var expected_columns = ["Rol","Posición","Jugador","Calidad","Estado físico","Ritmo","Moral","Forma (últimos 5)","PJ","G","A","Media"]
        for column in range(12):
            if not require(table.get_column_title(column).begins_with(expected_columns[column]),"Orden de columnas de plantilla incorrecto"):
                return false
        if not require(table.get_column_title(1).contains("↑"),"El orden inicial no es por posición"):
            return false
        var ranks = ["POR","DFC","LD","LI","MCD","MC","MP","MD","MI","ED","EI","SD","DC"]
        var previous_rank = -1
        for row in table.get_root().get_children():
            var rank = ranks.find(row.get_text(1))
            if not require(rank >= previous_rank,"Orden futbolístico inicial incorrecto"):
                return false
            previous_rank = rank
            if not require(row.get_text(0) in ["Titular","Suplente","Reserva"] and row.get_meta("cell_text_3") == "—" and row.get_meta("cell_text_4") == "—","Asignación ausente o calidad/condición inventada"):
                return false
            for column in [3,4,5,7]:
                if not require(row.get_meta("visual_elements_"+str(column)) == 5,"El indicador no contiene cinco elementos"):
                    return false
            if not require(row.get_meta("visual_elements_6") == 1 and row.get_meta("visual_6") == "Morale","Moral no utiliza círculo y descripción"):
                return false
            for column in [8,9,10]:
                if not require(row.get_text(column) == "0","Se inventaron estadísticas iniciales"):
                    return false
            if not require(row.get_text(11) == "—","Se inventó una nota media"):
                return false
        for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
            root.size = resolution
            await create_timer(0.1).timeout
            if not require(table.get_global_rect().end.x <= root.size.x+1 and find_named(screen,"ViewPlayer").get_global_rect().end.y <= root.size.y+1,"La plantilla o navegación queda fuera de pantalla"):
                return false
            var first_height = table.get_item_area_rect(table.get_root().get_first_child()).size.y
            for row in table.get_root().get_children():
                if not require(absf(table.get_item_area_rect(row).size.y-first_height) < 1,"Las filas de plantilla no tienen altura uniforme"):
                    return false
            await capture_economy_screen("career-squad-"+str(resolution.x)+"x"+str(resolution.y))
        root.size = Vector2i(1280,720)
        table.get_root().get_first_child().select(0)
        table.emit_signal("item_selected")
        var selected_player_id = table.get_selected().get_metadata(0)
        for direction in range(2):
            table.emit_signal("column_title_clicked",2,MOUSE_BUTTON_LEFT)
            await process_frame
            table = find_trees(find_named(screen,"SquadTable"))[0]
            if not require(table.get_selected().get_metadata(0) == selected_player_id,"La ordenación de plantilla cambia la identidad seleccionada"):
                return false
        find_named(screen,"ViewPlayer").emit_signal("pressed")
        await process_frame
        if not require(find_attribute_rows(screen).size() == 51 and find_named(screen,"CareerPrimaryNavigation") != null,"Se perdió la ficha o navegación de carrera"):
            return false
        find_named(screen,"BackSquad").emit_signal("pressed")
        await process_frame
        table = find_trees(find_named(screen,"SquadTable"))[0]
        if not require(table.get_selected().get_metadata(0) == selected_player_id,"Volver de la ficha pierde la selección"):
            return false
        table.emit_signal("item_activated")
        await process_frame
        if not require(find_attribute_rows(screen).size() == 51,"No se abre el futbolista por activación de su nombre/fila"):
            return false
        find_named(screen,"BackSquad").emit_signal("pressed")
        await process_frame
        if not await check_lineup(screen):
            return false
        find_named(screen,"NavArea_Club").emit_signal("pressed")
        await process_frame
        find_named(screen,"NavPage_OwnEconomy").emit_signal("pressed")
        await process_frame
        if not require(find_named(screen,"EconomyCashValue").text != "No disponible","No se consulta la economía de carrera"):
            return false
        find_named(screen,"NavPage_OwnHistory").emit_signal("pressed")
        await process_frame
        if not require(find_named(screen,"ClubHistoryPage") != null,"No se conserva Historia"):
            return false
    else:
        if not require(find_named(screen,"CareerNoContract") != null,"El entrenador libre muestra contrato activo"):
            return false
    find_named(screen,"NavArea_Club").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"NavPage_PublicClubs") == null and find_named(screen,"ChooseClub") == null and find_named(screen,"StartUnemployed") == null and find_named(screen,"CareerEmployer").text == original_employer,"La carrera conserva un selector o cambia empleador"):
        return false
    if not require((find_named(screen,"ClubInformationPage") != null) == employed,"Club no abre el equipo contratado o atribuye uno al entrenador libre"):
        return false
    if not employed and not require(find_named(screen,"NoClubInformation") != null,"Falta el estado explícito Sin club"):
        return false
    await capture_economy_screen("career-own-club" if employed else "career-no-club")
    find_named(screen,"NavArea_Home").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CareerHeaderDate").text == original_date,"La navegación cambió la fecha"):
        return false
    if not employed:
        if not require(find_named(screen,"EconomyTransferBudgetValue") == null and find_named(screen,"CareerOffersList").get_meta("entry_count") == 0 and find_named(screen,"CareerVacanciesList").get_meta("entry_count") == 0,"El escritorio libre asigna recursos u ofertas ficticias"):
            return false
    print("FURVO_WORKSPACE_OK: "+("contratado" if employed else "en paro")+", seis áreas, permisos, consulta, contrato y fecha estable")
    return true

func check_lineup(screen: Node) -> bool:
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    if not require(find_marked_nodes(screen,"slot_id").size() == 11,"Faltan puestos en la alineación"):
        return false
    var first = find_marked_nodes(screen,"slot_id")[0]
    first.emit_signal("mouse_entered")
    await process_frame
    if not require(find_named(screen,"LineupCandidateGroup0") != null and find_named(screen,"LineupCandidateGroup1") != null,"Hover no actualiza candidatos"):
        return false
    first.emit_signal("pressed")
    var position_text = find_named(screen,"LineupCandidatePosition").text
    find_marked_nodes(screen,"slot_id")[1].emit_signal("mouse_entered")
    await process_frame
    if not require(find_named(screen,"LineupCandidatePosition").text == position_text,"El hover pierde la selección fijada"):
        return false
    var old_player = first.get_meta("player_id")
    for candidate in find_marked_nodes(screen,"eligibility"):
        if candidate.get_meta("player_id") != old_player:
            candidate.emit_signal("pressed")
            break
    await process_frame
    if not require(find_named(screen,"LineupPage").get_meta("lineup_revision") == 1,"No se guardó la sustitución"):
        return false
    var picker = find_named(screen,"LineupFormation")
    picker.select((picker.selected+1)%picker.item_count)
    picker.emit_signal("item_selected",picker.selected)
    await process_frame
    if not require(find_named(screen,"LineupPage").get_meta("lineup_revision") == 2,"No se guardó la formación"):
        return false
    var ids: Array = []
    for circle in find_marked_nodes(screen,"slot_id"):
        ids.append(circle.get_meta("player_id"))
    if not require(ids.size() == 11 and ids.count(ids[0]) == 1,"Once inválido"):
        return false
    find_marked_nodes(screen,"slot_id")[0].emit_signal("pressed")
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size = resolution
        await create_timer(0.2).timeout
        var pitch = find_named(screen,"LineupPitch")
        var side = find_named(screen,"LineupCandidateScroll")
        if not require(not pitch.get_global_rect().intersects(side.get_global_rect()) and side.get_global_rect().end.x <= root.size.x+1 and find_named(screen,"LineupSummary").get_global_rect().end.y <= root.size.y+1,"Desbordamiento de alineación"):
            return false
        await capture_economy_screen("career-lineup-"+str(resolution.x)+"x"+str(resolution.y))
    root.size = Vector2i(1280,720)
    find_named(screen,"NavPage_OwnSquad").emit_signal("pressed")
    await process_frame
    var counts = {"Titular":0,"Suplente":0,"Reserva":0}
    for row in find_trees(find_named(screen,"SquadTable"))[0].get_root().get_children():
        counts[row.get_text(0)] += 1
    if not require(counts == {"Titular":11,"Suplente":7,"Reserva":6},"Roles de plantilla incoherentes"):
        return false
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"LineupPage").get_meta("lineup_revision") == 2,"Abrir pantalla regeneró alineación"):
        return false
    print("FURVO_LINEUP_OK: hover, selección fijada, sustitución, formación, persistencia, roles y tres resoluciones")
    return true

func return_career_main_menu(screen: Node) -> void:
    find_named(screen,"CareerGameMenu").emit_signal("pressed")
    await process_frame
    find_named(screen,"ReturnMainMenu").emit_signal("pressed")
    await process_frame

func run_test() -> void:
    root.size = Vector2i(1280, 720)
    var screen = load("res://Scenes/Main.tscn").instantiate()
    var test_root = ProjectSettings.globalize_path("res://../../artifacts/career-ui-" + str(Time.get_ticks_usec()))
    screen.ConfigureCareerStorage(test_root)
    root.add_child(screen)
    await process_frame
    if not require(find_named(screen, "NewGame") != null, "Falta pantalla inicial"):
        return
    if not require(find_named(screen, "LoadGame").disabled and find_named(screen, "Options") != null and find_named(screen, "ExitGame") != null, "Faltan opciones del menú principal"):
        return
    find_named(screen, "Options").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen, "DisplayOptions") != null, "No abren las opciones del menú principal"):
        return
    find_named(screen, "CancelOptions").emit_signal("pressed")
    await process_frame
    find_named(screen, "NewGame").emit_signal("pressed")
    for attempt in range(200):
        if find_named(screen,"CoachProfileForm") != null:
            break
        await create_timer(0.05).timeout
    if not require(find_named(screen,"CoachProfileForm") != null,"No ofrece crear el primer entrenador"):
        return
    var backgrounds = find_named(screen,"CoachExperience")
    if not require(backgrounds.item_count == 2 and backgrounds.get_item_id(0) == 1 and backgrounds.get_item_id(1) == 2,"Antecedentes no permitidos para nuevas altas"):
        return
    if not await check_career_layout(screen,"SaveCoachProfile"):
        return
    find_named(screen,"SaveCoachProfile").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"CoachFormError").text != "" and not FileAccess.file_exists(test_root.path_join("coach-profiles/profiles.db")),"Se guardó un perfil incompleto"):
        return
    find_named(screen,"CoachFirstName").text = "Prueba"
    find_named(screen,"CoachLastName").text = "Interfaz"
    find_named(screen,"CoachBirthDate").text = "01/01/1980"
    find_named(screen,"CoachBirthCity").text = "Ciudad de prueba"
    find_named(screen,"CoachNationality").select(1)
    find_named(screen,"SaveCoachProfile").emit_signal("pressed")
    find_named(screen,"SaveCoachProfile").emit_signal("pressed")
    for attempt in range(200):
        if find_named(screen,"SelectCoachProfile") != null:
            break
        await create_timer(0.05).timeout
    if not require(find_marked_nodes(screen,"profile_id").size() == 1,"La creación por doble pulsación duplicó o perdió el perfil"):
        return
    if not await check_career_layout(screen,"CreateCoachProfile"):
        return
    find_named(screen,"SelectCoachProfile").emit_signal("pressed")
    await process_frame
    # La lectura SQLite es asíncrona. Se espera por condición con límite temporal, no por una pausa fija.
    for attempt in range(200):
        if find_named(screen, "ViewClub") != null:
            break
        if find_named(screen, "Retry") != null:
            require(false, "La publicación no pudo cargarse")
            return
        await create_timer(0.05).timeout
    if not require(find_named(screen, "ViewClub") != null, "Tiempo de carga agotado"):
        return
    # Se consulta una ficha real de tres clubes distintos, sin generar ni alterar datos.
    var hidden = ["professionalism", "ambition", "consistency", "big_matches", "adaptability", "injury_proneness", "pressure_resistance", "temperament", "loyalty", "learning_ability"]
    var opened_ids: Array[String] = []
    for sample_index in range(4):
        var club_index = [0, 0, 6, 19][sample_index]
        var clubs = find_trees(screen)[0]
        if not require(clubs.get_root().get_child_count() == 20, "Se esperaban veinte clubes"):
            return
        if sample_index == 0:
            await process_frame
            var total_width = 0.0
            for column in range(3):
                total_width += clubs.get_column_width(column)
            var actual_ratios: Array[float] = []
            for column in range(3):
                var ratio = clubs.get_column_width(column) / total_width
                actual_ratios.append(ratio)
                if not require(absf(ratio - [0.45, 0.30, 0.25][column]) < 0.005, "Proporción incorrecta de columna"):
                    return
            print("FURVO_CLUB_COLUMNS: ", actual_ratios)
            if not require(clubs.get_column_title_alignment(0) == HORIZONTAL_ALIGNMENT_LEFT and clubs.get_column_title_alignment(1) == HORIZONTAL_ALIGNMENT_CENTER and clubs.get_column_title_alignment(2) == HORIZONTAL_ALIGNMENT_CENTER, "Alineación de encabezados incorrecta"):
                return
            var fragua_found = false
            for row in clubs.get_root().get_children():
                if not require(row.get_text_alignment(0) == HORIZONTAL_ALIGNMENT_LEFT and row.get_text_alignment(1) == HORIZONTAL_ALIGNMENT_CENTER and row.get_text_alignment(2) == HORIZONTAL_ALIGNMENT_CENTER, "Alineación de valores incorrecta"):
                    return
                if row.get_metadata(0) == "72026027-0003-4000-8000-000000000005":
                    fragua_found = row.get_text(2) == "2"
            if not require(fragua_found, "Fragua del Sur debe mostrar reputación 2 en la publicación activa"):
                return
        var expected_club_id = clubs.get_root().get_child(club_index).get_metadata(0)
        clubs.get_root().get_child(club_index).select(0)
        clubs.emit_signal("item_selected")
        if not require(not find_named(screen, "ViewClub").disabled, "La selección no habilita la plantilla"):
            return
        if club_index == 6:
            clubs.emit_signal("item_activated") # Mismo evento que doble clic o Enter.
        else:
            find_named(screen, "ViewClub").emit_signal("pressed")
        await process_frame
        var club_tabs = find_named(screen, "ClubTabs") as TabContainer
        if not require(club_tabs != null and club_tabs.get_tab_count() == 4 and club_tabs.current_tab == 0, "El club no abre Información con cuatro pestañas"):
            return
        if not require([club_tabs.get_tab_title(0), club_tabs.get_tab_title(1), club_tabs.get_tab_title(2), club_tabs.get_tab_title(3)] == ["Información", "Plantilla", "Historia", "Economía"], "Orden de pestañas incorrecto"):
            return
        if not require(find_named(screen, "ClubHeader").get_meta("club_id") == expected_club_id, "La cabecera pertenece a otro club"):
            return
        if not require(find_named(screen, "ClubFoundationYearValue").text != "Pendiente de definir" and find_named(screen, "StadiumCapacityValue").text != "Pendiente de definir", "No se cargaron los datos existentes"):
            return
        for pending in ["ClubSubscribersValue", "ClubNicknameValue", "ClubRivalriesValue", "StadiumCityValue"]:
            if not require(find_named(screen, pending).text == "Pendiente de definir", "Se presentó un dato pendiente como real"):
                return
        var info_page = find_named(screen, "ClubInformationPage")
        if not require(find_named(info_page, "FootballPitch") != null and find_named(info_page, "FootballPitch").get_meta("end_area_count") == 4, "Falta el campo horizontal corregido"):
            return
        for view in ["StadiumExterior", "StadiumInterior", "StadiumPitchCaption", "StadiumExteriorCaption", "StadiumInteriorCaption"]:
            if not require(find_named(info_page, view) != null, "Falta una vista o descripción de estadio"):
                return
        if not require(find_named(screen, "ClubKits").get_child(0).get_child_count() == 3, "Se esperaban tres equipaciones"):
            return
        for pending in ["ClubAssociationsValue", "ClubOwnershipValue", "ClubPresidentValue", "ClubOwnerValue", "ClubGroupValue", "StadiumCategoryValue", "StadiumDimensionsValue", "StadiumOpeningValue"]:
            if not require(find_named(info_page, pending).text == "Pendiente de definir", "Se inventaron datos institucionales o de estadio"):
                return
        for section in ["HistoryTrajectoryPanel", "HistoryLegendsPanel", "HistoryRecordsPanel"]:
            if not require(find_named(info_page, section) != null, "Falta una sección de síntesis histórica"):
                return
        if not require(find_named(info_page, "HistoryHonoursPanel") == null and find_named(find_named(screen, "ClubHistoryPage"), "HistoryLegendsPanel") == null, "Palmarés o Leyendas aparecen en la pestaña equivocada"):
            return
        var direction_panels = find_named(info_page, "ClubDirectionPanels")
        if not require(direction_panels != null and direction_panels.get_meta("club_id") == expected_club_id and direction_panels.get_meta("proposal_status") == "draft", "Faltan paneles de borrador para el club seleccionado"):
            return
        if not require(find_marked_nodes(direction_panels, "principle_code").size() == 4 and find_marked_nodes(direction_panels, "objective_code").size() == 3, "Faltan principios u objetivos de la propuesta"):
            return
        for panel in ["ClubPhilosophyPanel", "ClubBoardExpectationsPanel"]:
            if not require(find_named(direction_panels, panel) != null and "Borrador" in find_named(find_named(direction_panels, panel), "DirectionDraftNotice").text, "La propuesta no se identifica como borrador"):
                return
        for tab_index in [2, 3, 0, 1]:
            club_tabs.current_tab = tab_index
            await process_frame
            if not require(club_tabs.get_child(tab_index).is_visible_in_tree() and club_tabs.get_child(tab_index).get_meta("club_id") == expected_club_id, "La pestaña no conserva el club seleccionado"):
                return
            if sample_index == 0:
                for resolution in [Vector2i(960, 640), Vector2i(1600, 900), Vector2i(1920, 1080), Vector2i(1280, 720)]:
                    root.size = resolution
                    await process_frame
                    await process_frame
                    if tab_index == 0:
                        var overview = find_named(info_page, "ClubOverviewColumns")
                        if not require(direction_panels.vertical == (resolution.x < 1040), "Los paneles de filosofía no se adaptan al ancho"):
                            return
                        if not require(direction_panels.get_global_rect().position.y >= overview.get_global_rect().end.y - 1 and direction_panels.get_global_rect().end.y <= find_named(info_page, "ClubHistorySummary").get_global_rect().position.y + 1, "Los paneles de filosofía no están entre identidad e historia"):
                            return
                        if not direction_panels.vertical and not require(direction_panels.get_child(0).get_global_rect().end.x <= direction_panels.get_child(1).get_global_rect().position.x + 1, "Los paneles de filosofía se solapan"):
                            return
                        if not require(overview.vertical == (resolution.x < 1040), "La composición institucional no se adapta al ancho"):
                            return
                        if not overview.vertical:
                            var left_panel = find_named(info_page, "ClubInstitutionPanel") as Control
                            var right_panel = find_named(info_page, "ClubStadiumPanel") as Control
                            if not require(left_panel.get_global_rect().end.x <= right_panel.get_global_rect().position.x + 1, "Identidad y estadio se solapan"):
                                return
                        var view_row = find_named(info_page, "StadiumViews")
                        var previous_view: Control = null
                        for view_column in view_row.get_children():
                            if previous_view != null and not require(previous_view.get_global_rect().end.x <= view_column.get_global_rect().position.x + 1, "Las vistas del estadio se solapan"):
                                return
                            previous_view = view_column
                        var info_scroll = find_named(info_page, "ClubInfoScroll") as ScrollContainer
                        info_scroll.scroll_vertical = int(info_scroll.get_v_scroll_bar().max_value)
                        await process_frame
                        await process_frame
                        if not require(find_named(info_page, "ClubHistorySummary").get_global_rect().end.y <= info_scroll.get_global_rect().end.y + 1, "La síntesis inferior no es accesible con desplazamiento"):
                            return
                        print("FURVO_CLUB_REDESIGN_LAYOUT: ", resolution, " footer visible; stadium views=3")
                    var club_footer = find_named(screen, "BackClubs") as Control
                    var choose = find_named(screen, "ChooseClub") as Button
                    var footer = find_named(screen, "ClubFooter") as Control
                    if not require(choose.is_visible_in_tree() and footer.get_global_rect().has_point(choose.get_global_rect().get_center()) and choose.get_global_rect().end.y <= root.size.y - 1, "Elegir este club queda fuera de la navegación"):
                        return
                    if not require(club_footer.get_global_rect().end.x <= choose.get_global_rect().position.x or club_footer.get_global_rect().end.y <= choose.get_global_rect().position.y, "Los botones se solapan"):
                        return
                    var active_page = club_tabs.get_child(tab_index)
                    if tab_index in [0, 2]:
                        var record_panel = find_named(active_page, "HistoryRecordsPanel") as Control
                        for field_key in ["HistoryCoachAppearances", "HistoryAdditionalRecords"]:
                            var value = find_named(active_page, field_key + "Value") as Control
                            var label = find_named(active_page, field_key + "Label") as Control
                            if not require(record_panel.is_ancestor_of(value) and record_panel.get_global_rect().encloses(value.get_global_rect()) and record_panel.get_global_rect().encloses(label.get_global_rect()), "Un récord queda fuera de su tarjeta"):
                                return
                            if not require(value.get_global_rect().position.x - label.get_global_rect().end.x <= 13, "Separación excesiva entre etiqueta y valor de récord"):
                                return
                    if tab_index == 1:
                        var squad_tree = find_trees(find_named(screen, "SquadTable"))[0]
                        var width_sum = 0.0
                        for column in range(4):
                            width_sum += squad_tree.get_column_width(column)
                        for column in range(4):
                            if not require(absf(squad_tree.get_column_width(column)/width_sum - [0.35,0.30,0.10,0.25][column]) < 0.006, "Proporción incorrecta de Plantilla"):
                                return
                        if not require(squad_tree.get_column_title_alignment(0) == HORIZONTAL_ALIGNMENT_LEFT and squad_tree.get_column_title_alignment(1) == HORIZONTAL_ALIGNMENT_LEFT and squad_tree.get_column_title_alignment(2) == HORIZONTAL_ALIGNMENT_CENTER, "Encabezados de Plantilla desalineados"):
                            return
                        var description = find_named(screen, "SquadDescription") as Label
                        if not require(description.get_global_rect().position.x >= 20 and description.get_global_rect().end.x <= root.size.x - 20 and description.text.begins_with("24 futbolistas"), "Cabecera de Plantilla cortada o incorrecta"):
                            return
                        var expected_height = -1.0
                        for item in squad_tree.get_root().get_children():
                            if not require(item.get_text_alignment(0) == HORIZONTAL_ALIGNMENT_LEFT and item.get_text_alignment(1) == HORIZONTAL_ALIGNMENT_LEFT and item.get_text_alignment(2) == HORIZONTAL_ALIGNMENT_CENTER, "Celdas de Plantilla desalineadas"):
                                return
                            var row_height = squad_tree.get_item_area_rect(item).size.y
                            if expected_height < 0:
                                expected_height = row_height
                            if not require(absf(row_height - expected_height) < 1, "Las filas de Plantilla no son uniformes"):
                                return
                        var view_player = find_named(screen, "ViewPlayer") as Control
                        if not require(view_player.is_visible_in_tree() and footer.get_global_rect().encloses(view_player.get_global_rect()), "Ver futbolista queda fuera del pie"):
                            return
                    if not require(club_footer.get_global_rect().end.y <= root.size.y + 1 and club_tabs.get_global_rect().end.y <= club_footer.get_global_rect().position.y + 1, "La ficha de club oculta o solapa la navegación al redimensionar"):
                        return
            if tab_index == 2:
                var historic_table = find_trees(find_named(screen, "HistoryStandingsTable"))[0]
                if not require(historic_table.get_root().get_child_count() == 0 and find_named(screen, "ClubHistoryChart").get_meta("result_count") == 0, "Se inventaron clasificaciones históricas"):
                    return
            if tab_index == 3:
                var economy = find_named(screen, "ClubEconomyPage")
                var season_selector = find_named(economy,"EconomySeasonSelection") as OptionButton
                if not require(season_selector.item_count == 1 and season_selector.get_item_text(0) == "2026/27" and season_selector.disabled, "El selector inventa temporadas o permite cambiar una temporada única"):
                    return
                var indicators = find_named(economy,"EconomyIndicators")
                if not require(find_marked_nodes(indicators,"economic_indicator").size() == 4 and find_named(indicators,"EconomySituationIndicatorValue").text == "No evaluado", "Indicadores financieros incompletos o estado inventado"):
                    return
                if not require(find_named(find_named(economy,"EconomyObligationsPanel"),"EconomyInterestValue") == null and find_named(find_named(economy,"EconomyFinancialExpensesPanel"),"EconomyInterestValue") != null, "Los intereses se duplican o están fuera de Gastos financieros"):
                    return
                if not require(find_named(economy,"EconomyRemainingBudgetLabel").text == "Fichajes restante" and find_named(economy,"EconomyRemainingWagesLabel").text == "Margen salarial restante" and find_named(economy,"EconomySubscribersLabel").text == "Abonos", "Terminología presupuestaria incorrecta"):
                    return
                for section in ["EconomyBudgetsPanel", "EconomyIncomePanel", "EconomyExpensesPanel", "EconomyObligationsPanel", "EconomyFinancialEvolutionPanel"]:
                    if not require(find_named(economy, section) != null, "Falta un bloque económico"):
                        return
                var economic_fields = find_marked_nodes(economy, "economic_unavailable")
                if not require(economic_fields.size() > 0 and economic_fields.size() < 34 and find_named(economy,"EconomyCashValue").text != "No disponible", "No se cargaron recursos económicos aprobados"):
                    return
                for field in economic_fields:
                    if not require(field.text == "No disponible", "Se inventó un importe financiero"):
                        return
                for amount in ["EconomyCommittedWagesValue", "EconomyRemainingWagesValue", "EconomyBalanceValue"]:
                    if not require(find_named(screen, amount).text == "No disponible", "Se inventó una cifra económica"):
                        return
                for amount in ["EconomyCashValue","EconomyTransferBudgetValue","EconomyWageBudgetValue","EconomyDebtValue","EconomyTelevisionValue","EconomyStaffValue"]:
                    if not require(find_named(economy,amount).text.ends_with(" EUR"),"Falta una cifra económica aprobada en EUR"):
                        return
                if not require(find_named(economy,"ForecastNotice") != null,"No se distingue la previsión del resultado real"):
                    return
                if sample_index == 0 and OS.get_cmdline_user_args().has("--capture"):
                    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
                        root.size = resolution
                        await process_frame
                        await process_frame
                        await capture_economy_screen("economy-"+str(resolution.x)+"x"+str(resolution.y))
                    root.size = Vector2i(1280,720)
                    await process_frame
        if not require(find_named(screen, "ViewPlayer").visible, "No se puede abrir un futbolista desde Plantilla"):
            return
        var squad = find_trees(screen)[0]
        if not require(squad.get_root().get_child_count() == 24, "Se esperaban veinticuatro jugadores"):
            return
        var initial_roles = ["Portero", "Defensa central", "Lateral derecho", "Lateral izquierdo", "Mediocentro defensivo", "Centrocampista central", "Mediapunta", "Centrocampista derecho", "Centrocampista izquierdo", "Delantero centro", "Extremo derecho", "Extremo izquierdo", "Segundo delantero"]
        var prior_role = -1
        for row in squad.get_root().get_children():
            var role_index = initial_roles.find(row.get_text(1))
            if not require(role_index >= prior_role and role_index >= 0, "Plantilla no abre por grupos/posiciones o muestra Banda"):
                return
            prior_role = role_index
        if sample_index == 0:
            # Esc se inyecta como entrada real del árbol; no se llama directamente a métodos C# de menú.
            await press_escape()
            var pause = find_named(screen,"ExplorationMenu")
            if not require(pause != null and pause.visible, "Esc no abre el menú de exploración"):
                return
            await press_escape()
            if not require(not pause.visible, "Esc apila menús o no permite continuar"):
                return
            await press_escape()
            find_named(pause,"Options").emit_signal("pressed")
            await process_frame
            if not require(find_named(screen,"DisplayOptions") != null and not pause.visible, "Opciones se superpone a otro menú exclusivo"):
                return
            find_named(screen,"CancelOptions").emit_signal("pressed")
            await process_frame
            if not require(pause.visible, "Cancelar opciones no vuelve al menú Esc"):
                return
            find_named(pause,"ContinueExploration").emit_signal("pressed")
            await process_frame
            if not require(not pause.visible and find_named(screen,"ClubTabs") == club_tabs and club_tabs.current_tab == 1, "Continuar pierde club o pestaña"):
                return
            for column in [0, 1, 2]:
                for direction in range(2):
                    squad.emit_signal("column_title_clicked", column, MOUSE_BUTTON_LEFT)
                    await process_frame
                    squad = find_trees(screen)[0]
                    if not require(squad.get_root().get_child_count() == 24, "Ordenar perdió jugadores"):
                        return
                    if column == 2:
                        var rows = squad.get_root().get_children()
                        for index in range(1, rows.size()):
                            var previous = int(rows[index - 1].get_text(2))
                            var current = int(rows[index].get_text(2))
                            if not require(previous <= current if direction == 0 else previous >= current, "Orden de edad incorrecto"):
                                return
        var selected_row = squad.get_root().get_first_child()
        if sample_index == 0:
            selected_row = null
            for row in squad.get_root().get_children():
                if row.get_text(1) == "Portero":
                    selected_row = row
                    break
        elif sample_index == 1:
            # Fixture existente de Mosaico: mediocentro central con dos posiciones secundarias registradas.
            selected_row = null
            for row in squad.get_root().get_children():
                if row.get_text(0) == "Adrián Alcendor Vallescuro":
                    selected_row = row
                    break
        if not require(selected_row != null, "No se encontró el futbolista de referencia"):
            return
        var selected_id = selected_row.get_metadata(0)
        var expected_name = selected_row.get_text(0)
        opened_ids.append(str(selected_id))
        selected_row.select(0)
        squad.emit_signal("item_selected")
        if not require(not find_named(screen, "ViewPlayer").disabled, "La selección no habilita la ficha"):
            return
        find_named(screen, "ViewPlayer").emit_signal("pressed")
        await process_frame
        if not require(find_named(screen, "PlayerName").text == expected_name, "La ficha no coincide con la selección"):
            return
        if not require(find_named(screen, "AttributeTabs") == null, "No debe existir una pestaña de portero"):
            return
        var columns = find_named(screen, "AttributeColumns") as GridContainer
        var third = find_named(screen, "PhysicalAndGoalkeeper")
        if not require(columns != null and columns.columns == 3 and columns.get_child_count() == 3, "Se esperaban tres columnas"):
            return
        if not require(third.get_child_count() == 2 and third.get_child(0).name == "Category_Physical" and third.get_child(1).name == "Category_Goalkeeper", "Portero debe estar debajo de físicos"):
            return
        await process_frame
        await process_frame
        var general = find_named(screen, "AttributesOverview") as ScrollContainer
        print("FURVO_SHEET_LAYOUT: ", root.size, " content=", general.get_v_scroll_bar().max_value, " page=", general.get_v_scroll_bar().page)
        var attributes = find_attribute_rows(screen)
        if not require(attributes.size() == 51, "Se esperaban cincuenta y un atributos visibles"):
            return
        var unique_codes: Dictionary = {}
        for row in attributes:
            var code = row.get_meta("attribute_code")
            if not require(not code in hidden and not unique_codes.has(code), "Se filtró un atributo oculto o repetido"):
                return
            unique_codes[code] = true
            var value = int(row.get_node("Value").text)
            if not require(value >= 1 and value <= 20, "Valor fuera de escala"):
                return
        if not require(find_named(screen, "PortraitImage").texture != null, "No se cargó el avatar provisional"):
            return
        if not require(find_named(screen, "PortraitImage").texture.resource_path == "res://Assets/Portraits/avatar.svg", "Se esperaba la silueta de distribución"):
            return
        for category_name in ["Category_Technical", "Category_Mental", "Category_Physical", "Category_Goalkeeper"]:
            if not require(find_named(screen, category_name).is_visible_in_tree(), "Un bloque no está disponible en la ficha"):
                return
        if not require(find_attribute_rows(find_named(screen, "Category_Goalkeeper")).size() == 10, "Faltan atributos de portero"):
            return
        var positions = find_named(screen, "PositionsPanel") as Control
        var insights = find_named(screen, "InsightsPanel") as Control
        var lower = find_named(screen, "PlayerExtraPanels") as GridContainer
        var pitch = find_named(screen, "PositionPitch")
        if not require(positions != null and insights != null and lower != null and pitch != null, "Faltan los paneles inferiores"):
            return
        var markers = find_marked_nodes(pitch, "position_role")
        var primary_count = 0
        for marker in markers:
            if marker.get_meta("position_primary"):
                primary_count += 1
        if not require(primary_count == 1, "El campo debe distinguir una sola posición principal"):
            return
        if sample_index == 0:
            if not require(markers.size() == 1 and markers[0].get_meta("position_role") == 1 and pitch.get_meta("primary_role") == 1, "El portero no se representa correctamente"):
                return
        elif sample_index == 1:
            var roles: Array[int] = []
            for marker in markers:
                roles.append(int(marker.get_meta("position_role")))
            roles.sort()
            if not require(roles == [5, 6, 7] and pitch.get_meta("primary_role") == 6, "No se conservaron exactamente las secundarias de referencia"):
                return
        var strengths = 0
        var weaknesses = 0
        var goalkeeper_codes = "reflexes handling punching one_on_ones aerial_claims rushing_out goalkeeping_positioning command_of_area hand_distribution defensive_communication".split(" ")
        for row in find_marked_nodes(insights, "insight_code"):
            var code = row.get_meta("insight_code")
            var value = int(row.get_meta("insight_value"))
            if not require(not code in hidden and unique_codes.has(code), "Se filtró un atributo no público a la descripción"):
                return
            if sample_index == 0 and not require(code in goalkeeper_codes, "Una descripción de portero usa atributos de campo irrelevantes"):
                return
            if row.get_meta("insight_kind") == "strength":
                strengths += 1
                if not require(value >= 14 and value <= 20, "Fortaleza fuera de límites descriptivos"):
                    return
            else:
                weaknesses += 1
                if not require(value >= 1 and value <= 7, "Debilidad fuera de límites descriptivos"):
                    return
        if not require(strengths <= 3 and weaknesses <= 3, "Demasiados atributos descriptivos"):
            return
        general.scroll_vertical = int(general.get_v_scroll_bar().max_value)
        await process_frame
        await process_frame
        if not require(lower.get_global_rect().position.y >= columns.get_global_rect().end.y, "Los nuevos paneles se solapan con los atributos"):
            return
        if not require(lower.columns == 2 and positions.get_global_rect().end.x <= insights.get_global_rect().position.x, "Los paneles no están a izquierda y derecha"):
            return
        if not require(lower.get_global_rect().end.y <= general.get_global_rect().end.y + 1, "No se puede alcanzar el final de los paneles"):
            return
        var back_button = find_named(screen, "BackSquad") as Control
        if not require(back_button.get_global_rect().position.y >= general.get_global_rect().end.y - 1, "El botón se solapa con la zona desplazable"):
            return
        if sample_index == 0:
            for resolution in [Vector2i(960, 640), Vector2i(1600, 900)]:
                root.size = resolution
                await process_frame
                await process_frame
                if not require(lower.columns == (1 if resolution.x < 1040 else 2), "Los paneles no se adaptan al ancho disponible"):
                    return
                if lower.columns == 1 and not require(positions.get_global_rect().end.y <= insights.get_global_rect().position.y, "Los paneles apilados se solapan"):
                    return
                var footer = find_named(screen, "BackSquad") as Control
                if not require(footer.get_global_rect().end.y <= root.size.y + 1 and footer.size.x <= root.size.x, "Navegación fuera de ventana al redimensionar"):
                    return
            root.size = Vector2i(1280, 720)
            await process_frame
        find_named(screen, "BackSquad").emit_signal("pressed")
        await process_frame
        if not require(find_named(screen, "ClubTabs").current_tab == 1, "El regreso no abrió Plantilla"):
            return
        squad = find_trees(screen)[0]
        if not require(squad.get_selected().get_metadata(0) == selected_id, "No se conservó la selección al volver"):
            return
        find_named(screen, "BackClubs").emit_signal("pressed")
        await process_frame
    if not require(opened_ids.size() == 4 and opened_ids[0] != opened_ids[1] and opened_ids[1] != opened_ids[2] and opened_ids[2] != opened_ids[3], "No se probaron cuatro futbolistas distintos"):
        return
    # Elegir y volver no publica carreras; la firma aprobada solo persiste después de confirmar.
    var selection = find_trees(screen)[0]
    selection.get_root().get_first_child().select(0)
    selection.emit_signal("item_selected")
    find_named(screen,"ViewClub").emit_signal("pressed")
    await process_frame
    find_named(screen,"ChooseClub").emit_signal("pressed")
    await process_frame
    if not await check_career_layout(screen,"BackChooseClub"):
        return
    if not require(find_named(screen,"ManagerOfferDocument") != null and not find_named(screen,"SignManagerContract").disabled,"La oferta aprobada no permite firmar"):
        return
    if not require(find_named(screen,"OfferDurationLabel") == null and find_named(screen,"OfferDurationMonthsLabel") == null and find_named(screen,"OfferCurrencyLabel") != null,"La oferta conserva duración en temporadas o no muestra moneda"):
        return
    if not require(find_named(screen,"OfferEndValue").text == "30/06/2027 (1 año)" and find_named(screen,"OfferCurrencyValue").text == "EUR (€)","Fechas o moneda inicial incorrectas"):
        return
    if not require(find_named(screen,"OfferHiringClubLabel").text == "Club" and find_named(screen,"OfferIncorporationSeasonLabel").text == "Temporada de incorporación","Literales de club o temporada incorrectos"):
        return
    if not require(find_named(screen,"ManagerOfferDocument").get_meta("signing_block_count") == 0,"Se bloquea por apartados opcionales pendientes"):
        return
    for reason in ["OfferSalaryMissing"]:
        if not require(find_named(screen,reason) == null,"Se mantiene un bloqueo salarial resuelto"):
            return
    for old_reason in ["OfferTermsPending","OfferCommitmentsPending","OfferTacticalPending","OfferEndMissing","OfferCurrencyMissing","EconomyRemainingBudgetLabel"]:
        if not require(find_named(screen,old_reason) == null,"Se muestra un bloqueo genérico u opcional obsoleto"):
            return
    if not require(find_marked_nodes(screen,"principle_code").size() == 4 and find_marked_nodes(screen,"objective_code").size() == 3,"La oferta no conserva compromisos del explorador"):
        return
    if not require(not DirAccess.dir_exists_absolute(test_root.path_join("careers")),"Elegir club creó una partida anticipada"):
        return
    find_named(screen,"BackChooseClub").emit_signal("pressed")
    await process_frame
    find_named(screen,"StartUnemployed").emit_signal("pressed")
    await process_frame
    find_named(screen,"CancelCareerCreation").emit_signal("pressed")
    await process_frame
    if not require(not DirAccess.dir_exists_absolute(test_root.path_join("careers")),"Cancelar confirmación creó una partida"):
        return
    find_named(screen,"StartUnemployed").emit_signal("pressed")
    await process_frame
    if not await check_career_layout(screen,"ConfirmCareerCreation"):
        return
    find_named(screen,"ConfirmCareerCreation").emit_signal("pressed")
    find_named(screen,"ConfirmCareerCreation").emit_signal("pressed")
    for attempt in range(200):
        if find_named(screen,"CareerDashboard") != null:
            break
        await create_timer(0.05).timeout
    var welcome = find_named(screen,"CareerDashboard")
    if not require(welcome != null and find_named(screen,"CareerEmploymentValue").text == "En paro" and find_named(screen,"CareerEmployer").text == "Entrenador libre — Sin club","No comienza carrera válida en paro"):
        return
    if not await check_career_layout(screen,"CareerGameMenu"):
        return
    if not await check_career_workspace(screen,false):
        return
    welcome = find_named(screen,"CareerDashboard")
    var first_career = welcome.get_meta("career_id")
    var first_coach = welcome.get_meta("coach_id")
    if not require(DirAccess.get_files_at(test_root.path_join("careers")).size() == 1,"La doble confirmación creó carreras duplicadas"):
        return
    await return_career_main_menu(screen)
    await process_frame
    find_named(screen,"NewGame").emit_signal("pressed")
    await process_frame
    if not require(find_marked_nodes(screen,"profile_id").size() == 1,"No se recupera el perfil existente"):
        return
    find_named(screen,"SelectCoachProfile").emit_signal("pressed")
    await process_frame
    selection = find_trees(screen)[0]
    selection.get_root().get_first_child().select(0)
    selection.emit_signal("item_selected")
    find_named(screen,"ViewClub").emit_signal("pressed")
    await process_frame
    find_named(screen,"ChooseClub").emit_signal("pressed")
    await process_frame
    find_named(screen,"RejectAndStartUnemployed").emit_signal("pressed")
    await process_frame
    find_named(screen,"ConfirmCareerCreation").emit_signal("pressed")
    for attempt in range(200):
        if find_named(screen,"CareerDashboard") != null:
            break
        await create_timer(0.05).timeout
    welcome = find_named(screen,"CareerDashboard")
    if not require(welcome != null and welcome.get_meta("career_id") != first_career and welcome.get_meta("coach_id") != first_coach,"Las dos carreras comparten identidad"):
        return
    if not require(DirAccess.get_files_at(test_root.path_join("careers")).size() == 2,"No se publicaron dos carreras independientes"):
        return
    # Tercera carrera: contrato aprobado, confirmación única y recursos consultados desde la partida.
    await return_career_main_menu(screen)
    await process_frame
    find_named(screen,"NewGame").emit_signal("pressed")
    await process_frame
    find_named(screen,"SelectCoachProfile").emit_signal("pressed")
    await process_frame
    selection = find_trees(screen)[0]
    selection.get_root().get_first_child().select(0)
    selection.emit_signal("item_selected")
    find_named(screen,"ViewClub").emit_signal("pressed")
    await process_frame
    find_named(screen,"ChooseClub").emit_signal("pressed")
    await process_frame
    var accepted_salary = find_named(screen,"OfferAnnualSalaryValue").text
    find_named(screen,"SignManagerContract").emit_signal("pressed")
    await process_frame
    if not await check_career_layout(screen,"ConfirmCareerCreation"):
        return
    find_named(screen,"ConfirmCareerCreation").emit_signal("pressed")
    find_named(screen,"ConfirmCareerCreation").emit_signal("pressed")
    for attempt in range(200):
        if find_named(screen,"CareerDashboard") != null:
            break
        await create_timer(0.05).timeout
    welcome = find_named(screen,"CareerDashboard")
    if not require(welcome != null and find_named(screen,"CareerEmployer").text != "Entrenador libre — Sin club","No se inició carrera con contrato"):
        return
    find_named(screen,"NavArea_Career").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Contract").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"OfferAnnualSalaryValue").text == accepted_salary,"No se conserva la instantánea contractual"):
        return
    if not require(DirAccess.get_files_at(test_root.path_join("careers")).size() == 3,"La firma no publicó exactamente una carrera"):
        return
    if not await check_career_workspace(screen,true):
        return
    print("FURVO_CAREER_UI_OK: perfil reutilizado, firma aprobada, volver/cancelar sin partida, paro/rechazo y tres carreras independientes")
    await press_escape()
    if not require(find_named(screen,"ExplorationMenu").visible, "Esc no está disponible en el resumen de carrera"):
        return
    find_named(find_named(screen,"ExplorationMenu"), "ReturnMainMenu").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen, "NewGame") != null, "No regresó al inicio"):
        return
    screen.queue_free()
    await process_frame
    print("FURVO_UI_SMOKE_OK: club Información/Plantilla/Historia/Economía, estados pendientes, 24 jugadores, 51 atributos y navegación")
    quit(0)
