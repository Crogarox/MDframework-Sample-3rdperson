using Godot;
using System;

public class BulletKinematic : KinematicBody
{
    float timer = 0;
    [Export]
    public int damage = -1;
    [Export]
    public float speed = 100;
    [Export]
    public float lifetime = 15;
    [Signal]
    public delegate void DealDamage(byte damagePoints);
    [Export]
    public PackedScene sparks = (PackedScene)ResourceLoader.Load("res://Prefabs/sparks.tscn");
    protected int OwnerPeerId = -1;
    protected Vector3 MovementDirection = Vector3.Forward;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        timer += delta;
        if (timer > lifetime) QueueFree();

        var col = this.MoveAndCollide(-GlobalTransform.basis.z * speed * delta);
        if(col != null)
        {
            //GD.Print((col.Collider as Spatial).Name + " " + (col.Collider as Spatial).GetParent().Name + " " + col.ColliderId);
            //_OnCollisionEnter(col);
            var ParentOfCol = (col.Collider as Spatial).GetParent();
            ItHitSomething(ParentOfCol);
        }
    }
    public void SetOwner(int Owner)
    {
        OwnerPeerId = Owner;
    }
    public void SetOwnerException(Node Owner)
    {
        AddCollisionExceptionWith(Owner);
    }
    public void SetTarget(Vector3 Target)
    {
        LookAt(Target, Vector3.Up);
        MovementDirection = (Target - GlobalTransform.origin).Normalized();
    }

    private void ItHitSomething(Node body)
    {
        if (body.HasMethod("UpdateHealth"))
        {
            Connect(nameof(DealDamage), body, "UpdateHealth");
            EmitSignal(nameof(DealDamage), damage);
        }
        if (body.HasMethod("Hit"))
        {
            body.Call("Hit");
        }
        //if (body.HasMethod("InformParent"))        {            Godot.GD.Print(body.Call("InformParent", -1));        }
        Spatial newSparks = (Spatial)sparks.Instance();
        newSparks.Translation = Translation;//Translation;
        GetTree().Root.AddChild(newSparks);
        //newSparks.Rotation = Rotation;
        QueueFree();
    }

    private void _OnCollisionEnter(Node body)
    {

        if (body.HasMethod("UpdateHealth"))
        {
            Connect(nameof(DealDamage), body, "UpdateHealth");
            EmitSignal(nameof(DealDamage), damage);
        }
        if (body.HasMethod("Hit"))
        {
            body.Call("Hit");
        }
        if(body.HasMethod("InformParent"))
        {
            Godot.GD.Print(body.Call("InformParent", -1));
        }
        /*
        else if(body.GetParent().Name.Contains("Player") || body.Name.Contains("Player"))
        {
            if (body.HasMethod("Hit"))
            {
                body.Call("Hit");
            }
        }
        */
        //Godot.GD.Print(body.Name);
        Spatial newSparks = (Spatial)sparks.Instance();
        newSparks.Translation = Translation;//Translation;
        GetTree().Root.AddChild(newSparks);
        //newSparks.Rotation = Rotation;
        QueueFree();
    }


}

