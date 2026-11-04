extends SceneTree
# Prueba de integración de la escena C# real. Los arrastres usan eventos de ratón y coordenadas de controles reales.
# Ejecutar con Godot .NET tras compilar; solo crea carreras aisladas en artifacts, nunca modifica datos personales.

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
        if OS.get_cmdline_user_args().has("--analysis-only"):
            return await check_sports_modules(screen)
        if not await check_lineup(screen):
            return false
        if not await check_academy(screen):
            return false
        if not await check_advanced_tactics(screen):
            return false
        if not await check_sports_modules(screen):
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
    if not require(find_named(screen,"LineupRole_Substitute") != null and find_named(screen,"LineupRole_Reserve") != null,"Hover no actualiza candidatos"):
        return false
    first.emit_signal("pressed")
    var position_text = find_named(screen,"LineupCandidatePosition").text
    find_marked_nodes(screen,"slot_id")[1].emit_signal("mouse_entered")
    await process_frame
    if not require(find_named(screen,"LineupCandidatePosition").text == position_text,"El hover pierde la selección fijada"):
        return false
    var old_player = first.get_meta("player_id")
    for candidate in find_marked_nodes(screen,"eligibility"):
        if candidate.get_meta("player_id") != old_player and not candidate.disabled:
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
        var viewport = find_named(screen,"TacticLineupScroll")
        for circle in find_marked_nodes(screen,"slot_id"):
            if not require(circle.get_global_rect().position.y >= viewport.get_global_rect().position.y and circle.get_global_rect().end.y <= viewport.get_global_rect().end.y+1,"Un titular queda fuera del campo visible"):
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
    if not await check_tactics(screen):
        return false
    print("FURVO_LINEUP_OK: hover, selección fijada, sustitución, formación, persistencia, roles y tres resoluciones")
    return true

func choose_control(screen: Node, name: String, index: int) -> void:
    var control = find_named(screen,name)
    control.select(index)
    control.emit_signal("item_selected",index)
    await process_frame
    await process_frame
    if name == "LineupFormation" and find_named(screen,"PlanOverwriteDialog") != null:
        find_named(screen,"PlanOverwriteDialog").emit_signal("confirmed")
        await process_frame
        await process_frame

func drag_mouse(from: Vector2, to: Vector2, cancel: bool = false) -> void:
    var move = InputEventMouseMotion.new()
    move.position = from
    move.global_position = from
    root.push_input(move,true)
    await process_frame
    var down = InputEventMouseButton.new()
    down.button_index = MOUSE_BUTTON_LEFT
    down.pressed = true
    down.position = from
    down.global_position = from
    root.push_input(down,true)
    await process_frame
    for step in range(1,12):
        var motion = InputEventMouseMotion.new()
        motion.position = from.lerp(to,float(step)/11.0)
        motion.global_position = motion.position
        motion.relative = (to-from)/11.0
        motion.button_mask = MOUSE_BUTTON_MASK_LEFT
        root.push_input(motion,true)
        await process_frame
    if cancel:
        await press_escape()
    var up = InputEventMouseButton.new()
    up.button_index = MOUSE_BUTTON_LEFT
    up.pressed = false
    up.position = to
    up.global_position = to
    root.push_input(up,true)
    await process_frame
    await process_frame
    await process_frame

func check_dragging(screen: Node) -> bool:
    var slot = find_named(screen,"LineupSlot_gk")
    slot.emit_signal("pressed")
    await process_frame
    var candidate = find_marked_nodes(screen,"eligibility")[0]
    var source_id = slot.get_meta("player_id")
    var target_id = candidate.get_meta("player_id")
    var revision = find_named(screen,"LineupPage").get_meta("lineup_revision")
    find_named(screen,"LineupCandidateScroll").ensure_control_visible(candidate)
    await process_frame
    await drag_mouse(slot.get_global_rect().position+Vector2(18,14),candidate.get_global_rect().get_center())
    if not require(find_named(screen,"LineupSlot_gk").get_meta("player_id") == target_id and find_named(screen,"LineupPage").get_meta("lineup_revision") == revision+1,"Arrastre titular → suplente no intercambia"):
        return false
    candidate = find_named(screen,"LineupCandidate_"+source_id)
    slot = find_named(screen,"LineupSlot_gk")
    find_named(screen,"LineupCandidateScroll").ensure_control_visible(candidate)
    await process_frame
    await drag_mouse(candidate.get_global_rect().get_center(),slot.get_global_rect().position+Vector2(18,14))
    if not require(find_named(screen,"LineupSlot_gk").get_meta("player_id") == source_id,"Arrastre suplente → titular no funciona"):
        return false
    revision = find_named(screen,"LineupPage").get_meta("lineup_revision")
    slot = find_named(screen,"LineupSlot_gk")
    await drag_mouse(slot.get_global_rect().position+Vector2(18,14),Vector2(5,5))
    if not require(find_named(screen,"LineupPage").get_meta("lineup_revision") == revision,"Soltar fuera persiste cambios"):
        return false
    slot = find_named(screen,"LineupSlot_gk")
    candidate = find_marked_nodes(screen,"eligibility")[0]
    find_named(screen,"LineupCandidateScroll").ensure_control_visible(candidate)
    await process_frame
    await drag_mouse(slot.get_global_rect().position+Vector2(18,14),candidate.get_global_rect().get_center(),true)
    if not require(find_named(screen,"LineupPage").get_meta("lineup_revision") == revision and (find_named(screen,"ExplorationMenu") == null or not find_named(screen,"ExplorationMenu").visible),"Escape no cancela solo el arrastre"):
        return false
    slot = find_named(screen,"LineupSlot_gk")
    slot.emit_signal("pressed")
    await process_frame
    var reserve: Node
    for row in find_marked_nodes(screen,"eligibility"):
        if row.get_meta("assignment") == "Reserve" and row.get_meta("eligibility") == 0:
            reserve = row
            break
    if not require(reserve != null,"Falta reserva elegible para comprobar el arrastre"):
        return false
    var reserve_id = reserve.get_meta("player_id")
    var goalkeeper_id = slot.get_meta("player_id")
    find_named(screen,"LineupCandidateScroll").ensure_control_visible(reserve)
    await process_frame
    await process_frame
    await drag_mouse(slot.get_global_rect().position+Vector2(18,14),reserve.get_global_rect().get_center())
    if not require(find_named(screen,"LineupSlot_gk").get_meta("player_id") == reserve_id,"Titular → reserva no funciona"):
        return false
    reserve = find_named(screen,"LineupCandidate_"+goalkeeper_id)
    find_named(screen,"LineupCandidateScroll").ensure_control_visible(reserve)
    await process_frame
    await process_frame
    slot = find_named(screen,"LineupSlot_gk")
    await drag_mouse(reserve.get_global_rect().get_center(),slot.get_global_rect().position+Vector2(18,14))
    if not require(find_named(screen,"LineupSlot_gk").get_meta("player_id") == goalkeeper_id,"Reserva → titular no funciona"):
        return false
    var central: Node
    var attacker: Node
    for circle in find_marked_nodes(screen,"slot_id"):
        if circle.get_child(0).text == "DFC":
            central = circle
        if circle.get_child(0).text == "DC":
            attacker = circle
    var central_slot = central.get_meta("slot_id")
    var attacker_slot = attacker.get_meta("slot_id")
    var central_x = central.get_meta("tactical_x")
    var central_y = central.get_meta("tactical_y")
    var attacker_x = attacker.get_meta("tactical_x")
    var attacker_y = attacker.get_meta("tactical_y")
    var central_id = central.get_meta("player_id")
    var attacker_id = attacker.get_meta("player_id")
    await drag_mouse(central.get_global_rect().position+Vector2(18,14),attacker.get_global_rect().position+Vector2(18,14))
    if not require(find_named(screen,"LineupSlot_"+central_slot).get_meta("player_id") == central_id and find_named(screen,"LineupSlot_"+attacker_slot).get_meta("player_id") == attacker_id and absf(find_named(screen,"LineupSlot_"+central_slot).get_meta("tactical_x")-attacker_x) < .001 and absf(find_named(screen,"LineupSlot_"+central_slot).get_meta("tactical_y")-attacker_y) < .001 and absf(find_named(screen,"LineupSlot_"+attacker_slot).get_meta("tactical_x")-central_x) < .001 and absf(find_named(screen,"LineupSlot_"+attacker_slot).get_meta("tactical_y")-central_y) < .001,"Arrastre entre titulares no intercambia posiciones"):
        return false
    print("FURVO_DRAG_OK: ratón nativo en ambos sentidos, cancelación fuera y Escape")
    return true

func check_tactics(screen: Node) -> bool:
    if not await check_dragging(screen):
        return false
    await choose_control(screen,"TacticsMentality",4)
    for index in [1,2,3]:
        find_named(screen,"TacticTab_"+str(index)).emit_signal("pressed")
        await process_frame
        var form = find_named(screen,"TacticsFormationRow")
        var mental = find_named(screen,"TacticsMentalityRow")
        if not require(form.get_global_rect().end.y <= mental.get_global_rect().position.y and find_named(screen,"LineupFormation") == null,"Formación no es informativa o está fuera de sitio"):
            return false
        if index == 1:
            if not require(find_named(screen,"Instruction_passing") != null and find_named(screen,"Instruction_pressing") == null,"Con balón mezcla controles"):
                return false
            await choose_control(screen,"Instruction_passing",3)
            await choose_control(screen,"TacticalPhaseSelector",1)
            await process_frame
            if not require(find_named(screen,"Instruction_passing") == null and find_named(screen,"Instruction_pressing") != null,"Sin balón no filtra controles"):
                return false
            await choose_control(screen,"Instruction_pressing",3)
            await choose_control(screen,"TacticalPhaseSelector",0)
            await process_frame
            if not require(find_named(screen,"Instruction_passing").selected == 3,"Cambiar de fase pierde instrucciones"):
                return false
        elif index == 2:
            await choose_control(screen,"Instruction_recovery",1)
            if not require(find_named(screen,"TacticsPreview").get_meta("preview_focus") == "recovery","Recuperación mezclada"):
                return false
            await choose_control(screen,"Instruction_loss",3)
        else:
            # Marcajes reales y barrera: sin inventar asignaciones.
            await choose_control(screen,"SetPieceKind",4)
            await choose_control(screen,"SetPieceRoutine",3) # man
            await choose_control(screen,"SetPieceWall",3) # dos jugadores
            await choose_control(screen,"SetPieceLying",1)
            for role in ["Marker1","Marker2","Marker3","Wall1","Wall2","Prone"]:
                await choose_control(screen,"SetPieceRole_"+role,1)
            await process_frame
            if not require(find_named(screen,"TacticsPreview").get_meta("diagram_marked") == 3 and find_named(screen,"TacticsPreview").get_meta("diagram_lying") == 1,"No representa marcas y tumbado"):
                return false
            await capture_economy_screen("career-tactics-defensive-wall")
            await choose_control(screen,"SetPieceRoutine",1)
            if not require(find_named(screen,"TacticsPreview").get_meta("diagram_zones") > 0 and find_named(screen,"TacticsPreview").get_meta("diagram_marked") == 0,"Zonal no cambia la representación"):
                return false
            await capture_economy_screen("career-tactics-defensive-zonal")
            await choose_control(screen,"SetPieceRoutine",2)
            if not require(find_named(screen,"TacticsPreview").get_meta("diagram_zones") > 0 and find_named(screen,"TacticsPreview").get_meta("diagram_marked") == 2,"Mixto no combina zonas y marcas"):
                return false
            await capture_economy_screen("career-tactics-defensive-mixed")
            await choose_control(screen,"SetPieceRoutine",3)
            await choose_control(screen,"SetPieceKind",2)
            await choose_control(screen,"FreeKickLocation",0)
            await choose_control(screen,"SetPieceRoutine",1) # centro lateral
            await choose_control(screen,"SetPieceCrossTarget",2)
            await choose_control(screen,"SetPieceMovement",3)
            for role in ["SetPieceTaker","SetPieceRole_Finisher","SetPieceRole_Receiver","SetPieceRole_StayBack"]:
                await choose_control(screen,role,1)
            await capture_economy_screen("career-tactics-attacking-cross")
            await choose_control(screen,"SetPieceKind",5)
            if not require(find_named(screen,"SetPieceRoutine") == null and find_named(screen,"SetPieceTaker") == null,"Penaltis conserva instrucciones de disparo"):
                return false
            for ordinal in range(1,6):
                await choose_control(screen,"PenaltyTaker_"+str(ordinal),1)
            await capture_economy_screen("career-tactics-five-takers")
            await choose_control(screen,"SetPieceKind",6)
            for routine in range(1,7):
                await choose_control(screen,"SetPieceRoutine",routine)
                if find_named(screen,"SetPieceVariant") != null:
                    await choose_control(screen,"SetPieceVariant",1)
                await process_frame
                if not require(find_named(screen,"TacticsPreview").get_meta("diagram_arrows") > 0,"Estrategia de banda sin trayectorias"):
                    return false
            await capture_economy_screen("career-tactics-rehearsed-throw")
        for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
            root.size = resolution
            await create_timer(.2).timeout
            var preview = find_named(screen,"TacticsPreview")
            var controls = find_named(screen,"TacticsInstructionScroll")
            if not require(not preview.get_global_rect().intersects(controls.get_global_rect()) and controls.get_global_rect().end.x <= root.size.x+1 and controls.get_global_rect().end.y <= root.size.y+1,"Paneles solapados"):
                return false
            await capture_economy_screen("career-tactics-iter2-"+str(index)+"-"+str(resolution.x)+"x"+str(resolution.y))
        root.size = Vector2i(1280,720)
    find_named(screen,"NavPage_OwnSquad").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TacticsMentality").selected == 4 and find_marked_nodes(screen,"slot_id").size() == 11,"Reapertura pierde estado"):
        return false
    find_named(screen,"TacticTab_3").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"SetPieceKind",5)
    for ordinal in range(1,6):
        if not require(find_named(screen,"PenaltyTaker_"+str(ordinal)).selected > 0,"No se recupera el orden de lanzadores"):
            return false
    find_named(screen,"TacticTab_0").emit_signal("pressed")
    await process_frame
    for formation_index in range(find_named(screen,"LineupFormation").item_count):
        await choose_control(screen,"LineupFormation",formation_index)
        var ids: Array = []
        for circle in find_marked_nodes(screen,"slot_id"):
            if not require(not ids.has(circle.get_meta("player_id")),"Cambio de formación duplica titulares"):
                return false
            ids.append(circle.get_meta("player_id"))
        if not require(ids.size() == 11,"Una formación pierde titulares"):
            return false
    if not await check_plan_editor(screen):
        return false
    if not await check_instruction_expansion(screen):
        return false
    print("FURVO_TACTICS_ITER2_OK: fases, asignaciones reales, barrera/tumbado, cinco lanzadores, banda y reapertura")
    return true

