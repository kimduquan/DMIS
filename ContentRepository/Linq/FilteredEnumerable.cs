using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentRepository.Storage;
using System.Linq.Expressions;

namespace ContentRepository.Linq
{
    public class FilteredEnumerable : IEnumerable<Node>
    {
        private IEnumerable _enumerable;
        private int _top;
        private int _skip;

        Func<Content, bool> _isOk;

        public int AllCount { get; private set; }

        public FilteredEnumerable(IEnumerable enumerable, LambdaExpression filterExpression, int top, int skip)
        {
            _enumerable = enumerable;
            _top = top;
            _skip = skip;

            var func = filterExpression.Compile();
            _isOk = func as Func<Content, bool>;
            if (_isOk == null)
                throw new InvalidOperationException("Invalid filterExpression (LambdaExpression): return value must be bool, parameter must be " + typeof(Node).FullName);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public IEnumerator<Node> GetEnumerator()
        {
            var skipped = 0;
            var count = 0;
            foreach (Node item in _enumerable)
            {
                AllCount++;
                if (skipped++ < _skip)
                    continue;
                if (_top == 0 || count++ < _top)
                    if (_isOk(Content.Create(item)))
                        yield return item;
            }
        }
    }
}
