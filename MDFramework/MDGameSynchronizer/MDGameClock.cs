using Godot;
using System;

namespace MD
{
    ///<summary>
    /// The MDGameClock ticks once per _PhysicsProcess(), it will pause when the game is paused.
    /// <para>The MDGameClock will automatically adjust it's offset for remote ticks based on connected players ping</para>
    ///</summary>
    [MDAutoRegister]
    public class MDGameClock : Node
    {
        private const string LOG_CAT = "LogGameClock";
        private const string DEBUG_CAT = "GameClock";

        ///<summary>If we are calculating offset from ping this is the minimum offset we can have</summary>
        public int MinimumOffset = 5;

        ///<summary>We add some extra buffer to the offset just in case</summary>
        public int OffsetBuffer = 5;

        ///<summary>Used to control how much we are allowing to be off by for the tick offset</summary>
        public int MaxTickDesynch = 5;

        protected static readonly float TICK_INTERVAL_MILLISECONDS = 1000f / Engine.IterationsPerSecond;

        protected static readonly float TICK_INTERVAL_DELTA = 1f / Engine.IterationsPerSecond;

        public delegate void GameTickEvent(uint Tick);

        ///<summary>Event triggers every time the clock ticks</summary>
        public event GameTickEvent OnGameTick = delegate { };

        ///<summary>Event triggers if a local tick is skipped (happens if framerate drops for any reason)</summary>
        public event GameTickEvent OnLocalSkippedTick = delegate { };

        ///<summary>Event triggers if a remote tick is skipped to adjust remote tick offset</summary>
        public event GameTickEvent OnRemoteSkippedTick = delegate { };

        ///<summary>Event triggers when ticks are adjusted from the outside, such as when you join a game in progress</summary>
        public event GameTickEvent OnGameTickChanged = delegate { };

        // The current tick
        protected uint CurrentTick = 0;

        // Used to adjust local tick
        protected int TickSynchAdjustment = 0;

        // The current remote tick
        protected int LastRemoteTickOffset = 0;

        // The current remote tick
        protected int CurrentRemoteTickOffset = 0;

        // Target for the current remote tick
        protected int CurrentRemoteTickOffsetTarget = 0;

        ///<summary>If set it will enforce this offset to remote ticks</summary>
        public int RemoteTickOffset { get; set; } = 0;

        ///<summary>As long as highest ping is within tolerance we will not change offset</summary>
        public float RemoteTickPingTolerance { get; set; } = 0.4f;

        ///<summary>The highest ping will be multiplied by this value before we calculate what the remote tick should be.</summary>
        public float RemoteTickPingModifier { get; set; } = 1f;

        protected bool LastTickDuplicated = false;

        protected float DeltaTickCounter = 0f;

        protected MDGameSynchronizer GameSynchronizer;


        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            MDLog.AddLogCategoryProperties(LOG_CAT, new MDLogProperties(MDLogLevel.Info));
            LoadConfig();
            GameSynchronizer = this.GetGameSynchronizer();
            CurrentRemoteTickOffset = MinimumOffset;
            CurrentRemoteTickOffsetTarget = MinimumOffset;
            LastRemoteTickOffset = MinimumOffset;
            GameSynchronizer.OnPlayerPingUpdatedEvent += OnPlayerPingUpdatedEvent;

            // TODO: Remove this, only here for debug
            MDOnScreenDebug.AddOnScreenDebugInfo(DEBUG_CAT, "GameClock Current Tick", () => CurrentTick.ToString());
            MDOnScreenDebug.AddOnScreenDebugInfo(DEBUG_CAT, "OS GetTickMsec", () => OS.GetTicksMsec().ToString());
            MDOnScreenDebug.AddOnScreenDebugInfo(DEBUG_CAT, "GameClock Remote Offset",
                () =>
                    $"{CurrentRemoteTickOffset} ({(int) (CurrentRemoteTickOffset * TICK_INTERVAL_MILLISECONDS)} Msec)");
            MDOnScreenDebug.AddOnScreenDebugInfo(DEBUG_CAT, "GameClock Remote Target Offset",
                () =>
                    $"{CurrentRemoteTickOffsetTarget} ({(int) (CurrentRemoteTickOffsetTarget * TICK_INTERVAL_MILLISECONDS)} Msec)");
        }

        public override void _ExitTree()
        {
            GameSynchronizer.OnPlayerPingUpdatedEvent -= OnPlayerPingUpdatedEvent;
        }