func move_free(screen: Node, slot: String, advance: float, width: float) -> void:
    find_named(screen,"LineupSlot_"+slot).emit_signal("pressed")
    await process_frame
    await process_frame
    var field = find_named(screen,"LineupPitch")
    var rect: Rect2 = field.get_meta("field_rect")
    var destination = field.global_position+rect.position+rect.size*Vector2(width,1.0-advance)
    var player = find_named(screen,"LineupSlot_"+slot)
    if DisplayServer.get_name() != "headless":
        root.warp_mouse(destination)
        await process_frame
        await process_frame
    await drag_mouse(player.global_position+Vector2(18,14),destination)
    if not require(find_named(screen,"TacticalRoleDialog") == null and find_named(screen,"ProposedTacticalRole") == null,"Arrastrar abrió un diálogo de demarcación"):
        return

func confirm_overwrite(screen: Node, accept: bool) -> bool:
    var dialog = find_named(screen,"PlanOverwriteDialog")
    if not require(dialog != null,"Falta confirmación de sobrescritura"):
        return false
    dialog.emit_signal("confirmed" if accept else "canceled")
    await process_frame
    await process_frame
    return true

func check_plan_editor(screen: Node) -> bool:
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    if OS.get_cmdline_user_args().has("--manual-plans"):
        root.title = "Furvo · Planes QA"
        print("FURVO_MANUAL_PLANS_READY")
        await create_timer(300).timeout
        return true
    var original_ids: Array = []
    for circle in find_marked_nodes(screen,"slot_id"):
        original_ids.append(circle.get_meta("player_id"))
    var before_x = find_named(screen,"LineupSlot_s1").get_meta("tactical_x")
    await move_free(screen,"s1",.91,.3)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.91) < .015 and find_named(screen,"LineupSlot_s1").get_meta("tactical_role") == "Striker" and find_named(screen,"CustomFormation") != null,"Movimiento libre o demarcación automática fallidos"):
        return false
    await choose_control(screen,"TacticalPhaseSelector",0)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-before_x) < .015,"Mover Sin balón alteró Con balón"):
        return false
    var with_ball_x = find_named(screen,"LineupSlot_s1").get_meta("tactical_x")
    var phase_ids: Array = []
    for circle in find_marked_nodes(screen,"slot_id"):
        phase_ids.append(circle.get_meta("player_id"))
    phase_ids.sort()
    original_ids.sort()
    if not require(phase_ids == original_ids,"Las fases tienen distintos futbolistas"):
        return false
    await choose_control(screen,"TacticalPhaseSelector",1)
    await choose_control(screen,"TacticsMentality",2)
    await choose_control(screen,"TacticalPlanSelector",1)
    if not require(find_named(screen,"TacticsMentality").selected == 0,"Ganando heredó una edición posterior de Inicial"):
        return false
    await choose_control(screen,"TacticsMentality",5)
    find_named(screen,"TacticTab_1").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPhaseSelector",0)
    await choose_control(screen,"Instruction_passing",1)
    await choose_control(screen,"TacticalPlanSelector",0)
    if not require(find_named(screen,"TacticsMentality").selected == 2 and find_named(screen,"Instruction_passing").selected == 3,"Las instrucciones o mentalidad se comparten entre planes"):
        return false
    find_named(screen,"TacticTab_0").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPhaseSelector",1)
    await choose_control(screen,"TacticalPlanSelector",2)
    var copy = find_named(screen,"CopyTacticalPlan")
    copy.select(1)
    copy.emit_signal("item_selected",1)
    await process_frame
    if not await confirm_overwrite(screen,false):
        return false
    if not require(find_named(screen,"TacticsMentality").selected == 0,"Cancelar copia sobrescribió el plan"):
        return false
    copy = find_named(screen,"CopyTacticalPlan")
    copy.select(1)
    copy.emit_signal("item_selected",1)
    await process_frame
    if not await confirm_overwrite(screen,true):
        return false
    if not require(find_named(screen,"TacticsMentality").selected == 2 and absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.91) < .015,"Copiar plan no copia ambas disposiciones y mentalidad"):
        return false
    await move_free(screen,"s1",.83,.8)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.83) < .015,"La copia no admite edición independiente"):
        return false
    await choose_control(screen,"TacticalPlanSelector",0)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.91) < .015,"Editar copia alteró el origen"):
        return false
    # La formación personalizada exige confirmación y cancelar preserva las coordenadas.
    var formation = find_named(screen,"LineupFormation")
    var index = 0
    for i in range(formation.item_count):
        if formation.get_item_text(i) == "4-4-2":
            index = i
    formation.select(index)
    formation.emit_signal("item_selected",index)
    await process_frame
    if not await confirm_overwrite(screen,false):
        return false
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.91) < .015,"Cancelar formación perdió personalización"):
        return false
    await choose_control(screen,"LineupFormation",index)
    if not require(find_named(screen,"CustomFormation") == null,"Cambiar plantilla no restablece su geometría"):
        return false
    await move_free(screen,"s1",.91,.28)
    find_named(screen,"ResetDisposition").emit_signal("pressed")
    await process_frame
    if not await confirm_overwrite(screen,true):
        return false
    if not require(find_named(screen,"CustomFormation") == null,"Restablecer no recupera la plantilla"):
        return false
    await choose_control(screen,"TacticalPlanSelector",2)
    var disposition = find_named(screen,"CopyDisposition")
    disposition.select(1)
    disposition.emit_signal("item_selected",1)
    await process_frame
    if not await confirm_overwrite(screen,true):
        return false
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-with_ball_x) < .015,"Copia de disposición incorrecta"):
        return false
    find_named(screen,"ShowPhaseReference").emit_signal("toggled",true)
    await process_frame
    if not require(find_named(screen,"LineupPitch").get_meta("reference_positions") == 11,"No muestra las posiciones de referencia"):
        return false
    for plan in range(3):
        await choose_control(screen,"TacticalPlanSelector",plan)
        for phase in range(2):
            await choose_control(screen,"TacticalPhaseSelector",phase)
            for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
                root.size = resolution
                await create_timer(.15).timeout
                if not require(find_marked_nodes(screen,"slot_id").size() == 11 and find_named(screen,"TacticalPlanSelector").selected == plan and find_named(screen,"TacticalPhaseSelector").selected == phase,"Selector no corresponde a la disposición"):
                    return false
                var viewport = find_named(screen,"TacticLineupScroll").get_global_rect()
                for circle in find_marked_nodes(screen,"slot_id"):
                    if not require(circle.get_global_rect().position.y >= viewport.position.y-1 and circle.get_global_rect().end.y <= viewport.end.y+1,"Redimensionar deja titulares fuera del editor"):
                        return false
                await capture_economy_screen("career-plan-"+str(plan)+"-phase-"+str(phase)+"-"+str(resolution.x)+"x"+str(resolution.y))
    root.size = Vector2i(1280,720)
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    find_named(screen,"SaveTacticalPlans").emit_signal("pressed")
    await process_frame
    if not await check_auto_position_drop(screen):
        return false
    print("FURVO_PLANS_OK: seis disposiciones, arrastre libre, demarcación, independencia, copia/cancelación, restablecimiento y referencia")
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

func check_instruction_expansion(screen: Node) -> bool:
    find_named(screen,"TacticTab_1").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPhaseSelector",0)
    if not require(find_named(screen,"TacticStylePhases") == null,"Selector de fase duplicado"):
        return false
    for mode in range(1,4):
        await choose_control(screen,"Instruction_movement",mode)
        await capture_economy_screen("tactics-movement-"+str(mode))
    await choose_control(screen,"TacticalPhaseSelector",1)
    for mode in range(1,4):
        await choose_control(screen,"Instruction_marking",mode)
        await capture_economy_screen("tactics-marking-"+str(mode))
    find_named(screen,"TacticTab_2").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"Instruction_recovery",2)
    if not require(find_named(screen,"Instruction_counter_route") == null,"Vía visible sin contraatacar"):
        return false
    await choose_control(screen,"Instruction_recovery",1)
    if not require(find_named(screen,"Instruction_counter_route").item_count == 6,"Faltan vías de contraataque"):
        return false
    for route in range(1,6):
        await choose_control(screen,"Instruction_counter_route",route)
        if not require(find_named(screen,"TacticsPreview").get_meta("diagram_arrows") > 0,"Vía sin trayectorias"):
            return false
        await capture_economy_screen("tactics-counter-"+str(route))
    await choose_control(screen,"Instruction_keeper_target",0)
    await choose_control(screen,"Instruction_keeper",2)
    await capture_economy_screen("tactics-keeper-mixed")
    find_named(screen,"TacticTab_3").emit_signal("pressed")
    await process_frame
    for kind in [2,4]:
        await choose_control(screen,"SetPieceKind",kind)
        for location in range(4):
            await choose_control(screen,"FreeKickLocation",location)
            var expected = 4 if kind == 4 or location != 2 else 5
            if not require(find_named(screen,"SetPieceRoutine").item_count == expected,"Opciones no contextuales"):
                return false
            await choose_control(screen,"SetPieceRoutine",1)
            if kind == 4:
                await choose_control(screen,"SetPieceWall",3)
                if location < 2:
                    if not require(find_named(screen,"SetPieceLying") == null,"Tumbado disponible en lateral"):
                        return false
                else:
                    await choose_control(screen,"SetPieceLying",1)
                    if not require(find_named(screen,"SetPieceRole_Prone") != null,"Falta selector de tumbado"):
                        return false
            var assignment_grid = find_named(screen,"SetPieceAssignments")
            var instruction_scroll = find_named(screen,"TacticsInstructionScroll")
            if assignment_grid != null:
                instruction_scroll.scroll_vertical = int(instruction_scroll.get_v_scroll_bar().max_value)
                await process_frame
                await process_frame
                if not require(assignment_grid.get_global_rect().end.x <= root.size.x - 10,"Asignaciones recortadas horizontalmente"):
                    return false
                await capture_economy_screen("tactics-assignments-"+str(kind)+"-"+str(location))
                instruction_scroll.scroll_vertical = 0
                await process_frame
            await capture_economy_screen("tactics-free-"+str(kind)+"-"+str(location))
    await choose_control(screen,"SetPieceKind",2)
    await choose_control(screen,"FreeKickLocation",0)
    if not require(find_named(screen,"SetPieceRoutine").selected == 1,"Ubicación no conserva jugada propia"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080),Vector2i(1280,720)]:
        root.size = resolution
        await create_timer(.2).timeout
        var preview = find_named(screen,"TacticsPreview")
        var controls = find_named(screen,"TacticsInstructionScroll")
        if not require(not preview.get_global_rect().intersects(controls.get_global_rect()),"Nuevos controles solapados"):
            return false
        await capture_economy_screen("tactics-expansion-"+str(resolution.x)+"x"+str(resolution.y))
    print("FURVO_INSTRUCTIONS_OK: movimientos, marcajes, cinco vías, portero mixto, ocho contextos de faltas y selectores dependientes")
    return true

