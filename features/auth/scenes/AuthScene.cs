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
        // Phase 2.38 (B7): use the registered spelling so "Huynh" and
        // "huynh" always map to the same save file and hero name.
        gm.CurrentUsername = LocalAuth.CanonicalUsername(gm.AccountsPath, Username) ?? Username;
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
        try
        {
            if (LocalAuth.Login(gm.AccountsPath, Username, Password)) GoHub(gm);
            else SetStatus("Đăng nhập thất bại: sai tài khoản hoặc mật khẩu.");
        }
        catch (System.IO.InvalidDataException e) { SetStatus(e.Message); }
    }

    private void OnRegisterPressed()
    {
        var gm = GetNode<GameManager>("/root/GameManager");
        try
        {
            if (LocalAuth.Register(gm.AccountsPath, Username, Password, out var error)) GoHubSeeded(gm);
            else SetStatus("Đăng ký thất bại: " + error);
        }
        catch (System.IO.InvalidDataException e) { SetStatus(e.Message); }
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null) _statusLabel.Text = text;
    }
}
