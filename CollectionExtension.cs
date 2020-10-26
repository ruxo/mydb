using System.Collections.Generic;
using LanguageExt;
using static LanguageExt.Prelude;

namespace RZ.App
{
    public static class CollectionExtension
    {
        public static Option<T> TryPop<T>(this Stack<T> stack) => stack.Count > 0 ? Some(stack.Pop()) : None;
    }
}