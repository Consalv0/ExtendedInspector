using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using Void = ExtendedInspector.InspectorUtils.Void;
using TimeUnit = ExtendedInspector.TimeUnit;

namespace ExtendedInspector.Editor
{
    [UxmlElement]
    public abstract partial class TimeSpanFieldBase : VisualElement
    {
        protected long m_FieldValue;
        protected string m_PropertyPath;
        protected TimeUnitFlags m_UnitFlags;
        protected TimeRange m_TimeRange;
        protected IntegerField[] m_IntegerFields;

        public TimeSpanFieldBase( )
        {
        }

        public TimeSpanFieldBase( TimeUnitFlags unitFlags, TimeRange timeRange )
        {
            this.m_UnitFlags = unitFlags;
            this.m_TimeRange = timeRange;

            m_IntegerFields = new IntegerField[ (int)TimeUnit.Days ];
        }

        protected abstract void CreateGUI( );

        public void DrawTimeSpanField( BindableElement textField )
        {
            if ( textField == null ) return;

            this.style.flexDirection = FlexDirection.Row;
            this.style.alignContent = Align.Stretch;
            this.style.alignItems = Align.Stretch;
            textField.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
            textField.style.flexGrow = 0.3F;
            textField.style.marginRight = 4;

            VisualElement textInput;
            if ( (textInput = textField.Q( TextField.ussClassName )) != null )
            {
                textInput.style.display = DisplayStyle.None;
            }

            VisualElement timeSpanField = new VisualElement();
            timeSpanField.AddToClassList( BaseField<Void>.ussClassName );
            timeSpanField.AddToClassList( BaseField<Void>.noLabelVariantUssClassName );
            timeSpanField.AddToClassList( BaseCompositeField<uint, UnsignedIntegerField, uint>.ussClassName );
            timeSpanField.AddToClassList( Vector3IntField.ussClassName );
            timeSpanField.AddToClassList( BaseField<Void>.ussClassName + "__inspector-field" );
            timeSpanField.style.flexGrow = 0.7F;

            bool first = true;
            if ( (this.m_UnitFlags & TimeUnitFlags.Day) > 0 )
            {
                timeSpanField.Add( CreateNumericField( TimeUnit.Days, first ) );
                first = false;
            }
            if ( (this.m_UnitFlags & TimeUnitFlags.Hour) > 0 )
            {
                timeSpanField.Add( CreateNumericField( TimeUnit.Hours, first ) );
                first = false;
            }
            if ( (this.m_UnitFlags & TimeUnitFlags.Minute) > 0 )
            {
                timeSpanField.Add( CreateNumericField( TimeUnit.Minutes, first ) );
                first = false;
            }
            if ( (this.m_UnitFlags & TimeUnitFlags.Second) > 0 )
            {
                timeSpanField.Add( CreateNumericField( TimeUnit.Seconds, first ) );
                first = false;
            }
            if ( (this.m_UnitFlags & TimeUnitFlags.Mili) > 0 )
            {
                timeSpanField.Add( CreateNumericField( TimeUnit.Milis, first ) );
                first = false;
            }
            this.Add( textField );
            this.Add( timeSpanField );
        }

        protected void OnChangeUnitValue( TimeUnit timeUnit, int prevValue, int nextValue )
        {
            int value = nextValue - prevValue;
            long addedTime = GetTotalTimeUnits( GetTimeSpan( timeUnit, System.Math.Abs( value ) ), m_TimeRange.timeUnit );
            m_FieldValue = m_TimeRange.ClampTime( m_FieldValue + addedTime * System.Math.Sign( value ) );
            OnChangeTimeUnit( notify: true );
        }

        protected virtual void OnChangeTimeUnit( bool notify )
        {
            for ( int i = 0; i < m_IntegerFields.Length; i++ )
            {
                IntegerField integerField = m_IntegerFields[ i ];
                if ( integerField != null )
                {
                    integerField.SetValueWithoutNotify( GetTimeUnit( GetTimeSpan( m_TimeRange.timeUnit, m_FieldValue ), (TimeUnit)i ) );
                }
            }
        }

