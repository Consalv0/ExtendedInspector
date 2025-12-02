
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using Void = ExtendedInspector.InspectorUtils.Void;

namespace ExtendedInspector.Editor
{
    public class Inspector
    {
        public const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        private VisualElement m_Container;
        private SortedList<PropertyOrderInfo, InspectorElement> m_Elements = new( new PropertyOrderComparer() );
        private Dictionary<int, NewfangledFieldGroup> m_FieldGroups = new();

        private List<Inspector> m_ChildInspectors;

        protected System.Func<object> m_Get;
        protected System.Action<object> m_Set;
        protected System.Type m_ValueType;
        protected object m_CachedTarget;
        // protected Object[] m_Targets;
        protected SerializedProperty m_SerializedProperty;
        protected SerializedObject m_SerializedObject;
        protected Inspector m_Parent;
        protected int m_Level;
        protected bool m_Serialized;
        protected long m_TickDelay;

        public long TickDelay => m_TickDelay;
        public VisualElement Container => m_Container;

        protected object GetTarget( ) => Target;

        protected object Target
        {
            get
            {
                if ( m_Get != null )
                {
                    m_CachedTarget = m_Get.Invoke();
                }
                return m_CachedTarget;
            }
            set
            {
                if ( m_Set != null )
                {
                    m_Set.Invoke( value );
                }
                m_CachedTarget = value;
            }
        }

        public Inspector( UnityEngine.Object[] targets, SerializedObject serializedObject, long tickDelay = -1 )
        {
            m_CachedTarget = targets[ 0 ];
            m_TickDelay = tickDelay < 0 ? 500 : tickDelay;
            // m_Targets = targets;
            m_ValueType = m_CachedTarget.GetType();
            m_SerializedObject = serializedObject;
            m_Serialized = true;
        }

        public Inspector( System.Type valueType, SerializedProperty serializedProperty, Inspector parent, long tickDelay = -1 )
        {
            if ( parent != null ) m_Level = parent.m_Level + 1;
            m_TickDelay = tickDelay < 0 ? parent == null ? parent.m_TickDelay : 500 : tickDelay;
            m_Parent = parent;
            m_Get = ( ) => serializedProperty.boxedValue;
            m_Set = ( object value ) => serializedProperty.boxedValue = value;
            m_ValueType = valueType;
            if ( serializedProperty != null )
            {
                m_SerializedObject = serializedProperty.serializedObject;
                m_SerializedProperty = serializedProperty;
            }
            m_Serialized = m_SerializedProperty != null;
        }

        public Inspector( System.Type valueType, System.Func<object> get, System.Action<object> set, SerializedProperty serializedProperty, Inspector parent, long tickDelay = -1 )
        {
            if ( parent != null ) m_Level = parent.m_Level + 1;
            m_TickDelay = tickDelay < 0 ? parent == null ? parent.m_TickDelay : 500 : tickDelay;
            m_Parent = parent;
            m_Get = get;
            m_Set = set;
            m_ValueType = valueType;
            if ( serializedProperty != null )
            {
                m_SerializedObject = serializedProperty.serializedObject;
                m_SerializedProperty = serializedProperty;
            }
            m_Serialized = m_SerializedProperty != null;
        }

        public virtual VisualElement CreateInspectorGUI( System.Action<Inspector> customElementsAction = null )
        {
            m_Container = new VisualElement();
            m_Elements.Clear();

            if ( m_Serialized ) GetDefaultInspectorElements();
            GetNewfangledElements();
            customElementsAction?.Invoke( this );

            foreach ( var element in m_Elements )
            {
                m_Container.Add( element.Value.VisualElement );
            }

            m_Container.schedule.Execute( ( ) => { if ( m_Container.style.display != DisplayStyle.None ) UpdateMemberInfoRules(); } ).Every( m_TickDelay );
            return m_Container;
        }

        private void AddElement( MemberInfo memberInfo, VisualElement inputField, long tickDelay, bool forceDisabled = false )
        {
            AddElement( memberInfo, GetMemberOrder( memberInfo ), memberInfo.MetadataToken, inputField, tickDelay, forceDisabled );
        }

        private void AddElement( MemberInfo memberInfo, int order, VisualElement inputField, long tickDelay, bool forceDisabled = false )
        {
            AddElement( memberInfo, order, memberInfo != null ? memberInfo.MetadataToken : 0, inputField, tickDelay, forceDisabled );
        }

        private void AddElement( MemberInfo memberInfo, int order, int metadataToken,
            VisualElement inputField, long tickDelay, bool forceDisabled = false )
        {
            if ( memberInfo != null && memberInfo.GetCustomAttribute<FoldoutGroupAttribute>() is FoldoutGroupAttribute fouldoutGroup )
            {
                NewfangledFieldGroup newfangledGroup;
                if ( m_FieldGroups.TryGetValue( fouldoutGroup.id, out newfangledGroup ) == false )
                {
                    newfangledGroup = new ( fouldoutGroup.id, fouldoutGroup.label, order, metadataToken, fouldoutGroup.expanded, GetTarget, m_TickDelay, forceDisabled );
                    m_FieldGroups.Add( fouldoutGroup.id, newfangledGroup );
                    m_Elements.Add( newfangledGroup.OrderInfo, newfangledGroup );
                }
                else
                {
                    if ( fouldoutGroup.label.GetHashCode() != fouldoutGroup.id ) newfangledGroup.Name = fouldoutGroup.label;
                }
                NewfangledMemberField newfangledField = new ( memberInfo, order, metadataToken, m_ValueType, inputField, GetTarget, m_TickDelay, forceDisabled );
                newfangledGroup.AddElement( newfangledField );
            }
            else
            {
                NewfangledMemberField newfangledField = new ( memberInfo, order, metadataToken, m_ValueType, inputField, GetTarget, m_TickDelay, forceDisabled );
                m_Elements.Add( newfangledField.OrderInfo, newfangledField );
            }
        }

        public void AddElement( VisualElement visualElement, int order = 0, int metadataToken = 0, bool forceDisabled = false )
        {
            NewfangledMemberField newfangledMemberField = new( null, order, metadataToken, null, visualElement, null, 0, forceDisabled );
            m_Elements.Add( newfangledMemberField.OrderInfo, newfangledMemberField );
        }