func check_auto_position_drop(screen: Node) -> bool:
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    var midfielder: Node = null
    for circle in find_marked_nodes(screen,"slot_id"):
        if circle.get_meta("tactical_role") == "CentralMidfielder":
            midfielder = circle
            break
    if not require(midfielder != null,"Falta MC para verificar arrastre automático"):
        return false
    var slot: String = midfielder.get_meta("slot_id")
    var player: String = midfielder.get_meta("player_id")
    var natural: String = midfielder.get_meta("natural_role")
    var secondaries = midfielder.get_meta("secondary_roles")
    var expected = 0 if natural == "AttackingMidfielder" else (1 if secondaries.has("AttackingMidfielder") else 2)
    var colors = ["27754e","9a601f","a33644"]
    var previous_revision = find_named(screen,"LineupPage").get_meta("lineup_revision")
    await move_free(screen,slot,.70,.46)
    var updated = find_named(screen,"LineupSlot_"+slot)
    if not require(find_named(updated,"TacticalRoleLabel").text == "MP" and updated.get_meta("compatibility") == expected and updated.get_meta("suitability_color") == colors[expected],"No actualiza MP y adecuación automáticamente"):
        return false
    if not require(updated.get_meta("player_id") == player and updated.get_meta("natural_role") == natural and find_named(screen,"LineupPage").get_meta("lineup_revision") == previous_revision+1,"El arrastre cambia identidad o guarda en dos pasos"):
        return false
    if not require(find_named(screen,"EditTacticalRole") == null and find_named(screen,"LineupSwap") == null,"Persisten controles retirados"):
        return false
    await choose_control(screen,"TacticalPhaseSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    if not require(find_named(screen,"LineupSlot_"+slot).get_meta("tactical_role") == "AttackingMidfielder","Cambio de fase pierde demarcación"):
        return false
    find_named(screen,"SaveTacticalPlans").emit_signal("pressed")
    await process_frame
    screen.ReopenIsolatedCareerForValidation()
    await process_frame
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    updated = find_named(screen,"LineupSlot_"+slot)
    if not require(absf(updated.get_meta("tactical_x")-.70)<.015 and absf(updated.get_meta("tactical_y")-.46)<.015 and updated.get_meta("tactical_role") == "AttackingMidfielder","Reabrir el editor no recupera el cambio guardado"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080),Vector2i(1280,720)]:
        root.size = resolution
        await create_timer(.2).timeout
        var heading = find_named(screen,"TacticsHeading")
        var tabs = find_named(screen,"TacticsTabs")
        var config = find_named(screen,"TacticsFormationRow")
        var mentality = find_named(screen,"TacticsMentalityRow")
        var content = find_named(screen,"LineupPage")
        if not require(heading.get_global_rect().position.y-find_named(screen,"CareerContextNavigation").get_global_rect().end.y>=19 and tabs.get_global_rect().position.y-heading.get_global_rect().end.y>=15 and config.get_global_rect().position.y-tabs.get_global_rect().end.y>=19 and mentality.get_global_rect().position.y-config.get_global_rect().end.y>=15 and content.get_global_rect().position.y-mentality.get_global_rect().end.y>=23,"Separación entre niveles insuficiente"):
            return false
        for group in ["TacticalPlanGroup","TacticalPhaseGroup","TacticalFormationGroup"]:
            if not require(find_named(screen,group).get_global_rect().end.x<=root.size.x-15,"Configuración fuera del ancho visible"):
                return false
        if not require(find_named(screen,"TacticalRoleDialog") == null,"Apareció un popup tras reabrir"):
            return false
        await capture_economy_screen("career-auto-position-"+str(resolution.x)+"x"+str(resolution.y))
    print("FURVO_AUTO_POSITION_OK: MC a MP sin popup, abreviatura/color, guardado atómico, fases, reapertura de carrera desde SQLite y espaciado en tres resoluciones")
    return true

func open_academy_by_id(screen: Node, team_id: String) -> void:
    find_named(screen,"NavPage_Academy").emit_signal("pressed")
    await process_frame
    var table = find_trees(find_named(screen,"AcademyTeamsTable"))[0]
    for row in table.get_root().get_children():
        if row.get_metadata(0) == team_id:
            row.select(0)
            table.emit_signal("item_selected")
            find_named(screen,"OpenAcademyTeam").emit_signal("pressed")
            await process_frame
            return

func check_academy(screen: Node) -> bool:
    root.size = Vector2i(1920,1080)
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    var primary_players: Array = []
    var primary_positions: Dictionary = {}
    for plan in range(3):
        await choose_control(screen,"TacticalPlanSelector",plan)
        for phase in range(2):
            await choose_control(screen,"TacticalPhaseSelector",phase)
            var positions: Dictionary = {}
            for circle in find_marked_nodes(screen,"slot_id"):
                positions[circle.get_meta("slot_id")] = [circle.get_meta("tactical_x"),circle.get_meta("tactical_y"),circle.get_meta("tactical_role")]
                if not primary_players.has(circle.get_meta("player_id")):
                    primary_players.append(circle.get_meta("player_id"))
            primary_positions[str(plan)+":"+str(phase)] = positions
    find_named(screen,"NavPage_Academy").emit_signal("pressed")
    await process_frame
    var teams = find_trees(find_named(screen,"AcademyTeamsTable"))[0]
    if not require(teams.get_root().get_child_count() == 4,"Mosaico no conserva sus cuatro equipos verificados"):
        return false
    var selected_team: String = teams.get_root().get_first_child().get_metadata(0)
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size = resolution
        await create_timer(.2).timeout
        if not require(find_named(screen,"OpenAcademyTeam").get_global_rect().end.y <= root.size.y-10,"Cantera oculta navegación"):
            return false
        await capture_economy_screen("career-academy-list-"+str(resolution.x)+"x"+str(resolution.y))
    await open_academy_by_id(screen,selected_team)
    var coach = find_named(screen,"AcademyCoachValue").text
    await choose_control(screen,"AcademyManagementSelector",0)
    find_named(screen,"AcademySection_1").emit_signal("pressed")
    await process_frame
    var squad = find_trees(find_named(screen,"AcademyRosterTable"))[0]
    if not require(squad.get_root().get_child_count() == 22,"El filial no tiene 22 futbolistas propios"):
        return false
    for row in squad.get_root().get_children():
        if not require(not primary_players.has(row.get_metadata(0)),"La cantera copió identificadores del primer equipo"):
            return false
    squad.get_root().get_first_child().select(0)
    squad.emit_signal("item_selected")
    find_named(screen,"ViewAcademyPlayer").emit_signal("pressed")
    await process_frame
    if not require(find_attribute_rows(screen).size() == 51,"Ficha de cantera incompleta o filtra ocultos"):
        return false
    await capture_economy_screen("career-academy-player")
    find_named(screen,"BackAcademyPlayer").emit_signal("pressed")
    await process_frame
    find_named(screen,"AcademySection_3").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"AcademyDevelopmentNotice") != null and find_trees(find_named(screen,"AcademyRosterTable"))[0].columns == 5,"Seguimiento sin situación ni aviso de sistemas pendientes"):
        return false
    find_named(screen,"AcademySection_2").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TacticsMentality").disabled and find_named(screen,"LineupFormation").disabled,"La sincronizada permite editar esquema heredado"):
        return false
    for plan in range(3):
        await choose_control(screen,"TacticalPlanSelector",plan)
        for phase in range(2):
            await choose_control(screen,"TacticalPhaseSelector",phase)
            for circle in find_marked_nodes(screen,"slot_id"):
                var expected = primary_positions[str(plan)+":"+str(phase)][circle.get_meta("slot_id")]
                if not require(absf(circle.get_meta("tactical_x")-expected[0])<.001 and absf(circle.get_meta("tactical_y")-expected[1])<.001 and circle.get_meta("tactical_role")==expected[2] and not primary_players.has(circle.get_meta("player_id")),"No sincroniza seis disposiciones con futbolistas propios"):
                    return false
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    await choose_control(screen,"AcademyTacticSelector",1)
    if not require(not find_named(screen,"LineupFormation").disabled and not find_named(screen,"TacticsMentality").disabled,"Propia no habilita editor reutilizado"):
        return false
    var scroll = find_named(screen,"AcademyTacticsScroll") as ScrollContainer
    scroll.ensure_control_visible(find_named(screen,"LineupPitch"))
    await create_timer(.2).timeout
    await move_free(screen,"s1",.94,.21)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.94)<.015 and find_named(screen,"TacticalRoleDialog")==null,"Cantera propia no admite arrastre directo sin popup"):
        return false
    await choose_control(screen,"TacticsMentality",5)
    find_named(screen,"TacticTab_1").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPhaseSelector",0)
    await choose_control(screen,"Instruction_passing",1)
    find_named(screen,"TacticTab_2").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"Instruction_recovery",1)
    await choose_control(screen,"Instruction_counter_route",3)
    await choose_control(screen,"AcademyTacticSelector",0)
    if not require(find_named(screen,"PlanOverwriteDialog") != null,"Reactivar sincronización no pide confirmación"):
        return false
    if not await confirm_overwrite(screen,false):
        return false
    if not require(find_named(screen,"AcademyTacticSelector").selected==1,"Cancelar deja selector táctico incoherente"):
        return false
    await choose_control(screen,"AcademyTacticSelector",0)
    if not await confirm_overwrite(screen,true):
        return false
    if not require(find_named(screen,"TacticsMentality").disabled,"Reactivar no restablece relación de lectura"):
        return false
    # Volver al primer equipo y modificar una coordenada; el vínculo debe leerla al regresar.
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-primary_positions["0:1"]["s1"][0])<.001,"Editar cantera alteró primer equipo"):
        return false
    await move_free(screen,"s1",.92,.29)
    await open_academy_by_id(screen,selected_team)
    find_named(screen,"AcademySection_2").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",1)
    if not require(absf(find_named(screen,"LineupSlot_s1").get_meta("tactical_x")-.92)<.015,"La sincronización no refleja una edición posterior del primer equipo"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size = resolution
        await create_timer(.2).timeout
        var back = find_named(screen,"BackAcademy")
        if not require(back.get_global_rect().end.y<=root.size.y-10,"Editor de cantera recorta el botón de regreso"):
            return false
        await capture_economy_screen("career-academy-tactics-"+str(resolution.x)+"x"+str(resolution.y))
        var viewport = find_named(screen,"AcademyTacticsScroll") as ScrollContainer
        viewport.ensure_control_visible(find_named(screen,"LineupPitch"))
        await create_timer(.2).timeout
        await capture_economy_screen("career-academy-field-"+str(resolution.x)+"x"+str(resolution.y))
    find_named(screen,"AcademySection_0").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"AcademyCoachValue").text==coach,"Una política sustituyó al entrenador responsable"):
        return false
    await choose_control(screen,"AcademyManagementSelector",1)
    screen.ReopenIsolatedCareerForValidation()
    await process_frame
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    await open_academy_by_id(screen,selected_team)
    if not require(find_named(screen,"AcademyManagementSelector").selected==1 and find_named(screen,"AcademyTacticSelector").selected==0 and find_named(screen,"AcademyCoachValue").text==coach,"La reapertura pierde políticas de cantera"):
        return false
    find_named(screen,"BackAcademy").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_OwnSquad").emit_signal("pressed")
    await process_frame
    if not require(find_trees(find_named(screen,"SquadTable"))[0].get_root().get_child_count()==24,"La cantera contaminó la plantilla principal"):
        return false
    root.size = Vector2i(1280,720)
    print("FURVO_ACADEMY_OK: cuatro equipos de Mosaico, plantillas propias, 51 atributos, seis disposiciones sincronizadas, editor propio, confirmación, actualización continua, políticas, reapertura y tres resoluciones")
    return true