        private void LoadConfig()
        {
            // Since these variables are hit very often we don't want to go to the config every time
            MinimumOffset = this.GetConfiguration().GetInt(MDConfiguration.ConfigurationSections.GameClock, MDConfiguration.MINIMUM_OFFSET, MinimumOffset);
            OffsetBuffer = this.GetConfiguration().GetInt(MDConfiguration.ConfigurationSections.GameClock, MDConfiguration.OFFSET_BUFFER, OffsetBuffer);
            MaxTickDesynch = this.GetConfiguration().GetInt(MDConfiguration.ConfigurationSections.GameClock, MDConfiguration.MAX_TICK_DESYNCH, MaxTickDesynch);
        }

        public override void _Process(float delta)
        {
            if (GetTree().Paused)
            {
                return;
            }

            // Adjust ourselves in synch with server at most once per tick
            if (TickSynchAdjustment > 0)
            {
                MDLog.Trace(LOG_CAT, $"Tick {CurrentTick}, was skipped because adjustment was {TickSynchAdjustment}");
                OnLocalSkippedTick(CurrentTick);
                CurrentTick++;
                TickSynchAdjustment--;
            }
            else if (TickSynchAdjustment < 0)
            {
                MDLog.Trace(LOG_CAT, $"Tick {CurrentTick}, was repeated because adjustment was {TickSynchAdjustment}");
                LastTickDuplicated = true;
                CurrentTick--;
                TickSynchAdjustment++;
            }

            DeltaTickCounter += delta;
            if (DeltaTickCounter >= TICK_INTERVAL_DELTA)
            {
                // Increase tick counter
                DeltaTickCounter -= TICK_INTERVAL_DELTA;
                CurrentTick++;

                // Allow skipping a single tick per update to catch up
                if (DeltaTickCounter >= TICK_INTERVAL_DELTA)
                {
                    MDLog.Trace(LOG_CAT, $"Tick {CurrentTick}, was skipped because delta was {delta}");
                    OnLocalSkippedTick(CurrentTick);
                    DeltaTickCounter -= TICK_INTERVAL_DELTA;
                    CurrentTick++;
                }
            }

            AdjustRemoteTickOffset();
            OnGameTick(CurrentTick);
        }

        /// <summary>
        /// Sets the current tick
        /// </summary>
        /// <param name="Tick">The new tick</param>
        public void SetCurrentTick(uint Tick)
        {
            MDLog.Debug(LOG_CAT, $"Tick changed by code from {CurrentTick} to {Tick}");
            CurrentTick = Tick;
            TickSynchAdjustment = 0;
            OnGameTickChanged(CurrentTick);
        }

        ///<summary>Returns the current local tick</summary>
        public uint GetTick()
        {
            return Math.Max(CurrentTick, 0);
        }

        ///<summary>Returns the current remote tick used for synchronizing remote players</summary>
        public uint GetRemoteTick()
        {
            return (uint) Math.Max(CurrentTick - CurrentRemoteTickOffset, 0);
        }

        /// <summary>
        /// Get time in milliseconds (OS.GetTickMsec() format) until the tick or since the tick
        /// </summary>
        /// <param name="Tick">The tick</param>
        public long GetTimeOfTick(uint Tick)
        {
            long differenceBetweenCurrent = Tick - CurrentTick;
            long timeInMilliseconds = (long) Mathf.Floor(differenceBetweenCurrent * TICK_INTERVAL_MILLISECONDS);
            return timeInMilliseconds;
        }

        /// <summary>
        /// Returns the tick that we will be in at the given offset (msec)
        /// </summary>
        /// <param name="Offset">The offset time</param>
        public uint GetTickAtTimeOffset(long Offset)
        {
            int tickOffset = (int) Mathf.Round(Offset / TICK_INTERVAL_MILLISECONDS);
            return (uint) (GetTick() + tickOffset);
        }

