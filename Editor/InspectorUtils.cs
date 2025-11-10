using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ExtendedInspector
{
    public static partial class InspectorUtils
    {
        static MethodInfo setIconEnabled;
        static MethodInfo SetIconEnabled => setIconEnabled = setIconEnabled ??
            Assembly.GetAssembly( typeof( UnityEditor.Editor ) )
            ?.GetType( "UnityEditor.AnnotationUtility" )
            ?.GetMethod( "SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic );

        public static void SetGizmoIconEnabled( Type type, bool on )
        {
            if ( SetIconEnabled == null ) return;
            const int MONO_BEHAVIOR_CLASS_ID = 114; // https://docs.unity3d.com/Manual/ClassIDReference.html
            SetIconEnabled.Invoke( null, new object[] { MONO_BEHAVIOR_CLASS_ID, type.FullName, on ? 1 : 0 } );
        }

        public static Texture GetIcon( string iconName )
        {
            if ( iconName == null )
                return null;

            GUIContent content = EditorGUIUtility.IconContent( iconName );
            if ( content != null )
            {
                return content.image;
            }
            return null;
        }

        [System.Serializable]
        public struct Void { }

    }
}