        public IntegerField CreateNumericField( TimeUnit timeUnit, bool first )
        {
            IntegerField integerField = new IntegerField( timeUnit.ToString()[ 0 ].ToString(), GetRangeOfTimeUnit( timeUnit ) );
            integerField.style.marginRight = 4;
            integerField.AddToClassList( BaseCompositeField<int, IntegerField, int>.fieldUssClassName );
            if ( first ) integerField.AddToClassList( BaseCompositeField<int, IntegerField, int>.firstFieldVariantUssClassName );
            integerField.SetValueWithoutNotify( GetTimeUnit( GetTimeSpan( m_TimeRange.timeUnit, m_FieldValue ), timeUnit ) );
            integerField.RegisterValueChangedCallback( ( ChangeEvent<int> change ) => { OnChangeUnitValue( timeUnit, change.previousValue, change.newValue ); } );
            m_IntegerFields[ (int)timeUnit ] = integerField;
            return integerField;
        }
        
        public static int GetRangeOfTimeUnit( TimeUnit timeUnit )
        {
            return timeUnit switch
            {
                TimeUnit.Milis   => 1000,
                TimeUnit.Seconds => 1000,
                TimeUnit.Minutes => 60,
                TimeUnit.Hours   => 60,
                TimeUnit.Days    => 24,
                _ => 0
            };
        }

        /// <summary> Converts TimeSpan to specific time unit </summary>
        public long GetTotalTimeUnits( System.TimeSpan timeSpan, TimeUnit timeUnit )
        {
            switch ( timeUnit )
            {
                case TimeUnit.Milis:
                    return (long)timeSpan.TotalMilliseconds;
                case TimeUnit.Seconds:
                    return (long)timeSpan.TotalSeconds;
                case TimeUnit.Minutes:
                    return (long)timeSpan.TotalMinutes;
                case TimeUnit.Hours:
                    return (long)timeSpan.TotalHours;
                case TimeUnit.Days:
                    return (long)timeSpan.TotalDays;
                default:
                    return timeSpan.Ticks;
            }
        }

        /// <summary> Get TimeSpan specific time unit </summary>
        public int GetTimeUnit( System.TimeSpan timeSpan, TimeUnit timeUnit )
        {
            switch ( timeUnit )
            {
                case TimeUnit.Milis:
                    return timeSpan.Milliseconds;
                case TimeUnit.Seconds:
                    return timeSpan.Seconds;
                case TimeUnit.Minutes:
                    return timeSpan.Minutes;
                case TimeUnit.Hours:
                    return timeSpan.Hours;
                case TimeUnit.Days:
                    return timeSpan.Days;
                default:
                    return 0;
            }
        }


        /// <summary> Converts unit time value to TimeSpan </summary>
        public System.TimeSpan GetTimeSpan( TimeUnit timeUnit, long time )
        {
            switch ( timeUnit )
            {
                case TimeUnit.Milis:
                    return System.TimeSpan.FromMilliseconds( time );
                case TimeUnit.Seconds:
                    return System.TimeSpan.FromSeconds( time );
                case TimeUnit.Minutes:
                    return System.TimeSpan.FromMinutes( time );
                case TimeUnit.Hours:
                    return System.TimeSpan.FromHours( time );
                case TimeUnit.Days:
                    return System.TimeSpan.FromDays( time );
                default: return System.TimeSpan.Zero;
            };
        }

        protected void OnChangeTimeValue<ValueType>( TextValueField<ValueType> textField, ChangeEvent<ValueType> change )
        {
            if ( textField is UnsignedLongField uint64field )
            {
                if ( change is ChangeEvent<ulong> uint64Change )
                {
                    m_FieldValue = m_TimeRange.ClampTime( (long)uint64Change.newValue );
                    uint64field.SetValueWithoutNotify( (ulong)m_FieldValue );
                    OnChangeTimeUnit( notify: false );
                }
                return;
            }
            if ( textField is LongField int54Field )
            {
                if ( change is ChangeEvent<long> int64Change )
                {
                    m_FieldValue = m_TimeRange.ClampTime( (long)int64Change.newValue );
                    int54Field.SetValueWithoutNotify( m_FieldValue );
                    OnChangeTimeUnit( notify: false );
                }
                return;
            }
            if ( textField is UnsignedIntegerField uint32Field )
            {
                if ( change is ChangeEvent<uint> uint32Change )
                {
                    m_FieldValue = m_TimeRange.ClampTime( (long)uint32Change.newValue );
                    uint32Field.SetValueWithoutNotify( (uint)m_FieldValue );
                    OnChangeTimeUnit( notify: false );
                }
                return;
            }
            if ( textField is IntegerField int32Field )
            {
                if ( change is ChangeEvent<int> int32Change )
                {
                    m_FieldValue = m_TimeRange.ClampTime( (long)int32Change.newValue );
                    int32Field.SetValueWithoutNotify( (int)m_FieldValue );
                    OnChangeTimeUnit( notify: false );
                }
                return;
            }
        }
    }

