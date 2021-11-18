using Godot;
using System;
using MD;

public class GameControllerMP : Spatial
{
    protected MDGameSession GameSession;
    [Export]
    public PackedScene PlayerResource;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PlayerResource = (PackedScene)ResourceLoader.Load("res://MDFramework/Example/PlayerMP.tscn");
        GameSession = this.GetGameSession();
        GameSession.OnPlayerJoinedEvent += OnPlayerJoinedEvent;
        GameSession.OnPlayerLeftEvent += OnPlayerLeftEvent;
    }

    public override void _ExitTree()
    {
        GameSession.OnPlayerJoinedEvent -= OnPlayerJoinedEvent;
        GameSession.OnPlayerLeftEvent -= OnPlayerLeftEvent;
    }

    protected virtual void OnPlayerJoinedEvent(int PeerId)
    {
        if (this.IsClient())
        {
            return;
        }
        CallDeferred(nameof(SpawnPlayer), PeerId);
    }

    protected void SpawnPlayer(int PeerId)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        //float my_random_number = rng.RandfRange(0, 5);
        int spawnpick = rng.RandiRange(0, 3);
        Vector3 spawnlocation = new Vector3();
        switch(spawnpick)
        {
            case 0: spawnlocation = new Vector3(150, 3, 150);   break;
            case 1: spawnlocation = new Vector3(100, 3, 100);   break;
            case 2: spawnlocation = new Vector3(50, 3, 50);     break;
            case 3: spawnlocation = new Vector3(125, 50, 125);  break;
            default:    spawnlocation = new Vector3(150, 50, 150); break;
                //case 3: spawnlocation = new Vector3(-50, 0, -50);   break; //bad location
                //case 3: spawnlocation = new Vector3(0, 3, 0); break; //bad location
        }
        GD.Print(spawnpick + " " + PeerId);
        //int addtoname = rng.RandiRange(0, 9999);
        //this.SpawnNetworkedNode(GetPlayerScene(), "Player", PeerId);
        //this.SpawnNetworkedNode(GetPlayerScene(), "Player" , PeerId, spawnlocation);//Original was -10 - 10, * 150
        this.SpawnNetworkedNode(PlayerResource, "Player", PeerId, spawnlocation);
    }

    private String GetPlayerScene()
    {
        // This is to avoid needing references
        return "res://MDFramework/Example/PlayerMP.tscn";
        //return "res://MDFramework/Example/PlayerMP_Rigid.tscn";
    }

    protected virtual void OnPlayerLeftEvent(int PeerId)
    {
        GD.Print(PeerId);
        foreach (Node node in GetTree().GetNodesInGroup(Player.PLAYER_GROUP))
        {
            if (node.GetNetworkMaster() == PeerId)
            {
                node.RemoveAndFree();
            }
        }
    }
}