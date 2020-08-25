﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LogicBuilder.Expressions.Utils.FilterBuilder.Operand
{
    public class CollectionConstantOperand : FilterPart
    {
        public CollectionConstantOperand(IDictionary<string, ParameterExpression> parameters, Type elementType, ICollection<object> constantValues) : base(parameters)
        {
            ElementType = elementType;
            ConstantValues = constantValues;
        }

        public Type ElementType { get; }
        public ICollection<object> ConstantValues { get; }

        public override Expression Build()
        {
            Type listType = typeof(List<>).MakeGenericType(ElementType);
            IList items = Activator.CreateInstance(listType) as IList;

            ConstantValues.Aggregate(items, (list, next) =>
            {
                list.Add(next);
                return list;
            });


            return Expression.Constant(items, listType);
        }
    }
}
