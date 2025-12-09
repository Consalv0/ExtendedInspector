using System;
using System.Diagnostics;

namespace ExtendedInspector
{
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.GenericParameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true )]
    [Conditional( "UNITY_EDITOR" )]
    public class EditorPrefVisibilityAttribute : ExtendedPropertyAttribute
    {
        public readonly EditorVisibility visibility;
        public readonly string key;

        public EditorPrefVisibilityAttribute( string key, EditorVisibility visibility = EditorVisibility.Show ) : base()
        {
            this.key = key;
            this.visibility = visibility;
        }

        public bool ChangesEnableState => visibility == EditorVisibility.Enable || visibility == EditorVisibility.Disable;
        public bool ChangesHiddenState => visibility == EditorVisibility.Show || visibility == EditorVisibility.Hide;

#if UNITY_EDITOR
        public bool IsEnabled_Editor
        {
            get
            {
                if ( visibility == EditorVisibility.Enable )
                {
                    return UnityEditor.EditorPrefs.GetBool( key, false );
                }
                else if ( visibility == EditorVisibility.Disable )
                {
                    return !UnityEditor.EditorPrefs.GetBool( key, false );
                }
                return true;
            }
        }

        public bool IsHidden_Editor
        {
            get
            {
                if ( visibility == EditorVisibility.Show )
                {
                    return !UnityEditor.EditorPrefs.GetBool( key, false );
                }
                else if ( visibility == EditorVisibility.Hide )
                {
                    return UnityEditor.EditorPrefs.GetBool( key, false );
                }
                return false;
            }
        }
#endif
    }
}