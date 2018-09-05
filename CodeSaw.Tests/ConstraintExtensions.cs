using NUnit.Framework.Constraints;

namespace CodeSaw.Tests
{
    public static class ConstraintExtensions
    {
        public static Constraint WithMessage(this IConstraint constraint, string message)
        {
            return new MessageConstraint(constraint.Builder.Resolve(), message);
        }

        private class MessageConstraint:Constraint
        {
            private readonly IConstraint _inner;

            public MessageConstraint(IConstraint inner, string message)
            {
                _inner = inner;
                Description = message;
            }

            public override ConstraintResult ApplyTo<TActual>(TActual actual)
            {
                var constraintResult = _inner.ApplyTo(actual);
                return constraintResult;
            }
        }
    }
}