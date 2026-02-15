using System;

namespace ExtendedInspector
{
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class TagFieldAttribute : ExtendedPropertyAttribute { }

    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class TagMaskAttribute : ExtendedPropertyAttribute
    {
        public readonly bool useButtons = false;

        public TagMaskAttribute( bool useButtons )
        {
            this.useButtons = useButtons;
        }

        public TagMaskAttribute()
        {
            this.useButtons = false;
        }
    }
}