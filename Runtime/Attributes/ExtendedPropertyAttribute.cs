using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ExtendedInspector
{
    /// <summary>Customizes how a field is rendered in the inspector.</summary>
    [Conditional( "UNITY_EDITOR" )]
    [AttributeUsage( AttributeTargets.All, Inherited = true, AllowMultiple = false )]
    public abstract class ExtendedPropertyAttribute : PropertyAttribute
    {
        public readonly int lineNumer;
        
        public ExtendedPropertyAttribute( [CallerLineNumber] int lineNumer = 0 )
        {
            this.lineNumer = lineNumer;
        }
    }
}