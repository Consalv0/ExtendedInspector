using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    public class ListCollectionView : CollectionView
    {
        protected IList m_Value;
        protected int m_Size;
        protected List<VisualElement> m_Elements;
        protected Label m_Label;
        protected Label m_SizeLabel;
        protected Foldout m_Foldout;
        protected ScrollView m_ScrollView;
        protected Button m_AddButton;
        protected Button m_RemoveButton;
        protected int m_AddRemoveIndex = -1;

        public ListCollectionView( string label, Type collectionType, Type elementType, MemberInfo memberInfo, System.Func<object> get, System.Action<object> set, SerializedProperty property, Inspector inspector )
            : base( collectionType, elementType, memberInfo, get, set, property, inspector )
        {
            m_Elements = new();
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
            m_Label.parent.Add( m_AddButton = IconButton( EditorGUIUtility.IconContent( "d_Toolbar Plus" ).image, AddElement ) );
            m_Label.parent.Add( m_RemoveButton = IconButton( EditorGUIUtility.IconContent( "d_Toolbar Minus" ).image, RemoveElement ) );

            UpdateCollectionCache();
            return m_Foldout;
        }

        protected void EvalAddRemoveFocusedElement( )
        {
            m_AddRemoveIndex = -1;

            for ( int i = 0; i < m_Size; i++ )
            {
                if ( m_Elements[ i ].hasHoverPseudoState )
                {
                    m_AddRemoveIndex = i;
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

        protected void RemoveElement( int index )
        {
            if ( m_Size == 0 )
                return;

            if ( m_Property != null )
            {
                m_Property.DeleteArrayElementAtIndex( index );
                m_Property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                int newSize = m_Size - 1;
                m_Value.RemoveAt( index );
                m_Set.Invoke( m_Value );
            }

            UpdateCollectionSize();
        }

        protected void RemoveElement( )
        {
            if ( m_Size == 0 )
                return;

            if ( m_AddRemoveIndex != -1 )
            {
                RemoveElement( m_AddRemoveIndex );
                m_AddRemoveIndex = Mathf.Min( m_AddRemoveIndex, m_Size - 1 );
                UpdateFocusedElements();
            }
            else
                RemoveElement( m_Size - 1 );
        }

        protected object CreateElementInstance( )
        {
            if ( m_ElementType.IsArray || m_ElementType.IsClass || m_ElementType.IsAbstract || m_ElementType.IsInterface )
            {
                return null;
            }

            return Activator.CreateInstance( m_ElementType );
        }

        protected void AddElementAt( int index )
        {
            if ( m_Property != null )
            {
                m_Property.InsertArrayElementAtIndex( Mathf.Max( 0, index ) );
                m_Property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                int newSize = m_Size + 1;

                if ( m_Value == null )
                {
                    m_Value = Activator.CreateInstance( m_CollectionType ) as IList;
                }
                if ( m_Value != null )
                {
                    if ( m_Size > 0 )
                        m_Value.Insert( index, CreateElementInstance() );
                    else
                        m_Value.Add( CreateElementInstance() );
                }

                m_Set.Invoke( m_Value );
            }

            UpdateCollectionSize();
        }

        protected void AddElement( )
        {
            if ( m_AddRemoveIndex != -1 )
                AddElementAt( m_AddRemoveIndex );
            else
                AddElementAt( m_Size - 1 );
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
                System.Func<object> get;
                System.Action<object> set;
                if ( m_Property == null )
                {
                    get = ( ) => m_Value[ index ];
                    if ( m_Set == null ) set = null;
                    else
                    {
                        set = ( value ) => m_Value[ index ] = value;
                    }
                }
                else
                {
                    get = () => m_Get.Invoke() is IList array ? array[ index ] : null;
                    set = ( object value ) => { m_Value = m_Get.Invoke() as IList; if ( m_Value != null ) m_Value[ index ] = value; m_Set?.Invoke( m_Value ); };
                }

                VisualElement element = new();
                element.name = "list-view__item";
                SerializedProperty serializedElement = m_Property?.GetArrayElementAtIndex( i );
                VisualElement field = m_Inspector.CreateFieldForType(
                    m_ElementType, m_ElementType, $"[{index}] {serializedElement?.displayName}", get, set, serializedElement,
                    Inspector.AreNonSerializedMemberValuesDifferent( new[] { get } ), m_TickDelay
                );
                element.Add( field );
                element.style.paddingLeft = 12;
                element.style.paddingRight = 10;
                element.RegisterCallback<PointerDownEvent>( evt => EvalAddRemoveFocusedElement(), TrickleDown.TrickleDown );
                if ( m_Property == null ) element.AddManipulator( new ContextualMenuManipulator( null ) );
                element.RegisterCallback<ContextualMenuPopulateEvent>( ( evt ) =>
                {
                    evt.menu.AppendAction( "Move Element Up",
                        ( menuAction ) => { MoveElementUp( (int)menuAction.userData ); },
                        static ( DropdownMenuAction menuAction ) =>
                        {
                            return ((int)menuAction.userData) == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                        },
                        index );
                    evt.menu.AppendAction( "Move Element Down",
                        ( menuAction ) => { MoveElementDown( (((int, int))menuAction.userData).Item1 ); },
                        static ( DropdownMenuAction menuAction ) =>
                        {
                            return (((int, int))menuAction.userData).Item1 + 1 >= (((int, int))menuAction.userData).Item2 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                        },
                        (index, m_Size) );
                } );

                m_Elements.Add( element );
                m_ScrollView.Add( element );
            }
        }

        protected void MoveElementDown( int index )
        {
            if ( m_Property != null )
            {
                m_Property.MoveArrayElement( index, index + 1 );
                m_Property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                object temp = m_Value[ index ];
                m_Value[index] = m_Value[ index + 1 ];
                m_Value[index + 1] = temp;
            }
        }

        protected void MoveElementUp( int index )
        {
            if ( m_Property != null )
            {
                m_Property.MoveArrayElement( index, index - 1 );
                m_Property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                object temp = m_Value[ index ];
                m_Value[ index ] = m_Value[ index - 1 ];
                m_Value[ index - 1 ] = temp;
            }
        }

        protected override void UpdateCollectionCache( )
        {
            if ( m_Property == null )
            {
                m_Value = GetFieldValue() as IList;
            }

            UpdateCollectionSize();
        }
    }
}