using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    public class DictionaryCollectionView : CollectionView
    {
        protected IDictionary m_Value;
        protected int m_Size;
        protected List<VisualElement> m_Elements;
        protected Label m_Label;
        protected Label m_SizeLabel;
        protected Foldout m_Foldout;
        protected ScrollView m_ScrollView;
        protected Button m_OptionsButton;
        protected Button m_AddButton;
        protected Button m_RemoveButton;
        protected DictionaryEntry m_SelectedKey;
        protected int m_AddRemoveIndex = -1;
        protected object m_KeyTemplate;
        protected object m_ValueTemplate;
        protected VisualElement m_Template;
        protected bool m_ToggleTemplate;
        protected Type m_KeyType;
        protected Type m_ValueType;
        protected SerializedProperty m_PropertyKeys;
        protected SerializedProperty m_PropertyValues;

        public DictionaryCollectionView( string label, Type collectionType, Type elementType, MemberInfo memberInfo, System.Func<object> get, System.Action<object> set, SerializedProperty property, Inspector inspector )
            : base( collectionType, elementType, memberInfo, get, set, property, inspector )
        {
            m_Elements = new();
            Type[] types = elementType.GenericTypeArguments;
            m_KeyType = types[ 0 ];
            m_ValueType = types[ 1 ];
            m_KeyTemplate = CreateInstance( m_KeyType );
            m_ValueTemplate = CreateInstance( m_ValueType );

            if ( m_Property != null )
            {
                m_PropertyKeys = m_Property.FindPropertyRelative( "m_Keys" );
                if ( m_PropertyKeys == null ) m_PropertyKeys = m_Property.FindPropertyRelative( "m_keys" );
                if ( m_PropertyKeys == null ) m_PropertyKeys = m_Property.FindPropertyRelative( "_keys" );
                m_PropertyValues = m_Property.FindPropertyRelative( "m_Values" );
                if ( m_PropertyValues == null ) m_PropertyValues = m_Property.FindPropertyRelative( "m_values" );
                if ( m_PropertyValues == null ) m_PropertyValues = m_Property.FindPropertyRelative( "_values" );
            }

            Add( CreateCollectionView( label ) );
            schedule.Execute( UpdateCollectionCache ).Every( m_TickDelay );
        }

        public override void SetEditable( bool value )
        {
            if ( value == false || m_ReadOnly )
            {
                Inspector.DisablePickingElementsInHierarchy( m_ScrollView.contentContainer );
                m_OptionsButton.style.display = DisplayStyle.None;
                m_RemoveButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_OptionsButton.style.display = DisplayStyle.Flex;
                m_RemoveButton.style.display = DisplayStyle.Flex;
            }
        }

        protected override VisualElement CreateCollectionView( string label )
        {
            if ( string.IsNullOrEmpty( label ) )
                label = " ";

            m_Foldout = new() { text = label };
            m_Foldout.bindingPath = m_Property?.propertyPath ?? string.Empty;
            VisualElement container = m_Foldout.contentContainer;
            container.style.marginLeft = 5;
            container.style.paddingLeft = 4;
            container.style.borderLeftWidth = 1;
            container.style.borderLeftColor = Color.gray3;
            container.Add( m_ScrollView = new( ScrollViewMode.Vertical ) );

            m_Foldout.AddToClassList( BaseListView.foldoutHeaderUssClassName );
            m_Foldout.style.marginLeft = -12;

            m_Label = m_Foldout.Q<Label>();
            m_Label.parent.Add( m_SizeLabel = new() );

            m_SizeLabel.style.fontSize = 10;
            m_SizeLabel.style.paddingTop = 5;
            m_SizeLabel.style.marginRight = 5F;
            m_ScrollView.style.maxHeight = 400;
            m_Label.parent.Add( m_OptionsButton = IconButton( EditorGUIUtility.IconContent( "d_ToolsToggle" ).image, ToggleTemplateOptions ) );
            m_Label.parent.Add( m_AddButton = IconButton( EditorGUIUtility.IconContent( "d_Toolbar Plus" ).image, AddElement ) );
            m_Label.parent.Add( m_RemoveButton = IconButton( EditorGUIUtility.IconContent( "d_Toolbar Minus" ).image, RemoveElement ) );
            m_ToggleTemplate = true;

            System.Func<object> getKey = () => m_KeyTemplate;
            System.Action<object> setKey = ( value ) => m_KeyTemplate = value;
            System.Func<object> getValue = () => m_ValueTemplate;
            System.Action<object> setValue = ( value ) => m_ValueTemplate = value;

            m_Template = new();
            m_Template.style.backgroundColor = new Color( 1, 1, 1, 0.05F );
            m_Template.style.borderBottomWidth = 1;
            m_Template.style.borderLeftWidth = 1;
            m_Template.style.borderRightWidth = 1;
            m_Template.style.borderBottomLeftRadius = 5;
            m_Template.style.borderBottomRightRadius = 5;
            m_Template.style.borderLeftColor = Color.gray3;
            m_Template.style.borderRightColor = Color.gray3;
            m_Template.style.borderBottomColor = Color.gray3;
            m_Template.style.flexDirection = FlexDirection.Row;
            m_Template.name = "dictionary-view__template";
            VisualElement keyField = m_Inspector.CreateFieldForType(
                    m_KeyType, m_KeyType, string.Empty, getKey, setKey, null, false, m_TickDelay
                );
            VisualElement valueField = m_Inspector.CreateFieldForType(
                    m_ValueType, m_ValueType, string.Empty, getValue, setValue, null, false, m_TickDelay
                );
            VisualElement elementKey = new();
            elementKey.name = "dictionary-view__key";
            elementKey.AddToClassList( "unity-inspector-main-container" );
            elementKey.AddToClassList( "unity-inspector-element" );
            elementKey.style.marginRight = 15F;
            elementKey.Add( keyField );
            elementKey.style.minWidth = 0;
            elementKey.style.flexBasis = new StyleLength( new Length( 0, LengthUnit.Percent ) );
            elementKey.style.flexGrow = 0.5F;
            elementKey.style.flexShrink = 0;
            VisualElement elementValue = new();
            elementValue.name = "dictionary-view__value";
            elementValue.AddToClassList( "unity-inspector-main-container" );
            elementValue.AddToClassList( "unity-inspector-element" );
            elementValue.Add( valueField );
            elementValue.style.minWidth = 0;
            elementValue.style.flexBasis = new StyleLength( new Length( 0, LengthUnit.Percent ) );
            elementValue.style.flexGrow = 1;
            elementValue.style.flexShrink = 0;
            m_Template.Add( elementKey );
            m_Template.Add( elementValue );
            m_Template.style.paddingLeft = 12;
            m_Template.style.paddingRight = 3;
            m_ScrollView.Add( m_Template );

            ToggleTemplateOptions();
            UpdateCollectionCache();
            return m_Foldout;
        }

        protected void ToggleTemplateOptions( )
        {
            m_ToggleTemplate = !m_ToggleTemplate;
            m_Template.style.display = m_ToggleTemplate ? DisplayStyle.Flex : DisplayStyle.None;
            m_AddButton.style.display = m_ToggleTemplate ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected void EvalAddRemoveFocusedElement( )
        {
            m_SelectedKey = new DictionaryEntry();

            if ( m_PropertyKeys != null )
            {
                m_AddRemoveIndex = -1;
                for ( int i = 0; i < m_PropertyKeys.arraySize; i++ )
                {
                    m_AddRemoveIndex++;
                    if ( m_Elements[ m_AddRemoveIndex ].hasHoverPseudoState )
                    {
                        break;
                    }
                }
            }
            else
            {
                m_AddRemoveIndex = -1;
                if ( m_Value != null )
                {
                    foreach ( DictionaryEntry element in m_Value )
                    {
                        m_AddRemoveIndex++;
                        m_SelectedKey = element;
                        if ( m_Elements[ m_AddRemoveIndex ].hasHoverPseudoState )
                        {
                            break;
                        }
                    }
                }
            }

            UpdateFocusedElements();
        }

        protected void UpdateFocusedElements( )
        {
            for ( int i = 0; i < m_Size; i++ )
            {
                VisualElement element = m_Elements[ i ];
                if ( i != m_AddRemoveIndex )
                {
                    element.style.backgroundColor = Color.clear;
                }
                else
                {
                    element.style.backgroundColor = new Color( 0, 0, 0, 0.1F );
                }
            }
        }

        protected void RemoveElement()
        {
            if ( m_Size == 0 )
                return;

            if ( m_PropertyKeys != null && m_PropertyValues != null )
            {
                m_PropertyKeys.DeleteArrayElementAtIndex( Mathf.Max( 0, m_AddRemoveIndex ) );
                m_PropertyValues.DeleteArrayElementAtIndex( Mathf.Max( 0, m_AddRemoveIndex ) );
                m_Property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EvalAddRemoveFocusedElement();

                int newSize = m_Size - 1;
                m_Value.Remove( m_SelectedKey.Key );
                m_Set.Invoke( m_Value );
            }

            UpdateCollectionSize();
        }

        protected object CreateInstance( System.Type type )
        {
            if ( type.IsArray || type.IsAbstract || type.IsInterface )
            {
                return null;
            }

            if ( type.IsClass && type.IsSubclassOf( typeof( UnityEngine.Object ) ) )
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance( type );
            }
            catch
            {
                return null;
            }
        }

        protected void AddElement()
        {
            int newSize = m_Size + 1;

            if ( m_PropertyKeys != null && m_PropertyValues != null )
            {
                int index = Mathf.Max( 0, m_AddRemoveIndex );
                m_PropertyKeys.InsertArrayElementAtIndex( index );
                m_PropertyValues.InsertArrayElementAtIndex( index );
                var keyProperty = m_PropertyKeys.GetArrayElementAtIndex( index );
                keyProperty.SetValue( m_KeyTemplate );
                var valueProperty = m_PropertyValues.GetArrayElementAtIndex( index );
                valueProperty.SetValue( m_ValueTemplate );
                m_Property.serializedObject.ApplyModifiedProperties();

                m_KeyTemplate = CreateInstance( m_KeyType );
                m_ValueTemplate = CreateInstance( m_ValueType );
            }
            else
            {
                if ( m_Value == null )
                {
                    m_Value = Activator.CreateInstance( m_CollectionType ) as IDictionary;
                }
                if ( m_Value != null )
                {
                    m_Value.Add( m_KeyTemplate, m_ValueTemplate );
                    m_KeyTemplate = CreateInstance( m_KeyType );
                    m_ValueTemplate = CreateInstance( m_ValueType );
                }

                m_Set.Invoke( m_Value );
            }

            UpdateCollectionSize();
        }

        protected void UpdateCollectionSize()
        {
            int oldSize = m_Size;
            if ( m_PropertyKeys != null )
            {
                m_Size = m_PropertyKeys.arraySize;
            }
            else if ( m_Value == null )
            {
                m_Size = 0;
                m_SizeLabel.text = "null";
                foreach ( var element in m_Elements )
                {
                    m_ScrollView.Remove( element );
                }
                m_Elements.Clear();
                return;
            }
            else
            {
                m_Size = m_Value.Count;
            }

            if ( oldSize > m_Size )
            {
                for ( int i = 0; i < oldSize - m_Size; i++ )
                {
                    VisualElement element = m_Elements[ m_Size ];
                    m_Elements.Remove( element );
                    m_ScrollView.Remove( element );
                }
            }

            m_SizeLabel.text = $"{m_Size} element{(m_Size != 1 ? 's' : null)}";
            for ( int i = oldSize; i < m_Size; i++ )
            {
                int index = i; // capture local copy
                System.Func<object> getKey;
                System.Action<object> setKey;
                System.Func<object> getValue;
                System.Action<object> setValue;
                if ( m_Property == null )
                {
                    getKey = ( ) =>
                    {
                        int i = 0;
                        foreach ( var key in m_Value.Keys )
                        {
                            if ( index == i )
                                return key;
                            i++;
                        }
                        return null;
                    };
                    if ( m_Set == null ) setKey = null;
                    else
                    {
                        setKey = ( value ) =>
                        {
                            int i = 0;
                            foreach ( DictionaryEntry entry in m_Value )
                            {
                                if ( index == i )
                                {
                                    m_Value.Remove( entry.Key );
                                    m_Value.Add( value, entry.Value );
                                }
                                i++;
                            }
                        };
                    }
                    getValue = ( ) =>
                    {
                        int i = 0;
                        foreach ( var item in m_Value.Values )
                        {
                            if ( index == i )
                                return item;
                            i++;
                        }
                        return null;
                    };
                    if ( m_Set == null ) setValue = null;
                    else
                    {
                        setValue = ( value ) =>
                        {
                            int i = 0;
                            foreach ( DictionaryEntry entry in m_Value )
                            {
                                if ( index == i )
                                {
                                    m_Value.Remove( entry.Key );
                                    m_Value.Add( entry.Key, value );
                                }
                                i++;
                            }
                        };
                    }
                }
                else
                {
                    getKey = ( ) =>
                    {
                        var propertyValue = GetFieldValue() as IDictionary;
                        int i = 0;
                        foreach ( var key in propertyValue.Keys )
                        {
                            if ( index == i )
                                return key;
                            i++;
                        }
                        return null;
                    };
                    if ( m_Set == null ) setKey = null;
                    else
                    {
                        setKey = ( value ) =>
                        {
                            var propertyValue = GetFieldValue() as IDictionary;
                            int i = 0;
                            foreach ( DictionaryEntry entry in propertyValue )
                            {
                                if ( index == i )
                                {
                                    propertyValue.Remove( entry.Key );
                                    propertyValue.Add( value, entry.Value );
                                }
                                i++;
                            }
                            m_Set?.Invoke( propertyValue );
                        };
                    }
                    getValue = ( ) =>
                    {
                        var propertyValue = GetFieldValue() as IDictionary;
                        int i = 0;
                        foreach ( var item in propertyValue.Values )
                        {
                            if ( index == i )
                                return item;
                            i++;
                        }
                        UpdateCollectionCache();
                        return null;
                    };
                    if ( m_Set == null ) setValue = null;
                    else
                    {
                        setValue = ( value ) =>
                        {
                            var propertyValue = GetFieldValue() as IDictionary;
                            int i = 0;
                            foreach ( DictionaryEntry entry in propertyValue )
                            {
                                if ( index == i )
                                {
                                    propertyValue.Remove( entry.Key );
                                    propertyValue.Add( entry.Key, value );
                                    m_Set?.Invoke( propertyValue );
                                    return;
                                }
                                i++;
                            }
                            UpdateCollectionCache();
                        };
                    }
                }

                VisualElement element = new();
                element.style.flexDirection = FlexDirection.Row;
                element.name = "list-view__item";
                VisualElement keyField = m_Inspector.CreateFieldForType(
                    m_KeyType, m_KeyType, string.Empty, getKey, setKey, m_PropertyKeys?.GetArrayElementAtIndex( index ), Inspector.AreNonSerializedMemberValuesDifferent( new[] { getKey } ), m_TickDelay
                );
                VisualElement valueField = m_Inspector.CreateFieldForType(
                    m_ValueType, m_ValueType, string.Empty, getValue, setValue, m_PropertyValues?.GetArrayElementAtIndex( index ), Inspector.AreNonSerializedMemberValuesDifferent( new[] { getValue } ), m_TickDelay
                );
                VisualElement elementKey = new();
                elementKey.name = "dictionary-view__key";
                elementKey.AddToClassList( "unity-inspector-main-container" );
                elementKey.AddToClassList( "unity-inspector-element" );
                elementKey.style.marginRight = 15F;
                elementKey.Add( keyField );
                elementKey.style.minWidth = 0;
                elementKey.style.flexBasis = new StyleLength( new Length( 0, LengthUnit.Percent ) );
                elementKey.style.flexGrow = 0.5F;
                elementKey.style.flexShrink = 0;
                VisualElement elementValue = new();
                elementValue.name = "dictionary-view__value";
                elementValue.AddToClassList( "unity-inspector-main-container" );
                elementValue.AddToClassList( "unity-inspector-element" );
                elementValue.Add( valueField );
                elementValue.style.minWidth = 0;
                elementValue.style.flexBasis = new StyleLength( new Length( 0, LengthUnit.Percent ) );
                elementValue.style.flexGrow = 1;
                elementValue.style.flexShrink = 0;
                element.Add( elementKey );
                element.Add( elementValue );
                element.style.paddingLeft = 12;
                element.style.paddingRight = 10;
                element.RegisterCallback<PointerDownEvent>( evt => EvalAddRemoveFocusedElement(), TrickleDown.TrickleDown );

                m_Elements.Add( element );
                m_ScrollView.Add( element );
            }
        }

        protected override void UpdateCollectionCache( )
        {
            if ( m_Property == null )
            {
                m_Value = GetFieldValue() as IDictionary;
            }

            UpdateCollectionSize();
        }
    }
}