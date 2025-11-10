using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    public abstract class CollectionView : VisualElement
    {
        protected Inspector m_Inspector;
        protected SerializedProperty m_Property;
        protected MemberInfo m_MemberInfo;
        protected System.Func<object> m_Get;
        protected System.Action<object> m_Set;
        protected bool m_ReadOnly;
        protected bool m_CanEdit;
        protected long m_TickDelay;

        protected System.Type m_CollectionType;
        protected System.Type m_ElementType;

        protected CollectionView( System.Type collectionType, System.Type elementType, MemberInfo memberInfo, System.Func<object> get, System.Action<object> set, SerializedProperty property, Inspector inspector )
        {
            this.m_Inspector = inspector;
            this.m_TickDelay = m_Inspector != null ? m_Inspector.TickDelay : 500;
            this.m_CollectionType = collectionType;
            this.m_ElementType = elementType;
            this.m_Property = property;
            this.m_MemberInfo = memberInfo;
            this.m_Get = get;
            this.m_Set = set;
            if ( m_Set == null ) m_ReadOnly = true;
        }

        public static bool HasCollectionViewForType( System.Type type )
        {
            if ( type.IsArray ) return true;
            if ( ContainsGenericInterface( type, typeof( IList<> ) ) ) return true;
            if ( ContainsGenericInterface( type, typeof( ISet<> ) ) ) return true;
            if ( ContainsGenericInterface( type, typeof( IDictionary<,> ) ) ) return true;
            if ( ContainsGenericInterface( type, typeof( IEnumerable<> ) ) ) return true;

            return false;
        }

        public static VisualElement Create( string label, System.Type collectionType, System.Type elementType, MemberInfo memberInfo, System.Func<object> get, System.Action<object> set, SerializedProperty property, Inspector inspector )
        {
            if ( collectionType.IsArray )
            {
                return new ArrayCollectionView( label, collectionType, elementType, memberInfo, get, set, property, inspector );
            }
            if ( ContainsGenericInterface( collectionType, typeof( IList<> ) ) )
            {
                return new ListCollectionView( label, collectionType, elementType, memberInfo, get, set, property, inspector );
            }
            if ( ContainsGenericInterface( collectionType, typeof( ISet<> ) ) )
            {
                return new SetCollectionView( label, collectionType, elementType, memberInfo, get, set, inspector );
            }
            if ( ContainsGenericInterface( collectionType, typeof( IDictionary<,> ) ) )
            {
                return new DictionaryCollectionView( label, collectionType, elementType, memberInfo, get, set, property, inspector );
            }
            if ( ContainsGenericInterface( collectionType, typeof( IEnumerable<> ) ) )
            {
                return new EnumerableCollectionView( label, collectionType, elementType, memberInfo, get, inspector );
            }
            else
            {
                Foldout foldout = new Foldout() { text = label };
                return foldout;
            }
        }

        protected abstract VisualElement CreateCollectionView( string label );

        protected abstract void UpdateCollectionCache( );

        protected object GetFieldValue( )
        {
            object fieldValue;
            try
            {
                if ( m_Get == null )
                {
                    Debug.LogAssertion( "get is empty for <b>" + m_MemberInfo.ToString() + "</b>" );
                    throw new System.NullReferenceException( "get" );
                }
                fieldValue = m_Get.Invoke();
            }
            catch ( System.Exception e )
            {
                fieldValue = null;
                Debug.LogError( e );
            }
            return fieldValue;
        }

        protected Type GetFieldValueSafe<Type>( )
        {
            object fieldValue = GetFieldValue();
            if ( fieldValue == null ) return default;
            else return (Type)fieldValue;
        }

        public abstract void SetEditable( bool value );

        protected static Button IconButton( Texture iconTexture, System.Action onClick )
        {
            Button button = new( onClick );
            button.style.paddingLeft = 4F;

            if ( iconTexture is Texture2D texture )
            {
                Background icon = Background.FromTexture2D( texture );
                button.iconImage = icon;
            }

            return button;
        }

        protected static bool IsInstanceOfGenericType( Type type, Type genericType )
        {
            while ( type != null )
            {
                if ( type.IsGenericType &&
                    type.GetGenericTypeDefinition() == genericType )
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        protected static bool ContainsGenericInterface( Type type, Type genericInterface )
        {
            if ( IsInstanceOfGenericType( type, genericInterface ) ) return true;
            System.Type[] interfaces = type.GetInterfaces();
            foreach ( System.Type iType in interfaces )
            {
                if ( IsInstanceOfGenericType( iType, genericInterface ) ) { return true; }
            }
            return false;
        }
    }
}