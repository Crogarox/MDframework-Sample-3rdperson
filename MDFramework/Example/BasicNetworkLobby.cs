using Godot;
using System;
using MD;

/*
    Very simple example on how you can host / join games
*/
[MDAutoRegister]
public class BasicNetworkLobby : Node2D
{
    private const string LOG_CAT = "LogBasicNetworkLobby";

    private const string TEXT_JOIN_SERVER = "Join Server";

    private const string TEXT_CONNECTING = "Connecting...";

    [MDBindNode("CanvasLayer/CenterContainer/ParentGrid/CenterContainer/GridContainer/GridContainer/TextAddress")]
    protected TextEdit TextHost;

    [MDBindNode("CanvasLayer/CenterContainer/ParentGrid/CenterContainer/GridContainer/GridContainer/TextPort")]
    protected TextEdit TextPort;

    [MDBindNode("CanvasLayer/CenterContainer/ParentGrid/CenterContainer/GridContainer/BtnJoin")]
    protected Button ButtonJoin;

    [MDBindNode("CanvasLayer/CenterContainer/ParentGrid/CenterContainer/GridContainer/BtnHost")]
    protected Button ButtonHost;

    [MDBindNode("CanvasLayer/CenterContainer/ParentGrid/CenterContainer/GridContainer/BtnSinglePlayer")]
    protected Button ButtonSinglePlayer;

    [MDBindNode("CanvasLayer/BtnDisconnect")]
    protected Button ButtonDisconnect;


    [MDBindNode("CanvasLayer/CenterContainer")]
    protected Control InterfaceRoot;

