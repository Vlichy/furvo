extends SceneTree
# Prueba con renderizado real: cambia solo la ventana de esta instancia y comprueba salida normal.
func find_named(node: Node, target: String) -> Node:
    if node.name == target:
        return node
    for child in node.get_children():
        var found = find_named(child,target)
        if found != null:
            return found
    return null
func check(value: bool, message: String) -> bool:
    if not value:
        push_error(message)
        quit(1)
    return value
func escape() -> void:
    var input = InputEventKey.new()
    input.keycode = KEY_ESCAPE
    input.pressed = true
    Input.parse_input_event(input)
    await process_frame
    var release = InputEventKey.new()
    release.keycode = KEY_ESCAPE
    Input.parse_input_event(release)
    await process_frame
func _initialize() -> void:
    call_deferred("run_test")
func run_test() -> void:
    if not check(DisplayServer.get_name() != "headless","Esta prueba necesita renderizado y ventana reales"):
        return
    var args = OS.get_cmdline_user_args()
    var full = args.size() > 0 and args[0] == "fullscreen"
    var screen = load("res://Scenes/Main.tscn").instantiate()
    root.add_child(screen)
    await process_frame
    if not check(find_named(screen,"NewGame") != null and find_named(screen,"LoadGame").disabled,"Menú principal incorrecto"):
        return
    var before_mode = DisplayServer.window_get_mode()
    var before_size = DisplayServer.window_get_size()
    # Elegir sin aplicar y cancelar no debe cambiar el modo ni la resolución.
    find_named(screen,"Options").emit_signal("pressed")
    await process_frame
    var mode = find_named(screen,"DisplayModeSelection") as OptionButton
    mode.select(1)
    mode.emit_signal("item_selected",1)
    if not check(find_named(screen,"ResolutionSelection").disabled,"Pantalla completa no deshabilita resolución de ventana"):
        return
    find_named(screen,"CancelOptions").emit_signal("pressed")
    await process_frame
    if not check(DisplayServer.window_get_mode()==before_mode and DisplayServer.window_get_size()==before_size,"Cancelar opciones alteró la ventana"):
        return
    find_named(screen,"Options").emit_signal("pressed")
    await process_frame
    mode = find_named(screen,"DisplayModeSelection")
    mode.select(1 if full else 0)
    mode.emit_signal("item_selected",1 if full else 0)
    if not full:
        var resolution = find_named(screen,"ResolutionSelection") as OptionButton
        var selected_resolution = -1
        for i in range(resolution.item_count):
            if resolution.get_item_text(i) == "960 × 640":
                selected_resolution = i
        if not check(selected_resolution >= 0 and not resolution.disabled,"Falta una resolución de ventana disponible"):
            return
        resolution.select(selected_resolution)
    find_named(screen,"ApplyOptions").emit_signal("pressed")
    for i in range(5):
        await process_frame
    var applied = DisplayServer.window_get_mode()
    if not check(applied in [DisplayServer.WINDOW_MODE_FULLSCREEN,DisplayServer.WINDOW_MODE_EXCLUSIVE_FULLSCREEN] if full else applied==DisplayServer.WINDOW_MODE_WINDOWED,"Aplicar no cambió el modo real de ventana"):
        return
    if not full:
        if not check(DisplayServer.window_get_size() == Vector2i(960,640),"Aplicar resolución no cambió el tamaño real de ventana"):
            return
        print("FURVO_APPLICATION_SMOKE_OK: windowed 960x640, Cancelar conserva modo, Aplicar resolución y Salir principal")
        find_named(screen,"ExitGame").emit_signal("pressed")
        return
    find_named(screen,"NewGame").emit_signal("pressed")
    for i in range(200):
        if find_named(screen,"ViewClub") != null:
            break
        await create_timer(0.05).timeout
    await escape()
    var menu = find_named(screen,"ExplorationMenu")
    if not check(menu != null and menu.visible,"Esc no abre el menú en pantalla completa"):
        return
    print("FURVO_APPLICATION_SMOKE_OK: fullscreen, modo real aplicado, Esc y Salir exploración")
    find_named(menu,"ExitGame").emit_signal("pressed")
