using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expected
{
    enum ErrorCode
    {
        DivideByZero = 1,
    }

    class Program
    {
        static Expected<int, ErrorCode> SafeDivide(int x, int y)
        {
            if (y == 0)
            {
                return Expected.Unexpected(ErrorCode.DivideByZero);
            }
            else
            {
                return x / y;
            }
        }

        // Foo x y z = do e1 <- SafeDivide(x, d)
        //                e2 <- SafeDivide(y, d)
        //                e1 + e2
        static Expected<int, ErrorCode> Foo(int x, int y, int d)
        {
            return SafeDivide(x, d).Bind(e1
                => SafeDivide(y, d).Bind(e2
                // This lambda function captures s1 and is allocated in the heap on every invocation.
                => e1 + e2));
        }

        static Expected<int, ErrorCode> Foo2(int x, int y, int d)
        {
            // Hmm...
            var e1 = SafeDivide(x, d);
            if (!e1.HasValue)
            {
                return e1;
            }

            var e2 = SafeDivide(y, d);
            if (!e2.HasValue)
            {
                return e2;
            }

            return e1.Value + e2.Value;
        }

        static Expected<int, ErrorCode> Foo3(int x, int y, int d)
        {
            // This lambda function below captures nothing and is allocated only once.
            // Note SafeDivide(y, d) will be evaluated even if SafeDivide(x, d) returns error.
            return Expected.BulkBind(
                SafeDivide(x, d),
                SafeDivide(y, d),
                (e1, e2) => e1 + e2);
        }

        static void Main(string[] args)
        {
            var es = new[] {
                Foo(2, 4, 2),
                Foo(2, 4, 0),
            };

            foreach (var e in es)
            {
                e.Match(
                    value: (v) => Console.WriteLine(v),
                    error: (u) => Console.WriteLine(u));
            }
        }
    }
}