        ///<summary>Adjust the remote tick offset if necessary</summary>
        private void AdjustRemoteTickOffset()
        {
            LastTickDuplicated = false;
            if (CurrentRemoteTickOffsetTarget != CurrentRemoteTickOffset)
            {
                // Ensure that we don't modify the offset two consecutive frames in a row
                if (LastRemoteTickOffset != CurrentRemoteTickOffset)
                {
                    LastRemoteTickOffset = CurrentRemoteTickOffset;
                    return;
                }

                if (CurrentRemoteTickOffsetTarget > CurrentRemoteTickOffset)
                {
                    MDLog.Debug(LOG_CAT,
                        $"Increasing remote tick offset from {CurrentRemoteTickOffset} to {CurrentRemoteTickOffset + 1}");
                    // Notify that we are skipping this tick
                    OnRemoteSkippedTick(GetRemoteTick());
                    CurrentRemoteTickOffset++;
                }
                else
                {
                    MDLog.Debug(LOG_CAT,
                        $"Decreasing remote tick offset from {CurrentRemoteTickOffset} to {CurrentRemoteTickOffset - 1}");
                    CurrentRemoteTickOffset--;
                    // We need to mark that last tick was a duplicate
                    LastTickDuplicated = true;
                }
            }
        }

        /// <summary>
        /// Called when player ping is updated
        /// </summary>
        /// <param name="PeerId">The peer the ping has updated for</param>
        /// <param name="Ping">The new ping</param>
        protected void OnPlayerPingUpdatedEvent(int PeerId, int Ping)
        {
            if (!GetTree().Paused)
            {
                CalculateRemoteOffset();
            }
        }

        /// <summary>Attempts to calculate what the remote offset should be based on the current ping</summary>
        private void CalculateRemoteOffset()
        {
            // Check if we got a fixed offset
            if (RemoteTickOffset > 0)
            {
                MDLog.Trace(LOG_CAT, $"Ping offset is set to be static at {RemoteTickOffset}");
                CurrentRemoteTickOffsetTarget = RemoteTickOffset + OffsetBuffer;
                return;
            }

            // Check if we got any ping
            int HighestPing = (int) Mathf.Ceil(GameSynchronizer.GetMaxPlayerPing() * RemoteTickPingModifier);
            if (HighestPing == 0)
            {
                CurrentRemoteTickOffsetTarget = MinimumOffset + OffsetBuffer;
                MDLog.Trace(LOG_CAT, $"We got no ping setting offset to minimum offset of {MinimumOffset}");
                return;
            }

            // Calculate offset based on ping
            int newOffset = (int) Mathf.Ceil(HighestPing / TICK_INTERVAL_MILLISECONDS);

            // If it is less than minimum set our target to minimum
            if (newOffset <= MinimumOffset)
            {
                CurrentRemoteTickOffsetTarget = MinimumOffset + OffsetBuffer;
                MDLog.Trace(LOG_CAT, $"Ping offset of {newOffset} is less than our minimum offset of {MinimumOffset}");
                return;
            }

            // Calculate the difference between new and old
            float difference = (float) (newOffset - CurrentRemoteTickOffset) / newOffset;
            difference = Mathf.Abs(difference);

            // Is the difference larger than our allowed tolerance?
            if (Mathf.Abs(difference) >= RemoteTickPingTolerance)
            {
                MDLog.Trace(LOG_CAT,
                    $"Ping difference is too large adjust remote tick offset target from {CurrentRemoteTickOffsetTarget} to {newOffset}");
                // We need to adjust the remote tick offset
                CurrentRemoteTickOffsetTarget = newOffset + OffsetBuffer;
            }
        }

        /// <summary>
        /// Clients receive this from the server as a way to attempt to keep them in synch in case they freeze for any reason
        /// </summary>
        /// <param name="EstimateTime">The estimated time for the tick</param>
        /// <param name="EstimatedTick">The estimated tick for the time</param>
        public void CheckSynch(long EstimateTime, long EstimatedTick)
        {
            long currentTime = Convert.ToInt64(OS.GetTicksMsec());
            if (EstimateTime < currentTime)
            {
                // This packet was delayed and is already in the past, ignore
                MDLog.Trace(LOG_CAT,
                    $"[{currentTime}] Ignoring tick packet as it was in the past {EstimatedTick} at {EstimateTime}");
                return;
            }

            // Figure out what tick we would be at when the estimated time is hit
            long localTickAtTime = GetTickAtTimeOffset(EstimateTime - currentTime);
            long tickOffset = EstimatedTick - localTickAtTime;
            if (Math.Abs(tickOffset) > MaxTickDesynch)
            {
                MDLog.Trace(LOG_CAT,
                    $"[{currentTime}] We are out of synch, we should be at tick {EstimatedTick} at {EstimateTime}");
                MDLog.Trace(LOG_CAT,
                    $"[{currentTime}] We will be at tick {localTickAtTime} which is off by {tickOffset}");
                // We are too far out of synch
                TickSynchAdjustment = (int) tickOffset;
            }
        }
    }
}