﻿using System;
using System.Linq.Expressions;

namespace LogicBuilder.Expressions.Utils.ExpressionBuilder
{
    public class ConvertToEnumOperator : IExpressionPart
    {
        public ConvertToEnumOperator(object constantValue, Type type)
        {
            Type = type;
            ConstantValue = constantValue;
        }

        public Type Type { get; }
        public object ConstantValue { get; }

        public Expression Build() => DoBuild();

        private Expression DoBuild()
        {
            if (ConstantValue == null)
                return Expression.Constant(null);

            return ConstantValue.ToString().TryParseEnum(Type, out object enumValue) 
                ? Expression.Constant(enumValue, Type) 
                : Expression.Constant(null);
        }
    }
}
