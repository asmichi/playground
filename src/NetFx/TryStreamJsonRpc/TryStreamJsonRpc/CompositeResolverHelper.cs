// Copyright 2018 @asmichi (on github). Licensed under the MIT License. See LICENCE in the project root for details.

using MessagePack;
using MessagePack.Formatters;

namespace TryStreamJsonRpc
{
    internal sealed class CompositeResolverHelper
    {
        public static IMessagePackFormatter<T> GetFormatter<T>(IFormatterResolver[] resolvers)
        {
            foreach (var item in resolvers)
            {
                var f = item.GetFormatter<T>();
                if (f != null)
                {
                    return f;
                }
            }

            return null;
        }
    }
}
