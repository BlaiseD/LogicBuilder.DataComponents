﻿using System.Linq.Expressions;

namespace LogicBuilder.Expressions.Utils.ExpressionBuilder.DateTimeOperators
{
    public class HourOperator : IExpressionPart
    {
        public HourOperator(IExpressionPart operand)
        {
            Operand = operand;
        }

        public IExpressionPart Operand { get; private set; }

        public Expression Build() => Build(Operand.Build());

        private Expression Build(Expression operandExpression) 
            => operandExpression.MakeHourSelector();
    }
}
