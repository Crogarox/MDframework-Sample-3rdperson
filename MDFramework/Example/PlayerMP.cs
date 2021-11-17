using Godot;
using System;
using MD;

[MDAutoRegister]
public class PlayerMP : KinematicBody
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
    public float MaxSpeed = 2550f;

    [Export]
    public float Acceleration = 2000f;

    [Export]
    public float WeaponCooldown = 0.5f;
    [Export] float mouse_sensitivity = 0.05f;

    [MDBindNode("Camera")]
    protected Godot.Camera Camera;

    [MDBindNode("HitCounter")]
    protected Godot.Label HitCounter;

    [Export]
    public bool IsLocalPlayer = false;
    [Export]
    public CameraJoint spring_arm;
    [Export]
    public Spatial _model;
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
    //public const float moveSpeed = 500f;
    //public const float maxSpeed = 1000.0f;
    public const float moveSpeed = 500f; //Default was 100f
    public const float maxSpeed = 3000.0f;//Default was 50f
    public const float gravity = 0f;//Default was -25.0f
    public float rotationx = 0.0f;
    public Camera cammy;
    public float mouseY = 0.0f;
    public static float MouseLastY = 0.0f;
    public PackedScene prebullet;
    public Sprite3D crosshair3d;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AddToGroup(PLAYER_GROUP);
        SetupPlayer(GetNetworkMaster());

        spring_arm = GetNode<CameraJoint>("SpringArm");
        cammy = GetNode<Camera>("SpringArm/Camera");
        _model = (this as Spatial);

        if (IsLocalPlayer)
        {
            prebullet = GetBulletScene();
            crosshair3d = GetNode<Sprite3D>("Crosshair3d");

            HitCounter = GetNode<Godot.Label>("CanvasLayer/HitCounter");
            RandomNumberGenerator rnd = new RandomNumberGenerator();
            rnd.Randomize();

            // Let's set our color
            NetworkedPlayerSettings = new PlayerSettings();
            NetworkedPlayerSettings.PlayerColor = new Color(rnd.Randf(), rnd.Randf(), rnd.Randf());
            NetworkedPlayerSettings.PlayerShotCounter = 0;
            NetworkedPlayerSettings.PlayerString = $"Some string {rnd.RandiRange(0, 10000)}";
            //Modulate = NetworkedPlayerSettings.PlayerColor;
            SpatialMaterial newMaterial = new SpatialMaterial();
            newMaterial.AlbedoColor = NetworkedPlayerSettings.PlayerColor;
            var currentmaterial = GetNode("CollisionShape/CSGCylinder") as CSGCylinder;
            currentmaterial.MaterialOverride = newMaterial;
        }
        else
        {
            RandomNumberGenerator rnd = new RandomNumberGenerator();
            rnd.Randomize();
            SpatialMaterial newMaterial = new SpatialMaterial();
            newMaterial.AlbedoColor = new Color(rnd.Randf(), rnd.Randf(), rnd.Randf());
            var currentmaterial = GetNode("CollisionShape/CSGCylinder") as CSGCylinder;
            currentmaterial.MaterialOverride = newMaterial;
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
        GD.Print(HitCounterValue);
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
            Bullet bullet = (Bullet)prebullet.Instance();
            var bulletGlobalTransform = GlobalTransform;
            bulletGlobalTransform.origin = GlobalTransform.origin;
            bullet.GlobalTransform = bulletGlobalTransform;
            //bullet.GlobalPosition = GlobalPosition;
            //bullet.GlobalPosition = GlobalPosition;
            bullet.SetOwner(GetNetworkMaster());
            bullet.SetOwnerException(this);
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

    public override void _Process(float delta)
    {
        //base._Process(delta);
        if (IsLocalPlayer)
        {
            spring_arm.Translation = Translation;
        }
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
                //var cam = GetNode<Godot.Camera>("SpringArm/Camera");
                var mouseposition = GetViewport().GetMousePosition();
                var from = cammy.ProjectRayOrigin(mouseposition);
                var to = from + cammy.ProjectRayNormal(mouseposition) * 10f;
                var tranformz = this.GlobalTransform;
                tranformz.origin = to;

                //FMM = tranformz;

                // Shoot towards mouse position
                //this.MDRpc(nameof(OnShoot), GetGlobalMousePosition());
                this.MDRpc(nameof(OnShoot), to);

                //this.MDRpc(nameof(OnShoot));
                //this.MDRpc(nameof(OnShoot), "test", "test2");

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
            
            MovementAxis = (GetInputAxis() + GetInputDirection(spring_arm.Rotation.x)) * 2f;
            MovementAxis = MovementAxis.Rotated(Vector3.Up, spring_arm.Rotation.y).Normalized();

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

            //this.RotateX((Input.GetActionStrength("move_rotate_down") - Input.GetActionStrength("move_rotate_up")) / 10);
            //Motion = Motion.Rotated(Vector3.Up, spring_arm.Rotation.y).Normalized();
            Motion = CalculateVelocity(Motion, MovementAxis, delta);
            //ApplyImpulse(GlobalTransform.origin, Motion);
            Motion = MoveAndSlide(Motion, Vector3.Up);


            //var collisionmade = MoveAndCollide(Motion);
            /*
            if (Motion.Length() > 0.2)
            {
                Vector2 look_direction = new Vector2(Motion.z, Motion.x);
                //Rotation = new Vector3(Rotation.x, look_direction.Angle(), Rotation.z);
                //Rotate(Vector3.Up, look_direction.Angle());
                _model.Rotation = new Vector3(_model.Rotation.x, look_direction.Angle(), _model.Rotation.z);
            }
            */

            //Motion = 
            //this.ApplyImpulse(Vector3.Zero,  Motion);
            //NetworkedPosition = Position;
            Rpc("Update_Rotation", RotationDegrees);
            NetworkedPosition = GlobalTransform.origin;
        }
    }
    [Remote]
    public void Update_Rotation(Vector3 Rota)
    {
        if (Rota != null)
        {
            RotationDegrees = Rota;
        }
    }

    /*
    public override void _Input(InputEvent @event)
    {
        // Mouse in viewport coordinates.
        if (@event is InputEventMouseButton eventMouseButton)
        {
            GD.Print("Mouse Click/Unclick at: ", eventMouseButton.Position);
        }
    }
    */
    public override void _UnhandledInput(InputEvent @event)
    {

        if (IsLocalPlayer)
        {
            //base._UnhandledInput(@event);
            if (@event is InputEventMouseMotion)
            {
                var eventy = @event as InputEventMouseMotion;
                //RotationDegrees.x -= evey.relative.y
                var rotationDegreesX = RotationDegrees.x;
                var rotationDegreesY = RotationDegrees.y;
                mouseY = eventy.Relative.x * mouse_sensitivity;

                //rotationDegreesX -= eventy.Relative.y * mouse_sensitivity;
                //rotationDegreesX = Mathf.Clamp(rotationDegreesX, -180.0f, 180.0f);
                rotationDegreesY -= eventy.Relative.x * mouse_sensitivity;
                rotationDegreesY = Mathf.Wrap(rotationDegreesY, 0, 360.0f);
                var springarmclamp = Mathf.Clamp(spring_arm.rotationDegreesX, -90.0f, 90.0f);
                //RotationDegrees = new Vector3(rotationDegreesX, rotationDegreesY, RotationDegrees.z);
                //RotateX(eventy.Relative.y * mouse_sensitivity);

                RotationDegrees = new Vector3(springarmclamp, rotationDegreesY, RotationDegrees.z);
            }
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
    public static Vector3 GetInputDirection(float MouseY)
    {
        float LR = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");//Right is negative - left is 1
        float FB = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_front");//Back is + 1, Forward is -1
        float Ymove = 0;
        if (FB != 0)
        {
            Ymove = Mathf.Sin(MouseY);
            //GD.Print("----- | "+FB + " " + Ymove);
            if (FB < 0)
            {
                if (Ymove < 0)
                {
                    FB = FB + Ymove * -1;
                }
                else if (Ymove > 0)
                {
                    FB = FB + Ymove;
                }
            }
            else if (FB > 0)
            {
                Ymove = Ymove * -1;
                if(Ymove > 0)
                {
                    FB = FB - Ymove;
                }
                else if (Ymove < 0)
                {
                    FB = FB + Ymove;
                }
            }
        }
        //GD.Print("----- | " + FB + " " + (Ymove + (Input.GetActionStrength("move_up") - Input.GetActionStrength("move_down"))));
        return new Vector3(
            LR,
            (Ymove) + (Input.GetActionStrength("move_up") - Input.GetActionStrength("move_down")),//Input.GetActionStrength("move_up") - Input.GetActionStrength("move_down"),//0.0f,
            FB
            ).Normalized();
    }
    public static Vector3 GetInputDirection()
    {
        return new Vector3(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            Input.GetActionStrength("move_up") - Input.GetActionStrength("move_down"),//0.0f,
            Input.GetActionStrength("move_back") - Input.GetActionStrength("move_front")
            ).Normalized();
    }
    protected Vector3 GetInputAxis()
    {
        Vector3 axis = Vector3.Zero;
        axis.x = IsActionPressed("ui_right") - IsActionPressed("ui_left");
        axis.y = IsActionPressed("ui_up") - IsActionPressed("ui_down");
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
       // velocityNew.y = velocityCurrent.y + gravity * delta;

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