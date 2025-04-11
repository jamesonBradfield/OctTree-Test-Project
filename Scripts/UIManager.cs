using Godot;

public partial class UIManager : Control
{
    Control pause_menu_content;
    Control options_menu_content;
    Control movement_menu_content;
    Slider mouse_sens_slider;
    Slider controller_sens_slider;
    Player player;
    Head head;
    StateMachine fsm;
    Label stateText;
    Label wishDirText;
    Label velocityText;
    public override void _Ready()
    {
        stateText = GetNode<Label>("MovementDebug/PanelContainer/MarginContainer/VBoxContainer/StatePanelContainer/MarginContainer/VBoxContainer/CurrentState");
        wishDirText = GetNode<Label>("MovementDebug/PanelContainer/MarginContainer/VBoxContainer/StatePanelContainer/MarginContainer/VBoxContainer/WishDir");
        velocityText = GetNode<Label>("MovementDebug/PanelContainer/MarginContainer/VBoxContainer/StatePanelContainer/MarginContainer/VBoxContainer/Velocity");
        pause_menu_content = GetNode<Control>("PausePanel");
        options_menu_content = GetNode<Control>("OptionsPanel");
        movement_menu_content = GetNode<Control>("MovementPanel");
        mouse_sens_slider = GetNode<Slider>("OptionsPanel/MarginContainer/OptionsContainer/MouseContainer/MouseSlider");
        controller_sens_slider = GetNode<Slider>("OptionsPanel/MarginContainer/OptionsContainer/ControllerContainer/ControllerSlider");
        player = GetNode<Player>("/root/Main/Player");
        head = player.GetNode<Head>("Head");
        fsm = player.GetNode<StateMachine>("FSM");
        stateText.Text = "CurrentState : " + fsm.CurrentState.ToString();
        wishDirText.Text = "WishDir : " + player.WishDir;
        velocityText.Text = "Velocity : " + player.Velocity;
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        stateText.Text = "CurrentState : " + fsm.CurrentState.ToString();
        wishDirText.Text = "WishDir : " + player.WishDir;
        velocityText.Text = "Velocity : " + player.Velocity;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else if (@event.IsActionPressed("pause"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (pause_menu_content.Visible)
            {
                pause_menu_content.Hide();
                //show cursor
            }
            else if (!pause_menu_content.Visible && !options_menu_content.Visible)
            {
                pause_menu_content.Show();
                //hide cursor
            }
            else if (!pause_menu_content.Visible && options_menu_content.Visible)
            {
                options_menu_content.Hide();
                //show cursor
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            else if (!pause_menu_content.Visible && movement_menu_content.Visible)
            {
                movement_menu_content.Hide();
                //show cursor
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }
    public void resume()
    {
        pause_menu_content.Hide();
        //Show Cursor
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    public void quit()
    {
        GetTree().Quit();
    }
    public void ReturnToPauseMenu()
    {
        pause_menu_content.Show();
        movement_menu_content.Hide();
        options_menu_content.Hide();
    }
    public void OptionsPressed()
    {
        pause_menu_content.Hide();
        options_menu_content.Show();
        mouse_sens_slider.Value = head.Sensitivity;
        controller_sens_slider.Value = head.ControllerSensitivity;
    }
    public void MovementPressed()
    {
        pause_menu_content.Hide();
        movement_menu_content.Show();
        mouse_sens_slider.Value = head.Sensitivity;
        controller_sens_slider.Value = head.ControllerSensitivity;
    }
    public void Retry()
    {
        GetTree().ReloadCurrentScene();
    }
}
