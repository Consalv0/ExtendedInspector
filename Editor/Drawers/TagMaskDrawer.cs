using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace ExtendedInspector.Editor
{
    [CustomPropertyDrawer( typeof( TagMaskAttribute ) )]
    public class TagMaskAttributeDrawer : ExtendedPropertyDrawer
    {
        private ToggleButtonGroup m_MaskField;
        private TagField m_TagField;
        private bool m_UseButtons;

        public override void OnUpdateValue( )
        {
            if ( m_UseButtons )
            {
                ToggleButtonGroupState buttonGroupState = new ToggleButtonGroupState((ulong)GetValue(), UnityEditorInternal.InternalEditorUtility.tags.Length );
                m_MaskField.value = buttonGroupState;
            }
            else
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
        }

        public override VisualElement CreatePropertyGUI( SerializedProperty property )
        {
            if ( attribute is TagMaskAttribute attributeMask )
            {
                m_UseButtons = attributeMask.useButtons;
            }

            if ( m_UseButtons )
            {
                return CreateButtonGroup( property );
            }
            else
            {
                return CreateDropdown( property );
            }
        }

        public VisualElement CreateButtonGroup( SerializedProperty property )
        {
            switch ( property.propertyType )
            {
                case SerializedPropertyType.Integer:
                    int tagIndex;
                    switch ( property.numericType )
                    {
                        case SerializedPropertyNumericType.Int8:
                        case SerializedPropertyNumericType.Int16:
                        case SerializedPropertyNumericType.Int32:
                            tagIndex = property.intValue;
                            break;
                        case SerializedPropertyNumericType.UInt8:
                        case SerializedPropertyNumericType.UInt16:
                        case SerializedPropertyNumericType.UInt32:
                            tagIndex = (int)property.uintValue;
                            break;
                        case SerializedPropertyNumericType.Int64:
                            tagIndex = (int)property.longValue;
                            break;
                        case SerializedPropertyNumericType.UInt64:
                            tagIndex = (int)property.ulongValue;
                            break;
                        default:
                            return null;
                    }
                    List<string> tags = UnityEditorInternal.InternalEditorUtility.tags.ToList();
                    if ( tagIndex < 0 || tagIndex >= tags.Count )
                    {
                        tagIndex = 0;
                    }
                    m_MaskField = new ToggleButtonGroup( property.displayName, new( 1ul << tagIndex, tags.Count ) );
                    m_MaskField.isMultipleSelection = true;
                    m_MaskField.allowEmptySelection = true;
                    foreach ( string tag in tags )
                    {
                        Button button = new Button() { text = tag };
                        button.style.flexGrow = 1;
                        button.style.fontSize = 8;
                        button.style.paddingLeft = 1;
                        button.style.paddingRight = 1;
                        button.style.paddingTop = 2;
                        button.style.paddingBottom = 2;
                        button.style.marginTop = 0;
                        button.style.marginBottom = 0;
                        m_MaskField.Add( button );
                    }
                    m_MaskField.label = property.displayName;
                    m_MaskField.BindProperty( property );
                    return m_MaskField;
                default:
                    return null;
            }
        }

        public VisualElement CreateDropdown( SerializedProperty property )
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