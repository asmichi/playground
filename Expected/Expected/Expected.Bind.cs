using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expected
{
    public static partial class Expected
    {
        // TODO: Generate Compose variants through N values with T4 template.
        public static Expected<U, E> BulkBind<T1, T2, U, E>(
            Expected<T1, E> e1,
            Expected<T2, E> e2,
            Func<T1, T2, Expected<U, E>> f)
        {
            if (!e1.HasValue)
            {
                return Unexpected(e1.Error);
            }
            if (!e2.HasValue)
            {
                return Unexpected(e2.Error);
            }

            return f(e1.Value, e2.Value);
        }

        public static Expected<U, E> BulkBind<T1, T2, U, E>(
            Expected<T1, E> e1,
            Expected<T2, E> e2,
            Func<T1, T2, U> f)
        {
            if (!e1.HasValue)
            {
                return Unexpected(e1.Error);
            }
            if (!e2.HasValue)
            {
                return Unexpected(e2.Error);
            }

            return f(e1.Value, e2.Value);
        }
    }
}
