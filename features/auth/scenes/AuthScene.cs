using Godot;

namespace LongTuong;

[GlobalClass]
public partial class AuthScene : Control
{
    private Label? _statusLabel;

    public override void _Ready()
    {
        UiTheme.ApplyBackground(this);
        UiTheme.StyleTitle(GetNode<Label>("TitleLabel"));
        UiTheme.StyleButton(GetNode<Button>("LoginButton"), true);
        UiTheme.StyleButton(GetNode<Button>("RegisterButton"));
        _statusLabel = GetNode<Label>("StatusLabel");
        UiTheme.StyleBody(_statusLabel, 15);
        GetNode<LineEdit>("PasswordEdit").Secret = true;
    }

    private string Username => GetNode<LineEdit>("UsernameEdit").Text.Trim();
    private string Password => GetNode<LineEdit>("PasswordEdit").Text;

    private void GoHub(GameManager gm)
    {
        gm.CurrentUsername = Username;
        gm.LoadGame();
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }

    private void GoHubSeeded(GameManager gm)
    {
        gm.CreateSeededSave(Username);
        GetTree().ChangeSceneToFile("res://features/hub/scenes/HubScene.tscn");
    }

    private void OnLoginPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (LocalAuth.Login(gm.AccountsPath, Username, Password)) GoHub(gm);
        else if (_statusLabel != null) _statusLabel.Text = "Đăng nhập thất bại: sai tài khoản hoặc mật khẩu.";
    }

    private void OnRegisterPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        if (LocalAuth.Register(gm.AccountsPath, Username, Password, out var error)) GoHubSeeded(gm);
        else if (_statusLabel != null) _statusLabel.Text = "Đăng ký thất bại: " + error;
    }
}