func check_advanced_tactics(screen: Node) -> bool:
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    find_named(screen,"TacticTab_0").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPlanSelector",0)
    await choose_control(screen,"TacticalPhaseSelector",0)
    var keeper: Node = null
    for circle in find_marked_nodes(screen,"slot_id"):
        if circle.get_meta("tactical_role") == "Goalkeeper":
            keeper = circle
    if not require(keeper != null,"No hay portero para revisar roles"):
        return false
    var keeper_slot: String = keeper.get_meta("slot_id")
    var keeper_id: String = keeper.get_meta("player_id")
    keeper.emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"IndividualRole").item_count == 3,"Faltan roles de portero"):
        return false
    await choose_control(screen,"IndividualRole",2)
    find_named(screen,"IndividualExpand").set_pressed(true)
    await process_frame
    if not require(find_named(screen,"Individual_shooting") == null and find_named(screen,"Individual_risk") != null,"Órdenes absurdas de portero o editor ausente"):
        return false
    await choose_control(screen,"Individual_risk",0)
    if not require(find_named(screen,"IndividualRoleLabel").text.contains("personalizado"),"No identifica personalización"):
        return false
    await choose_control(screen,"IndividualRole",1)
    if not require(find_named(screen,"PlanOverwriteDialog") != null,"Reemplaza personalización sin confirmación"):
        return false
    if not await confirm_overwrite(screen,false):
        return false
    if not require(find_named(screen,"IndividualRole").selected==2,"Cancelar no conserva rol"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var field=find_named(screen,"LineupPitch") as Control
        var side=find_named(screen,"LineupCandidateScroll") as Control
        if not require(not field.get_global_rect().intersects(side.get_global_rect()) and side.get_global_rect().end.x<=root.size.x+1 and field.size.x>200,"Panel individual solapado o pizarra recortada"):
            return false
        await capture_economy_screen("career-individual-"+str(resolution.x)+"x"+str(resolution.y))
    if not require(find_named(screen,"LineupSlot_"+keeper_slot).get_meta("player_id")==keeper_id,"Cambiar rol altera el once"):
        return false
    find_named(screen,"TacticTab_1").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TacticalPhaseSelector",1)
    for instruction in ["press_direction","opponent_keeper","offside_trap","press_beaten"]:
        await choose_control(screen,"Instruction_"+instruction,2)
        await process_frame
        var preview=find_named(screen,"TacticsPreview")
        if not require(preview.get_meta("diagram_arrows")>0 or preview.get_meta("diagram_zones")>0,"Falta representación de "+instruction):
            return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        find_named(screen,"TacticsInstructionScroll").ensure_control_visible(find_named(screen,"Instruction_press_beaten"))
        await capture_economy_screen("career-advanced-defence-"+str(resolution.x)+"x"+str(resolution.y))
    find_named(screen,"TacticTab_0").emit_signal("pressed")
    await process_frame
    find_named(screen,"PersonnelExpand").set_pressed(true)
    await process_frame
    await choose_control(screen,"Personnel_Captain_0",1)
    var captain=find_named(screen,"Personnel_Captain_0").get_item_text(1)
    screen.ReopenIsolatedCareerForValidation()
    await process_frame
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    find_named(screen,"TacticTab_0").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"Personnel_Captain_0").selected==1 and find_named(screen,"Personnel_Captain_0").get_item_text(1)==captain,"Reapertura pierde capitán"):
        return false
    root.size=Vector2i(1280,720)
    print("FURVO_ADVANCED_TACTICS_OK: roles, personalización, cancelación segura, once intacto, cuatro diagramas defensivos, capitanes persistentes y tres resoluciones")
    return true

func check_sports_modules(screen: Node) -> bool:
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    for name in ["OwnSquad","Lineup","Training","Analysis","Academy"]:
        if not require(find_named(screen,"NavPage_"+name)!=null,"Falta navegación Equipo "+name):
            return false
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    find_named(screen,"LineupSlot_gk").emit_signal("pressed")
    await process_frame
    var left=find_named(screen,"IndividualSideScroll") as Control
    var field=find_named(screen,"LineupPitch") as Control
    var right=find_named(screen,"LineupCandidateScroll") as Control
    if not require(left.get_global_rect().end.x<=field.get_global_rect().position.x and field.get_global_rect().end.x<=right.get_global_rect().position.x,"Roles no están a la izquierda o se solapan"):
        return false
    if not require(find_named(screen,"Personnel_Captain_0")!=null and find_named(screen,"Personnel_Captain_1")!=null and find_named(screen,"Personnel_Captain_2")==null,"No hay exactamente dos puestos de capitán"):
        return false
    find_named(screen,"NavPage_Training").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingPage")!=null,"Entrenamiento no abre"):
        return false
    if not require(find_named(screen,"TrainingWeeklyCalendar")!=null and find_named(screen,"TrainingSessionForm")==null and find_named(screen,"TrainingDailyPanel")==null,"Entrenamiento no abre solo con calendario"):
        return false
    if not await check_training_calendar(screen):
        return false
    if not await check_training_group_fixes(screen):
        return false
    if not await check_training_free_kicks(screen):
        return false
    if not await check_training_availability(screen):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        if not require(find_named(screen,"TrainingScroll").size.y>100,"Entrenamiento sin espacio utilizable"):
            return false
        await capture_economy_screen("career-training-"+str(resolution.x)+"x"+str(resolution.y))
    for tab in range(1,7):
        find_named(screen,"TrainingTab_"+str(tab)).emit_signal("pressed")
        await process_frame
        if not require(find_named(screen,"TrainingExercise").item_count>0,"Modalidad de entrenamiento vacía"):
            return false
    await choose_control(screen,"SportsTeamSelector",1)
    find_named(screen,"TrainingTab_0").emit_signal("pressed")
    await process_frame
    if not await check_training_delegation_panel(screen):
        return false
    await choose_control(screen,"SportsTeamSelector",0)
    find_named(screen,"NavPage_Analysis").emit_signal("pressed")
    await process_frame
    for tab in range(4):
        find_named(screen,"AnalysisTab_"+str(tab)).emit_signal("pressed")
        await process_frame
        if not require(find_named(screen,"SportsAnalysisPage")!=null,"Análisis pierde navegación"):
            return false
    await choose_control(screen,"AnalysisReportStaff",1)
    find_named(screen,"AnalysisCreateSnapshot").emit_signal("pressed")
    await process_frame
    var reports: Array[Node]=[]
    for child in find_named(screen,"SportsAnalysisScroll").get_child(0).get_children():
        if str(child.name).begins_with("AnalysisReport_"):
            reports.append(child)
    if not require(reports.size()==1,"No crea informe de datos actuales"):
        return false
    if not await check_analysis_reports(screen):
        return false
    find_named(screen,"NavArea_Club").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Facilities").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"FacilityOwnershipValue").text.contains("verificar"),"Inventa posesión inicial de Footbonaut"):
        return false
    find_named(screen,"FootbonautRequestQuote").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"FootbonautRequestQuote").disabled,"No registra presupuesto pendiente"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        await capture_economy_screen("career-sports-technology-"+str(resolution.x)+"x"+str(resolution.y))
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Academy").emit_signal("pressed")
    await process_frame
    var academy_tree=find_trees(find_named(screen,"AcademyTeamsTable"))[0]
    var academy_team: String=academy_tree.get_root().get_first_child().get_metadata(0)
    await open_academy_by_id(screen,academy_team)
    find_named(screen,"AcademySection_1").emit_signal("pressed")
    await process_frame
    var roster_tree=find_trees(find_named(screen,"AcademyRosterTable"))[0]
    roster_tree.get_root().get_first_child().select(0)
    roster_tree.emit_signal("item_activated")
    await process_frame
    find_named(screen,"MovePlayerTeam").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"MovementExecute").disabled and find_named(screen,"MovementStatus").text.contains("licencia"),"Permite movimiento sin licencia verificada"):
        return false
    find_named(screen,"MovementRecommendationReason").text="Propuesta QA basada en datos disponibles"
    find_named(screen,"MovementRecommend").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"MovementStatus").text.contains("pendiente"),"No registra propuesta sin mover jugador"):
        return false
    screen.ReopenIsolatedCareerForValidation()
    await process_frame
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Training").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingGroupEdit").item_count==3 and find_marked_nodes(screen,"session_id").size()==4,"La reapertura pierde grupos o ejercicios compartidos"):
        return false
    root.size=Vector2i(1280,720)
    print("FURVO_SPORTS_OK: roles izquierda, dos capitanes, planificación, grupos, seis áreas, delegación parcial, informes actuales, tecnología pendiente, persistencia y tres resoluciones")
    return true

# Contrasta geometría real, ámbitos, grupos compartidos y persistencia de prioridades.
func check_analysis_reports(screen: Node) -> bool:
    if not require(find_named(screen,"AnalysisReportPlayer")==null,"El informe colectivo muestra selector individual"):
        return false
    for scope in [1,2]:
        await choose_control(screen,"AnalysisReportScope",scope)
        if scope==1:
            if not require(find_named(screen,"AnalysisReportPlayer").item_count==24,"Selector individual ajeno a plantilla"):
                return false
        find_named(screen,"AnalysisCreateSnapshot").emit_signal("pressed")
        await process_frame
        if not require(find_named(screen,"AnalysisStatus").text=="","Error al generar ámbito "+str(scope)):
            return false
    await choose_control(screen,"AnalysisReportScope",3)
    find_named(screen,"AnalysisManageGroups").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TrainingGroupEdit",0)
    find_named(screen,"TrainingGroupName").text="Grupo análisis QA"
    find_named(screen,"TrainingGroupObjective").text="Seguimiento observado"
    find_named(screen,"TrainingGroupMembers").select(0,false)
    find_named(screen,"TrainingGroupMembers").select(1,false)
    find_named(screen,"TrainingSaveGroup").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"AnalysisReportGroup").item_count>0,"No comparte grupos de entrenamiento"):
        return false
    find_named(screen,"AnalysisManageGroups").emit_signal("pressed")
    await process_frame
    find_named(screen,"AnalysisCreateSnapshot").emit_signal("pressed")
    await process_frame
    var report_table=find_named(screen,"AnalysisReportPlayers")
    var tree=find_trees(report_table)[0]
    if not require(tree.get_root().get_child_count()==2,"El grupo no conserva sus integrantes"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"SportsAnalysisScroll") as ScrollContainer
        scroll.scroll_vertical=0
        await process_frame
        await capture_economy_screen("career-analysis-controls-"+str(resolution.x)+"x"+str(resolution.y))
        scroll.scroll_vertical+=int(report_table.global_position.y-scroll.global_position.y)-45
        await process_frame
        var item=tree.get_root().get_first_child()
        var expected=[HORIZONTAL_ALIGNMENT_LEFT,HORIZONTAL_ALIGNMENT_LEFT,HORIZONTAL_ALIGNMENT_CENTER,HORIZONTAL_ALIGNMENT_RIGHT,HORIZONTAL_ALIGNMENT_CENTER]
        for column in range(5):
            if not require(tree.get_column_title_alignment(column)==expected[column] and item.get_text_alignment(column)==expected[column],"Encabezado y celda no coinciden"):
                return false
        if not require(item.get_text(3).contains(","),"Carga sin formato decimal español"):
            return false
        await capture_economy_screen("career-analysis-result-"+str(resolution.x)+"x"+str(resolution.y))
        var priorities=find_named(screen,"AnalysisPriorityHeading").get_parent()
        for child in priorities.get_children():
            if child.get_child_count()==2 and child.get_child(0) is CheckBox:
                var check=child.get_child(0) as CheckBox
                var label=child.get_child(1) as Label
                if not require(check.get_global_rect().end.x<=label.get_global_rect().position.x,"Checkbox solapado con texto"):
                    return false
                check.disabled=true
                await process_frame
                await capture_economy_screen("career-analysis-disabled-"+str(resolution.x)+"x"+str(resolution.y))
                check.disabled=false
                check.button_pressed=true
                check.grab_focus()
                break
        scroll.scroll_vertical=int(scroll.get_v_scroll_bar().max_value)
        await process_frame
        await capture_economy_screen("career-analysis-priorities-"+str(resolution.x)+"x"+str(resolution.y))
    find_named(screen,"AnalysisSavePriorities").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"AnalysisReportHistory").item_count==4,"Se pierde histórico entre ámbitos"):
        return false
    await choose_control(screen,"AnalysisReportScope",0)
    if not require(find_named(screen,"AnalysisReportPlayer")==null and find_named(screen,"AnalysisReportPlayers")!=null,"Filtros alteran histórico"):
        return false
    find_named(screen,"AnalysisTab_1").emit_signal("pressed")
    await process_frame
    if not await check_analysis_players(screen):
        return false
    var players=find_trees(find_named(screen,"AnalysisPlayers"))[0]
    var last=players.get_root().get_first_child()
    while last.get_next()!=null:
        last=last.get_next()
    players.scroll_to_item(last)
    await process_frame
    if not require(players.get_root().get_child_count()==24,"Tabla pierde jugadores al desplazar"):
        return false
    find_named(screen,"AnalysisTab_3").emit_signal("pressed")
    await process_frame
    return true

