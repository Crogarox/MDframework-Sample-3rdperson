using Godot;
using System;
using MD;

[MDAutoRegister]
public class Player : KinematicBody
{
    public class PlayerSettings
    {
        [MDReplicated]
        public Color PlayerColor { get; set; }

        [MDReplicated]
        public int PlayerShotCounter { get; set; }

        [MDReplicated]
        public String PlayerString { get; set; }
    }

    public const string PLAYER_GROUP = "PLAYERS";

    [Export]
    public float MaxSpeed = 150f;

    [Export]
    public float Acceleration = 2000f;

    [Export]
    public float WeaponCooldown = 1f;

    [MDBindNode("Camera")]
    protected Godot.Camera Camera;

    [MDBindNode("HitCounter")]
    protected Godot.Label HitCounter;

    [Export]
    public bool IsLocalPlayer = false;
    //protected bool IsLocalPlayer = true;

    protected Vector3 MovementAxis = Vector3.Zero;
    protected Vector3 Motion = Vector3.Zero;

    protected float WeaponActiveCooldown = 0f;

    protected float RsetActiveCooldown = 0f;

    protected PackedScene BulletScene = null;

    protected int HitCounterValue = 0;

    [MDReplicated(MDReliability.Unreliable, MDReplicatedType.Interval)]
    [MDReplicatedSetting(MDReplicator.Settings.GroupName, "PlayerPositions")]
    [MDReplicatedSetting(MDReplicator.Settings.ProcessWhilePaused, false)]
    [MDReplicatedSetting(MDReplicatedMember.Settings.OnValueChangedEvent, nameof(OnPositionChanged))]
    protected Vector3 NetworkedPosition;

    [MDReplicated(MDReliability.Reliable, MDReplicatedType.OnChange)]
    [MDReplicatedSetting(MDReplicatedMember.Settings.OnValueChangedEvent, nameof(UpdateColor))]
    protected PlayerSettings NetworkedPlayerSettings { get; set; }

    [Puppet]
    protected String RsetTest = "";
    public const float moveSpeed = 100f;
    public const float maxSpeed = 50.0f;
    public const float gravity = -25.0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AddToGroup(PLAYER_GROUP);
        SetupPlayer(GetNetworkMaster());

