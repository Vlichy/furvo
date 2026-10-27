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

func run_test() -> void:
    root.size = Vector2i(1280, 720)
    var screen = load("res://Scenes/Main.tscn").instantiate()
    root.add_child(screen)
    await process_frame
    if not require(find_named(screen, "BrowseClubs") != null, "Falta pantalla inicial"):
        return
    find_named(screen, "BrowseClubs").emit_signal("pressed")
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
        for tab_index in [2, 3, 0, 1]:
            club_tabs.current_tab = tab_index
            await process_frame
            if not require(club_tabs.get_child(tab_index).is_visible_in_tree() and club_tabs.get_child(tab_index).get_meta("club_id") == expected_club_id, "La pestaña no conserva el club seleccionado"):
                return
            if sample_index == 0:
                for resolution in [Vector2i(960, 640), Vector2i(1600, 900), Vector2i(1280, 720)]:
                    root.size = resolution
                    await process_frame
                    await process_frame
                    var club_footer = find_named(screen, "BackClubs") as Control
                    if not require(club_footer.get_global_rect().end.y <= root.size.y + 1 and club_tabs.get_global_rect().end.y <= club_footer.get_global_rect().position.y + 1, "La ficha de club oculta o solapa la navegación al redimensionar"):
                        return
            if tab_index == 2:
                var historic_table = find_trees(find_named(screen, "HistoryStandingsTable"))[0]
                if not require(historic_table.get_root().get_child_count() == 0 and find_named(screen, "ClubHistoryChart").get_meta("result_count") == 0, "Se inventaron clasificaciones históricas"):
                    return
            if tab_index == 3:
                for amount in ["EconomyCashValue", "EconomyTransferBudgetValue", "EconomyWageBudgetValue", "EconomyCommittedWagesValue", "EconomyIncomeValue", "EconomyExpensesValue", "EconomyBalanceValue", "EconomyDebtValue", "EconomySituationValue"]:
                    if not require(find_named(screen, amount).text == "No disponible", "Se inventó una cifra económica"):
                        return
        if not require(find_named(screen, "ViewPlayer").visible, "No se puede abrir un futbolista desde Plantilla"):
            return
        var squad = find_trees(screen)[0]
        if not require(squad.get_root().get_child_count() == 24, "Se esperaban veinticuatro jugadores"):
            return
        if sample_index == 0:
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
    find_named(screen, "Home").emit_signal("pressed")
    await process_frame
    if not require(find_named(screen, "BrowseClubs") != null, "No regresó al inicio"):
        return
    screen.queue_free()
    await process_frame
    print("FURVO_UI_SMOKE_OK: club Información/Plantilla/Historia/Economía, estados pendientes, 24 jugadores, 51 atributos y navegación")
    quit(0)