# Solo consultas: categorías completas, comparación sin informe, selección por ID y geometría real.
func check_analysis_players(screen: Node) -> bool:
    var tree=find_trees(find_named(screen,"AnalysisPlayers"))[0]
    if not require(find_named(screen,"AnalysisPlayerPrompt")!=null and find_named(screen,"AnalysisAttributeRows")==null,"Atributos desplegados sin selección"):
        return false
    var first=tree.get_root().get_first_child()
    var selected_id=first.get_metadata(0)
    first.select(0)
    tree.emit_signal("item_selected")
    await process_frame
    if not require(find_named(screen,"AnalysisPlayerDetail").get_meta("selected_player")==selected_id,"Panel no corresponde a ID seleccionado"):
        return false
    if not require(find_named(screen,"AnalysisSelectedIdentity").text.contains(first.get_text(0)),"Panel sin identidad seleccionada"):
        return false
    if not require(find_named(screen,"AnalysisComparePlayer").item_count==24,"Comparación no excluye jugador seleccionado"):
        return false
    await choose_control(screen,"AnalysisComparePlayer",1)
    var count=0
    for entry in [["Technical",17],["Mental",16],["Physical",8],["Goalkeeper",10]]:
        find_named(screen,"AnalysisCategory_"+entry[0]).emit_signal("pressed")
        await process_frame
        var rows=find_named(screen,"AnalysisAttributeRows")
        count+=rows.get_child_count()
        if not require(rows.get_child_count()==entry[1],"Número de atributos incorrecto en "+entry[0]):
            return false
        for row in rows.get_children():
            if not require(row.get_child_count()==3,"Comparación sin ambas barras"):
                return false
            for side in ["Selected","Comparison"]:
                var bar=find_named(row,side).get_node("Bar")
                if not require(bar.value>=1 and bar.value<=20 and bar.max_value==20,"Barra fuera de escala"):
                    return false
    if not require(count==51,"No se conservan los 51 atributos visibles"):
        return false
    for sort in range(3):
        await choose_control(screen,"AnalysisAttributeSort",sort)
    find_named(screen,"AnalysisRecordedHistory").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"AnalysisNoRecordedHistory")!=null,"Se inventa evolución a partir de instantáneas de un solo día"):
        return false
    find_named(screen,"AnalysisCategory_Technical").emit_signal("pressed")
    await process_frame
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"SportsAnalysisScroll") as ScrollContainer
        var detail=find_named(screen,"AnalysisPlayerDetail")
        scroll.scroll_vertical+=int(detail.global_position.y-scroll.global_position.y)
        await process_frame
        for row in find_named(screen,"AnalysisAttributeRows").get_children():
            if not require(row.get_global_rect().position.x>=0 and row.get_global_rect().end.x<root.size.x,"Atributos fuera de pantalla"):
                return false
            var label=row.get_node("AttributeName")
            var bar=row.get_node("Selected")
            if not require(label.get_global_rect().end.x<=bar.get_global_rect().position.x,"Nombre solapado con barra"):
                return false
        await capture_economy_screen("career-player-analysis-"+str(resolution.x)+"x"+str(resolution.y))
    find_named(screen,"AnalysisCollapsePlayer").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"AnalysisAttributeRows")==null,"No contrae panel de atributos"):
        return false
    find_named(screen,"AnalysisExpandPlayer").emit_signal("pressed")
    await process_frame
    tree=find_trees(find_named(screen,"AnalysisPlayers"))[0]
    tree.get_root().get_first_child().get_next().select(0)
    tree.emit_signal("item_selected")
    await process_frame
    if not require(find_named(screen,"AnalysisPlayerDetail").get_meta("selected_player")!=selected_id,"Selección mantiene jugador anterior"):
        return false
    print("FURVO_PLAYER_ANALYSIS_OK: selección por ID, comparación, 51 atributos, cuatro categorías, ordenación, sin evolución inventada, contraer y tres resoluciones")
    return true

# Calendario compartido: una sola sesión por ID, borradores seguros y acciones reales.
func group_text(screen: Node, field: String, value: String) -> void:
    find_named(screen,field).text=value
    find_named(screen,field).emit_signal("text_changed",value)

func check_training_group_fixes(screen: Node) -> bool:
    await training_tab(screen,0)
    await choose_control(screen,"TrainingGroupEdit",0)
    group_text(screen,"TrainingGroupName","Borrador temporal QA")
    await training_tab(screen,2)
    await training_tab(screen,0)
    if not require(find_named(screen,"TrainingGroupName").text=="Borrador temporal QA" and find_named(screen,"TrainingGroupDraftState").text.contains("sin guardar"),"Cambio de pestaña pierde borrador de grupo"):
        return false
    await choose_control(screen,"SportsTeamSelector",1)
    var dialog=find_named(screen,"TrainingGroupExitDialog")
    if not require(dialog!=null,"Cambiar equipo no protege borrador de grupo"):
        return false
    dialog.emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"TrainingGroupExitDialog")!=null and find_named(screen,"TrainingGroupExitDialog").get_label().text.contains("objetivo") and find_named(screen,"SportsTeamSelector").selected==0,"Guardar grupo incompleto abandona la edición o crea datos"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        await capture_economy_screen("career-training-fixes1-group-dialog-"+str(resolution.x)+"x"+str(resolution.y))
    dialog.emit_signal("canceled")
    await process_frame
    if not require(find_named(screen,"TrainingGroupName").text=="Borrador temporal QA","Cancelar salida pierde borrador"):
        return false
    group_text(screen,"TrainingGroupObjective","Control y pase del grupo QA")
    var members=find_named(screen,"TrainingGroupMembers") as ItemList
    members.select(0,false)
    members.emit_signal("multi_selected",0,true)
    await choose_control(screen,"SportsTeamSelector",1)
    find_named(screen,"TrainingGroupExitDialog").emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"SportsTeamSelector").selected==1,"Guardar cambios no permite cambiar equipo"):
        return false
    await choose_control(screen,"SportsTeamSelector",0)
    await training_tab(screen,0)
    await choose_clock(screen,"TrainingGroupEdit","Borrador temporal QA")
    var selector=find_named(screen,"TrainingGroupEdit") as OptionButton
    var saved_id=str(selector.get_item_metadata(selector.selected))
    group_text(screen,"TrainingGroupName","Nombre sin guardar")
    find_named(screen,"TrainingCancelGroup").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingGroupExitDialog").emit_signal("custom_action","discard")
    await process_frame
    if not require(find_named(screen,"TrainingGroupName").text=="Borrador temporal QA","Descartar no recupera grupo guardado"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        scroll.scroll_vertical+=int(find_named(screen,"TrainingGroupsTitle").global_position.y-scroll.global_position.y)-8
        await process_frame
        for field in ["TrainingGroupName","TrainingGroupObjective","TrainingGroupMembers","TrainingDeleteGroup"]:
            if not require(find_named(screen,field).get_global_rect().end.x<=root.size.x,"Editor de grupos recortado"):
                return false
        await capture_economy_screen("career-training-fixes1-group-editor-"+str(resolution.x)+"x"+str(resolution.y))
    await training_tab(screen,1)
    await select_training_day(screen,"2026-07-01")
    await choose_control(screen,"TrainingDayKind",2)
    await choose_clock(screen,"TrainingScope","Grupo personalizado")
    await choose_clock(screen,"TrainingGroup","Borrador temporal QA")
    await choose_clock(screen,"TrainingTurn","Mañana")
    await choose_clock(screen,"TrainingTime","08:30")
    await choose_clock(screen,"TrainingEndTime","08:45")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingFormStatus").text=="","No guarda ejercicio del grupo"):
        return false
    await training_tab(screen,0)
    find_named(screen,"TrainingDeleteGroup").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingGroupStatus").text.contains("en uso") and find_named(screen,"TrainingGroupStatus").text.contains("01/07/2026") and find_marked_nodes(screen,"daily_block_id").size()==5,"Eliminación de grupo usado no conserva referencias o no las explica"):
        return false
    var activity=""
    for row in find_marked_nodes(screen,"daily_block_id"):
        if find_named(row,"TrainingDailyExercise").text.begins_with("08:30"):
            activity=str(row.get_meta("daily_block_id"))
    find_named(screen,"TrainingDailyDelete_"+activity).emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    find_named(screen,"TrainingDeleteGroup").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingConfirmDialog")!=null,"Grupo sin referencias se elimina sin confirmar"):
        return false
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    selector=find_named(screen,"TrainingGroupEdit")
    for index in range(1,selector.item_count):
        if not require(str(selector.get_item_metadata(index))!=saved_id,"Grupo eliminado sigue en el listado"):
            return false
    screen.ReopenIsolatedCareerForValidation()
    await process_frame
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Training").emit_signal("pressed")
    await process_frame
    await select_training_day(screen,"2026-07-01")
    if not require(find_named(screen,"TrainingGroupEdit").item_count==2 and find_marked_nodes(screen,"daily_block_id").size()==4,"Reapertura recupera grupo eliminado o pierde ejercicios"):
        return false
    print("FURVO_TRAINING_GROUP_FIXES_OK: borrador entre pestañas, salida guardar/descartar/cancelar, validación, eliminación con referencias y reapertura")
    return true

func check_training_free_kicks(screen: Node) -> bool:
    find_named(screen,"NavPage_Lineup").emit_signal("pressed")
    await process_frame
    find_named(screen,"TacticTab_3").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"SetPieceKind",2)
    await choose_control(screen,"FreeKickLocation",0)
    await choose_control(screen,"SetPieceRoutine",1)
    find_named(screen,"NavPage_Training").emit_signal("pressed")
    await process_frame
    await training_tab(screen,4)
    await select_training_day(screen,"2026-07-01")
    await choose_clock(screen,"TrainingExercise","Práctica de faltas")
    var kind=find_named(screen,"TrainingRoutine") as OptionButton
    if not require(kind.item_count==3 and find_named(screen,"TrainingRoutineLocation").text.contains("izquierda") and not find_named(screen,"TrainingSaveSession").disabled,"Falta diseñada por ubicación no está disponible"):
        return false
    await choose_control(screen,"TrainingRoutineLocation",1)
    if not require(find_named(screen,"TrainingRoutineDescription").text.contains("No hay ninguna falta") and find_named(screen,"TrainingSaveSession").disabled,"Ubicación sin diseñar permite guardar"):
        return false
    await choose_control(screen,"TrainingRoutineLocation",0)
    await choose_control(screen,"TrainingDayKind",2)
    await choose_clock(screen,"TrainingTurn","Mañana")
    await choose_clock(screen,"TrainingTime","08:00")
    await choose_clock(screen,"TrainingEndTime","08:15")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingFormStatus").text=="","No guarda la falta izquierda configurada"):
        return false
    await training_tab(screen,0)
    var activity=""
    for row in find_marked_nodes(screen,"daily_block_id"):
        if find_named(row,"TrainingDailyExercise").text.begins_with("08:00"):
            activity=str(row.get_meta("daily_block_id"))
    find_named(screen,"TrainingDailyEdit_"+activity).emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingExercise").text=="Práctica de faltas" and find_named(screen,"TrainingRoutineLocation").text.contains("izquierda") and find_named(screen,"TrainingRoutineDescription").text.contains("Jugada diseñada"),"Editar pierde tipo, ubicación o referencia táctica"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        scroll.scroll_vertical+=int(find_named(screen,"TrainingRoutine").global_position.y-scroll.global_position.y)-35
        await process_frame
        for key in ["TrainingRoutine","TrainingRoutineLocation","TrainingRoutineDescription"]:
            if not require(find_named(screen,key).get_global_rect().end.x<=root.size.x,"Selector de faltas recortado"):
                return false
        await capture_economy_screen("career-training-fixes1-routine-"+str(resolution.x)+"x"+str(resolution.y))
    await training_tab(screen,0)
    find_named(screen,"TrainingDailyDelete_"+activity).emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    if not require(find_marked_nodes(screen,"daily_block_id").size()==4,"Prueba de faltas altera otras actividades"):
        return false
    print("FURVO_TRAINING_ROUTINES_OK: tipos filtrados, ubicación real, bloqueo sin diseño, guardado/edición y tres resoluciones")
    return true

