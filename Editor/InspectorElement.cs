using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExtendedInspector.Editor
{
    public class VisibleIfState
    {
        private struct Getter
        {
            private FieldInfo m_FieldInfo;
            private PropertyInfo m_PropertyInfo;
            private MethodInfo m_MethodInfo;
            private object m_Value;
            public readonly System.Type returnType;

            public Getter( System.Type memberType, string memberInfoPath )
            {
                if ( string.IsNullOrEmpty( memberInfoPath ) == false )
                {
                    m_FieldInfo = memberType.GetField( memberInfoPath, Inspector.BINDING_FLAGS );
                    m_PropertyInfo = memberType.GetProperty( memberInfoPath, Inspector.BINDING_FLAGS );
                    m_MethodInfo = memberType.GetMethod( memberInfoPath, Inspector.BINDING_FLAGS );
                }
                else
                {
                    m_FieldInfo = null;
                    m_PropertyInfo = null;
                    m_MethodInfo = null;
                }

                if ( m_FieldInfo != null )
                {
                    returnType = m_FieldInfo.FieldType;
                }
                else if ( m_PropertyInfo != null )
                {
                    returnType = m_PropertyInfo.PropertyType;
                }
                else if ( m_MethodInfo != null )
                {
                    returnType = m_MethodInfo.ReturnType;
                }
                else returnType = typeof( bool );

                if ( !returnType.IsClass && !returnType.IsInterface )
                    m_Value = Activator.CreateInstance( returnType );
                else m_Value = null;
            }

            public Getter( System.Type memberType, System.Type returnType, object value )
            {
                m_FieldInfo = null;
                m_PropertyInfo = null;
                m_MethodInfo = null;
                this.returnType = returnType;

                if ( returnType == typeof( bool ) )
                    m_Value = true;
                else if ( !returnType.IsClass && !returnType.IsInterface && value == null )
                    m_Value = Activator.CreateInstance( returnType );
                else m_Value = value;
            }

            public object GetObject( object target )
            {
                if ( m_FieldInfo != null )
                {
                    return m_FieldInfo.GetValue( target );
                }
                else if ( m_PropertyInfo != null )
                {
                    return m_PropertyInfo.GetValue( target );
                }
                else if ( m_MethodInfo != null )
                {
                    return m_MethodInfo.Invoke( target, null );
                }
                else return m_Value;
            }
        }

        private EditorVisibility m_Visibility;
        private Getter m_GetterA;
        private Getter m_GetterB;

        public bool IsEnabled( object target )
        {
            if ( m_Visibility == EditorVisibility.Enable )
            {
                return GetBool( target );
            }
            else if ( m_Visibility == EditorVisibility.Disable )
            {
                return GetBool( target ) == false;
            }
            return true;
        }

        public bool IsHidden( object target )
        {
            if ( m_Visibility == EditorVisibility.Show )
            {
                return GetBool( target ) == false;
            }
            else if ( m_Visibility == EditorVisibility.Hide )
            {
                return GetBool( target );
            }
            return false;
        }

        public VisibleIfState( System.Type memberType, VisibleIfAttribute attribute )
        {
            m_Visibility = attribute.visibility;

            m_GetterA = new Getter( memberType, attribute.memberInfoPathA );

            if ( string.IsNullOrEmpty( attribute.memberInfoPathB ) )
                m_GetterB = new Getter( memberType, m_GetterA.returnType, attribute.compareValue );
            else 
                m_GetterB = new Getter( memberType, attribute.memberInfoPathB );
        }

        public bool GetBool( object target )
        {
            object a = m_GetterA.GetObject( target );
            object b = m_GetterB.GetObject( target );

            if ( a == null && b == null )
                return true;
            else if ( a == null )
                return false;
            
            return a.Equals( b );
        }
    }

    public abstract class InspectorElement
    {
        protected PropertyOrderInfo m_OrderInfo;
        protected System.Func<object> m_Owner;
        protected long m_TickDelay;
        protected VisualElement m_InputField;
        protected bool m_ForceDisabled;

        public PropertyOrderInfo OrderInfo => m_OrderInfo;
        public VisualElement VisualElement => m_InputField;

        public InspectorElement( int order, int metadataToken, System.Func<object> owner, long tickDelay, bool forceDisabled )
        {
            m_OrderInfo = new PropertyOrderInfo( metadataToken, order );
            m_TickDelay = tickDelay;
            m_Owner = owner;
            m_ForceDisabled = forceDisabled;
        }

        internal virtual void UpdateVisibility( )
        {
            GetVisibilityStatus( out bool enabled, out bool hidden );
            if ( m_InputField is InlineField inlineField )
            {
                inlineField.m_PropertyField.SetEnabled( enabled );
            }
            else if ( m_InputField is Foldout )
            {
                m_InputField.contentContainer.SetEnabled( enabled );
                m_InputField.Q<Toggle>().SetEnabled( enabled );
            }
            else if ( m_InputField is CollectionView collectionView )
            {
                m_InputField.Q<Toggle>().SetEnabled( enabled );
                collectionView.SetEditable( enabled );
            }
            else
            {
                m_InputField.SetEnabled( enabled );
            }
            m_InputField.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected abstract void GetVisibilityStatus( out bool enabled, out bool hidden );
    }

    public class FieldGroup : InspectorElement
    {
        protected SortedList<PropertyOrderInfo, InspectorElement> m_Elements = new( new PropertyOrderComparer() );
        protected Foldout m_Foldout;
        protected VisualElement m_Container;
        protected int m_Id;
        protected string m_Name;

        public string Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                m_Foldout.text = m_Name;
            }
        }

        public void AddElement( InspectorElement inspectorElement )
        {
            foreach ( var element in m_Elements )
            {
                m_Container.Remove( element.Value.VisualElement );
            }

            m_Elements.Add( inspectorElement.OrderInfo, inspectorElement );

            foreach ( var element in m_Elements )
            {
                m_Container.Add( element.Value.VisualElement );
            }
        }

        public void RemoveElement( InspectorElement inspectorElement )
        {
            m_Elements.Remove( inspectorElement.OrderInfo );
            m_Container.Remove( inspectorElement.VisualElement );
        }

        public FieldGroup( int id, string name, int order, int metadataToken, bool expanded, System.Func<object> owner, long tickDelay, bool forceDisabled )
            : base( order, metadataToken, owner, tickDelay, forceDisabled )
        {
            m_Id = id;
            m_Name = name;

            m_Foldout = new() { text = m_Name };
            m_Foldout.AddToClassList( BaseListView.foldoutHeaderUssClassName );
            m_Foldout.Q<Toggle>().style.marginLeft = -12;
            m_Foldout.name = $"Group {m_Id}";
            m_Foldout.value = expanded;
            m_Container = m_Foldout.contentContainer;
            m_Container.style.paddingLeft = 15;
            m_Container.style.marginLeft = -7;
            m_Container.style.borderLeftWidth = 1;
            m_Container.style.borderLeftColor = Color.gray3;
            m_InputField = m_Foldout;
        }

        internal override void UpdateVisibility( )
        {
            foreach ( var item in m_Elements )
            {
                item.Value.UpdateVisibility( );
            }

            GetVisibilityStatus( out bool enabled, out bool hidden );
            if ( enabled )
            {
                m_Foldout.contentContainer.RemoveFromClassList( PropertyField.disabledUssClassName );
                m_Foldout.Q<Label>().RemoveFromClassList( PropertyField.disabledUssClassName );
            }
            else
            {
                m_Foldout.contentContainer.AddToClassList( PropertyField.disabledUssClassName );
                m_Foldout.Q<Label>().AddToClassList( PropertyField.disabledUssClassName );
            }
            m_Foldout.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected override void GetVisibilityStatus( out bool enabled, out bool hidden )
        {
            enabled = true;
            hidden = false;
        }
    }

    public class MemberField : InspectorElement
    {
        protected MemberInfo m_MemberInfo;
        protected System.Type m_MemberType;
        protected IEnumerable<ExtendedPropertyAttribute> m_PropertyAttributes;
        protected VisibleIfState m_VisibleIfState;

        public MemberField( MemberInfo memberInfo, int order, int metadataToken, System.Type memberType,
            VisualElement inputField, System.Func<object> owner, long tickDelay, bool forceDisabled )
            : base( order, metadataToken, owner, tickDelay, forceDisabled )
        {
            m_MemberInfo = memberInfo;
            m_MemberType = memberType;
            m_InputField = inputField;
        }

        protected override void GetVisibilityStatus( out bool enabled, out bool hidden )
        {
            enabled = true;
            hidden = false;

            if ( m_MemberInfo == null )
                return;

            if ( m_ForceDisabled )
            {
                enabled = false;
            }

            if ( m_PropertyAttributes == null )
                m_PropertyAttributes = m_MemberInfo.GetCustomAttributes<ExtendedPropertyAttribute>();

            foreach ( ExtendedPropertyAttribute propertyAttribute in m_PropertyAttributes )
            {
                if ( enabled == true )
                {
                    if ( propertyAttribute is ReadOnlyAttribute readOnlyAttribute )
                    {
                        enabled = false;
                        continue;
                    }
                    if ( EditorApplication.isPlayingOrWillChangePlaymode && propertyAttribute is DisableInPlayModeAttribute disableInPlayModeAttribute )
                    {
                        enabled = false;
                        continue;
                    }
                    if ( EditorApplication.isPlayingOrWillChangePlaymode == false && propertyAttribute is DisableInEditorModeAttribute disableInEditorModeAttribute )
                    {
                        enabled = false;
                        continue;
                    }
                    if ( propertyAttribute is VisibleIfAttribute hideIfAttribute && hideIfAttribute.ChangesEnableState )
                    {
                        if ( m_VisibleIfState == null ) m_VisibleIfState = new VisibleIfState( m_MemberType, hideIfAttribute );
                        enabled = m_VisibleIfState.IsEnabled( m_Owner?.Invoke() );
                        continue;
                    }
                    if ( propertyAttribute is EditorPrefVisibilityAttribute editorPrefVisibilityAttribute && editorPrefVisibilityAttribute.ChangesEnableState )
                    {
                        enabled = editorPrefVisibilityAttribute.IsEnabled_Editor;
                        continue;
                    }
                }

                if ( hidden == false )
                {
                    if ( EditorApplication.isPlayingOrWillChangePlaymode == false && propertyAttribute is HideInEditorModeAttribute hideInEditorModeAttribute )
                    {
                        hidden = true;
                        continue;
                    }
                    if ( EditorApplication.isPlayingOrWillChangePlaymode && propertyAttribute is HideInPlayModeAttribute hideInPlayModeAttribute )
                    {
                        hidden = true;
                        continue;
                    }
                    if ( propertyAttribute is VisibleIfAttribute hideIfAttribute && hideIfAttribute.ChangesHiddenState )
                    {
                        if ( m_VisibleIfState == null ) m_VisibleIfState = new VisibleIfState( m_MemberType, hideIfAttribute );
                        hidden = m_VisibleIfState.IsHidden( m_Owner?.Invoke() );
                        continue;
                    }
                    if ( propertyAttribute is EditorPrefVisibilityAttribute editorPrefVisibilityAttribute && editorPrefVisibilityAttribute.ChangesHiddenState )
                    {
                        hidden = editorPrefVisibilityAttribute.IsHidden_Editor;
                        continue;
                    }
                }
            }
        }
    }

}
