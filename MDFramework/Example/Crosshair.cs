using Godot;
using System;

public class Crosshair : Sprite
{
	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GlobalPosition = new Vector2(GetViewportRect().Size / 2);
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(float delta)
  {
		//if (Input.GetMouseMode() == Input.MouseMode.Captured)
		if(Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			GlobalPosition = new Vector2(GetViewportRect().Size / 2);
		}
		else
		{
			GlobalPosition = new Vector2(GetGlobalMousePosition());
		}
	}
}
