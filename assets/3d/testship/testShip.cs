using Godot;
using System;

public class testShip : StaticBody
{
    //public Node mainlevelnode;
    public override void _Ready()
    {
        //mainlevelnode = GetTree().Root.GetChild(GetTree().Root.GetChildCount() - 1);
        //this.AddCollisionExceptionWith(mainlevelnode);
        this.AddCollisionExceptionWith(GetParent());
    }
}