        protected virtual void GetDefaultInspectorElements( )
        {
            List<SerializedProperty> serializedProperties;
            if ( m_SerializedProperty != null )
                serializedProperties = m_SerializedProperty.GetVisibleChildren().SortProperties();
            else
                serializedProperties = m_SerializedObject.GetIterator().GetVisibleChildren().SortProperties();

            foreach ( SerializedProperty property in serializedProperties )
            {
                FieldInfo fieldInfo = FindFieldInfo( property.name, Target );
                int order = GetMemberOrder( fieldInfo );

                if ( fieldInfo != null && CollectionView.HasCollectionViewForType( fieldInfo.FieldType ) )
                {
                    if ( GetPropertyDrawer( fieldInfo.FieldType ) == null && HasPropertyDrawer( fieldInfo.FieldType ) == false )
                    {
                        VisualElement fieldElement = CreateField( fieldInfo, fieldInfo.FieldType,
                            ( ) => fieldInfo.GetValue( Target ),
                            ( object value ) => { fieldInfo.SetValue( m_CachedTarget, value ); m_Set?.Invoke( m_CachedTarget ); },
                            property, m_TickDelay
                        );
                        AddElement( fieldInfo, order, fieldElement, m_TickDelay );
                        continue;
                    }
                }

                if ( fieldInfo != null && property.propertyType == SerializedPropertyType.Generic && property.hasChildren )
                {
                    if ( GetPropertyDrawer( fieldInfo.FieldType ) == null && HasPropertyDrawer( fieldInfo.FieldType ) == false )
                    {
                        if ( m_ChildInspectors == null ) m_ChildInspectors = new();

                        Inspector newfangledInspector = new ( fieldInfo.FieldType, property.Copy(), this );
                        m_ChildInspectors.Add( newfangledInspector );

                        if ( fieldInfo.GetCustomAttribute<InlinePropertyAttribute>() is InlinePropertyAttribute inlineProperty )
                        {
                            VisualElement visualElement = newfangledInspector.CreateInspectorGUI();
                            AddDecorators( fieldInfo, ref visualElement );
                            AddElement( fieldInfo, order, visualElement, m_TickDelay );
                        }
                        else
                        {
                            Foldout serializedObjectFoldout = new Foldout { text = property.displayName };
                            serializedObjectFoldout.AddToClassList( BaseListView.foldoutHeaderUssClassName );
                            serializedObjectFoldout.Q<Toggle>().style.marginLeft = -12;
                            serializedObjectFoldout.bindingPath = property.propertyPath;
                            VisualElement container = serializedObjectFoldout.contentContainer;
                            container.style.paddingLeft = 15;
                            container.style.marginLeft = -7;
                            container.style.borderLeftWidth = 1;
                            container.style.borderLeftColor = Color.gray3;
                            VisualElement visualElement = newfangledInspector.CreateInspectorGUI();
                            AddDecorators( fieldInfo, ref visualElement );
                            container.Add( visualElement );
                            AddElement( fieldInfo, order, serializedObjectFoldout, m_TickDelay );
                        }
                        continue;
                    }
                }

                if ( m_Level == 0 && property.name == "m_Script" )
                {
                    PropertyField propertyField = new( property.Copy() );
                    propertyField.BindProperty( property.serializedObject );
                    AddElement( typeof( MonoScript ), -100, -100, propertyField, m_TickDelay, forceDisabled: true );
                    continue;
                }

                VisualElement field;
                if ( fieldInfo?.GetCustomAttribute<TimeSpanAttribute>() is TimeSpanAttribute timeSpan )
                {
                    PropertyField propertyField = new( property.Copy() );
                    propertyField.BindProperty( property.serializedObject );
                    TimeSpanFieldBase inlineField = new TimeSpanPropertyField( timeSpan.unitFlags, timeSpan.timeRange, propertyField, property.Copy() );
                    field = inlineField;
                }
                if ( fieldInfo?.GetCustomAttribute<InlineEditorAttribute>() is InlineEditorAttribute inlineEditor )
                {
                    PropertyField propertyField = new( property.Copy() );
                    propertyField.BindProperty( property.serializedObject );
                    InlineField inlineField = new InlineField( property.displayName, propertyField, property.Copy(), inlineEditor.expanded );
                    field = inlineField;
                }
                else
                {
                    PropertyField propertyField = new( property.Copy() );
                    propertyField.BindProperty( property.serializedObject );
                    field = propertyField;
                }

                AddElement( fieldInfo, order, field, m_TickDelay );
            }
        }

        public static bool HasPropertyDrawer( System.Type fieldType )
        {
            IEnumerable<System.Attribute> propertyAttributes = fieldType.GetCustomAttributes<System.Attribute>( inherit: true );
            foreach ( var property in propertyAttributes )
            {
                if ( GetPropertyDrawer( property.GetType() ) != null )
                    return true;
            }

            if ( GetPropertyDrawer( fieldType ) != null )
                return true;

            return false;
        }

        public static System.Type GetPropertyDrawer( System.Type classType )
        {
            Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            object scriptAttributeUtility = assembly.CreateInstance("UnityEditor.ScriptAttributeUtility");
            System.Type scriptAttributeUtilityType = scriptAttributeUtility.GetType();

            BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            MethodInfo getDrawerTypeForType = scriptAttributeUtilityType.GetMethod("GetDrawerTypeForType", bindingFlags);

            return (System.Type)getDrawerTypeForType.Invoke( scriptAttributeUtility, new object[] { classType, null, false } );
        }