func check_training_delegation_panel(screen: Node) -> bool:
    var toggle=find_named(screen,"TrainingDelegationToggle") as Button
    var details=find_named(screen,"TrainingDelegationDetails") as Control
    if not require(not details.visible and toggle.text.contains("Gestión y delegación") and find_named(screen,"TrainingDelegationArea")==null and find_named(screen,"TrainingApplyDelegation")==null,"Panel antiguo duplicado o panel nuevo no compacto"):
        return false
    if not require(toggle.global_position.y>=find_named(screen,"TrainingTabs").get_global_rect().end.y,"Panel no está debajo de pestañas"):
        return false
    toggle.emit_signal("pressed")
    await process_frame
    if not require(details.visible and find_named(screen,"TrainingDelegationTable").get_child_count()==30,"Tabla no unifica nueve áreas reales"):
        return false
    var keeper=find_named(screen,"TrainingDelegationMode_Goalkeepers") as OptionButton
    if not require(keeper.is_item_disabled(1) and keeper.is_item_disabled(2),"Permite delegar Porteros sin técnico elegible"):
        return false
    await choose_control(screen,"TrainingDelegationMode_Collective",2)
    if not require(find_named(screen,"TrainingSaveDelegation").disabled,"Permite guardar delegación sin responsable"):
        return false
    await choose_control(screen,"TrainingDelegationOwner_Collective",1)
    if not require(not find_named(screen,"TrainingSaveDelegation").disabled,"No habilita guardado con responsable real"):
        return false
    find_named(screen,"TrainingDelegationToggle").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingConfirmDialog")!=null,"Contraer descarta cambios sin advertir"):
        return false
    find_named(screen,"TrainingConfirmDialog").emit_signal("canceled")
    await process_frame
    var calendar=training_calendar_signature(screen)
    await training_tab(screen,1)
    if not require(find_named(screen,"TrainingDelegationMode_Collective").selected==2 and training_calendar_signature(screen)==calendar,"Cambio de pestaña pierde borrador o calendario"):
        return false
    await choose_control(screen,"SportsTeamSelector",0)
    if not require(find_named(screen,"TrainingConfirmDialog")!=null,"Cambio de equipo descarta delegación sin confirmar"):
        return false
    find_named(screen,"TrainingConfirmDialog").emit_signal("canceled")
    await process_frame
    if not require(find_named(screen,"SportsTeamSelector").selected==1,"Cancelar cambio de equipo altera contexto"):
        return false
    find_named(screen,"TrainingSaveDelegation").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingDelegationStatus").text.contains("guardada") and find_named(screen,"TrainingDelegationToggle").text.contains("mixta"),"No guarda modalidad mixta con confirmación visual"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var outer=find_named(screen,"TrainingScroll") as ScrollContainer
        outer.scroll_vertical=0
        await process_frame
        if not require(find_named(screen,"TrainingDelegationScroll").size.y<=180,"Tabla de delegación ocupa demasiada altura"):
            return false
        for key in ["TrainingDelegationOwner_Collective","TrainingSaveDelegation","TrainingDelegationToggle"]:
            if not require(find_named(screen,key).get_global_rect().end.x<=root.size.x,"Control de delegación recortado"):
                return false
        await capture_economy_screen("career-training-delegation-panel-"+str(resolution.x)+"x"+str(resolution.y))
        find_named(screen,"TrainingDelegationToggle").emit_signal("pressed")
        await process_frame
        if not require(not find_named(screen,"TrainingDelegationDetails").visible and find_named(screen,"TrainingWeeklyCalendar").global_position.y<root.size.y,"Contraer no recupera espacio del calendario"):
            return false
        await capture_economy_screen("career-training-delegation-collapsed-"+str(resolution.x)+"x"+str(resolution.y))
        find_named(screen,"TrainingDelegationToggle").emit_signal("pressed")
        await process_frame
    find_named(screen,"TrainingAllManual").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"TrainingDelegationMode_Collective").selected==0 and not find_named(screen,"TrainingSaveDelegation").disabled,"Restablecer manual no crea un borrador guardable"):
        return false
    find_named(screen,"TrainingSaveDelegation").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingDelegationToggle").text.contains("Control manual"),"No conserva control manual"):
        return false
    find_named(screen,"TrainingDelegationToggle").emit_signal("pressed")
    await process_frame
    print("FURVO_DELEGATION_PANEL_OK: panel único, nueve áreas, modos, elegibilidad, guardado explícito, borradores protegidos y tres resoluciones")
    return true

func training_tab(screen: Node, tab: int) -> void:
    find_named(screen,"TrainingTab_"+str(tab)).emit_signal("pressed")
    await process_frame

func training_add(screen: Node, tab: int, shift: String, start: String, end: String) -> bool:
    await training_tab(screen,tab)
    await select_training_day(screen,"2026-07-01")
    await choose_control(screen,"TrainingDayKind",2)
    await choose_clock(screen,"TrainingTurn",shift)
    await choose_clock(screen,"TrainingTime",start)
    await choose_clock(screen,"TrainingEndTime",end)
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    return require(find_named(screen,"TrainingFormStatus").text=="","No guarda actividad en categoría "+str(tab))

# La categoría nunca entra en la firma de la agenda: solo cambia el formulario.
func training_calendar_signature(screen: Node) -> Array:
    var signature: Array=[]
    for day in find_named(screen,"TrainingWeeklyCalendar").get_children():
        signature.append([day.get_meta("calendar_date"),find_named(day,"TrainingDayType").text,day.get_meta("is_selected")])
        for activity in find_marked_nodes(day,"session_id"):
            signature.append([activity.get_meta("session_id"),activity.tooltip_text])
    return signature

func check_training_global_tabs(screen: Node) -> bool:
    var expected=training_calendar_signature(screen)
    var week=find_named(screen,"TrainingWeek").text
    var field_id=""
    for row in find_marked_nodes(screen,"daily_block_id"):
        if find_named(row,"TrainingDailyExercise").text.begins_with("15:00"):
            field_id=str(row.get_meta("daily_block_id"))
    for tab in range(7):
        await training_tab(screen,tab)
        for index in range(7):
            var button=find_named(screen,"TrainingTab_"+str(index))
            var style=button.get_theme_stylebox("normal") as StyleBoxFlat
            if not require(button.get_meta("training_active")== (index==tab) and style.border_width_bottom==3 and button.custom_minimum_size.y==44,"Estilo activo o altura de pestañas incoherente"):
                return false
            if index==tab and not require(style.border_color==Color("f1b96b") and button.get_theme_font("font") is FontVariation,"Pestaña activa sin borde naranja o negrita"):
                return false
        if tab==0:
            var members=find_named(screen,"TrainingGroupMembers") as ItemList
            for index in range(members.item_count):
                if not require(members.get_item_text(index).contains(" — ") and str(members.get_item_metadata(index))!="","Grupo sin posición o identificador"):
                    return false
        if not require(find_named(screen,"TrainingCalendarFilter")==null,"Queda un selector que oculta la agenda"):
            return false
        if not require(training_calendar_signature(screen)==expected and find_named(screen,"TrainingWeek").text==week,"Calendario, jornada, fecha o semana cambian en pestaña "+str(tab)):
            return false
        if not require(find_marked_nodes(screen,"daily_block_id").size()==4 and find_named(screen,"TrainingDailyTotal").text=="90 min","La lista diaria oculta actividades en pestaña "+str(tab)):
            return false
    await training_tab(screen,2)
    find_named(screen,"TrainingSession_"+field_id).emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingTab_1").get_theme_color("font_color")==Color("f1b96b") and find_named(screen,"TrainingDate").text=="2026-07-01" and find_named(screen,"TrainingTime").text=="15:00","Campo no se abre al seleccionar su actividad desde Gimnasio"):
        return false
    find_named(screen,"TrainingBackSummary").emit_signal("pressed")
    await process_frame
    if not require(training_calendar_signature(screen)==expected,"La edición cruzada mueve o duplica actividades"):
        return false
    await training_tab(screen,2)
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        find_named(screen,"TrainingScroll").scroll_vertical=0
        await process_frame
        if not require(find_marked_nodes(screen,"session_id").size()==4,"Gimnasio oculta Campo al redimensionar"):
            return false
        await capture_economy_screen("career-training-global-gym-"+str(resolution.x)+"x"+str(resolution.y))
    await training_tab(screen,0)
    print("FURVO_TRAINING_GLOBAL_OK: siete calendarios idénticos, sin selector, fecha/semana/jornada conservadas, lista global y edición Campo desde Gimnasio")
    return true

