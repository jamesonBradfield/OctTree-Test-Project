extends Node

@onready var ui_parent : Control = get_node_or_null("/root/Main/UIParent")
@onready var ui := {
    "velocity":ui_parent.get_node_or_null("VisibleUI/PanelContainer/MarginContainer/VBoxContainer/VelocityIndicator"),
    "health":ui_parent.get_node_or_null("VisibleUI/PanelContainer/MarginContainer/VBoxContainer/HealthBar"),
    "ammo":ui_parent.get_node_or_null("VisibleUI/PanelContainer/MarginContainer/VBoxContainer/AmmoIndicator"),
    "pause":ui_parent.get_node_or_null("Menus/PausePanel"),
    "death":ui_parent.get_node_or_null("Menus/YouDiedPanel"),
    "end_round":ui_parent.get_node_or_null("Menus/RoundEndPanel"),
}
@onready var player : Player = get_node_or_null("/root/Main/Player")


