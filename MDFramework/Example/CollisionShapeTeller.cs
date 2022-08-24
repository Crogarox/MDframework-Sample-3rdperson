using Godot;
using System;

public class CollisionShapeTeller : CollisionShape
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    [Export]
    public PlayerMP StoreSelf;
    public override void _Ready()
    {
        if(StoreSelf == null)
        {
            StoreSelf = (this.GetParent() as PlayerMP);
            //GD.Print("IN HERE! " + StoreSelf + " " + StoreSelf.Name);
        }
        //GD.Print(InformParent(-1));
    }
    public string InformParent(int Damage)
    {
        StoreSelf.UpdateHealth(Damage);
        StoreSelf.Hit();
        var PS = StoreSelf.GetPlayerSettings();
        return PS.PlayerString + " " + PS.PlayerColor + " " + PS.PlayerShotCounter;
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //public override void _Process(float delta)
  //{        if (Input.IsKeyPressed((int)KeyList.H)) {            GD.Print(InformParent(-1));        }  }
}