        /// <summary>
        /// Draws all the members marked with the ShowInInspector attribute
        /// </summary>
        /// <returns>A visual element containing all non serialized member fields</returns>
        protected virtual void GetNewfangledElements( )
        {
            if ( Target == null )
                return;

            IEnumerable<FieldInfo> nonSerializedFields = m_ValueType.GetFields(BINDING_FLAGS).Where( (field) => 
                (m_Serialized == false && (field.IsPublic || field.IsNotSerialized == false) && field.IsStatic == false && field.IsLiteral == false )
                || field.GetCustomAttributes<PropertyAttribute>().Any() );

            foreach ( var nonSerializedField in nonSerializedFields )
            {
                ShowInInspectorAttribute showInInspector = nonSerializedField.GetCustomAttribute<ShowInInspectorAttribute>();
                if ( m_Serialized == false || showInInspector is ShowInInspectorAttribute )
                {
                    if ( HasRestrictedAttributes( nonSerializedField, out string errorMessage ) )
                    {
                        AddElement( nonSerializedField,
                            new HelpBox( errorMessage, HelpBoxMessageType.Error ),
                            m_TickDelay );
                        continue;
                    }

                    VisualElement field = CreateField( nonSerializedField, nonSerializedField.FieldType,
                        () =>
                        {
                            if ( nonSerializedField.IsStatic )
                                return nonSerializedField.GetValue( null );
                            else
                            {
                                object target = Target;
                                if ( target == null ) return null;
                                else
                                {
                                    return nonSerializedField.GetValue( target );
                                }
                            }
                        },
                        (object value ) => 
                        {
                            if ( nonSerializedField.IsStatic )
                                nonSerializedField.SetValue( null, value );
                            else
                            {
                                object target = Target;
                                if ( target == null ) return;
                                else
                                {
                                    nonSerializedField.SetValue( target, value ); m_Set?.Invoke( target );
                                }
                            }
                        },
                        m_SerializedProperty, m_Serialized == false ? 0L : showInInspector.tickDelay
                    );
                    AddElement( nonSerializedField, field, m_Serialized == false ? 0L : showInInspector.tickDelay );
                }
            }

            IEnumerable<System.Reflection.PropertyInfo> nonSerializedProperties = m_ValueType.GetProperties(BINDING_FLAGS).Where((field) => field.GetCustomAttributes<PropertyAttribute>().Any() );

            foreach ( var nonSerializedProperty in nonSerializedProperties )
            {
                if ( nonSerializedProperty.GetCustomAttribute<ShowInInspectorAttribute>() is ShowInInspectorAttribute showInInspector )
                {
                    if ( HasRestrictedAttributes( nonSerializedProperty, out string errorMessage ) )
                    {
                        AddElement( nonSerializedProperty,
                            new HelpBox( errorMessage, HelpBoxMessageType.Error ),
                            m_TickDelay );
                        continue;
                    }

                    MethodInfo setMethod = nonSerializedProperty.GetSetMethod();
                    System.Action<object> set = null;
                    if ( setMethod != null )
                    {
                        set = ( object value ) =>
                        {
                            if ( setMethod.IsStatic )
                                nonSerializedProperty.SetValue( null, value );
                            else
                            {
                                object target = Target;
                                if ( target == null ) return;
                                else
                                {
                                    nonSerializedProperty.SetValue( target, value );
                                }
                            }
                        };
                    }
                    MethodInfo getMethod = nonSerializedProperty.GetGetMethod( nonPublic: true );
                    System.Func<object> get = null;
                    if ( getMethod != null )
                    {
                        get = ( ) =>
                        {
                            if ( getMethod.IsStatic )
                                return nonSerializedProperty.GetValue( null );
                            else
                            {
                                object target = Target;
                                if ( target == null ) return null;
                                else
                                {
                                    return nonSerializedProperty.GetValue( target );
                                }
                            }
                        };
                    }
                    else continue;

                    VisualElement field = CreateField( nonSerializedProperty, nonSerializedProperty.PropertyType, get, set, null, showInInspector.tickDelay );
                    AddElement( nonSerializedProperty, field, showInInspector.tickDelay, forceDisabled: setMethod == null );
                }
                else if ( nonSerializedProperty.GetCustomAttribute<ButtonAttribute>() is ButtonAttribute buttonAttribute )
                {
                    MethodInfo setMethod = nonSerializedProperty.GetSetMethod();
                    if ( setMethod != null )
                    {
                        ParameterInfo[] parameterInfos = nonSerializedProperty.SetMethod.GetParameters();
                        if ( parameterInfos.Length > 0 )
                        {
                            MethodButtonField button = new( nonSerializedProperty.SetMethod.IsStatic ? null : Target, nonSerializedProperty.SetMethod,
                                buttonAttribute.label, InspectorUtils.GetIcon( buttonAttribute.iconName ), parameterInfos );
                            AddElement( nonSerializedProperty, button, m_TickDelay );
                        }
                        else
                        {
                            MethodButtonField button = new( nonSerializedProperty.SetMethod.IsStatic ? null : Target, nonSerializedProperty.SetMethod,
                                buttonAttribute.label, InspectorUtils.GetIcon( buttonAttribute.iconName ), null );
                            AddElement( nonSerializedProperty, button, m_TickDelay );
                        }
                    }
                }
            }

            IEnumerable<MethodInfo> nonSerializedMethods = m_ValueType.GetMethods(BINDING_FLAGS).Where((field) => field.GetCustomAttributes<PropertyAttribute>().Any() );

            foreach ( var nonSerializedMethod in nonSerializedMethods )
            {
                if ( nonSerializedMethod.GetCustomAttribute<ShowInInspectorAttribute>() is ShowInInspectorAttribute showInInspector )
                {
                    if ( HasRestrictedAttributes( nonSerializedMethod, out string errorMessage ) )
                    {
                        AddElement( nonSerializedMethod, new HelpBox( errorMessage, HelpBoxMessageType.Error ), m_TickDelay );
                        continue;
                    }

                    if ( nonSerializedMethod.ReturnParameter.ParameterType == typeof(void) )
                    {
                        AddElement( nonSerializedMethod,
                            new HelpBox( $"Method <b>{nonSerializedMethod.Name}</b> cannot be drawn because is not a function", HelpBoxMessageType.Error ),
                            m_TickDelay );
                        continue;
                    }

                    if ( nonSerializedMethod.GetParameters().Length > 0 || nonSerializedMethod.ContainsGenericParameters )
                    {
                        AddElement( nonSerializedMethod,
                            new HelpBox( $"Method <b>{nonSerializedMethod.Name}</b> cannot be drawn because it has parameters or is generic", HelpBoxMessageType.Error ),
                            m_TickDelay );
                        continue;
                    }

                    VisualElement field = CreateField( nonSerializedMethod, nonSerializedMethod.ReturnType,
                        () =>
                        {
                            if ( nonSerializedMethod.IsStatic )
                                return nonSerializedMethod.Invoke( null, null );
                            else
                            {
                                object target = Target;
                                if ( target == null ) return null;
                                else
                                {
                                    return nonSerializedMethod.Invoke( target, null );
                                }
                            }
                        }, null, null, showInInspector.tickDelay );
                    AddElement( nonSerializedMethod, field, m_TickDelay, forceDisabled: true );
                }
                else if ( nonSerializedMethod.GetCustomAttribute<ButtonAttribute>() is ButtonAttribute buttonAttribute )
                {
                    if ( nonSerializedMethod.ContainsGenericParameters )
                    {
                        AddElement( nonSerializedMethod,
                            new HelpBox( $"Method <b>{nonSerializedMethod.Name}</b> cannot be drawn because is generic", HelpBoxMessageType.Error ),
                            m_TickDelay );
                        continue;
                    }

                    ParameterInfo[] parameterInfos = nonSerializedMethod.GetParameters();
                    if ( parameterInfos.Length > 0 )
                    {
                        MethodButtonField button = new( nonSerializedMethod.IsStatic ? null : Target, nonSerializedMethod,
                                buttonAttribute.label, InspectorUtils.GetIcon( buttonAttribute.iconName ), parameterInfos );
                        AddElement( nonSerializedMethod, button, m_TickDelay );
                    }
                    else
                    {
                        MethodButtonField button = new( nonSerializedMethod.IsStatic ? null : Target, nonSerializedMethod,
                                buttonAttribute.label, InspectorUtils.GetIcon( buttonAttribute.iconName ), null );
                        AddElement( nonSerializedMethod, button, m_TickDelay );
                    }
                }
            }
        }

        private int GetMemberOrder( MemberInfo memberInfo )
        {
            if ( memberInfo == null )
                return 0;

            int order = 0;
            IEnumerable<PropertyAttribute> propertyAttributes = memberInfo.GetCustomAttributes<PropertyAttribute>();

            foreach ( PropertyAttribute propertyAttribute in propertyAttributes )
            {
                if ( propertyAttributes is FoldoutGroupAttribute )
                    continue;

                SetOrder( ref order, propertyAttribute );
            }
            return order;
        }

        private void UpdateMemberInfoRules( )
        {
            foreach ( var element in m_Elements )
            {
                element.Value.UpdateVisibility();
            }
        }

        private static void SetOrder( ref int order, PropertyAttribute propertyAttribute )
        {
            if ( order == 0 )
            {
                order = propertyAttribute.order;
            }
            else
            {
                if ( propertyAttribute.order != 0 )
                {
                    order = propertyAttribute.order;
                }
            }
        }

