﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LogicBuilder.Expressions.Utils.ExpressionBuilder.Lambda
{
    public abstract class SelectorMethodOperatorBase
    {
        public SelectorMethodOperatorBase(IDictionary<string, ParameterExpression> parameters, IExpressionPart sourceOperand, IExpressionPart selectorBody, string selectorParameterName)
        {
            SourceOperand = sourceOperand;
            SelectorBody = selectorBody;
            SelectorParameterName = selectorParameterName;
            Parameters = parameters;
        }

        public SelectorMethodOperatorBase(IExpressionPart sourceOperand)
        {
            SourceOperand = sourceOperand;
        }

        public IExpressionPart SourceOperand { get; }
        public IExpressionPart SelectorBody { get; }
        public string SelectorParameterName { get; }
        public IDictionary<string, ParameterExpression> Parameters { get; }

        public Expression Build() => Build(SourceOperand.Build());

        protected abstract Expression Build(Expression operandExpression);

        protected Expression[] GetParameters(Expression operandExpression)
        {
            if (SelectorBody == null)
                return new Expression[0];

            return new Expression[]
            {
                GetLambdaOperator(operandExpression.GetUnderlyingElementType()).Build()
            };
        }

        protected LambdaExpression GetSelector(Expression operandExpression)
            => (LambdaExpression)GetLambdaOperator(operandExpression.GetUnderlyingElementType()).Build();

        protected virtual IExpressionPart GetLambdaOperator(Type elementType)
            => new SelectorLambdaOperator
            (
                Parameters,
                SelectorBody,
                elementType,
                SelectorParameterName
            );
    }
}
