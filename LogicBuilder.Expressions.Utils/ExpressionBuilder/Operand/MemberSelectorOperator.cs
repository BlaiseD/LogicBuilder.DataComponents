﻿using System.Linq.Expressions;

namespace LogicBuilder.Expressions.Utils.ExpressionBuilder.Operand
{
    public class MemberSelectorOperator : IExpressionPart
    {
        public MemberSelectorOperator(string memberFullName, IExpressionPart sourceOperand)
        {
            MemberFullName = memberFullName;
            SourceOperand = sourceOperand;
        }

        public string MemberFullName { get; set; }
        public IExpressionPart SourceOperand { get; set; }

        public Expression Build() 
            => SourceOperand.Build().BuildSelectorExpression(MemberFullName);
    }
}