func check_training_calendar(screen: Node) -> bool:
    if not require(find_named(screen,"TrainingTab_0").text=="Resumen" and find_named(screen,"TrainingAgendaSummary")!=null,"Falta Resumen global"):
        return false
    await select_training_day(screen,"2026-07-01")
    if not require(find_named(screen,"TrainingSessionForm")==null,"Resumen duplica formulario especializado"):
        return false
    find_named(screen,"TrainingDailyAddAnother").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TrainingSummaryCategory",0)
    find_named(screen,"TrainingSummaryOpenCategory").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"TrainingResponsible",1)
    await choose_control(screen,"TrainingDayKind",2)
    await choose_clock(screen,"TrainingTurn","Tarde")
    await choose_clock(screen,"TrainingTime","15:00")
    await choose_clock(screen,"TrainingEndTime","15:45")
    find_named(screen,"TrainingObjective").text="Práctica técnica de QA"
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingTime").text=="15:45","Guardar no conserva contexto ni propone siguiente hueco"):
        return false
    await training_tab(screen,0)
    find_named(screen,"TrainingGroupName").text="Grupo QA"
    find_named(screen,"TrainingGroupObjective").text="Pase coordinado"
    find_named(screen,"TrainingGroupMembers").select(0,false)
    find_named(screen,"TrainingGroupMembers").select(1,false)
    find_named(screen,"TrainingSaveGroup").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingGroupEdit").item_count==2,"No persiste grupo compartido"):
        return false
    for entry in [[2,"Mañana","10:00","10:15"],[1,"Mañana","11:00","11:15"],[5,"Mañana","12:00","12:15"]]:
        if not await training_add(screen,entry[0],entry[1],entry[2],entry[3]):
            return false
    await training_tab(screen,0)
    var rows=find_marked_nodes(screen,"daily_block_id")
    if not require(rows.size()==4 and find_named(screen,"TrainingDailyTotal").text=="90 min" and find_marked_nodes(screen,"session_id").size()==4,"Agenda global no refleja las categorías"):
        return false
    if not await check_training_global_tabs(screen):
        return false
    rows=find_marked_nodes(screen,"daily_block_id")
    var gym_id=""
    for row in rows:
        if find_named(row,"TrainingDailyExercise").text.begins_with("10:00"):
            gym_id=str(row.get_meta("daily_block_id"))
    find_named(screen,"TrainingDailyEdit_"+gym_id).emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingTab_2").get_theme_color("font_color")==Color("f1b96b") and find_named(screen,"TrainingDate").text=="2026-07-01","Editar no abre Gimnasio en la misma fecha"):
        return false
    await choose_clock(screen,"TrainingEndTime","10:30")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingBackSummary").emit_signal("pressed")
    await process_frame
    if not require(find_marked_nodes(screen,"daily_block_id").size()==4 and find_named(screen,"TrainingDailyEdit_"+gym_id)!=null and find_named(screen,"TrainingDailyTotal").text=="105 min","Edición cruzada duplica el ID o no actualiza total"):
        return false
    find_named(screen,"TrainingDailyEdit_"+gym_id).emit_signal("pressed")
    await process_frame
    await choose_clock(screen,"TrainingEndTime","10:15")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    # La agenda de Gimnasio también valida los conflictos con Campo.
    await choose_clock(screen,"TrainingTurn","Tarde")
    await choose_clock(screen,"TrainingTime","15:15")
    await choose_clock(screen,"TrainingEndTime","15:30")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingFormStatus").text.contains("Sesiones solapadas") and find_named(screen,"TrainingFormStatus").text.contains("15:00"),"Conflicto global no identifica la actividad existente"):
        return false
    find_named(screen,"TrainingCancelSession").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    # Tres categorías crean, aparecen y desaparecen de la misma agenda.
    await training_tab(screen,3)
    var keeper_scope=find_named(screen,"TrainingScope") as OptionButton
    if not require(keeper_scope.item_count==2 and keeper_scope.get_item_text(0)=="Individual" and keeper_scope.get_item_text(1)=="Por demarcación","Porteros ofrece Equipo o Grupo personalizado"):
        return false
    var keeper_players=find_named(screen,"TrainingPlayer") as OptionButton
    for index in range(keeper_players.item_count):
        if not require(keeper_players.get_item_text(index).ends_with(" — Portero"),"Porteros permite elegir un jugador de campo"):
            return false
    if not require(find_named(screen,"TrainingPosition").item_count==1,"Porteros ofrece demarcaciones de campo"):
        return false
    await choose_clock(screen,"TrainingScope","Por demarcación")
    await choose_clock(screen,"TrainingPosition","Portero")
    await choose_control(screen,"TrainingDayKind",2)
    await choose_clock(screen,"TrainingTurn","Mañana")
    await choose_clock(screen,"TrainingTime","09:00")
    await choose_clock(screen,"TrainingEndTime","09:30")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingFormStatus").text=="","Porteros no guarda"):
        return false
    await training_tab(screen,0)
    var keeper_id=""
    for row in find_marked_nodes(screen,"daily_block_id"):
        if find_named(row,"TrainingDailyExercise").text.begins_with("09:00"):
            keeper_id=str(row.get_meta("daily_block_id"))
    if not require(keeper_id!="" and find_marked_nodes(screen,"daily_block_id").size()==5,"Porteros no aparece en Resumen"):
        return false
    find_named(screen,"TrainingDailyEdit_"+keeper_id).emit_signal("pressed")
    await process_frame
    await choose_clock(screen,"TrainingScope","Individual")
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingFormStatus").text=="","Porteros no guarda selección individual"):
        return false
    await training_tab(screen,0)
    if not require(find_named(screen,"TrainingDailyEdit_"+keeper_id)!=null and find_marked_nodes(screen,"daily_block_id").size()==5,"Cambio a individual duplica el ejercicio"):
        return false
    print("FURVO_KEEPER_SCOPE_OK: solo Individual y Por demarcación, jugadores porteros, guardado de ambas opciones y mismo ID")
    find_named(screen,"TrainingDailyDelete_"+keeper_id).emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("canceled")
    await process_frame
    if not require(find_marked_nodes(screen,"daily_block_id").size()==5,"Cancelar elimina actividad"):
        return false
    find_named(screen,"TrainingDailyDelete_"+keeper_id).emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    await training_tab(screen,3)
    if not require(find_marked_nodes(screen,"session_id").size()==4,"Eliminación no actualiza agenda global desde Porteros"):
        return false
    await training_tab(screen,1)
    find_named(screen,"TrainingObjective").text="Borrador protegido"
    find_named(screen,"TrainingObjective").emit_signal("text_changed","Borrador protegido")
    await training_tab(screen,0)
    if not require(find_named(screen,"TrainingConfirmDialog")!=null,"Cambio de pestaña descarta borrador sin confirmar"):
        return false
    find_named(screen,"TrainingConfirmDialog").emit_signal("canceled")
    await process_frame
    if not require(find_named(screen,"TrainingObjective").text=="Borrador protegido","Cancelar pierde borrador"):
        return false
    await choose_control(screen,"SportsTeamSelector",1)
    if not require(find_named(screen,"TrainingConfirmDialog")!=null,"Cambiar equipo descarta borrador sin advertir"):
        return false
    find_named(screen,"TrainingConfirmDialog").emit_signal("canceled")
    await process_frame
    if not require(find_named(screen,"SportsTeamSelector").selected==0 and find_named(screen,"TrainingObjective").text=="Borrador protegido","Cancelar cambio de equipo pierde contexto"):
        return false
    await training_tab(screen,0)
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    if not await check_rest_correction(screen):
        return false
    await training_tab(screen,1)
    if not await check_training_blocks(screen):
        return false
    await training_tab(screen,0)
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        scroll.scroll_vertical=0
        await process_frame
        if not require(find_named(screen,"TrainingWeeklyCalendar").columns==7 and find_named(screen,"TrainingWeekNavigation").global_position.y<find_named(screen,"TrainingWeeklyCalendar").global_position.y,"Resumen pierde calendario o navegación"):
            return false
        await capture_economy_screen("career-training-summary-calendar-"+str(resolution.x)+"x"+str(resolution.y))
        scroll.scroll_vertical+=int(find_named(screen,"TrainingDailyPanel").global_position.y-scroll.global_position.y)
        await process_frame
        for row in find_marked_nodes(screen,"daily_block_id"):
            if not require(row.has_meta("training_area") and find_named(row,"TrainingDailyParticipants").text.contains(" · "),"Fila sin categoría explícita"):
                return false
            if not require(row.get_global_rect().end.x<=root.size.x,"Fila de Resumen desborda"):
                return false
        if not require(find_named(screen,"TrainingSessionForm")==null,"Resumen vuelve a duplicar formularios"):
            return false
        await capture_economy_screen("career-training-summary-day-"+str(resolution.x)+"x"+str(resolution.y))
    print("FURVO_TRAINING_SUMMARY_OK: agenda única, seis categorías, edición cruzada por ID, confirmaciones, conflictos globales, descanso y tres resoluciones")
    return true

func choose_clock(screen: Node, name: String, text: String) -> void:
    var control=find_named(screen,name) as OptionButton
    for index in range(control.item_count):
        if control.get_item_text(index)==text:
            control.select(index)
            control.emit_signal("item_selected",index)
            await process_frame
            return
    require(false,"No existe horario o turno "+text+" en "+name)

func check_training_visual_controls(screen: Node) -> bool:
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        var clock=find_named(screen,"TrainingTime") as OptionButton
        var end=find_named(screen,"TrainingEndTime") as OptionButton
        scroll.scroll_vertical+=int(clock.global_position.y-scroll.global_position.y)-70
        await process_frame
        if not require(clock.size.x>=140 and clock.size.x<=180 and end.size.x>=140 and end.size.x<=180 and absf(clock.global_position.y-end.global_position.y)<1,"Selectores horarios no están compactos en una fila"):
            return false
        var selected=clock.selected
        clock.show_popup()
        await create_timer(.15).timeout
        var popup=clock.get_popup()
        print("FURVO_CLOCK_POPUP: visible=",popup.visible," size=",popup.size," max=",popup.max_size," items=",popup.item_count," wrap=",popup.wrap_controls)
        if not require(popup.visible and popup.size.y>=180 and popup.size.y<=popup.max_size.y and popup.size.y<320 and popup.item_count>8,"Popup horario sin altura limitada y desplazamiento"):
            return false
        await capture_economy_screen("career-training-visual-clock-"+str(resolution.x)+"x"+str(resolution.y))
        popup.hide()
        await process_frame
        if not require(clock.selected==selected,"Abrir/cerrar horario cambia selección"):
            return false
    await choose_control(screen,"TrainingScope",1)
    var player=find_named(screen,"TrainingPlayer") as OptionButton
    var excluded=find_named(screen,"TrainingExcluded") as ItemList
    for index in range(player.item_count):
        if not require(player.get_item_text(index).contains(" — ") and player.get_item_text(index)==excluded.get_item_text(index) and player.get_item_metadata(index)==excluded.get_item_metadata(index),"Participantes/excepciones sin misma identidad y posición"):
            return false
    await choose_control(screen,"TrainingPlayer",1)
    var chosen=player.get_item_metadata(player.selected)
    var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
    scroll.scroll_vertical+=int(player.global_position.y-scroll.global_position.y)-40
    await process_frame
    player.show_popup()
    await create_timer(.15).timeout
    if not require(player.get_popup().size.y>=180 and player.get_popup().size.y<=player.get_popup().max_size.y,"Selector de jugadores excesivamente alto"):
        return false
    await capture_economy_screen("career-training-visual-player-menu")
    player.get_popup().hide()
    await process_frame
    if not require(player.get_item_metadata(player.selected)==chosen,"Cerrar lista de jugadores pierde ID"):
        return false
    find_named(screen,"TrainingToggleExclusions").emit_signal("pressed")
    await process_frame
    excluded.select(0,false)
    excluded.select(2,false)
    var selected=excluded.get_selected_items()
    find_named(screen,"TrainingToggleExclusions").emit_signal("pressed")
    find_named(screen,"TrainingToggleExclusions").emit_signal("pressed")
    await process_frame
    if not require(excluded.get_selected_items()==selected and selected.size()==2,"Excepciones pierden selección múltiple al contraer"):
        return false
    scroll.scroll_vertical+=int(excluded.global_position.y-scroll.global_position.y)-70
    await process_frame
    await capture_economy_screen("career-training-visual-players")
    find_named(screen,"TrainingCancelSession").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    print("FURVO_TRAINING_VISUAL_OK: pestañas estables, horas compactas, popup con scroll, nombre/posición e ID y selección múltiple conservada")
    return true

func check_training_blocks(screen: Node) -> bool:
    var date=find_named(screen,"TrainingDate") as LineEdit
    if not require(not date.editable and date.text=="2026-07-01","Fecha del formulario no está fijada al día"):
        return false
    if not await check_training_visual_controls(screen):
        return false
    var start=find_named(screen,"TrainingTime") as OptionButton
    var finish=find_named(screen,"TrainingEndTime") as OptionButton
    if not require(start.get_item_text(0)=="07:00" and finish.get_item_text(finish.item_count-1)=="20:00","Horario operativo no es 07–20"):
        return false
    await choose_clock(screen,"TrainingTime","11:00")
    var proposed=find_named(screen,"TrainingDailyProposal")
    if proposed!=null and not require(not proposed.is_visible_in_tree(),"La propuesta obsoleta sigue visible tras editar la hora"):
        return false
    await choose_clock(screen,"TrainingEndTime","11:00")
    if not require(find_named(screen,"TrainingTimeError").text!="" and find_named(screen,"TrainingCalculatedDuration").text=="—" and find_named(screen,"TrainingSaveSession").disabled,"Horario inválido no muestra error junto a controles o muestra duración negativa"):
        return false
    find_named(screen,"TrainingCancelSession").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    var selected=find_named(find_named(screen,"TrainingWeeklyCalendar"),"TrainingDay_20260701")
    if not require(selected.get_meta("is_selected")==true,"Día seleccionado no permanece resaltado"):
        return false
    find_named(screen,"TrainingObjective").text="Borrador antes de cambiar fecha"
    find_named(screen,"TrainingObjective").emit_signal("text_changed","Borrador antes de cambiar fecha")
    await select_training_day(screen,"2026-07-02")
    if not require(find_named(screen,"TrainingConfirmDialog")!=null and find_named(screen,"TrainingDate").text=="2026-07-01","Fecha cambia sin advertir"):
        return false
    find_named(screen,"TrainingConfirmDialog").emit_signal("canceled")
    await process_frame
    await select_training_day(screen,"2026-07-02")
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"TrainingDailyEmpty")!=null and find_marked_nodes(screen,"daily_block_id").size()==0,"Día vacío muestra ejercicios ajenos o descanso automático"):
        return false
    await select_training_day(screen,"2026-07-01")
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        scroll.scroll_vertical=0
        await process_frame
        await capture_economy_screen("career-training-daily-calendar-"+str(resolution.x)+"x"+str(resolution.y))
        var panel=find_named(screen,"TrainingDailyPanel")
        scroll.scroll_vertical+=int(panel.global_position.y-scroll.global_position.y)
        await process_frame
        for row in find_marked_nodes(screen,"daily_block_id"):
            if not require(row.get_global_rect().end.x<=root.size.x,"Fila diaria desborda la pantalla"):
                return false
        await capture_economy_screen("career-training-daily-list-"+str(resolution.x)+"x"+str(resolution.y))
        var form=find_named(screen,"TrainingSessionForm")
        scroll.scroll_vertical+=int(form.global_position.y-scroll.global_position.y)
        await process_frame
        start=find_named(screen,"TrainingTime")
        finish=find_named(screen,"TrainingEndTime")
        if not require(absf(start.global_position.y-finish.global_position.y)<1 and finish.get_global_rect().end.x<=root.size.x,"Inicio y fin no comparten fila o se recortan"):
            return false
        await capture_economy_screen("career-training-daily-form-"+str(resolution.x)+"x"+str(resolution.y))
    print("FURVO_TRAINING_DAILY_LAYOUT_OK: orden calendario/lista/formulario, fecha fija, 07–20, validación sin negativos, cambio seguro de fecha y tres resoluciones")
    return true

