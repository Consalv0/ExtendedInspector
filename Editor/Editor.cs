using UnityEditor;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    [CanEditMultipleObjects, CustomEditor( typeof( UnityEngine.Object ), true )]
    public class Editor : UnityEditor.Editor
    {
        protected Inspector m_NewfangledInspector;

        protected virtual void OnEnable( )
        {
        }

        public override VisualElement CreateInspectorGUI( )
        {
            m_NewfangledInspector = new ( this.targets, this.serializedObject );
            return m_NewfangledInspector.CreateInspectorGUI();
        }
    }
}