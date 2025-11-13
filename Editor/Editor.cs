using UnityEditor;
using UnityEngine;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    [CanEditMultipleObjects, CustomEditor( typeof( UnityEngine.Object ), true )]
    public class Editor : UnityEditor.Editor
    {
        protected Inspector m_Inspector;

        protected virtual void OnEnable( )
        {
        }

        public override VisualElement CreateInspectorGUI( )
        {
            if ( EditorPrefs.GetBool( "ExtendedInspector.Editor.enabled", true ) )
            {
                m_Inspector = new( this.targets, this.serializedObject );
                return m_Inspector.CreateInspectorGUI();
            }
            else
            {
                return base.CreateInspectorGUI( );
            }
        }
    }

    public static partial class EditorToolbar
    {
        [MenuItem( "Tools/Extended Inspector/Enable", false, 1 )]
        public static void EnableToggle( )
        {
            bool enabled =  EditorPrefs.GetBool( "ExtendedInspector.Editor.enabled", true );
            EditorPrefs.SetBool( "ExtendedInspector.Editor.enabled", !enabled );
        }

        [MenuItem( "Tools/Extended Inspector/Enable", true, 1 )]
        public static bool EnableToggle_Validate( )
        {
            bool enabled =  EditorPrefs.GetBool( "ExtendedInspector.Editor.enabled", true );
            Menu.SetChecked( "Tools/Extended Inspector/Enable", enabled );
            return true;
        }

        [MainToolbarElement( "Extended Inspector", defaultDockPosition = MainToolbarDockPosition.Left )]
        public static MainToolbarElement MenuEnableToggle( )
        {
            MainToolbarToggle toggle = new MainToolbarToggle( new MainToolbarContent( "", EditorGUIUtility.IconContent("d_Profiler.UIDetails").image as Texture2D, "Toggle Extended Inspector" ),
                EditorPrefs.GetBool( "ExtendedInspector.Editor.enabled", true ), ( value ) => { EditorPrefs.SetBool( "ExtendedInspector.Editor.enabled", value ); } );

            return toggle;
        }
    }
}