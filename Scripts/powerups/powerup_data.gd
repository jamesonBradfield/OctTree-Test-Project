class_name PowerupData
extends Resource

@export var name : String
@export var powerup : PackedScene
@export_enum("Common:1000", "Rare:500", "Epic:250","Legendary:100") var rarity: int
