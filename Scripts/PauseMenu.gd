extends Control

@onready var pause_menu_content : Control = $PausePanel
@onready var options_menu_content : Control =$OptionsPanel 
@onready var movement_menu_content : Control =$MovementPanel 
@onready var mouse_sens_slider = $OptionsPanel/MarginContainer/OptionsContainer/MouseContainer/MouseSlider
@onready var controller_sens_slider = $OptionsPanel/MarginContainer/OptionsContainer/ControllerContainer/ControllerSlider

func _unhandled_input(event):
    if event is InputEventMouseButton:
        Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
    elif event.is_action_pressed("pause"):
        Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
        if pause_menu_content.visible:
            pause_menu_content.hide()
            %Cursor.show()
            Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
        elif not pause_menu_content.visible and not options_menu_content.visible:
            pause_menu_content.show()
            %Cursor.hide()
        elif not pause_menu_content.visible and options_menu_content.visible:
            options_menu_content.hide()
            %Cursor.show()
            Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
        elif not pause_menu_content.visible and movement_menu_content.visible:
            movement_menu_content.hide()
            %Cursor.show()
            Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)


func resume() -> void:
    pause_menu_content.hide()
    %Cursor.show()
    Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func quit() -> void:
    get_tree().quit()

func return_to_pause_menu() -> void:
    pause_menu_content.show()
    movement_menu_content.hide()
    options_menu_content.hide()

func _on_options_pressed() -> void:
    pause_menu_content.hide()
    options_menu_content.show()
    mouse_sens_slider.value = %Player.look_sensitivity
    controller_sens_slider.value = %Player.controller_look_sensitivity


func _on_movement_pressed() -> void:
    pause_menu_content.hide()
    movement_menu_content.show()
    mouse_sens_slider.value = %Player.look_sensitivity
    controller_sens_slider.value = %Player.controller_look_sensitivity


func retry() -> void:
    get_tree().reload_current_scene()