    protected MDGameSession GameSession;
    public PackedScene prepacked;
    //public PackedScene prepackedPlayer;
    //public GameControllerMP gamecontMP;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //gamecontMP = GetNode<GameControllerMP>("GameControllerMP");
        prepacked = (PackedScene)ResourceLoader.Load("res://src/Level/TestLevel3.tscn");
        //prepacked = (PackedScene)ResourceLoader.Load("res://src/Level/TestLevel2.tscn");
        //prepackedPlayer = (PackedScene)ResourceLoader.Load("res://src/PlayerMP.tscn");
        GameSession = this.GetGameSession();
        GameSession.OnPlayerJoinedEvent += OnPlayerJoined;
        GameSession.OnPlayerLeftEvent += OnPlayerLeft;
        GameSession.OnSessionStartedEvent += OnSessionStartedEvent;
        GameSession.OnSessionFailedEvent += OnSessionFailedOrEndedEvent;
        GameSession.OnSessionEndedEvent += OnSessionFailedOrEndedEvent;
        ToggleDisconnectVisible(false);
    }

    public override void _ExitTree()
    {
        GameSession.OnPlayerJoinedEvent -= OnPlayerJoined;
        GameSession.OnPlayerLeftEvent -= OnPlayerLeft;
        GameSession.OnSessionStartedEvent -= OnSessionStartedEvent;
        GameSession.OnSessionFailedEvent -= OnSessionFailedOrEndedEvent;
        GameSession.OnSessionEndedEvent += OnSessionFailedOrEndedEvent;
    }

    public void SetUpGameScenes()
    {
        //var testing = prepacked.Instance();
        //testing.Name = "levelz";
        //this.AddChild(testing);
        //GetTree().Root.AddChild(testing);
        //GetTree().Root.AddChild(testlevel);

        //Spatial testlevel = new Spatial();
        //testlevel.Name = "OneTwoThree";
        //this.AddNodeToRoot(testlevel);
        //GD.Print( "\n\n\n" + GetTree().GetNodeCount() + " " + GetTree().Root + " " + GetParent().Name + this);
        //this.AddChild(testing);
        //GetTree().Root.AddChild(this);
        //var netnode = GameSession.SpawnNetworkedNode(prepacked, this, "levelz", -1,Vector3.Down);
        //GetTree().Root.AddChild(netnode);
        //var testing = prepacked.Instance();
        //testing.Name = "levelz";
        //var netnode = GameSession.SpawnNetworkedNode(prepacked, "levelz", -1, Vector3.Zero);
        //this.AddChild(testing);
        //GetTree().Root.AddChild(testing);
        //GameSession.SpawnNetworkedNode("res://src/Level/TestLevel3.tscn", "levelz", -1, Vector3.Zero);
        //gamecontMP.SpawnScenez("res://src/Level/TestLevel3.tscn");
    }

    public void SetUpGamePlayerScenes()
    {
        //var playery = prepackedPlayer.Instance();
        //playery.Name = "Player2";
        //GetTree().Root.AddChild(playery);
        //GameSession.SpawnNetworkedNode(prepackedPlayer, "Player2", true);
        //GameSession.SpawnNetworkedNode("res://src/PlayerMP.tscn", "Player2", -1, Vector3.Forward);
    }

    #region USER INPUT

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            if (Input.GetMouseMode() != Input.MouseMode.Visible)
            {
                Input.SetMouseMode(Input.MouseMode.Visible);
            }
        }
        if (@event is InputEventKey eventKey)
        {
            if (eventKey.Pressed && eventKey.Scancode == (int)KeyList.K)
            {
                GameSession.SpawnNetworkedNode(prepacked, "levelz", -1, Vector3.Zero);
            }
        }
    }

    protected virtual void OnHostPressed()
    {
        GameSession.StartServer(GetPort());
        //SetUpGameScenes();
        //SetUpGamePlayerScenes();


    }

    private void OnDisconnectPressed()
    {
        if (GameSession.IsSessionStarted)
        {
            GameSession.Disconnect();
        }
    }

    protected virtual void OnJoinPressed()
    {
        // Attempt to connect as client
        if (GameSession.StartClient(GetHost(), GetPort()))
        {
            // Disable buttons while we try to join
            ToggleButtons(false);
            SetJoinButtonText(TEXT_CONNECTING);
            //SetUpGamePlayerScenes();
        }
        
    }

    protected virtual void OnSinglePlayerPressed()
    {
        // Start a single player game
        GameSession.StartStandalone();
        SetUpGameScenes();
        //SetUpGamePlayerScenes();


    }

    #endregion

    #region EVENTS

    protected virtual void OnSessionStartedEvent()
    {
        ToggleInterface(false);
        ToggleDisconnectVisible(true);
        //GameSession.SpawnNetworkedNode(prepacked, "levelz", -1, Vector3.Zero);
        
    }

    protected virtual void OnSessionFailedOrEndedEvent()
    {
        ToggleButtons(true);
        ToggleInterface(true);
        SetJoinButtonText(TEXT_JOIN_SERVER);
        ToggleDisconnectVisible(false);
    }

    protected virtual void OnPlayerLeft(int PeerId)
    {
        // TODO: Do cleanup code here
        // Note: You can't access PlayerInfo here, to access that override PreparePlayerInfoForRemoval in GameSession.
        MDLog.Info(LOG_CAT, $"Player left with PeerID {PeerId}");
    }

    protected virtual void OnPlayerJoined(int PeerId)
    {
        // TODO: Spawn player here, should be done with CallDeferred
        MDLog.Info(LOG_CAT,
            $"Player joined {GameSession.GetPlayerInfo(PeerId).PlayerName} with PeerID {PeerId}");
    }

    #endregion

    #region SUPPORT METHODS

    protected void SetJoinButtonText(String text)
    {
        if (ButtonJoin == null)
        {
            MDLog.Warn(LOG_CAT, "Could not find join button");
            return;
        }

        ButtonJoin.Text = text;
    }

    protected void ToggleInterface(bool visible)
    {
        if (InterfaceRoot == null)
        {
            MDLog.Warn(LOG_CAT, "Could not find interface root");
            return;
        }

        InterfaceRoot.Visible = visible;
    }

    protected void ToggleDisconnectVisible(bool visible)
    {
        if (ButtonDisconnect == null)
        {
            MDLog.Warn(LOG_CAT, "Could not find disconnect button");
            return;
        }

        ButtonDisconnect.Visible = visible;
    }


    private void ToggleButtons(bool Enabled)
    {
        ToggleButton(ButtonHost, Enabled);
        ToggleButton(ButtonJoin, Enabled);
        ToggleButton(ButtonSinglePlayer, Enabled);
    }

    private void ToggleButton(Button Button, bool Enabled)
    {
        if (Button == null)
        {
            MDLog.Warn(LOG_CAT, "A button was null");
            return;
        }

        Button.Disabled = !Enabled;
    }

    private String GetHost()
    {
        if (TextHost == null)
        {
            MDLog.Warn(LOG_CAT, "Could not find host textbox");
            return "127.0.0.1";
        }

        return TextHost.Text;
    }

    private int GetPort()
    {
        if (TextPort == null)
        {
            MDLog.Warn(LOG_CAT, "Could not find port textbox");
            return 1234;
        }

        return Int32.Parse(TextPort.Text);
    }

    #endregion
}
