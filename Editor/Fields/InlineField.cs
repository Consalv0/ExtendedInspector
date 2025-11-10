using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ExtendedInspector.Editor
{
    public class InlineField : VisualElement
    {
        internal VisualElement m_PropertyField;
        private SerializedProperty m_Property;
        private string m_PropertyPath;
        private UnityEngine.Object m_FieldValue;
        private bool m_StartValue;
        public string label;

        private UnityEditor.Editor m_Editor;
        private VisualElement m_EditorVisualElement;

        private VisualElement m_LabelElement;
        private Toggle m_Toggle;
        private Foldout m_Foldout;
        private float m_LabelWidthRatio;
        private float m_LabelExtraPadding;
        private float m_LabelBaseMinWidth;

        private VisualElement m_CachedContextWidthElement;
        private VisualElement m_CachedInspectorElement;

        public InlineField( )
        {
        }

        public InlineField( string label, VisualElement propertyField, SerializedProperty property, bool expanded )
        {
            this.m_FieldValue = property.objectReferenceValue;
            this.m_PropertyField = propertyField;
            this.m_PropertyPath = $"{property.serializedObject.targetObject.GetType()}.{property.propertyPath}";
            this.label = label;
            this.m_Property = property;
            this.m_StartValue = expanded;

            CreateGUI();
        }

        public InlineField( string label, VisualElement propertyField, UnityEngine.Object fieldValue, SerializedProperty property, bool expanded )
        {
            this.m_FieldValue = fieldValue;
            this.m_Property = property;
            this.m_PropertyField = propertyField;
            this.m_PropertyPath = $"{label}";
            this.label = label;
            this.m_StartValue = expanded;

            CreateGUI();
        }

        private void CreateGUI( )
        {
            this.name = "unity-list-feetTransforms";
            this.AddToClassList( BaseVerticalCollectionView.ussClassName );
            this.AddToClassList( BaseListView.ussClassName );
            this.AddToClassList( BaseListView.listViewWithHeaderUssClassName );

            m_Foldout = new Foldout();
            m_Foldout.RegisterValueChangedCallback( OnFoldChange );
            m_Foldout.value = m_StartValue;
            m_Foldout.bindingPath = m_PropertyPath;
            m_Foldout.AddToClassList( BaseListView.foldoutHeaderUssClassName );
            if ( m_Property != null )
            {
                m_Foldout.userData = m_Property.objectReferenceValue;
            }
            m_Foldout.text = label;
            m_Toggle = m_Foldout.hierarchy.ElementAt( 0 ) as Toggle;
            m_LabelElement = m_Toggle.ElementAt( 0 );
            m_LabelElement.style.flexBasis = 0;
            m_LabelElement.style.flexGrow = 0;
            VisualElement container = m_Foldout.contentContainer;
            container.style.paddingLeft = 15F;
            container.style.marginLeft = -7F;
            container.style.borderLeftWidth = 1;
            container.style.borderLeftColor = Color.gray3;
            m_Foldout.hierarchy.RemoveAt( 1 );
            m_Foldout.hierarchy.RemoveAt( 0 );

            m_Toggle.style.flexDirection = FlexDirection.Row;
            m_Toggle.style.borderTopLeftRadius = 3;
            m_Toggle.style.borderTopRightRadius = 3;
            m_Toggle.style.marginLeft = -12;

            if ( m_PropertyField is PropertyField pf )
            {
                pf.label = string.Empty;
                pf.RegisterValueChangeCallback( value =>
                {
                    m_Property = value.changedProperty;
                    m_FieldValue = value.changedProperty.objectReferenceValue;
                    UpdateEditorIn();
                } );
            }
            if ( m_PropertyField is ObjectField of )
            {
                of.label = string.Empty;
                of.RegisterValueChangedCallback( value =>
                {
                    m_FieldValue = value.newValue;
                    UpdateEditorIn();
                } );
            }

            m_PropertyField.style.flexGrow = 1;
            m_Toggle.Add( m_PropertyField );
            m_Foldout.hierarchy.Add( m_Toggle );
            m_Foldout.hierarchy.Add( container );
            UpdateEditorIn();
            this.Add( m_Foldout );

            RegisterCallback<AttachToPanelEvent>( OnAttachToPanel );
            RegisterCallback<DetachFromPanelEvent>( OnDetachFromPanel );
        }

        private void OnCustomStyleResolved( CustomStyleResolvedEvent evt )
        {
            AlignLabel();
        }

        private void OnFoldChange( ChangeEvent<bool> value )
        {
            if ( value.target == m_Foldout )
            {
                UpdateEditorIn();
            }
        }

        private void UpdateEditorIn()
        {
            if ( m_EditorVisualElement != null )
            {
                m_Foldout.Remove( m_EditorVisualElement );
                m_EditorVisualElement = null;
            }
            Object.DestroyImmediate( m_Editor );

            if ( m_Foldout.value == false )
            {
                return;
            }
            m_Editor = UnityEditor.Editor.CreateEditor( m_FieldValue );
            if ( m_Editor != null )
            {
                m_EditorVisualElement = m_Editor.CreateInspectorGUI();
            }
            else
            {
                m_EditorVisualElement = new VisualElement();
            }
            m_Foldout.Add( m_EditorVisualElement );
        }

        private void OnAttachToPanel( AttachToPanelEvent e )
        {
            if ( e.destinationPanel == null || e.destinationPanel.contextType == ContextType.Player )
            {
                return;
            }

            m_CachedInspectorElement = null;
            m_CachedContextWidthElement = null;
            for ( VisualElement visualElement = base.parent; visualElement != null; visualElement = visualElement.parent )
            {
                if ( visualElement.ClassListContains( InspectorElement.ussClassName ) )
                {
                    m_CachedInspectorElement = visualElement;
                }

                if ( visualElement.ClassListContains( "unity-inspector-main-container" ) )
                {
                    m_CachedContextWidthElement = visualElement;
                    break;
                }
            }

            m_LabelWidthRatio = 0.45f;
            m_LabelExtraPadding = 37f;
            m_LabelBaseMinWidth = 123f;

            RegisterCallback<CustomStyleResolvedEvent>( OnCustomStyleResolved );
            RegisterCallback<GeometryChangedEvent>( OnInspectorFieldGeometryChanged );
        }

        private void OnDetachFromPanel( DetachFromPanelEvent e )
        {
        }

        private void OnInspectorFieldGeometryChanged( GeometryChangedEvent e )
        {
            AlignLabel();
        }

        private void AlignLabel( )
        {
            if ( m_CachedInspectorElement != null )
            {
                float labelExtraPadding = m_LabelExtraPadding;
                float num = base.worldBound.x - m_CachedInspectorElement.worldBound.x - m_CachedInspectorElement.resolvedStyle.paddingLeft;
                labelExtraPadding += num;
                labelExtraPadding += base.resolvedStyle.paddingLeft;
                labelExtraPadding -= 10;
                float a = m_LabelBaseMinWidth - num - base.resolvedStyle.paddingLeft;
                VisualElement visualElement = m_CachedContextWidthElement ?? m_CachedInspectorElement;
                m_LabelElement.style.minWidth = Mathf.Max( a, 0f );
                float num2 = Mathf.Ceil(visualElement.resolvedStyle.width * m_LabelWidthRatio) - labelExtraPadding;
                if ( Mathf.Abs( m_LabelElement.resolvedStyle.width - num2 ) > 1E-30f )
                {
                    m_LabelElement.style.flexBasis = Mathf.Max( 0f, num2 );
                }
            }
        }
    }
}