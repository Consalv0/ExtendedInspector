using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;

namespace ExtendedInspector.Editor
{
    [CustomPropertyDrawer( typeof( TagFieldAttribute ) )]
    public class TagFieldAttributeDrawer : ExtendedPropertyDrawer
    {
        private TagField m_TagField;

        public override void OnUpdateValue( )
        {
            object value = GetValue();
            if ( value is string tagName )
            {
                m_TagField.value = tagName;
            }
            else if ( value is TagHandle handle )
            {
                m_TagField.value = handle.ToString();
            }
            else if ( value is int tagIndex )
            {
                string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
                if ( tagIndex < 0 || tagIndex >= tags.Length )
                {
                    tagIndex = 0;
                }
                m_TagField.value = tags[ tagIndex ];
            }
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            switch ( property.propertyType )
            {
                case SerializedPropertyType.Generic:
                    if ( property.boxedValue is TagHandle tagHandle )
                    {
                        m_TagField = new TagField( property.displayName, tagHandle.ToString() );
                        m_TagField.BindProperty( property );
                        return m_TagField;
                    }
                    else return null;
                case SerializedPropertyType.Integer:
                    uint tagIndex;
                    switch ( property.numericType )
                    {
                        case SerializedPropertyNumericType.Int8:
                        case SerializedPropertyNumericType.Int16:
                        case SerializedPropertyNumericType.Int32:
                            tagIndex = (uint)property.intValue;
                            break;
                        case SerializedPropertyNumericType.UInt8:
                        case SerializedPropertyNumericType.UInt16:
                        case SerializedPropertyNumericType.UInt32:
                            tagIndex = property.uintValue;
                            break;
                        case SerializedPropertyNumericType.Int64:
                            tagIndex = (uint)property.longValue;
                            break;
                        case SerializedPropertyNumericType.UInt64:
                            tagIndex = (uint)property.ulongValue;
                            break;
                        default:
                            return null;
                    }
                    string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
                    if ( tagIndex < 0 || tagIndex >= tags.Length )
                    {
                        tagIndex = 0;
                    }
                    m_TagField = new TagField( property.displayName, tags[ tagIndex ] );
                    m_TagField.BindProperty( property );
                    return m_TagField;
                case SerializedPropertyType.String:
                    m_TagField = new TagField( property.displayName, property.stringValue );
                    m_TagField.BindProperty( property );
                    return m_TagField;
                default:
                    return null;
            }
        }
    }
}