        if (IsLocalPlayer)
        {
            RandomNumberGenerator rnd = new RandomNumberGenerator();
            rnd.Randomize();

            // Let's set our color
            NetworkedPlayerSettings = new PlayerSettings();
            NetworkedPlayerSettings.PlayerColor = new Color(rnd.Randf(), rnd.Randf(), rnd.Randf());
            NetworkedPlayerSettings.PlayerShotCounter = 0;
            NetworkedPlayerSettings.PlayerString = $"Some string {rnd.RandiRange(0, 10000)}";
            //Modulate = NetworkedPlayerSettings.PlayerColor;
        }
        else
        {
            MDOnScreenDebug.AddOnScreenDebugInfo("RsetTest " + GetNetworkMaster().ToString(), () => { return RsetTest; });
        }
    }

    protected void UpdateColor()
    {
        if (NetworkedPlayerSettings == null || NetworkedPlayerSettings.PlayerColor == null)
        {
            return;
        }
        GD.Print($"NetworkedPlayerSettings Update, Counter: {NetworkedPlayerSettings.PlayerShotCounter}");
        //Modulate = NetworkedPlayerSettings.PlayerColor;
    }

    public void Hit()
    {
        HitCounterValue++;
        HitCounter.Text = HitCounterValue.ToString();
    }

    protected void OnPositionChanged()
    {
        if (!IsLocalPlayer)
        {
            //Position = NetworkedPosition;
            var transformz = GlobalTransform;
            transformz.origin = NetworkedPosition;
            GlobalTransform = transformz;
        }
    }

    [Remote]
    protected void OnShoot(Vector3 Target)
    {
        if (Target != Vector3.Zero)
        {
            Bullet bullet = (Bullet)GetBulletScene().Instance();
            var bulletGlobalTransform = GlobalTransform;
            bulletGlobalTransform.origin = GlobalTransform.origin + Vector3.Up;
            bullet.GlobalTransform = bulletGlobalTransform;
            //bullet.GlobalPosition = GlobalPosition;
            //bullet.GlobalPosition = GlobalPosition;
            bullet.SetOwner(GetNetworkMaster());
            //bullet.Owner = GetNetworkMaster();
            GetParent().AddChild(bullet);
            bullet.SetTarget(Target);
            (bullet as RigidBody).ApplyImpulse(new Vector3(0, 0, 0), -(bullet as Spatial).GlobalTransform.basis.z * bullet.speed);
        }
    }

    [Remote]
    protected void OnShoot()
    {
        GD.Print("OnShoot called");
    }

    [Remote]
    protected void OnShoot(String val, String val2)
    {
        GD.Print("OnShoot<String, String> called");
    }

    private PackedScene GetBulletScene()
    {
        if (BulletScene == null)
        {
            //BulletScene = (PackedScene)ResourceLoader.Load(Filename.GetBaseDir() + "/Bullet.tscn");
            //BulletScene = (PackedScene)ResourceLoader.Load(Filename.GetBaseDir() + "/../Prefabs/Bullets/Bullet.tscn");
            BulletScene = (PackedScene)ResourceLoader.Load("res://Prefabs/Bullets/Bullet.tscn");
        }

        return BulletScene;
    }

    public override void _PhysicsProcess(float delta)
    {
        //GD.Print(IsLocalPlayer);
        if (IsLocalPlayer)
        {
            WeaponActiveCooldown -= delta;
            RsetActiveCooldown -= delta;
            // Get input
            if (Input.IsMouseButtonPressed(1) && WeaponActiveCooldown <= 0f)
            {
                var cam = GetNode<Godot.Camera>("Camera");
                var mouseposition = GetViewport().GetMousePosition();
                var from = cam.ProjectRayOrigin(mouseposition);
                var to = from + cam.ProjectRayNormal(mouseposition) * 10f;
                var tranformz = this.GlobalTransform;
                tranformz.origin = to;
                //FMM = tranformz;

                // Shoot towards mouse position
                //this.MDRpc(nameof(OnShoot), GetGlobalMousePosition());
                this.MDRpc(nameof(OnShoot), to);
                this.MDRpc(nameof(OnShoot));
                this.MDRpc(nameof(OnShoot), "test", "test2");

                // Call it on local client, could do with RemoteSynch as well but then it won't work in standalone
                //OnShoot(GetGlobalMousePosition());
                OnShoot(to);
                NetworkedPlayerSettings.PlayerShotCounter++;
                WeaponActiveCooldown = WeaponCooldown;
            }
            else if (Input.IsMouseButtonPressed(2) && RsetActiveCooldown <= 0f)
            {
                RandomNumberGenerator rnd = new RandomNumberGenerator();
                rnd.Randomize();
                this.MDRset(nameof(RsetTest), rnd.RandiRange(0, 100000).ToString());
                RsetActiveCooldown = 0.1f;
            }

            MovementAxis = (GetInputAxis() + GetInputDirection()) * 2f;

            // Move
            if (MovementAxis == Vector3.Zero)
            {
                ApplyFriction(Acceleration * delta);
            }
            else
            {
                ApplyMovement(MovementAxis * Acceleration * delta, MaxSpeed);
            }

            //Motion = MoveAndSlide(Motion);
            // move character
            Motion = CalculateVelocity(Motion, MovementAxis, delta);
            Motion = MoveAndSlide(Motion, Vector3.Up);
            //Motion = 
            //this.ApplyImpulse(Vector3.Zero,  Motion);
            //NetworkedPosition = Position;
            NetworkedPosition = GlobalTransform.origin;
        }
    }

    protected virtual void ApplyMovement(Vector3 MovementSpeed, float Max)
    {
        this.Motion += MovementSpeed;
        this.Motion = Motion.Normalized();//Motion.Clamped(Max);
    }

    protected void ApplyFriction(float Amount)
    {
        if (Motion.Length() > Amount)
        {
            Motion -= Motion.Normalized() * Amount;
        }
        else
        {
            Motion = Vector3.Zero;
        }
    }
    public static Vector3 GetInputDirection()
    {
        return new Vector3(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            0.0f,
            Input.GetActionStrength("move_back") - Input.GetActionStrength("move_front")
            );
    }
    protected Vector3 GetInputAxis()
    {
        Vector3 axis = Vector3.Zero;
        axis.x = IsActionPressed("ui_right") - IsActionPressed("ui_left");
        axis.y = IsActionPressed("ui_down") - IsActionPressed("ui_up");
        return axis.Normalized();
    }

    Vector3 CalculateVelocity(Vector3 velocityCurrent, Vector3 moveDirection, float delta)
    {
        Vector3 velocityNew = velocityCurrent;
        velocityNew = moveDirection * delta * moveSpeed;
        if (velocityNew.Length() > maxSpeed)
        {
            velocityNew = velocityNew.Normalized() * maxSpeed;
        }
        // override because start value is 0.0f
        velocityNew.y = velocityCurrent.y + gravity * delta;

        return velocityNew;
    }

    protected int IsActionPressed(String Action)
    {
        if (Input.IsActionPressed(Action))
        {
            return 1;
        }

        return 0;
    }

    public void SetupPlayer(int PeerId)
    {
        if (PeerId == MDStatics.GetPeerId())
        {
            //Camera.Current = true;
            Camera.Current = true;
            IsLocalPlayer = true;
        }
    }
}