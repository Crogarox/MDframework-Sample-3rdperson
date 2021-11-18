using Godot;
using System;

public class BtnQuit : Button
{
    public override void _Ready()
    {
        //RectGlobalPosition = new Vector2((GetViewportRect().Size / 2) + (GetViewportRect().Size / 4) );
    }
    public void _on_BtnQuit_pressed()
    {
        GetTree().Quit();
    }
}