func debug_training_widths(node: Node) -> void:
    if node is Control and node.get_combined_minimum_size().x>1150:
        print("FURVO_MINIMUM_WIDTH: ",node.name," minimum=",node.get_combined_minimum_size()," rect=",node.get_global_rect())
    for child in node.get_children():
        debug_training_widths(child)

func select_training_day(screen: Node, date: String) -> void:
    var calendar=find_named(screen,"TrainingWeeklyCalendar")
    for day in calendar.get_children():
        if day.get_meta("calendar_date")==date:
            find_named(day,"TrainingDateHeading").emit_signal("pressed")
            await process_frame
            return
    require(false,"Fecha fuera de la semana: "+date)

func check_rest_correction(screen: Node) -> bool:
    if not await training_add(screen,1,"Mañana","07:00","09:30"):
        return false
    if not await training_add(screen,1,"Tarde","16:00","19:00"):
        return false
    await training_tab(screen,0)
    if not require(find_named(screen,"TrainingDailyTotal").text=="420 min","Se mantiene un límite arbitrario de duración"):
        return false
    var long_ids: Array[String]=[]
    for row in find_marked_nodes(screen,"daily_block_id"):
        var hours=find_named(row,"TrainingDailyExercise").text
        if hours.begins_with("07:00") or hours.begins_with("16:00"):
            long_ids.append(str(row.get_meta("daily_block_id")))
    for id in long_ids:
        find_named(screen,"TrainingDailyDelete_"+id).emit_signal("pressed")
        await process_frame
        find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
        await process_frame
    find_named(screen,"TrainingConfirmRestDay").emit_signal("pressed")
    await process_frame
    var dialog=find_named(screen,"TrainingConfirmDialog")
    if not require(dialog!=null and dialog.dialog_text.contains("Este día tiene ejercicios programados"),"Descanso desde Resumen no exige confirmación"):
        return false
    dialog.emit_signal("canceled")
    await process_frame
    if not require(find_marked_nodes(screen,"daily_block_id").size()==4,"Cancelar descanso pierde planificación"):
        return false
    find_named(screen,"TrainingConfirmRestDay").emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"TrainingRestConfirmed")!=null and find_marked_nodes(screen,"daily_block_id").size()==0 and find_named(screen,"TrainingNoBlocks")==null and find_named(screen,"TrainingSessionForm")==null,"Descanso conserva ejercicios o turnos vacíos"):
        return false
    var day=find_named(find_named(screen,"TrainingWeeklyCalendar"),"TrainingDay_20260701")
    var icons=0
    for icon in find_marked_nodes(day,"training_icon"):
        if icon.get_meta("training_icon")=="Rest":
            icons+=1
    if not require(icons==1 and find_named(screen,"TrainingDailyTotal").text=="0 min","Descanso sin icono único o con minutos"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        scroll.scroll_vertical=0
        await process_frame
        await capture_economy_screen("career-training-summary-rest-"+str(resolution.x)+"x"+str(resolution.y))
    # Se repone la misma agenda aislada de QA para comprobar reapertura y módulos anteriores.
    for entry in [[1,"Tarde","15:00","15:45"],[2,"Mañana","10:00","10:15"],[1,"Mañana","11:00","11:15"],[5,"Mañana","12:00","12:15"]]:
        if not await training_add(screen,entry[0],entry[1],entry[2],entry[3]):
            return false
    await training_tab(screen,0)
    return require(find_marked_nodes(screen,"session_id").size()==4 and find_named(screen,"TrainingDailyTotal").text=="90 min","Restauración de agenda no es coherente")

# Disponibilidad administrativa: se usa exclusivamente la carrera aislada de esta prueba.
func check_training_availability(screen: Node) -> bool:
    await training_tab(screen,1)
    await select_training_day(screen,"2026-07-02")
    await choose_clock(screen,"TrainingTime","08:00")
    await choose_clock(screen,"TrainingEndTime","08:15")
    await choose_clock(screen,"TrainingScope","Individual")
    await choose_control(screen,"TrainingPlayer",0)
    var player_id=str(find_named(screen,"TrainingPlayer").get_item_metadata(0))
    find_named(screen,"TrainingSaveSession").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingFormStatus").text=="","Disponibilidad: no guarda actividad inicial"):
        return false
    find_named(screen,"TrainingAvailabilityOpen").emit_signal("pressed")
    await process_frame
    var dialog=find_named(screen,"TrainingAvailabilityDialog")
    if not require(dialog!=null and str(find_named(dialog,"AvailabilityPlayer").get_item_metadata(0))==player_id,"Disponibilidad pierde ID de futbolista"):
        return false
    find_named(dialog,"AvailabilityStart").text="2026-07-02"
    find_named(dialog,"AvailabilityEnd").text="2026-07-01"
    dialog.emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"TrainingAvailabilityDialog")!=null and find_named(screen,"AvailabilityStatus").text!="","Fecha inválida guarda o cierra restricción"):
        return false
    find_named(dialog,"AvailabilityEnd").text="2026-07-03"
    find_named(dialog,"AvailabilityNotes").text="Permiso administrativo QA"
    await choose_clock(screen,"AvailabilityReason","Permiso personal")
    dialog.emit_signal("confirmed")
    await process_frame
    if not require(find_named(screen,"TrainingAvailabilityDialog")==null and find_named(screen,"TrainingAvailabilityConflict")!=null and find_marked_nodes(screen,"daily_block_id").size()==1,"Restricción posterior borra actividad o no muestra conflicto"):
        return false
    screen.ReopenIsolatedCareerForValidation()
    await process_frame
    find_named(screen,"NavArea_Team").emit_signal("pressed")
    await process_frame
    find_named(screen,"NavPage_Training").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen,"TrainingAvailabilityConflict")!=null,"Reapertura pierde restricción o conflicto"):
        return false
    find_named(screen,"TrainingAvailabilityOpen").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"AvailabilityEntries",1)
    if not require(find_named(screen,"AvailabilityStart").text=="2026-07-02" and find_named(screen,"AvailabilityNotes").text=="Permiso administrativo QA","Restricción no recupera datos"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var editor=find_named(screen,"TrainingAvailabilityDialog") as Window
        if not require(editor.position.x>=0 and editor.position.y>=0 and editor.position.x+editor.size.x<=root.size.x and editor.position.y+editor.size.y<=root.size.y,"Editor disponibilidad fuera de ventana"):
            return false
        await capture_economy_screen("career-training-closure-editor-"+str(resolution.x)+"x"+str(resolution.y))
    await choose_control(screen,"AvailabilityState",1)
    var allowed=find_named(screen,"AvailabilityAllowedExercises") as ItemList
    var index=-1
    for i in range(allowed.item_count):
        if str(allowed.get_item_metadata(i))=="active_recovery":
            index=i
    allowed.select(index,false)
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        find_named(screen,"TrainingAvailabilityEditorScroll").scroll_vertical=10000
        await process_frame
        if not require(allowed.is_selected(index) and find_named(screen,"AvailabilityFinish").get_global_rect().end.y<=root.size.y,"Editor parcial pierde autorización o no permite desplazarse hasta finalizar"):
            return false
        await capture_economy_screen("career-training-closure-authorization-"+str(resolution.x)+"x"+str(resolution.y))
    find_named(screen,"TrainingAvailabilityDialog").emit_signal("confirmed")
    await process_frame
    await training_tab(screen,1)
    await select_training_day(screen,"2026-07-02")
    await choose_clock(screen,"TrainingScope","Individual")
    var person=find_named(screen,"TrainingPlayer") as OptionButton
    if not require(person.is_item_disabled(0) and person.get_item_text(0).contains("parcial") and find_named(screen,"TrainingSaveSession").disabled,"Parcial no autorizada permite confirmar actividad"):
        return false
    await choose_clock(screen,"TrainingScope","Equipo")
    if not require(find_named(screen,"TrainingAvailabilityPreview").text.contains("Exclusiones") and not find_named(screen,"TrainingSaveSession").disabled,"Colectivo no presenta exclusión individual"):
        return false
    # Solo inspección: se descarta el borrador, sin alterar la actividad original.
    find_named(screen,"TrainingTab_5").emit_signal("pressed")
    await process_frame
    if find_named(screen,"TrainingConfirmDialog")!=null:
        find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
        await process_frame
    await select_training_day(screen,"2026-07-02")
    await choose_clock(screen,"TrainingScope","Individual")
    await choose_clock(screen,"TrainingExercise","Recuperación activa")
    person=find_named(screen,"TrainingPlayer")
    if not require(not person.is_item_disabled(0) and person.get_item_text(0).contains("parcial"),"Recuperación explícitamente autorizada bloqueada"):
        return false
    for resolution in [Vector2i(1280,720),Vector2i(1600,900),Vector2i(1920,1080)]:
        root.size=resolution
        await create_timer(.2).timeout
        var scroll=find_named(screen,"TrainingScroll") as ScrollContainer
        scroll.scroll_vertical+=int(find_named(screen,"TrainingPlayer").global_position.y-scroll.global_position.y)-30
        await process_frame
        if not require(find_named(screen,"TrainingSaveSession").get_global_rect().end.x<=root.size.x,"Disponibilidad recorta formulario"):
            return false
        await capture_economy_screen("career-training-closure-participants-"+str(resolution.x)+"x"+str(resolution.y))
    # Finalización expresa, inclusiva. No se convierte en una inexistencia de cláusula/restricción.
    find_named(screen,"TrainingAvailabilityOpen").emit_signal("pressed")
    await process_frame
    await choose_control(screen,"AvailabilityEntries",1)
    find_named(screen,"AvailabilityEnd").text="2026-07-02"
    find_named(screen,"AvailabilityFinish").emit_signal("pressed")
    await process_frame
    await select_training_day(screen,"2026-07-03")
    if find_named(screen,"TrainingConfirmDialog")!=null:
        find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
        await process_frame
    await choose_clock(screen,"TrainingScope","Individual")
    if not require(not find_named(screen,"TrainingPlayer").is_item_disabled(0),"Restricción finalizada sigue bloqueando día posterior"):
        return false
    await training_tab(screen,0)
    if find_named(screen,"TrainingConfirmDialog")!=null:
        find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
        await process_frame
    await select_training_day(screen,"2026-07-02")
    var row=find_marked_nodes(screen,"daily_block_id")[0]
    find_named(screen,"TrainingDailyDelete_"+str(row.get_meta("daily_block_id"))).emit_signal("pressed")
    await process_frame
    find_named(screen,"TrainingConfirmDialog").emit_signal("confirmed")
    await process_frame
    await select_training_day(screen,"2026-07-01")
    root.size=Vector2i(1280,720)
    print("FURVO_TRAINING_AVAILABILITY_OK: restricción por ID, conflicto sin reescribir actividad, reapertura, parcial explícita, selección colectiva, finalización inclusiva y tres resoluciones")
    return true
