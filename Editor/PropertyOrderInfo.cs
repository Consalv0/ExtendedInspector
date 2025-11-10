using System.Collections.Generic;

namespace ExtendedInspector.Editor
{
    public readonly struct PropertyOrderInfo
    {
        public readonly int token;
        public readonly int order;

        public PropertyOrderInfo( int token, int order )
        {
            this.token = token;
            this.order = order;
        }
    }

    public class PropertyOrderComparer : IComparer<PropertyOrderInfo>
    {
        public int Compare( PropertyOrderInfo a, PropertyOrderInfo b )
        {
            if ( a.order == b.order )
            {
                return a.token.CompareTo( b.token );
            }
            else
            {
                return a.order.CompareTo( b.order );
            }
        }
    }
}