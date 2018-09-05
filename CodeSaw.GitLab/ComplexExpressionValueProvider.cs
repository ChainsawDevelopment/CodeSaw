using System;
using Newtonsoft.Json.Serialization;

namespace CodeSaw.GitLab
{
    public class ComplexExpressionValueProvider<TObject, TValue> :IValueProvider
    {
        private readonly Action<TObject, TValue> _set;

        public ComplexExpressionValueProvider(Action<TObject, TValue> set)
        {
            _set = set;
        }

        public void SetValue(object target, object value)
        {
            _set((TObject) target, (TValue) value);
        }

        public object GetValue(object target)
        {
            throw new System.NotImplementedException();
        }
    }
}