using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtendedInspector
{
    /// <summary>Customizes how a field is rendered in the inspector.</summary>
    [Conditional( "UNITY_EDITOR" )]
    [AttributeUsage( AttributeTargets.All, Inherited = true, AllowMultiple = false )]
    public abstract class PropertyAttribute : UnityEngine.PropertyAttribute
    {
        public readonly int lineNumer;
        
        public PropertyAttribute( [CallerLineNumber] int lineNumer = 0 )
        {
            this.lineNumer = lineNumer;
        }
    }
}