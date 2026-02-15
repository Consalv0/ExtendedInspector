using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;

namespace ExtendedInspector.Editor
{
    public static class ToggleButtonGroupStateReader
    {
        private static readonly Type TargetType =
        Type.GetType("UnityEngine.UIElements.ToggleButtonGroupState, UnityEngine.UIElementsModule");

        private static readonly FieldInfo DataField =
        TargetType.GetField("m_Data", BindingFlags.Instance | BindingFlags.NonPublic);

        public static ulong GetRawData( in ToggleButtonGroupState toggleState )
        {
            return (ulong)DataField.GetValue( toggleState );
        }
    }

    [CustomPropertyDrawer( typeof( TagMaskAttribute ) )]
    public class TagMaskAttributeDrawer : ExtendedPropertyDrawer
    {
        private ToggleButtonGroup m_ToggleField;
        private Mask64Field m_MaskField;
        private bool m_UseButtons;

        public override void OnUpdateValue( )
        {
            if ( m_UseButtons )
            {
                ToggleButtonGroupState buttonGroupState = new ToggleButtonGroupState((ulong)GetValue(), UnityEditorInternal.InternalEditorUtility.tags.Length );
                m_ToggleField.value = buttonGroupState;
            }
            else
            {
                object value = GetValue();
                ulong tagMask = (ulong)value;
                m_MaskField.value = tagMask;
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
                    string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
                    m_ToggleField = new ToggleButtonGroup( property.displayName, new( GetTagMaskFromNumericProperty( property ), tags.Length ) );
                    m_ToggleField.isMultipleSelection = true;
                    m_ToggleField.allowEmptySelection = true;
                    m_ToggleField.label = property.displayName;
                    m_ToggleField.BindProperty( property );
                    m_ToggleField.TrackPropertyValue( property, ( SerializedProperty property ) => {
                        m_ToggleField.SetValueWithoutNotify( new ToggleButtonGroupState( GetTagMaskFromNumericProperty( property ), UnityEditorInternal.InternalEditorUtility.tags.Length ) );
                    } );
                    m_ToggleField.RegisterValueChangedCallback( ( ChangeEvent<ToggleButtonGroupState> changeEvent ) => {
                        SetTagMaskFromNumericProperty( property, ToggleButtonGroupStateReader.GetRawData( changeEvent.newValue ) );
                        property.serializedObject.ApplyModifiedProperties();
                    } );
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
                        m_ToggleField.Add( button );
                    }
                    m_ToggleField.RegisterCallback<GeometryChangedEvent>(
                        ( _ ) => m_ToggleField.SetValueWithoutNotify( new ToggleButtonGroupState( GetTagMaskFromNumericProperty( property ), UnityEditorInternal.InternalEditorUtility.tags.Length ) )
                    );
                    return m_ToggleField;
                default:
                    return null;
            }
        }

        public void SetTagMaskFromNumericProperty( SerializedProperty property, ulong value )
        {
            switch ( property.numericType )
            {
                case SerializedPropertyNumericType.Int8:
                case SerializedPropertyNumericType.Int16:
                case SerializedPropertyNumericType.Int32:
                    property.intValue = (int)value;
                    break;
                case SerializedPropertyNumericType.UInt8:
                case SerializedPropertyNumericType.UInt16:
                case SerializedPropertyNumericType.UInt32:
                    property.uintValue = (uint)value;
                    break;
                case SerializedPropertyNumericType.Int64:
                    property.longValue = (long)value;
                    break;
                case SerializedPropertyNumericType.UInt64:
                    property.ulongValue = value;
                    break;
                default:
                    break;
            }
        }

        public ulong GetTagMaskFromNumericProperty( SerializedProperty property )
        {
            ulong tagIndex;
            switch ( property.numericType )
            {
                case SerializedPropertyNumericType.Int8:
                case SerializedPropertyNumericType.Int16:
                case SerializedPropertyNumericType.Int32:
                    tagIndex = (ulong)property.intValue;
                    break;
                case SerializedPropertyNumericType.UInt8:
                case SerializedPropertyNumericType.UInt16:
                case SerializedPropertyNumericType.UInt32:
                    tagIndex = property.uintValue;
                    break;
                case SerializedPropertyNumericType.Int64:
                    tagIndex = (ulong)property.longValue;
                    break;
                case SerializedPropertyNumericType.UInt64:
                    tagIndex = property.ulongValue;
                    break;
                default:
                    return 0;
            }
            return tagIndex;
        }

        public VisualElement CreateDropdown( SerializedProperty property )
        {
            switch ( property.propertyType )
            {
                case SerializedPropertyType.Integer:
                    m_MaskField = new Mask64Field( UnityEditorInternal.InternalEditorUtility.tags.ToList(), GetTagMaskFromNumericProperty( property ) );
                    m_MaskField.label = property.displayName;
                    m_MaskField.BindProperty( property );
                    m_MaskField.TrackPropertyValue( property, ( SerializedProperty property ) => {
                        m_MaskField.SetValueWithoutNotify( GetTagMaskFromNumericProperty( property ) );
                    } );
                    m_MaskField.RegisterValueChangedCallback( ( ChangeEvent<ulong> changeEvent ) => {
                        SetTagMaskFromNumericProperty( property, changeEvent.newValue );
                        property.serializedObject.ApplyModifiedProperties();
                    } );
                    return m_MaskField;
                default:
                    return null;
            }
        }
    }
}