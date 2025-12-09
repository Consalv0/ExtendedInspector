using System.Reflection;
using UnityEditor;

namespace ExtendedInspector.Editor
{
    public abstract class ExtendedPropertyDrawer : PropertyDrawer
    {
        protected readonly System.Func<object> m_Get;
        protected readonly System.Action<object> m_Set;
        protected MemberInfo m_MemberInfo;
        protected bool m_Serialized;
        protected SerializedProperty m_Property;

        public MemberInfo MemberInfo => m_MemberInfo;

        private static FieldInfo s_PreferredLabelField = null;

        public ExtendedPropertyDrawer( MemberInfo memberInfo, string label, System.Func<object> get, System.Action<object> set, SerializedProperty property )
        {
            m_Get = get;
            m_Set = set;
            m_Property = property;
            m_Serialized = m_Property != null;

            m_MemberInfo = memberInfo;
            if ( s_PreferredLabelField == null )
                s_PreferredLabelField = typeof(PropertyDrawer).GetField("m_PreferredLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            s_PreferredLabelField.SetValue( this, label );
        }

        public ExtendedPropertyDrawer( )
        {
        }

        protected object GetValue( )
        {
            if ( m_Serialized )
            {
                return m_Property.GetValue();
            }
            else
            {
                return m_Get?.Invoke();
            }
        }

        protected void SetValue( object value )
        {
            if ( m_Serialized )
            {
                m_Property.SetValue( value );
            }
            else
            {
                m_Set?.Invoke( value );
            }
        }

        public void Initialize( SerializedProperty property )
        {
            m_Property = property;
            m_Serialized = property != null;
            if ( m_Serialized ) m_MemberInfo = fieldInfo;
        }

        public virtual void OnUpdateValue( )
        {
        }
    }
}