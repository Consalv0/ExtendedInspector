using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;

using Void = ExtendedInspector.InspectorUtils.Void;

namespace ExtendedInspector.Editor
{
    [UxmlObject]
    public partial class Option
    {
        [UxmlAttribute]
        public string name { get; set; }

        [UxmlAttribute]
        public bool bold { get; set; }

        [UxmlAttribute]
        public Color color;
    }

    [UxmlElement]
    public partial class MethodButtonField : VisualElement
    {
        private ParameterInfo[] m_ParameterInfo;
        private BindableElement[] m_BindableElements;
        private Foldout m_Foldout;
        private VisualElement m_Label;
        private VisualElement m_Container;
        private Button m_Button;

        public MethodButtonField( ) { }

        public MethodButtonField( object target, MethodInfo methodInfo, string label, Texture icon, ParameterInfo[] parameterInfos )
        {
            m_Button = IconButton(
                label == null ? ObjectNames.NicifyVariableName( methodInfo.Name ) : label, icon,
                ( ) => methodInfo.Invoke( target, GetParameters() )
            );

            if ( parameterInfos != null )
            {
                m_ParameterInfo = parameterInfos;
                m_BindableElements = new BindableElement[ m_ParameterInfo.Length ];
                m_Foldout = new Foldout();
                m_Container = m_Foldout.contentContainer;
                m_Container.style.marginLeft = 6;
                m_Container.style.marginRight = 3;
                m_Container.style.marginTop = -5;
                m_Container.style.paddingTop = 5;
                m_Container.style.paddingLeft = 11;
                m_Container.style.paddingRight = 8;
                m_Container.style.paddingBottom = 5;
                m_Container.style.borderBottomColor = Color.gray4;
                m_Container.style.borderLeftColor = Color.gray4;
                m_Container.style.borderRightColor = Color.gray4;
                m_Container.style.borderBottomLeftRadius = 5;
                m_Container.style.borderBottomRightRadius = 5;
                m_Container.style.borderLeftWidth = 1;
                m_Container.style.borderRightWidth = 1;
                m_Container.style.borderBottomWidth = 1;
                for ( int i = 0; i < m_ParameterInfo.Length; i++ )
                {
                    ParameterInfo parameter = m_ParameterInfo[ i ];
                       
                    object defaultValue = null;
                    if ( parameter.Attributes.HasFlag( ParameterAttributes.Optional ) && parameter.Attributes.HasFlag( ParameterAttributes.HasDefault ) )
                        defaultValue = parameter.DefaultValue;
                    string name = ObjectNames.NicifyVariableName( parameter.Name );
                    System.Type type = parameter.ParameterType;
                    
                         if ( type == typeof( bool ) )          { Toggle                field = new( name ); field.value = defaultValue is bool               val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( int ) )           { IntegerField          field = new( name ); field.value = defaultValue is int                val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( uint ) )          { UnsignedIntegerField  field = new( name ); field.value = defaultValue is uint               val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( long ) )          { LongField             field = new( name ); field.value = defaultValue is long               val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( ulong ) )         { UnsignedLongField     field = new( name ); field.value = defaultValue is ulong              val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( float ) )         { FloatField            field = new( name ); field.value = defaultValue is float              val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( double ) )        { DoubleField           field = new( name ); field.value = defaultValue is double             val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( string ) )        { TextField             field = new( name ); field.value = defaultValue is string             val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Vector2 ) )       { Vector2Field          field = new( name ); field.value = defaultValue is Vector2            val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Vector2Int ) )    { Vector2IntField       field = new( name ); field.value = defaultValue is Vector2Int         val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Vector3 ) )       { Vector3Field          field = new( name ); field.value = defaultValue is Vector3            val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Vector3Int ) )    { Vector3IntField       field = new( name ); field.value = defaultValue is Vector3Int         val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Vector4 ) )       { Vector4Field          field = new( name ); field.value = defaultValue is Vector4            val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Rect ) )          { RectField             field = new( name ); field.value = defaultValue is Rect               val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( RectInt ) )       { RectIntField          field = new( name ); field.value = defaultValue is RectInt            val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Bounds ) )        { BoundsField           field = new( name ); field.value = defaultValue is Bounds             val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( BoundsInt ) )     { BoundsIntField        field = new( name ); field.value = defaultValue is BoundsInt          val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Hash128 ) )       { Hash128Field          field = new( name ); field.value = defaultValue is Hash128            val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Quaternion ) )    { Vector3Field          field = new( name ); field.value = defaultValue is Quaternion         val ? val.eulerAngles 
                                                                                                                                                                        : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( Color ) )         { ColorField            field = new( name ) { hdr = true, showAlpha = true };
                                                                                                             field.value = defaultValue is Color              val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type.IsEnum && type.GetCustomAttribute<System.FlagsAttribute>( ) is System.FlagsAttribute )                                            
                                                                { EnumFlagsField        field = new( name, (System.Enum)type.GetEnumValues().GetValue( 0 ) );
                                                                                                             field.value = defaultValue is System.Enum        val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type.IsEnum )                     { EnumField             field = new( name, (System.Enum)type.GetEnumValues().GetValue( 0 ) );
                                                                                                             field.value = defaultValue is System.Enum        val ? val : default; m_BindableElements[ i ] = field; }
                    else if ( type == typeof( UnityEngine.Object ) || type.IsSubclassOf( typeof( UnityEngine.Object ) ) || type.IsInterface )                                     
                                                                { ObjectField           field = new( name ); field.value = defaultValue is UnityEngine.Object val ? val : default; m_BindableElements[ i ] = field;
                                                                                                             field.objectType = type; }
                    else 
                    {
                        ObjectField field = new( name ); field.value = null; m_BindableElements[ i ] = field;
                        field.objectType = type;
                        m_BindableElements[ i ] = field;
                        m_Container.Add( new HelpBox( $"Method parameter <b>{parameter.Name}</b> cannot be drawn because type <b>{type.FullName}</b> is not supported", HelpBoxMessageType.Error ) );
                    }

                    m_BindableElements[ i ].style.flexGrow = 1;
                    m_BindableElements[ i ].AddToClassList( BaseField<Void>.ussClassName + "__inspector-field" );
                    // m_BindableElements[ i ].AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                    m_Container.Add( m_BindableElements[ i ] );
                }
                m_Label = m_Foldout.Q( null, BaseField<Void>.inputUssClassName );
                m_Label.Add( m_Button );
                m_Button.style.flexGrow = 1;
                Add( m_Foldout );
            }
            else
            {
                Add( m_Button );
            }
        }

        private object[] GetParameters( )
        {
            if ( m_BindableElements == null )
                return null;

            object[] objects = new object[ m_BindableElements.Length ];
            for ( int i = 0;i < objects.Length; i++ )
            {
                BindableElement bindableElement = m_BindableElements[ i ];

                if ( m_ParameterInfo[ i ].ParameterType == typeof( Quaternion ) )
                {
                    if ( bindableElement is BaseField<Vector3> vector3Field ) objects[ i ] = Quaternion.Euler( vector3Field.value );
                }
                else if ( bindableElement is BaseField<bool>        boolField       ) objects[ i ] = boolField.value;
                else if ( bindableElement is BaseField<int>         intField        ) objects[ i ] = intField.value;
                else if ( bindableElement is BaseField<uint>        uintField       ) objects[ i ] = uintField.value;
                else if ( bindableElement is BaseField<long>        longField       ) objects[ i ] = longField.value;
                else if ( bindableElement is BaseField<ulong>       ulongField      ) objects[ i ] = ulongField.value;
                else if ( bindableElement is BaseField<float>       floatField      ) objects[ i ] = floatField.value;
                else if ( bindableElement is BaseField<double>      doubleField     ) objects[ i ] = doubleField.value;
                else if ( bindableElement is BaseField<string>      stringField     ) objects[ i ] = stringField.value;
                else if ( bindableElement is BaseField<Vector2>     vector2Field    ) objects[ i ] = vector2Field.value;
                else if ( bindableElement is BaseField<Vector2Int>  vector2IntField ) objects[ i ] = vector2IntField.value;
                else if ( bindableElement is BaseField<Vector3>     vector3Field    ) objects[ i ] = vector3Field.value;
                else if ( bindableElement is BaseField<Vector3Int>  vector3IntField ) objects[ i ] = vector3IntField.value;
                else if ( bindableElement is BaseField<Vector4>     vector4Field    ) objects[ i ] = vector4Field.value;
                else if ( bindableElement is BaseField<Color>       colorField      ) objects[ i ] = colorField.value;
                else if ( bindableElement is BaseField<Rect>        rectField       ) objects[ i ] = rectField.value;
                else if ( bindableElement is BaseField<RectInt>     rectIntField    ) objects[ i ] = rectIntField.value;
                else if ( bindableElement is BaseField<Bounds>      boundsField     ) objects[ i ] = boundsField.value;
                else if ( bindableElement is BaseField<BoundsInt>   boundsIntField  ) objects[ i ] = boundsIntField.value;
                else if ( bindableElement is BaseField<Hash128>     hash128Field    ) objects[ i ] = hash128Field.value;
                else if ( bindableElement is EnumFlagsField         enumFlagsField  ) objects[ i ] = enumFlagsField.value;
                else if ( bindableElement is EnumField              enumField       ) objects[ i ] = enumField.value;
                else if ( bindableElement is ObjectField            objectField     ) objects[ i ] = objectField.value;
            }
            return objects;
        }

        private static Button IconButton( string text, Texture iconTexture, System.Action onClick )
        {
            Button button = new( onClick );
            button.text = text;

            if ( iconTexture is Texture2D texture )
            {
                Background icon = Background.FromTexture2D( texture );
                button.iconImage = icon;
            }

            return button;
        }
    }
}