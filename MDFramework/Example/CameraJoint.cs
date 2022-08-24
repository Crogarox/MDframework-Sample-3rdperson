using Godot;
using System;

public class CameraJoint : SpringArm
{
    [Export] float mouse_sensitivity = 0.05f;
    public float rotationDegreesX = 0.0f;
    public float rotationDegreesY = 0.0f;
    public override void _Ready()
    {
        SetAsToplevel(true);
        //Input.MouseMode =Input.MouseModeEnum.Captured);
        Input.MouseMode = Input.MouseModeEnum.Captured;
        var parentExclude = (GetParent() as KinematicBody);
        //var parentExclude = (GetParent() as RigidBody);

        AddExcludedObject(parentExclude.GetRid());
        //var originTemp = GlobalTransform;
        //originTemp.origin = new Vector3(0, 20, 0);
        //GlobalTransform = originTemp;
    }

    public override void _UnhandledInput(InputEvent @event)
    {

        //base._UnhandledInput(@event);
        if (@event is InputEventMouseMotion)
        {
            var eventy = @event as InputEventMouseMotion;
            //RotationDegrees.x -= evey.relative.y
            rotationDegreesX = RotationDegrees.x;
            rotationDegreesY = RotationDegrees.y;
            rotationDegreesX -= eventy.Relative.y * mouse_sensitivity;
            rotationDegreesX = Mathf.Clamp(rotationDegreesX, -90.0f, 90f);

            rotationDegreesY -= eventy.Relative.x * mouse_sensitivity;
            rotationDegreesY = Mathf.Wrap(rotationDegreesY, 0.0f, 360.0f);

            RotationDegrees = new Vector3(rotationDegreesX, rotationDegreesY, RotationDegrees.z);
        }
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
