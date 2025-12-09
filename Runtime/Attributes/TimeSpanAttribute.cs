using System;
using System.Diagnostics;

namespace ExtendedInspector
{
    public readonly struct TimeRange
    {
        public readonly TimeUnit timeUnit;
        public readonly ulong value;

        public TimeRange( TimeUnit timeUnit, ulong value )
        {
            this.timeUnit = timeUnit;
            this.value = value;
        }

        public long ClampTime( long time )
        {
            return Math.Clamp( time, 0L, (long)value );
        }
    }

    public enum TimeUnit
    {
        Milis   = 0,
        Seconds = 1,
        Minutes = 2,
        Hours   = 3,
        Days    = 4
    }

    [System.Flags]
    public enum TimeUnitFlags
    {
        None   = 0,
        Mili   = 1 << TimeUnit.Milis,
        Second = 1 << TimeUnit.Seconds,
        Minute = 1 << TimeUnit.Minutes,
        Hour   = 1 << TimeUnit.Hours,
        Day    = 1 << TimeUnit.Days
    }
}

namespace ExtendedInspector
{
    [Conditional( "UNITY_EDITOR" )]
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    public class TimeSpanAttribute : ExtendedPropertyAttribute
    {
        public TimeUnitFlags unitFlags;
        public TimeRange timeRange;

        public TimeSpanAttribute( TimeUnitFlags unitFlags, TimeUnit timeRangeUnit, ulong range ) : base()
        {
            this.unitFlags = unitFlags;
            this.timeRange = new( timeRangeUnit, range );
        }
    }
}