using System;
using System.Diagnostics;

namespace ExtendedInspector
{
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false )]
    [Conditional( "UNITY_EDITOR" )]
    public class ButtonAttribute : ExtendedPropertyAttribute
    {
        public readonly string label;
        public readonly string iconName;

        public ButtonAttribute( ) : base() { }

        public ButtonAttribute( string label, string iconName ) : base()
        {
            this.label = label;
            this.iconName = iconName;
        }

        public ButtonAttribute( string label ) : base()
        {
            this.label = label;
            this.iconName = null;
        }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class ShowInInspectorAttribute : ExtendedPropertyAttribute
    {
        public readonly long tickDelay;

        public ShowInInspectorAttribute( long tickDelay = 1000 ) : base()
        {
            this.tickDelay = tickDelay;
        }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class TrackSerializedPropertyAttribute : ExtendedPropertyAttribute
    {
        public string propertyPath;

        public TrackSerializedPropertyAttribute( string propertyPath ) : base()
        {
            this.propertyPath = propertyPath;
        }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class ReadOnlyAttribute : ExtendedPropertyAttribute
    {
        public ReadOnlyAttribute( ) : base() { }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class DisableInPlayModeAttribute : ExtendedPropertyAttribute
    {
        public DisableInPlayModeAttribute( ) : base() { }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class DisableInEditorModeAttribute : ExtendedPropertyAttribute
    {
        public DisableInEditorModeAttribute( ) : base() { }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class HideInEditorModeAttribute : ExtendedPropertyAttribute
    {
        public HideInEditorModeAttribute( ) : base() { }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class HideInPlayModeAttribute : ExtendedPropertyAttribute
    {
        public HideInPlayModeAttribute( ) : base() { }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class InlineEditorAttribute : ExtendedPropertyAttribute
    {
        public readonly bool expanded = false;

        public InlineEditorAttribute( bool expanded = false ) : base()
        {
            this.expanded = expanded;
        }
    }

    [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class InlinePropertyAttribute : ExtendedPropertyAttribute
    {
        public InlinePropertyAttribute( ) : base() { }
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class FoldoutGroupAttribute : ExtendedPropertyAttribute
    {
        public readonly bool expanded = false;
        public readonly int id;
        public readonly string label;

        public FoldoutGroupAttribute( string id, string label, bool expanded = false ) : base()
        {
            this.expanded = expanded;
            this.id = id.GetHashCode();
            this.label = label;
        }

        public FoldoutGroupAttribute( string id, bool expanded = false ) : base()
        {
            this.expanded = expanded;
            this.id = id.GetHashCode();
            this.label = id;
        }
    }
}