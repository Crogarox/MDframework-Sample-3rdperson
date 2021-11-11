using Godot;
using System.Collections.Generic;
using System.Reflection;

namespace MD
{
    /// <summary>
    /// Clocked interpolated vector2
    /// </summary>
    public class MDCRMInterpolatedVector2 : MDCRMInterpolatedValue<Vector2>
    {
        public MDCRMInterpolatedVector2(MemberInfo Member, bool Reliable, MDReplicatedType ReplicatedType,
            WeakRef NodeRef, MDReplicatedSetting[] Settings)
            : base(Member, Reliable, ReplicatedType, NodeRef, Settings)
        {
        }
        
        protected override Vector2 LinearInterpolate(Vector2 LastValue, Vector2 Value, float Alpha)
        {
            return LastValue.LinearInterpolate(Value, Alpha);
        }

        protected override bool HasValueChanged(Vector2 CurValue, Vector2 NewValue)
        {
            return CurValue.IsEqualApprox(NewValue) == false;
        }
    }
}