        private VisualElement CreateField( MemberInfo memberInfo, System.Type memberType, 
            System.Func<object> get, System.Action<object> set, 
            SerializedProperty serializedProperty, long tickDelay
        )
        {
            string fieldName = ObjectNames.NicifyVariableName( memberInfo.Name );

            VisualElement field;
            if ( memberInfo.GetCustomAttribute<TimeSpanAttribute>() is TimeSpanAttribute timeSpan )
            {
                object fieldValue;
                try
                {
                    fieldValue = get.Invoke();
                }
                catch
                {
                    fieldValue = null;
                    field = new();
                }

                if ( fieldValue != null )
                {
                    System.Type fieldType = fieldValue.GetType();
                    if ( m_SerializedObject != null && memberInfo.GetCustomAttribute<TrackSerializedPropertyAttribute>() is TrackSerializedPropertyAttribute trackProperty )
                    {
                        SerializedProperty property = m_SerializedObject.FindProperty( trackProperty.propertyPath );
                        PropertyField propertyField = new( property, fieldName );
                        propertyField.BindProperty( property.serializedObject );
                        if ( fieldType == typeof( uint ) )
                        {
                            propertyField.RegisterValueChangeCallback( ( changeEvent ) => { try { set?.Invoke( get != null ? get.Invoke() : changeEvent.changedProperty.uintValue ); } catch { } } );
                        }
                        else if ( fieldType == typeof( ulong ) )
                        {
                            propertyField.RegisterValueChangeCallback( ( changeEvent ) => { try { set?.Invoke( get != null ? get.Invoke() : changeEvent.changedProperty.ulongValue ); } catch { } } );
                        }
                        else if ( fieldType == typeof( long ) )
                        {
                            propertyField.RegisterValueChangeCallback( ( changeEvent ) => { try { set?.Invoke( get != null ? get.Invoke() : changeEvent.changedProperty.longValue ); } catch { } } );
                        }
                        else if ( fieldType == typeof( int ) )
                        {
                            propertyField.RegisterValueChangeCallback( ( changeEvent ) => { try { set?.Invoke( get != null ? get.Invoke() : changeEvent.changedProperty.intValue ); } catch { } } );
                        }
                        TimeSpanFieldBase timeSpanField = new TimeSpanPropertyField( timeSpan.unitFlags, timeSpan.timeRange, propertyField, property );
                        field = timeSpanField;
                    }
                    else if ( fieldType == typeof( int ) || fieldType == typeof( uint ) || fieldType == typeof( long ) || fieldType == typeof( ulong ) )
                    {
                        TimeSpanFieldBase timeSpanField;
                        if ( fieldType == typeof( uint ) )
                        {
                            UnsignedIntegerField valueField = new ( fieldName ) { value = (uint)fieldValue, showMixedValue = AreNonSerializedMemberValuesDifferent( new[] { get } ) };
                            valueField.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                            valueField.schedule.Execute( ( ) => valueField.value = (uint)get.Invoke() ).Every( tickDelay );
                            timeSpanField = new TimeSpanTextField<uint>( timeSpan.unitFlags, timeSpan.timeRange, valueField.label, valueField, (uint)fieldValue );
                        }
                        else if ( fieldType == typeof( ulong ) )
                        {
                            UnsignedLongField valueField = new ( fieldName ) { value = (ulong)fieldValue, showMixedValue = AreNonSerializedMemberValuesDifferent( new[] { get } ) };
                            valueField.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                            valueField.schedule.Execute( ( ) => valueField.value = (ulong)get.Invoke() ).Every( tickDelay );
                            timeSpanField = new TimeSpanTextField<ulong>( timeSpan.unitFlags, timeSpan.timeRange, valueField.label, valueField, (ulong)fieldValue );
                        }
                        else if ( fieldType == typeof( long ) )
                        {
                            LongField valueField = new ( fieldName ) { value = (long)fieldValue, showMixedValue = AreNonSerializedMemberValuesDifferent( new[] { get } ) };
                            valueField.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                            valueField.schedule.Execute( ( ) => valueField.value = (long)get.Invoke() ).Every( tickDelay );
                            timeSpanField = new TimeSpanTextField<long>( timeSpan.unitFlags, timeSpan.timeRange, valueField.label, valueField, (long)fieldValue );
                        }
                        else
                        {
                            IntegerField valueField = new ( fieldName ) { value = (int)fieldValue, showMixedValue = AreNonSerializedMemberValuesDifferent( new[] { get } ) };
                            valueField.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                            valueField.schedule.Execute( ( ) => valueField.value = (int)get.Invoke() ).Every( tickDelay );
                            timeSpanField = new TimeSpanTextField<int>( timeSpan.unitFlags, timeSpan.timeRange, valueField.label, valueField, (int)fieldValue );
                        }

                        field = timeSpanField;
                    }
                    else
                    {
                        field = new VisualElement();
                    }
                }
                else
                {
                    field = new VisualElement();
                }
            }
            else if ( memberInfo.GetCustomAttribute<InlineEditorAttribute>() is InlineEditorAttribute inlineEditor )
            {
                object fieldValue;
                try
                {
                    fieldValue = get.Invoke();
                }
                catch
                {
                    fieldValue = null;
                    field = new VisualElement();
                }

                System.Type fieldType = memberInfo switch
                {
                    FieldInfo fieldInfo => fieldInfo.FieldType,
                    PropertyInfo propertyInfo => propertyInfo.PropertyType,
                    MethodInfo methodInfo => methodInfo.ReturnType,
                    _ => null
                };
                if ( fieldValue != null )
                {
                    fieldType = fieldValue.GetType();
                }

                if ( fieldType != null && fieldType.IsSubclassOf( typeof( UnityEngine.Object ) ) || fieldType == typeof( UnityEngine.Object ) )
                {
                    ObjectField objectField = new ( fieldName ) { value = (UnityEngine.Object)fieldValue, showMixedValue = AreNonSerializedMemberValuesDifferent( new[] { get } ) };
                    objectField.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                    SerializedProperty serializeProperty;
                    if ( memberInfo.GetCustomAttribute<TrackSerializedPropertyAttribute>() is TrackSerializedPropertyAttribute trackSerializedProperty )
                    {
                        serializeProperty = m_SerializedObject.FindProperty( trackSerializedProperty.propertyPath );
                    }
                    else serializeProperty = null;
                    InlineField inlineField = new ( objectField.label, objectField, fieldValue as UnityEngine.Object, serializeProperty, inlineEditor.expanded );
                    field = inlineField;
                }
                else
                {
                    field = new HelpBox( $"Can't make '{nameof(InlineEditorAttribute)}' of type '{fieldType}'", HelpBoxMessageType.Error );
                }
            }
            else
            {
                if ( HasPropertyDrawer( memberType ) )
                {
                    System.Type propertyDrawerType = GetPropertyDrawer( memberType );

                    if ( propertyDrawerType.IsSubclassOf( typeof( NewfangledPropertyDrawer ) ) )
                    {
                        object propertyDrawer = System.Activator.CreateInstance( propertyDrawerType, new object[] { memberInfo, memberInfo.Name, get, set, serializedProperty } );
                        if ( propertyDrawer is NewfangledPropertyDrawer drawer )
                        {
                            VisualElement propertyField = new();
                            propertyField.AddToClassList( PropertyField.ussClassName );
                            propertyField.AddToClassList( PropertyField.inspectorElementUssClassName );
                            propertyField.contentContainer.Add( drawer.CreatePropertyGUI( serializedProperty ) );
                            // propertyField.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                            propertyField.schedule.Execute( ( so ) => drawer.OnUpdateValue() ).Every( tickDelay );
                            field = propertyField;
                            goto ReturnField;
                        }
                    }
                }

                field = CreateFieldForType( memberInfo, memberType, memberInfo.Name, get, set, serializedProperty, AreNonSerializedMemberValuesDifferent( new[] { get } ), tickDelay );
            ReturnField: { }
            }

            AddDecorators( memberInfo, ref field );

            return field;
        }

