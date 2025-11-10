using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ExtendedInspector.Editor
{
    public static class SerializedObjectUtils
    {
        private static readonly Regex ArrayIndexCapturePattern = new Regex(@"\[(\d+)\]", RegexOptions.Compiled);

        public static FieldInfo GetFieldViaPath( this System.Type type, string path )
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            System.Type parent = type;
            FieldInfo fi = parent.GetField(path, flags);
            string[] paths = path.Split('.');

            for ( int i = 0; i < paths.Length; i++ )
            {
                fi = parent.GetField( paths[ i ], flags );
                if ( fi != null )
                {
                    // there are only two container field type that can be serialized:
                    // Array and List<T>
                    if ( fi.FieldType.IsArray )
                    {
                        parent = fi.FieldType.GetElementType();
                        i += 2;
                        continue;
                    }
                    if ( fi.FieldType.IsGenericType )
                    {
                        parent = fi.FieldType.GetGenericArguments()[ 0 ];
                        i += 2;
                        continue;
                    }
                    parent = fi.FieldType;
                }
                else
                {
                    break;
                }
            }
            if ( fi == null )
            {
                if ( type.BaseType != null )
                {
                    return GetFieldViaPath( type.BaseType, path );
                }
                else
                {
                    return null;
                }
            }
            return fi;
        }

        /// <summary> Utility method for GetTarget </summary>
        private static object GetField( object target, string name, System.Type targetType = null )
        {
            if ( target == null )
                return null;

            if ( targetType == null )
                targetType = target.GetType();

            FieldInfo fi = null;
            while ( targetType != null )
            {
                fi = targetType.GetField( name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                if ( fi != null )
                    return fi.GetValue( target );

                targetType = targetType.BaseType;
            }

            return null;
        }

        /// <summary> Utility method for GetTarget </summary>
        private static int ParseArrayIndex( string propertyName )
        {
            var match = ArrayIndexCapturePattern.Match(propertyName);
            if ( !match.Success )
                throw new System.Exception( $"Invalid array index parsing in {propertyName}" );

            return int.Parse( match.Groups[ 1 ].Value );
        }

        /// <summary> Returns the field info of a target object based on the path </summary>
        private static FieldInfo GetFieldNested( object target, string path )
        {
            var fields = path.Split('.');
            var isNextPropertyArrayIndex = false;

            for ( int i = 0; i < fields.Length - 1; ++i )
            {
                var propName = fields[i];
                if ( propName == "Array" )
                {
                    isNextPropertyArrayIndex = true;
                }
                else if ( isNextPropertyArrayIndex )
                {
                    isNextPropertyArrayIndex = false;
                    var index = ParseArrayIndex(propName);
                    var targetAsList = target as IList;
                    if ( targetAsList != null && targetAsList.Count > index )
                        target = targetAsList[ index ];
                }
                else
                    target = GetField( target, propName );
            }

            FieldInfo fieldInfo = null;
            if ( target != null )
            {
                System.Type targetType = target.GetType();
                while ( targetType != null )
                {
                    fieldInfo = targetType.GetField( fields[ ^1 ], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                    if ( fieldInfo != null )
                        return fieldInfo;

                    targetType = targetType.BaseType;
                }
            }

            return fieldInfo;
        }

        /// <summary> Returns a serialized property in the same scope </summary>
        public static SerializedProperty FindParentProperty( this SerializedProperty property )
        {
            var path = property.propertyPath.Split('.');
            System.Array.Resize( ref path, path.Length - 1 );
            var newPath = string.Join('.', path);
            return property.serializedObject.FindProperty( newPath );
        }

        /// <summary> Returns an array of any <see cref="NewfangledAttribute"/> found in the property. Otherwise returns null. </summary>
        public static PropertyAttribute[] GetCustomAttributes( this SerializedProperty property )
        {
            // Arrays actually have a sibling "size". Their parent, is the actual property I will need to search in GetFieldNested.
            if ( property.name == "Array" )
                property = property.FindParentProperty();

            FieldInfo fieldInfo = GetFieldNested(property.serializedObject.targetObject, property.propertyPath);

            if ( fieldInfo != null )
            {
                PropertyAttribute[] attributes = (PropertyAttribute[])fieldInfo.GetCustomAttributes(typeof(PropertyAttribute), true);
                return attributes;
            }

            return null;
        }

        /// <summary>Gets visible children of a <see cref="SerializedProperty"/> at 1 level depth.</summary>
        public static List<SerializedProperty> GetVisibleChildren( this SerializedProperty propertyIterator )
        {
            SerializedProperty property = propertyIterator.Copy();

            List<SerializedProperty> visibleChildren = new();

            if ( property.NextVisible( true ) )
            {
                // If depth is same or bigger, iterator had no children.
                if ( property.depth <= propertyIterator.depth )
                    return visibleChildren;

                do
                {
                    visibleChildren.Add( property.Copy() );
                }
                while ( property.NextVisible( false ) && property.depth > propertyIterator.depth );
            }

            return visibleChildren;
        }

        /// <summary>
        /// Sort the properties based on the SortAttribute order.
        /// </summary>
        public static List<SerializedProperty> SortProperties( this List<SerializedProperty> properties )
        {
            Dictionary<SerializedProperty, int> sortOrderCache = new();
            bool needsSorting = false;

            foreach ( var property in properties )
            {
                if ( property.name == "m_Script" )
                {
                    sortOrderCache[ property ] = int.MinValue;
                }
                else
                {
                    PropertyAttribute[] attributes = property.GetCustomAttributes();
                    PropertyAttribute sortAttribute = attributes?.FirstOrDefault(attr => attr is PropertyAttribute);
                    sortOrderCache[ property ] = sortAttribute?.order ?? 0;
                    needsSorting = true;
                }
            }

            return needsSorting ? properties.OrderBy( p => sortOrderCache[ p ] ).ToList() : properties;
        }

        public static object GetValue( this SerializedProperty property )
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data", "");
            string[] fieldStructure = path.Split('.');
            for ( int i = 0; i < fieldStructure.Length; i++ )
            {
                if ( fieldStructure[ i ].Contains( "[" ) )
                {
                    int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
                    obj = GetFieldValueWithIndex( ArrayIndexCapturePattern.Replace( fieldStructure[ i ], "" ), obj, index );
                }
                else
                {
                    obj = GetFieldValue( fieldStructure[ i ], obj );
                }
            }
            return obj;
        }

        public static T GetValue<T>( this SerializedProperty property ) where T : class
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data", "");
            string[] fieldStructure = path.Split('.');
            for ( int i = 0; i < fieldStructure.Length; i++ )
            {
                if ( fieldStructure[ i ].Contains( "[" ) )
                {
                    int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
                    obj = GetFieldValueWithIndex( ArrayIndexCapturePattern.Replace( fieldStructure[ i ], "" ), obj, index );
                }
                else
                {
                    obj = GetFieldValue( fieldStructure[ i ], obj );
                }
            }
            return (T)obj;
        }

        public static void SetValue( this SerializedProperty property, object value )
        {
            switch( property.propertyType )
            {
                case SerializedPropertyType.Generic:
                {
                    property.boxedValue = value;
                } break;
                case SerializedPropertyType.Integer:
                {
                    switch ( property.type )
                    {
                        case "int": property.intValue = value == null ? default : (int)value; break;
                        case "uint": property.uintValue = value == null ? default : (uint)value; break;
                        case "long": property.longValue = value == null ? default : (long)value; break;
                        case "ulong": property.ulongValue = value == null ? default : (ulong)value; break;
                        default:
                            break;
                    }
                } break;
                case SerializedPropertyType.Boolean:
                {
                    property.boolValue = value == null ? default : (bool)value;
                } break;
                case SerializedPropertyType.Float:
                {
                    switch ( property.type )
                    {
                        case "float": property.floatValue = value == null ? default : (float)value; break;
                        case "double": property.doubleValue = value == null ? default : (double)value; break;
                        default:
                            break;
                    }
                } break;
                case SerializedPropertyType.String:
                {
                    property.stringValue = value as string;
                } break;
                case SerializedPropertyType.Color:
                {
                    property.vector4Value = value == null ? default : (Color)value;
                } break;
                case SerializedPropertyType.ObjectReference:
                {
                    property.objectReferenceValue = value as UnityEngine.Object;
                } break;
                case SerializedPropertyType.LayerMask:
                {
                    property.intValue = value == null ? default : (LayerMask)value;
                } break;
                case SerializedPropertyType.Enum: {
                        property.enumValueIndex = value == null ? default : (int)value;
                } break;
                case SerializedPropertyType.Vector2:
                {
                    property.vector2Value = value == null ? default : (Vector2)value;
                } break;
                case SerializedPropertyType.Vector3:
                {
                    property.vector3Value = value == null ? default : (Vector3)value;
                } break;
                case SerializedPropertyType.Vector4:
                {
                    property.vector4Value = value == null ? default : (Vector4)value;
                } break;
                case SerializedPropertyType.Rect:
                {
                    property.rectValue = value == null ? default : (Rect)value;
                } break;
                case SerializedPropertyType.ArraySize:
                {
                    property.arraySize = value == null ? default : (int)value;
                } break;
                case SerializedPropertyType.Character:
                {
                    property.boxedValue = value;
                } break;
                case SerializedPropertyType.AnimationCurve:
                {
                    property.animationCurveValue = value as AnimationCurve;
                } break;
                case SerializedPropertyType.Bounds:
                {
                    property.boundsValue = value == null ? default : (Bounds)value;
                } break;
                case SerializedPropertyType.Gradient:
                {
                    property.gradientValue = value as Gradient;
                } break;
                case SerializedPropertyType.Quaternion:
                {
                    property.quaternionValue = value == null ? default : (Quaternion)value;
                } break;
                case SerializedPropertyType.ExposedReference:
                {
                    property.exposedReferenceValue = value as UnityEngine.Object;
                } break;
                case SerializedPropertyType.FixedBufferSize: {
                } break;
                case SerializedPropertyType.Vector2Int:
                {
                    property.vector2IntValue = value == null ? default : (Vector2Int)value;
                } break;
                case SerializedPropertyType.Vector3Int:
                {
                    property.vector3IntValue = value == null ? default : (Vector3Int)value;
                } break;
                case SerializedPropertyType.RectInt:
                {
                    property.rectIntValue = value == null ? default : (RectInt)value;
                } break;
                case SerializedPropertyType.BoundsInt:
                {
                    property.boundsIntValue = value == null ? default : (BoundsInt)value;
                } break;
                case SerializedPropertyType.ManagedReference:
                {
                    property.managedReferenceValue = value;
                } break;
                case SerializedPropertyType.Hash128:
                {
                    property.hash128Value = value == null ? default : (Hash128)value;
                } break;
                case SerializedPropertyType.RenderingLayerMask:
                {
                    property.intValue = value == null ? default : (RenderingLayerMask)value;
                } break;
            }
        }

        public static bool SetValueToTargetMember<T>( this SerializedProperty property, T value ) where T : class
        {
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data", "");
            string[] fieldStructure = path.Split('.');
            for ( int i = 0; i < fieldStructure.Length - 1; i++ )
            {
                if ( fieldStructure[ i ].Contains( "[" ) )
                {
                    int index = System.Convert.ToInt32(new string(fieldStructure[i].Where(c => char.IsDigit(c)).ToArray()));
                    obj = GetFieldValueWithIndex( ArrayIndexCapturePattern.Replace( fieldStructure[ i ], "" ), obj, index );
                }
                else
                {
                    obj = GetFieldValue( fieldStructure[ i ], obj );
                }
            }

            string fieldName = fieldStructure.Last();
            if ( fieldName.Contains( "[" ) )
            {
                int index = System.Convert.ToInt32(new string(fieldName.Where(c => char.IsDigit(c)).ToArray()));
                return SetFieldValueWithIndex( ArrayIndexCapturePattern.Replace( fieldName, "" ), obj, index, value );
            }
            else
            {
                return SetFieldValue( fieldName, obj, value );
            }
        }

        private static object GetFieldValue( string fieldName, object obj, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null )
            {
                return field.GetValue( obj );
            }
            return default( object );
        }

        private static object GetFieldValueWithIndex( string fieldName, object obj, int index, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null )
            {
                object list = field.GetValue(obj);
                if ( list.GetType().IsArray )
                {
                    return ((object[])list)[ index ];
                }
                else if ( list is IEnumerable )
                {
                    return ((IList)list)[ index ];
                }
            }
            return default( object );
        }

        public static bool SetFieldValue( string fieldName, object obj, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null )
            {
                field.SetValue( obj, value );
                return true;
            }
            return false;
        }

        public static bool SetFieldValueWithIndex( string fieldName, object obj, int index, object value, bool includeAllBases = false, BindingFlags bindings = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
        {
            FieldInfo field = obj.GetType().GetField(fieldName, bindings);
            if ( field != null )
            {
                object list = field.GetValue(obj);
                if ( list.GetType().IsArray )
                {
                    ((object[])list)[ index ] = value;
                    return true;
                }
                else if ( list is IEnumerable )
                {
                    ((IList)list)[ index ] = value;
                    return true;
                }
            }
            return false;
        }
    }
}