extends PanelContainer

@export var cards_container : HBoxContainer
@export var powerup_card_base : PackedScene
@export var powerups : Array[PowerupData]
var rarities : Dictionary = {
    0:1000,
    1:500,
    2:250,
    3:100
    }
@export var powerups_displayed : int = 3
var rng = RandomNumberGenerator.new()

func _process(_delta):
    if self.visible:
        Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)

func get_rarity():
    rng.randomize()
    var weighted_sum = 0

    for n in rarities:
        weighted_sum += rarities[n]

    var item = rng.randi_range(0,weighted_sum)

    for n in rarities:
        if item <= rarities[n]:
            return n
        item -= rarities[n]

func get_powerup_by_rarity():
    var rarity = rarities[get_rarity()]
    var powerups_in_same_rarity : Array[PowerupData]
    for powerup in powerups:
        if powerup.rarity == rarity:
            powerups_in_same_rarity.append(powerup)
    if(powerups_in_same_rarity):
        if powerups_in_same_rarity.size() == 1:
            return powerups_in_same_rarity[0]
        else:
            return powerups_in_same_rarity[randi_range(0,powerups_in_same_rarity.size()-1)]    

func open_upgrade_menu():
    # remove children of our card ui before making more
    if cards_container.get_child_count() > 0:
        for child in cards_container.get_children():
            cards_container.remove_child(child)
    for i in range(0,powerups_displayed):
        var card_data : PowerupData = get_powerup_by_rarity()
        var new_card = powerup_card_base.instantiate()
        new_card.name = card_data.name 
        new_card.get_node("Name").text = card_data.name
        new_card.get_node("Rarity").text = str("Rarity : ",card_data.rarity)
        new_card.get_node("Button").connect("pressed",func(): 
            var powerup_node = card_data.powerup.instantiate()
            powerup_node.name = card_data.name
            Globals.powerups.add_child(powerup_node)
            if Globals.powerups.find_child(powerup_node.name):
                powerup_node.on_powerup_stack()
            else:
                powerup_node.on_powerup_add()
            self.hide()
            Globals.current_round_state = Globals.round_state.RESTART
        )
        cards_container.add_child(new_card)