        public static void AddDecorators( MemberInfo memberInfo, ref VisualElement field )
        {
            // Skip unity decorators if field is a PropertyField
            if ( field is PropertyField == false )
            {
                VisualElement container = null;
                foreach ( var spaceAttribute in memberInfo.GetCustomAttributes<SpaceAttribute>() )
                {
                    VisualElement space = new();

                    space.style.height = spaceAttribute.height;

                    space.AddToClassList( "unity-space-drawer" );
                    if ( container == null ) container = new();
                    container.Add( space );
                }

                HeaderAttribute headerAttribute = memberInfo.GetCustomAttribute<HeaderAttribute>();
                if ( headerAttribute != null )
                {
                    Label header = new()
                    {
                        style =
                        {
                            marginTop = 13,
                            marginLeft = 3,
                            marginRight = -2,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            unityTextAlign = TextAnchor.LowerLeft
                        }
                    };

                    header.AddToClassList( "unity-header-drawer__label" );
                    header.text = headerAttribute.header;
                    if ( container == null ) container = new();
                    container.Add( header );
                }

                if ( container != null )
                {
                    container.name = "unity-field-container";
                    container.Add( field );
                    field = container;
                }
            }
        }

        public static bool AreNonSerializedMemberValuesDifferent( System.Func<object>[] gets )
        {
            if ( gets.Length <= 1 )
            {
                return false;
            }

            object firstValue;
            try
            {
                firstValue = gets[ 0 ].Invoke();
            }
            catch
            {
                return false;
            }

            for ( int i = 1; i < gets.Length; i++ )
            {
                object otherValue;
                try
                {
                    otherValue = gets[ i ].Invoke();
                }
                catch
                {
                    return false;
                }

                if ( !Equals( firstValue, otherValue ) )
                    return true;
            }

            return false;
        }

        private bool HasRestrictedAttributes( MemberInfo memberInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            return false;
        }

        /// <summary>
        /// Sets the value of the appropriate field
        /// </summary>
        /// <param name="field">The visual element of the field</param>
        /// <param name="value">The value to set</param>
        /// <param name="notify">Whether to call the value change callback when setting the value</param>
        public static void SetFieldValue( VisualElement field, object value, bool notify = false )
        {
                 if ( field is IntegerField integerField )                  { if ( notify ) { integerField.value            = value == null ? default : (int)value;         } else { integerField.SetValueWithoutNotify(            value == null ? default : (int)value ); } }
            else if ( field is UnsignedIntegerField unsignedIntegerField )  { if ( notify ) { unsignedIntegerField.value    = value == null ? default : (uint)value;        } else { unsignedIntegerField.SetValueWithoutNotify(    value == null ? default : (uint)value ); } }
            else if ( field is LongField longField )                        { if ( notify ) { longField.value               = value == null ? default : (long)value;        } else { longField.SetValueWithoutNotify(               value == null ? default : (long)value ); } }
            else if ( field is UnsignedLongField unsignedLongField )        { if ( notify ) { unsignedLongField.value       = value == null ? default : (ulong)value;       } else { unsignedLongField.SetValueWithoutNotify(       value == null ? default : (ulong)value ); } }
            else if ( field is FloatField floatField )                      { if ( notify ) { floatField.value              = value == null ? default : (float)value;       } else { floatField.SetValueWithoutNotify(              value == null ? default : (float)value ); } }
            else if ( field is DoubleField doubleField )                    { if ( notify ) { doubleField.value             = value == null ? default : (double)value;      } else { doubleField.SetValueWithoutNotify(             value == null ? default : (double)value ); } }
            else if ( field is Toggle toggle )                              { if ( notify ) { toggle.value                  = value == null ? default : (bool)value;        } else { toggle.SetValueWithoutNotify(                  value == null ? default : (bool)value ); } }
            else if ( field is EnumField enumField )                        { if ( notify ) { enumField.value               = value == null ? default : (System.Enum)value; } else { enumField.SetValueWithoutNotify(               value == null ? default : (System.Enum)value ); } }
            else if ( field is Vector2Field vector2Field )                  { if ( notify ) { vector2Field.value            = value == null ? default : (Vector2)value;     } else { vector2Field.SetValueWithoutNotify(            value == null ? default : (Vector2)value ); } }
            else if ( field is Vector2IntField vector2IntField )            { if ( notify ) { vector2IntField.value         = value == null ? default : (Vector2Int)value;  } else { vector2IntField.SetValueWithoutNotify(         value == null ? default : (Vector2Int)value ); } }
            else if ( field is Vector3Field vector3Field )                  { if ( notify ) { vector3Field.value            = value == null ? default : (Vector3)value;     } else { vector3Field.SetValueWithoutNotify(            value == null ? default : (Vector3)value ); } }
            else if ( field is Vector3IntField vector3IntField )            { if ( notify ) { vector3IntField.value         = value == null ? default : (Vector3Int)value;  } else { vector3IntField.SetValueWithoutNotify(         value == null ? default : (Vector3Int)value ); } }
            else if ( field is Vector4Field vector4Field )                  { if ( notify ) { vector4Field.value            = value == null ? default : (Vector4)value;     } else { vector4Field.SetValueWithoutNotify(            value == null ? default : (Vector4)value ); } }
            else if ( field is ColorField colorField )                      { if ( notify ) { colorField.value              = value == null ? default : (Color)value;       } else { colorField.SetValueWithoutNotify(              value == null ? default : (Color)value ); } }
            else if ( field is LayerMaskField layerMaskField )              { if ( notify ) { layerMaskField.value          = value == null ? default : (LayerMask)value;   } else { layerMaskField.SetValueWithoutNotify(          value == null ? default : (LayerMask)value ); } }
            else if ( field is RectField rectField )                        { if ( notify ) { rectField.value               = value == null ? default : (Rect)value;        } else { rectField.SetValueWithoutNotify(               value == null ? default : (Rect)value ); } }
            else if ( field is RectIntField rectIntField )                  { if ( notify ) { rectIntField.value            = value == null ? default : (RectInt)value;     } else { rectIntField.SetValueWithoutNotify(            value == null ? default : (RectInt)value ); } }
            else if ( field is BoundsField boundsField )                    { if ( notify ) { boundsField.value             = value == null ? default : (Bounds)value;      } else { boundsField.SetValueWithoutNotify(             value == null ? default : (Bounds)value ); } }
            else if ( field is BoundsIntField boundsIntField )              { if ( notify ) { boundsIntField.value          = value == null ? default : (BoundsInt)value;   } else { boundsIntField.SetValueWithoutNotify(          value == null ? default : (BoundsInt)value ); } }
            else if ( field is TextField textField )                        { if ( notify ) { textField.value                                  = (string)value;             } else { textField.SetValueWithoutNotify(                                         (string)value ); } }
            else if ( field is GradientField gradientField )                { if ( notify ) { gradientField.value                              = (Gradient)value;           } else { gradientField.SetValueWithoutNotify(                                     (Gradient)value ); } }
            else if ( field is CurveField curveField )                      { if ( notify ) { curveField.value                                 = (AnimationCurve)value;     } else { curveField.SetValueWithoutNotify(                                        (AnimationCurve)value ); } }
            else if ( field is ObjectField objectField )                    { if ( notify ) { objectField.value                                = (UnityEngine.Object)value; } else { objectField.SetValueWithoutNotify(                                       (UnityEngine.Object)value ); } }
        }

