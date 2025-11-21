using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryBuilder
{

    public class JoinsType
    {
        public Type InnerType { get; set; } = default!;
        public Type OuterType { get; set; } = default!;
        public string OuterKey { get; set; } = string.Empty;
        public string InnerKey { get; set; } = string.Empty;
        public string JoinType { get; set; } = "Inner";
    }

    public class TransparentIdentifier<TOuter, TInner>
    {
        public TOuter Outer { get; set; }
        public TInner Inner { get; set; }

        public TransparentIdentifier(TOuter outer, TInner inner)
        {
            Outer = outer;
            Inner = inner;
        }
    }

    public class FilterCondition
    {
        public string PropertyName { get; set; } = "";
        public string Operator { get; set; } = "";
        public object Value { get; set; } = new object();
        public bool UseAnd { get; set; } = true;
        public Type TableType { get; set; } = default!;
    }


}
