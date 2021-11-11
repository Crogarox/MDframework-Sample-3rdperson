using Godot;
using System;
using MD;

public class GameControllerMP : Spatial
{
    protected MDGameSession GameSession;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
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
        //var rng = new RandomNumberGenerator();
        //float my_random_number = rng.RandfRange(0, 10);
        this.SpawnNetworkedNode(GetPlayerScene(), "Player", PeerId);
        //this.SpawnNetworkedNode(GetPlayerScene(), "Player" , PeerId, (new Vector3(my_random_number *2, 3, my_random_number *2)));
    }

    private String GetPlayerScene()
    {
        // This is to avoid needing references
        return "res://MDFramework/Example/PlayerMP.tscn";
    }

    protected virtual void OnPlayerLeftEvent(int PeerId)
    {
        foreach (Node node in GetTree().GetNodesInGroup(Player.PLAYER_GROUP))
        {
            if (node.GetNetworkMaster() == PeerId)
            {
                node.RemoveAndFree();
            }
        }
    }
}