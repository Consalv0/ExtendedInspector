using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    public class EnumerableCollectionView : CollectionView
    {
        protected IEnumerable m_Value;
        protected int m_Size;
        protected List<VisualElement> m_Elements;
        protected Label m_Label;
        protected Label m_SizeLabel;
        protected Foldout m_Foldout;
        protected ScrollView m_ScrollView;

        public EnumerableCollectionView( string label, Type collectionType, Type elementType, MemberInfo memberInfo, System.Func<object> get, Inspector inspector )
            : base( collectionType, elementType, memberInfo, get, null, null, inspector )
        {
            m_Elements = new();
            Add( CreateCollectionView( label ) );
            schedule.Execute( UpdateCollectionCache ).Every( m_TickDelay );
        }

        public override void SetEditable( bool value )
        {
            if ( value == false || m_ReadOnly )
            {
                Inspector.DisablePickingElementsInHierarchy( m_ScrollView.contentContainer );
            }
            else
            {
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

            UpdateCollectionCache();
            return m_Foldout;
        }

        protected object CreateElementInstance( )
        {
            if ( m_ElementType.IsArray || m_ElementType.IsClass || m_ElementType.IsAbstract || m_ElementType.IsInterface )
            {
                return null;
            }

            return Activator.CreateInstance( m_ElementType );
        }

        protected void UpdateCollectionSize()
        {
            int oldSize = m_Size;
            if ( m_Value == null )
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
                m_Size = 0;
                IEnumerator values = m_Value.GetEnumerator();
                while ( values.MoveNext() )
                {
                    m_Size++;
                }
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
                System.Func<object> get = () =>
                {
                    int i = 0;
                    IEnumerator values = m_Value.GetEnumerator();
                    while ( values.MoveNext() )
                    {
                        if ( index == i )
                            return values.Current;
                        i++;
                    }
                    UpdateCollectionCache();
                    return null;
                };

                VisualElement element = new();
                element.name = "list-view__item";
                VisualElement field = m_Inspector.CreateFieldForType( m_ElementType, m_ElementType, $"[{index}]", get, null, null, false, m_TickDelay );
                element.Add( field );
                element.style.paddingLeft = 12;
                element.style.paddingRight = 10;

                m_Elements.Add( element );
                m_ScrollView.Add( element );
            }
        }

        protected override void UpdateCollectionCache( )
        {
            if ( m_Property == null )
            {
                object fieldValue = GetFieldValue();
                m_Value = (IEnumerable)fieldValue;
            }

            UpdateCollectionSize();
        }
    }
}