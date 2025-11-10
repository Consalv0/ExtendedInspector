using System;
using System.Diagnostics;

namespace ExtendedInspector
{
    public enum EditorVisibility
    {
        Show,
        Hide,
        Enable,
        Disable
    }

    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class VisibleIfAttribute : PropertyAttribute
    {
        public readonly string memberInfoPathA;
        public readonly string memberInfoPathB;
        public readonly object compareValue;
        public readonly EditorVisibility visibility;

        public VisibleIfAttribute( string memberInfoPath, EditorVisibility visibility = EditorVisibility.Enable ) : base()
        {
            this.memberInfoPathA = memberInfoPath;
            this.memberInfoPathB = null;
            this.compareValue = null;
            this.visibility = visibility;
        }

        public VisibleIfAttribute( string memberInfoPathA, string memberInfoPathB, EditorVisibility visibility = EditorVisibility.Enable ) : base()
        {
            this.memberInfoPathA = memberInfoPathA;
            this.memberInfoPathB = memberInfoPathB;
            this.compareValue = null;
            this.visibility = visibility;
        }

        public VisibleIfAttribute( string memberInfoPathA, object compareValue, EditorVisibility visibility = EditorVisibility.Enable ) : base()
        {
            this.memberInfoPathA = memberInfoPathA;
            this.memberInfoPathB = null;
            this.compareValue = compareValue;
            this.visibility = visibility;
        }

        public bool ChangesEnableState => visibility == EditorVisibility.Enable || visibility == EditorVisibility.Disable;
        public bool ChangesHiddenState => visibility == EditorVisibility.Show || visibility == EditorVisibility.Hide;
    }
}