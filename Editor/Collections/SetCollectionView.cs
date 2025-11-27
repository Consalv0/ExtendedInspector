using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Void = ExtendedInspector.InspectorUtils.Void;

namespace ExtendedInspector.Editor
{
    public class SetCollectionView : CollectionView
    {
        protected IEnumerable m_Value;
        protected int m_Size;
        protected List<VisualElement> m_Elements;
        protected Label m_Label;
        protected Label m_SizeLabel;
        protected Foldout m_Foldout;
        protected ScrollView m_ScrollView;
        protected Button m_AddButton;
        protected Button m_RemoveButton;
        protected object m_SelectedElement;
        protected int m_AddRemoveIndex = -1;
        protected MethodInfo m_AddElement;
        protected MethodInfo m_RemoveElement;

        public SetCollectionView( string label, Type collectionType, Type elementType, MemberInfo memberInfo, System.Func<object> get, System.Action<object> set, Inspector inspector )
            : base( collectionType, elementType, memberInfo, get, set, null, inspector )
        {
            m_Elements = new();
            m_AddElement = collectionType.GetMethod( nameof( ISet<Void>.Add ) );
            m_RemoveElement = collectionType.GetMethod( nameof( ISet<Void>.Remove ) );
            Add( CreateCollectionView( label ) );
            schedule.Execute( UpdateCollectionCache ).Every( m_TickDelay );
        }

        public override void SetEditable( bool value )
        {
            if ( value == false || m_ReadOnly )
            {
                m_ScrollView.contentContainer.SetEnabled( false );
                m_AddButton.style.display = DisplayStyle.None;
                m_RemoveButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_AddButton.style.display = DisplayStyle.Flex;
                m_RemoveButton.style.display = DisplayStyle.Flex;
            }
        }

        protected override VisualElement CreateCollectionView( string label )
        {
            if ( string.IsNullOrEmpty( label ) )
                label = " ";

            m_Foldout = new() { text = label };
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
            m_Label.parent.Add( m_AddButton = IconButton( EditorGUIUtility.IconContent( "d_Toolbar Plus" ).image, AddElement ) );
            m_Label.parent.Add( m_RemoveButton = IconButton( EditorGUIUtility.IconContent( "d_Toolbar Minus" ).image, RemoveElement ) );

            UpdateCollectionCache();
            return m_Foldout;
        }

        protected void EvalAddRemoveFocusedElement( )
        {
            m_SelectedElement = null;

            m_AddRemoveIndex = -1;
            foreach ( var element in m_Value )
            {
                m_AddRemoveIndex++;
                m_SelectedElement = element;
                if ( m_Elements[ m_AddRemoveIndex ].hasHoverPseudoState )
                {
                    break;
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

            EvalAddRemoveFocusedElement();

            int newSize = m_Size - 1;
            m_RemoveElement.Invoke( m_Value, new object[] { m_SelectedElement } );
            m_Set.Invoke( m_Value );

            UpdateCollectionSize();
        }

        protected object CreateElementInstance( )
        {
            if ( m_ElementType.IsArray || m_ElementType.IsClass || m_ElementType.IsAbstract || m_ElementType.IsInterface )
            {
                return null;
            }

            return Activator.CreateInstance( m_ElementType );
        }

        protected void AddElement()
        {
            int newSize = m_Size + 1;

            if ( m_Value == null )
            {
                m_Value = Activator.CreateInstance( m_CollectionType ) as IEnumerable;
            }
            if ( m_Value != null )
            {
                m_AddElement.Invoke( m_Value, new object[] { CreateElementInstance() } );
            }

            m_Set.Invoke( m_Value );

            UpdateCollectionSize();
        }

        protected void UpdateCollectionSize()
        {
            int oldSize = m_Size;
            if ( m_Property != null )
            {
                m_Size = m_Property.arraySize;
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
                int i = 0;
                foreach ( var item in m_Value ) { i++; }
                m_Size = i;
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
                System.Func<object> get;
                get = ( ) =>
                {
                    int i = 0;
                    foreach ( var item in m_Value )
                    {
                        if ( index == i )
                            return item;
                        i++;
                    }
                    UpdateCollectionCache();
                    return null;
                };
                System.Action<object> set;
                if ( m_Set == null ) set = null;
                else
                {
                    set = ( value ) =>
                    {
                        int i = 0;
                        foreach ( var item in m_Value )
                        {
                            if ( index == i )
                            {
                                m_RemoveElement.Invoke( m_Value, new object[] { item } );
                                m_AddElement.Invoke( m_Value, new object[] { value } );
                                return;
                            }
                            i++;
                        }
                        UpdateCollectionCache();
                    };
                }

                VisualElement element = new();
                element.name = "list-view__item";
                VisualElement field = m_Inspector.CreateFieldForType(
                    m_ElementType, m_ElementType, $"[{index}]", get, set, m_Property?.GetArrayElementAtIndex( i ),
                    Inspector.AreNonSerializedMemberValuesDifferent( new[] { get } ), m_TickDelay
                );
                element.Add( field );
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
                m_Value = GetFieldValue() as IEnumerable;
            }

            UpdateCollectionSize();
        }
    }
}