    public class TimeSpanPropertyField : TimeSpanFieldBase
    {
        private PropertyField propertyField;
        private SerializedProperty property;
        private BindableElement textField;
        protected SerializedPropertyNumericType type;

        public TimeSpanPropertyField( TimeUnitFlags unitFlags, TimeRange timeRange, PropertyField propertyField, SerializedProperty property ) :
            base( unitFlags, timeRange )
        {
            this.m_FieldValue = property.numericType switch
            {
                SerializedPropertyNumericType.UInt64 => (long)property.ulongValue,
                SerializedPropertyNumericType.Int64 => property.longValue,
                SerializedPropertyNumericType.UInt32 => property.uintValue,
                SerializedPropertyNumericType.Int32 => property.intValue,
                SerializedPropertyNumericType.UInt16 => property.uintValue,
                SerializedPropertyNumericType.Int16 => property.intValue,
                SerializedPropertyNumericType.UInt8 => property.uintValue,
                SerializedPropertyNumericType.Int8 => property.intValue,
                _ => 0L
            };
            this.propertyField = propertyField;
            this.m_PropertyPath = $"{property.serializedObject.targetObject.GetType()}.{property.propertyPath}";
            this.property = property;
            this.type = property.numericType;

            CreateGUI();
        }

        protected override void CreateGUI( )
        {
            if ( propertyField is PropertyField pf )
            {
                pf.style.display = DisplayStyle.None;
            }

            switch ( type )
            {
                case SerializedPropertyNumericType.UInt64:
                    UnsignedLongField uint64field = new( property.displayName );
                    uint64field.Bind( property.serializedObject );
                    uint64field.BindProperty( property );
                    uint64field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( uint64field, change ) );
                    textField = uint64field;
                    break;
                case SerializedPropertyNumericType.Int64:
                    LongField int54Field = new( property.displayName );
                    int54Field.Bind( property.serializedObject );
                    int54Field.BindProperty( property );
                    int54Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( int54Field, change ) );
                    textField = int54Field;
                    break;
                case SerializedPropertyNumericType.UInt32:
                    UnsignedIntegerField uint32Field = new( property.displayName );
                    uint32Field.Bind( property.serializedObject );
                    uint32Field.BindProperty( property );
                    uint32Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( uint32Field, change ) );
                    textField = uint32Field;
                    break;
                case SerializedPropertyNumericType.Int32:
                    IntegerField int32Field = new( property.displayName );
                    int32Field.Bind( property.serializedObject );
                    int32Field.BindProperty( property );
                    int32Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( int32Field, change ) );
                    textField = int32Field;
                    break;
                case SerializedPropertyNumericType.UInt16:
                    UnsignedIntegerField uint16Field = new( property.displayName );
                    uint16Field.Bind( property.serializedObject );
                    uint16Field.BindProperty( property );
                    uint16Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( uint16Field, change ) );
                    textField = uint16Field;
                    break;
                case SerializedPropertyNumericType.Int16:
                    IntegerField int16Field = new( property.displayName );
                    int16Field.Bind( property.serializedObject );
                    int16Field.BindProperty( property );
                    int16Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( int16Field, change ) );
                    textField = int16Field;
                    break;
                case SerializedPropertyNumericType.UInt8:
                    UnsignedIntegerField uint8Field = new( property.displayName );
                    uint8Field.Bind( property.serializedObject );
                    uint8Field.BindProperty( property );
                    uint8Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( uint8Field, change ) );
                    textField = uint8Field;
                    break;
                case SerializedPropertyNumericType.Int8:
                    IntegerField int8Field = new( property.displayName );
                    int8Field.Bind( property.serializedObject );
                    int8Field.BindProperty( property );
                    int8Field.RegisterValueChangedCallback( ( change ) => OnChangeTimeValue( int8Field, change ) );
                    textField = int8Field;
                    break;
                default:
                    textField = null;
                    break;
            }

            this.Add( propertyField );
            DrawTimeSpanField( textField );
        }

        protected override void OnChangeTimeUnit( bool notify )
        {
            switch ( type )
            {
                case SerializedPropertyNumericType.UInt64:
                    if ( this.textField is UnsignedLongField uint64field )
                    {
                        if ( notify ) uint64field.value = (ulong)m_FieldValue;
                        else uint64field.SetValueWithoutNotify( (ulong)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.Int64:
                    if ( this.textField is LongField int64Field )
                    {
                        if ( notify ) int64Field.value = (long)m_FieldValue;
                        else int64Field.SetValueWithoutNotify( (long)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.UInt32:
                    if ( this.textField is UnsignedIntegerField uint32Field )
                    {
                        if ( notify ) uint32Field.value = (uint)m_FieldValue;
                        else uint32Field.SetValueWithoutNotify( (uint)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.Int32:
                    if ( this.textField is IntegerField int32Field )
                    {
                        if ( notify ) int32Field.value = (int)m_FieldValue;
                        else int32Field.SetValueWithoutNotify( (int)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.UInt16:
                    if ( this.textField is UnsignedIntegerField uint16Field )
                    {
                        if ( notify ) uint16Field.value = (uint)m_FieldValue;
                        else uint16Field.SetValueWithoutNotify( (uint)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.Int16:
                    if ( this.textField is IntegerField int16Field )
                    {
                        if ( notify ) int16Field.value = (int)m_FieldValue;
                        else int16Field.SetValueWithoutNotify( (int)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.UInt8:
                    if ( this.textField is UnsignedIntegerField uint8Field )
                    {
                        if ( notify ) uint8Field.value = (uint)m_FieldValue;
                        else uint8Field.SetValueWithoutNotify( (uint)m_FieldValue );
                    }
                    break;
                case SerializedPropertyNumericType.Int8:
                    if ( this.textField is IntegerField int8Field )
                    {
                        if ( notify ) int8Field.value = (int)m_FieldValue;
                        else int8Field.SetValueWithoutNotify( (int)m_FieldValue );
                    }
                    break;
                default:
                    textField = null;
                    break;
            }

            base.OnChangeTimeUnit( notify ); 
        }
    }

    public class TimeSpanTextField<ValueType> : TimeSpanFieldBase
    {
        TextValueField<ValueType> textField;
        protected string label;

        public TimeSpanTextField( TimeUnitFlags unitFlags, TimeRange timeRange, string label, TextValueField<ValueType> textField, ValueType fieldValue ) :
            base( unitFlags, timeRange )
        {
            this.m_FieldValue = System.Convert.ToInt64( fieldValue );
            this.textField = textField;
            this.m_PropertyPath = $"{fieldValue.GetType()}.{label}";
            this.label = label;
            textField.RegisterValueChangedCallback( OnChangeTimeValue );

            CreateGUI();
        }

        protected void OnChangeTimeValue( ChangeEvent<ValueType> change )
        {
            OnChangeTimeValue( textField, change );
            OnChangeTimeUnit( notify: false );
        }

        protected override void CreateGUI( )
        {
            DrawTimeSpanField( textField );
        }

        protected override void OnChangeTimeUnit( bool notify )
        {
            if ( this.textField is UnsignedLongField uint64field )
            {
                uint64field.value = (ulong)m_FieldValue;
            }
            else if ( this.textField is LongField int54Field )
            {
                int54Field.value = (long)m_FieldValue;
            }
            else if ( this.textField is UnsignedIntegerField uint32Field )
            {
                uint32Field.value = (uint)m_FieldValue;
            }
            else if ( this.textField is IntegerField int32Field )
            {
                int32Field.value = (int)m_FieldValue;
            }

            base.OnChangeTimeUnit( notify );
        }
    }
}