        /// <summary>
        /// Creates a field for a specific type
        /// </summary>
        /// <param name="fieldType">The type of the field to create</param>
        /// <param name="fieldName">The name of the field</param>
        /// <param name="fieldValue">The default value of the field</param>
        /// <param name="showMixedValue">Whether to show the mixed value state for the field</param>
        /// <returns>A visual element of the appropriate field</returns>
        public VisualElement CreateFieldForType( MemberInfo memberInfo, System.Type fieldType, string fieldName, System.Func<object> get, System.Action<object> set, SerializedProperty serializedProperty, bool showMixedValue, long tickDelay )
        {
            fieldName = ObjectNames.NicifyVariableName( fieldName );

            object GetFieldValue( )
            {
                object fieldValue;
                try
                {
                    if ( get == null )
                    {
                        Debug.LogAssertion( "get is empty for <b>" + memberInfo.ToString() + "</b>", Target is UnityEngine.Object unityObject ? unityObject : null );
                        throw new System.NullReferenceException( "get" );
                    }
                    fieldValue = get.Invoke();
                }
                catch ( System.Exception e )
                {
                    fieldValue = null;
                    Debug.LogError( e, Target is UnityEngine.Object unityObject ? unityObject : null );
                }
                return fieldValue;
            }

            Type GetFieldValueSafe<Type>( )
            {
                object fieldValue = GetFieldValue();
                if ( fieldValue == null ) return default;
                else return (Type)fieldValue;
            }

            PropertyField propertyField = null;

            if ( serializedProperty != null && ( IsSimpleUnitySerializable( fieldType ) || fieldType.IsSerializable ) )
            {
                if ( IsSimpleUnitySerializable( fieldType ) || GetPropertyDrawer( fieldType ) != null || HasPropertyDrawer( fieldType ) )
                {
                    propertyField = new( serializedProperty, fieldName );
                    propertyField.BindProperty( serializedProperty );
                    propertyField.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                    return propertyField;
                }
            }

            TrackSerializedPropertyAttribute trackProperty;
            if ( m_SerializedObject != null && (trackProperty = memberInfo.GetCustomAttribute<TrackSerializedPropertyAttribute>()) != null )
            {
                serializedProperty = m_SerializedObject.FindProperty( trackProperty.propertyPath );
                if ( fieldType == m_SerializedObject.targetObject.GetType().GetFieldViaPath( serializedProperty.propertyPath ).FieldType )
                {
                    propertyField = new( serializedProperty, fieldName );
                    propertyField.BindProperty( serializedProperty.serializedObject );
                    propertyField.RegisterValueChangeCallback( ( changeEvent ) => {
                        if ( propertyField.style.display != DisplayStyle.None )
                            try { set?.Invoke( get != null ? GetFieldValue() : changeEvent.changedProperty.GetValue() ); }
                            catch { }
                    } );
                }
            }

            object fieldValue = GetFieldValue();

            if ( fieldType == typeof( object ) && fieldValue != null )
            {
                fieldType = fieldValue.GetType();
            }
            if ( fieldType == typeof( string ) )
            {
                TextField field = new( fieldName ) { value = fieldValue as string, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<string>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( int ) )
            {
                BaseField<int> bindableElement;
                if ( memberInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute rangeAttribute )
                    bindableElement = new SliderInt( fieldName, (int)rangeAttribute.min, (int)rangeAttribute.max ) { showInputField = true, value = fieldValue == null ? default : (int)fieldValue, showMixedValue = showMixedValue };
                else
                    bindableElement = new IntegerField( fieldName ) { value = fieldValue == null ? default : (int)fieldValue, showMixedValue = showMixedValue };
                bindableElement.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                bindableElement.schedule.Execute( ( so ) => { bindableElement.SetValueWithoutNotify( GetFieldValueSafe<int>() ); } ).Every( tickDelay );
                bindableElement.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return bindableElement;
            }
            else if ( fieldType == typeof( uint ) )
            {
                UnsignedIntegerField field = new( fieldName ) { value = fieldValue == null ? default : (uint)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<uint>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( long ) )
            {
                LongField field = new( fieldName ) { value = fieldValue == null ? default : (long)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<long>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( ulong ) )
            {
                UnsignedLongField field = new( fieldName ) { value = fieldValue == null ? default : (ulong)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<ulong>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( float ) )
            {
                BaseField<float> bindableElement;
                if ( memberInfo.GetCustomAttribute<RangeAttribute>() is RangeAttribute rangeAttribute )
                    bindableElement = new Slider( fieldName, rangeAttribute.min, rangeAttribute.max ) { showInputField = true, value = fieldValue == null ? default : (float)fieldValue, showMixedValue = showMixedValue };
                else
                    bindableElement = new FloatField( fieldName ) { value = fieldValue == null ? default : (float)fieldValue, showMixedValue = showMixedValue };
                bindableElement.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                bindableElement.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                // Need to find a way to not repeat this on every element
                if ( propertyField != null )
                {
                    propertyField.style.marginRight = 4;
                    propertyField.style.flexGrow = 0.5F;
                    bindableElement.style.flexGrow = 0.5F;
                    bindableElement.label = "get";
                    bindableElement.labelElement.AddToClassList( Label.disabledUssClassName );
                    bindableElement.labelElement.style.flexBasis = 22;
                    VisualElement visualElement = new VisualElement();
                    visualElement.style.flexDirection = FlexDirection.Row;
                    visualElement.AddToClassList( PropertyField.ussClassName );
                    visualElement.Add( propertyField );
                    visualElement.Add( bindableElement );

                    bindableElement.RegisterValueChangedCallback( ( changeEvent ) => {
                        PrefabUtility.RecordPrefabInstancePropertyModifications( m_SerializedObject.targetObject );
                        serializedProperty.floatValue = get != null ? GetFieldValueSafe<float>() : changeEvent.newValue;
                    } );
                    propertyField.RegisterValueChangeCallback( ( changeEvent ) => {
                        try { bindableElement.SetValueWithoutNotify( get != null ? GetFieldValueSafe<float>() : serializedProperty.floatValue ); }
                        catch { }
                    } );
                    return visualElement;
                }
                bindableElement.schedule.Execute( ( so ) => { bindableElement.SetValueWithoutNotify( GetFieldValueSafe<float>() ); } ).Every( tickDelay );
                return bindableElement;
            }
            else if ( fieldType == typeof( double ) )
            {
                DoubleField field = new( fieldName ) { value = fieldValue == null ? default : (double)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<double>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( bool ) )
            {
                Toggle field = new( fieldName ) { value = fieldValue == null ? default : (bool)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<bool>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType.IsEnum )
            {
                EnumField field = new( fieldName, fieldValue == null ? default : (System.Enum)fieldValue ) { showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<System.Enum>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Vector2 ) )
            {
                Vector2Field field = new( fieldName ) { value = fieldValue == null ? default : (Vector2)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Vector2>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Vector2Int ) )
            {
                Vector2IntField field = new( fieldName ) { value = fieldValue == null ? default : (Vector2Int)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Vector2Int>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Vector3 ) )
            {
                Vector3Field field = new( fieldName ) { value = fieldValue == null ? default : (Vector3)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Vector3>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Vector3Int ) )
            {
                Vector3IntField field = new( fieldName ) { value = fieldValue == null ? default : (Vector3Int)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Vector3Int>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Vector4 ) )
            {
                Vector4Field field = new( fieldName ) { value = fieldValue == null ? default : (Vector4)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Vector4>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Color ) )
            {
                ColorField field = new( fieldName ) { value = fieldValue == null ? default : (Color)fieldValue, showMixedValue = showMixedValue };
                if ( memberInfo.GetCustomAttribute<ColorUsageAttribute>() is ColorUsageAttribute colorUsage )
                {
                    field.showAlpha = colorUsage.showAlpha;
                    field.hdr = colorUsage.hdr;
                }
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Color>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Gradient ) )
            {
                GradientField field = new( fieldName ) { value = (Gradient)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Gradient>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( AnimationCurve ) )
            {
                CurveField field = new( fieldName ) { value = (AnimationCurve)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<AnimationCurve>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( LayerMask ) )
            {
                LayerMaskField field = new( fieldName, fieldValue == null ? default : (LayerMask)fieldValue ) { showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<LayerMask>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Rect ) )
            {
                RectField field = new( fieldName ) { value = fieldValue == null ? default : (Rect)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Rect>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( RectInt ) )
            {
                RectIntField field = new( fieldName ) { value = fieldValue == null ? default : (RectInt)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<RectInt>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( Bounds ) )
            {
                BoundsField field = new( fieldName ) { value = fieldValue == null ? default : (Bounds)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<Bounds>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( BoundsInt ) )
            {
                BoundsIntField field = new( fieldName ) { value = fieldValue == null ? default : (BoundsInt)fieldValue, showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<BoundsInt>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( System.Guid ) )
            {
                TextField field = new( fieldName ) { value = (fieldValue == null ? default : (System.Guid)fieldValue).ToString(), showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( new System.Guid( changeEvent.newValue ) ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<System.Guid>().ToString() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType == typeof( System.Type ) )
            {
                TextField field = new( fieldName ) { value = (fieldValue == null ? string.Empty : ((System.Type)fieldValue).AssemblyQualifiedName), showMixedValue = showMixedValue };
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( System.Type.GetType( changeEvent.newValue, throwOnError: false ) ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<System.Type>()?.AssemblyQualifiedName ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( fieldType.IsSubclassOf( typeof( UnityEngine.Object ) ) || fieldType == typeof( UnityEngine.Object ) )
            {
                ObjectField field = new( fieldName ) { value = (UnityEngine.Object)fieldValue, showMixedValue = showMixedValue };
                field.objectType = fieldType;
                field.RegisterValueChangedCallback( ( changeEvent ) => { try { set?.Invoke( changeEvent.newValue ); } catch { } } );
                field.schedule.Execute( ( so ) => { field.SetValueWithoutNotify( GetFieldValueSafe<UnityEngine.Object>() ); } ).Every( tickDelay );
                field.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                return field;
            }
            else if ( CollectionView.HasCollectionViewForType( fieldType ) )
            {
                System.Type[] fieldTypeInterfaces = fieldType.GetInterfaces();
                System.Type enumerableType = null;
                if ( fieldType.IsGenericType || fieldTypeInterfaces.Contains( typeof( IEnumerable ) ) || fieldTypeInterfaces.Contains( typeof( IEnumerable ) ) )
                {
                    if ( fieldType.IsGenericType )
                    {
                        enumerableType = fieldType.GetGenericTypeDefinition();
                    }
                    if ( enumerableType != typeof( IEnumerable<> ) )
                        enumerableType = fieldTypeInterfaces.FirstOrDefault( type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof( IEnumerable<> ) );
                    else enumerableType = fieldType;
                }

                if ( enumerableType != null )
                {
                    System.Type elementType;
                    if ( enumerableType != null ) elementType = enumerableType.GetGenericArguments()[ 0 ];
                    else elementType = fieldType.GetElementType();

                    return CollectionView.Create( fieldName, fieldType, elementType, memberInfo, get, set, serializedProperty, this );
                }

                return new HelpBox( $"The type <b>{fieldType}</b> is not supported", HelpBoxMessageType.Error );
            }
            else if ( m_Level < 10 && fieldValue != null && !CollectionView.HasCollectionViewForType( fieldType ) )
            {
                if ( m_ChildInspectors == null ) m_ChildInspectors = new();

                Inspector newfangledInspector = new ( fieldType, get, set, serializedProperty, this, tickDelay );
                m_ChildInspectors.Add( newfangledInspector );

                if ( memberInfo.GetCustomAttribute<InlinePropertyAttribute>() is InlinePropertyAttribute inlineProperty )
                {
                    VisualElement visualElement = newfangledInspector.CreateInspectorGUI();
                    AddDecorators( memberInfo, ref visualElement );
                    return visualElement;
                }
                else
                {
                    Foldout serializedObjectFoldout = new Foldout { text = fieldName };
                    serializedObjectFoldout.AddToClassList( BaseListView.foldoutHeaderUssClassName );
                    serializedObjectFoldout.Q<Toggle>().style.marginLeft = -12;

                    VisualElement container = serializedObjectFoldout.contentContainer;
                    container.style.paddingLeft = 15;
                    container.style.marginLeft = -7;
                    container.style.borderLeftWidth = 1;
                    container.style.borderLeftColor = Color.gray3;
                    VisualElement visualElement = newfangledInspector.CreateInspectorGUI();
                    AddDecorators( memberInfo, ref visualElement );
                    container.Add( visualElement );
                    if ( serializedProperty != null ) serializedObjectFoldout.bindingPath = serializedProperty.propertyPath;

                    return serializedObjectFoldout;
                }
            }
            else
            {
                VisualElement visualElement;
                void SetFieldValue( object value )
                {
                    if ( value == null )
                    {
                        ObjectField objectField = new ( fieldName );
                        objectField.dataSourceType = fieldType;
                        visualElement = objectField;
                    }
                    else
                    {
                        TextField textField = new ( fieldName );
                        textField.SetValueWithoutNotify( value.ToString() );
                        visualElement = textField;
                    }
                }
                SetFieldValue( fieldValue );
                visualElement.AddToClassList( BaseField<Void>.ussClassName + "__inspector-field" );
                visualElement.AddToClassList( BaseField<Void>.alignedFieldUssClassName );
                visualElement.schedule.Execute( ( so ) => { SetFieldValue( GetFieldValue() ); } ).Every( tickDelay );

                return visualElement;
            }
        }

        private static bool IsSimpleUnitySerializable( System.Type type ) =>
               type.IsEnum || type == typeof( string ) || type == typeof( int ) || type == typeof( uint ) || type == typeof( long ) || type == typeof( ulong ) || type == typeof( float ) || type == typeof( double ) || type == typeof( bool )
            || type == typeof( Vector2 ) || type == typeof( Vector2Int ) || type == typeof( Vector3 ) || type == typeof( Vector3Int ) || type == typeof( Vector4 ) || type == typeof( Color )
            || type == typeof( LayerMask ) || type == typeof( Rect ) || type == typeof( RectInt ) || type == typeof( Bounds ) || type == typeof( BoundsInt ) || type == typeof( Gradient ) || type == typeof( AnimationCurve )
            || (type.IsSubclassOf( typeof( UnityEngine.Object ) ) || type == typeof( UnityEngine.Object ));

        /// <summary>
        /// Finds a field inside a serialized object
        /// </summary>
        /// <param name="fieldName">The name of the field to search</param>
        /// <param name="property">The serialized property</param>
        /// <returns>The field info of the desired field</returns>
        private static FieldInfo FindField( string fieldName, SerializedProperty property )
        {
            if ( fieldName.Contains( '.' ) )
                return GetStaticMemberInfoFromPath( fieldName, MemberTypes.Field ) as FieldInfo;

            var fieldInfo = FindFieldInfo( fieldName, property.serializedObject.targetObject );

            // If the field null we try to see if its inside a serialized object
            if ( fieldInfo == null )
            {
                var serializedObjectType = GetNestedObjectType( property, out _ );

                if ( serializedObjectType != null )
                    fieldInfo = serializedObjectType.GetField( fieldName, BINDING_FLAGS );
            }

            return fieldInfo;
        }

        internal static FieldInfo FindFieldInfo( string fieldName, object targetObject ) => FindMember( fieldName, targetObject?.GetType(), BINDING_FLAGS, MemberTypes.Field ) as FieldInfo;

        /// <summary>
        /// Finds a member from the target type
        /// </summary>
        /// <param name="memberName">The name of the member to look for</param>
        /// <param name="targetType">The type to get the member from</param>
        /// <param name="bindingFlags">The binding flags</param>
        /// <param name="memberType">The type of the member to look for. Only Field, Property and Method types are supported</param>
        /// <returns>The member info of the specified member type</returns>
        private static MemberInfo FindMember( string memberName, System.Type targetType, BindingFlags bindingFlags, MemberTypes memberType )
        {
            if ( string.IsNullOrEmpty( memberName ) || targetType == null )
                return null;

            // Always ensure we only search members declared on this type at each step
            bindingFlags |= BindingFlags.DeclaredOnly;

            while ( targetType != null )
            {
                MemberInfo result = memberType switch
                {
                    MemberTypes.Field    => targetType.GetField( memberName, bindingFlags ),
                    MemberTypes.Property => targetType.GetProperty( memberName, bindingFlags ),
                    MemberTypes.Method   => targetType.GetMethod( memberName, bindingFlags ),
                    _ => null
                };

                if ( result != null )
                    return result;

                targetType = targetType.BaseType;
            }

            return null;
        }


        /// <summary>
        /// Gets the type of a nested serialized object
        /// </summary>
        /// <param name="property">The serialized property</param>
        /// <param name="nestedObject">Outputs the serialized nested object</param>
        /// <returns>The nested object type</returns>
        private static System.Type GetNestedObjectType( SerializedProperty property, out object nestedObject )
        {
            try
            {
                nestedObject = property.serializedObject.targetObject;
                int cutPathIndex = property.propertyPath.LastIndexOf('.');

                if ( cutPathIndex == -1 ) // If the cutPathIndex is -1 it means that the member is not nested and we return null
                    return null;

                string path = property.propertyPath[..cutPathIndex].Replace(".Array.data[", "[");
                string[] elements = path.Split('.');

                foreach ( var element in elements )
                {
                    if ( element.Contains( "[" ) )
                    {
                        var elementName = element[..element.IndexOf("[")];
                        var index = System.Convert.ToInt32(element[element.IndexOf("[")..].Replace("[", "").Replace("]", ""));

                        nestedObject = GetValue( nestedObject, elementName, index );
                    }
                    else
                    {
                        nestedObject = GetValue( nestedObject, element );
                    }
                }

                return nestedObject?.GetType();
            }
            catch ( System.ObjectDisposedException )
            {
                nestedObject = null;
                return null;
            }
        }

        private static object GetValue( object source, string name, int index )
        {
            if ( GetValue( source, name ) is not IEnumerable enumerable )
                return null;

            var enumerator = enumerable.GetEnumerator();

            for ( int i = 0; i <= index; i++ )
            {
                if ( !enumerator.MoveNext() )
                    return null;
            }

            return enumerator.Current;
        }

        private static object GetValue( object source, string name )
        {
            if ( source == null )
                return null;

            var type = source.GetType();

            while ( type != null )
            {
                var field = FindMember(name, type, BINDING_FLAGS, MemberTypes.Field) as FieldInfo;

                if ( field != null )
                    return field.GetValue( source );

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Gets the info of a const or static member from the type specified in the path
        /// </summary>
        /// <param name="memberPath">The path on which to locate the member</param>
        /// <param name="memberTypes">The type of the member to look for. Only Field, Property and Method types are supported</param>
        /// <returns>The member info of the specified member type</returns>
        private static MemberInfo GetStaticMemberInfoFromPath( string memberPath, MemberTypes memberTypes )
        {
            MemberInfo memberInfo = null;

            string[] splitPath = memberPath.Split('.');

            string typeNamespace = GetNamespaceString(splitPath);
            string typeName = splitPath[^2];
            string actualFieldName = splitPath[^1];

            var matchingTypes = TypeCache.GetTypesDerivedFrom<object>().Where((type) => type.Name == typeName && type.Namespace == typeNamespace);

            foreach ( var type in matchingTypes )
            {
                memberInfo = FindMember( actualFieldName, type, BINDING_FLAGS ^ BindingFlags.Instance, memberTypes );

                if ( memberInfo == null )
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            return memberInfo;
        }

        private static string GetNamespaceString( string[] splitMemberPath )
        {
            var stringBuilder = new StringBuilder();

            string[] namespacePath = splitMemberPath[..^2];

            for ( int i = 0; i < namespacePath.Length; i++ )
            {
                stringBuilder.Append( namespacePath[ i ] );

                if ( i != namespacePath.Length - 1 )
                    stringBuilder.Append( '.' );
            }

            return stringBuilder.Length == 0 ? null : stringBuilder.ToString();
        }


        /// <summary>
        /// Gets the type of a member
        /// </summary>
        /// <param name="memberInfo">The member to get the type from</param>
        /// <returns>The type of the member</returns>
        private static System.Type GetMemberInfoType( MemberInfo memberInfo )
        {
            if ( memberInfo is FieldInfo fieldInfo )
            {
                return fieldInfo.FieldType;
            }
            else if ( memberInfo is System.Reflection.PropertyInfo propertyInfo )
            {
                return propertyInfo.PropertyType;
            }
            else if ( memberInfo is MethodInfo methodInfo )
            {
                return methodInfo.ReturnType;
            }

            return null;
        }
    }
}