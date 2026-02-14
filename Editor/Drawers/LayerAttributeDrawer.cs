using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

namespace ExtendedInspector.Editor
{
    [CustomPropertyDrawer( typeof( LayerFieldAttribute ) )]
    public class LayerFieldAttributeDrawer : ExtendedPropertyDrawer
    {
        private LayerField m_LayerField;

        public override void OnUpdateValue( )
        {
            m_LayerField.value = (int)GetValue();
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            m_LayerField = new LayerField( property.displayName, property.intValue );
            m_LayerField.BindProperty( property );
            return m_LayerField;
        }
    }
}