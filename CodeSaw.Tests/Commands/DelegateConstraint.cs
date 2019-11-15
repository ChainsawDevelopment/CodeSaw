using System;
using NUnit.Framework.Constraints;

namespace CodeSaw.Tests.Commands
{
    class DelegateConstraint<T> : Constraint
    {
        private readonly Func<T, bool> _predicate;

        public DelegateConstraint(Func<T, bool> predicate)
        {
            _predicate = predicate;
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (typeof(TActual) != typeof(T))
            {
                return new ConstraintResult(this, actual, ConstraintStatus.Failure);
            }

            var isOk = _predicate((T) (object) actual);

            return new ConstraintResult(this, actual, isOk);
        }
    }
}