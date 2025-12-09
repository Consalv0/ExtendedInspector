using System;

namespace ExtendedInspector
{
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class LayerAttribute : ExtendedPropertyAttribute { }
}