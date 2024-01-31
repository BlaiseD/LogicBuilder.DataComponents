using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LogicBuilder.Expressions.Utils.Expansions
{
    public static class ExpansionsHelper
    {
        public static IEnumerable<Expression<Func<TSource, object>>> GetExpansionSelectors<TSource>(this SelectExpandDefinition selectExpandDefinition) where TSource : class 
            => selectExpandDefinition.GetExpansions
            (
                typeof(TSource)
            )
            .Select(list => new List<Expansion>(list))
            .BuildIncludes<TSource>
            (
                selectExpandDefinition?.Selects ?? new List<string>()
            );

        public static List<List<ExpansionOptions>> GetExpansions(this SelectExpandDefinition selectExpandDefinition, Type sourceType)
        {
            if (selectExpandDefinition == null)
                return new List<List<ExpansionOptions>>();

            return selectExpandDefinition.ExpandedItems.GetExpansions
            (
                new HashSet<string>
                (
                    selectExpandDefinition.Selects ?? new List<string>(), 
                    new SelectsEqualityComparer()
                ), 
                sourceType
            );
        }

        private static List<List<ExpansionOptions>> GetExpansions(this IEnumerable<SelectExpandItem> selectExpandItems, HashSet<string> selects, Type sourceType)
        {
            if (selectExpandItems == null)
                return new List<List<ExpansionOptions>>();

            return selectExpandItems.Aggregate(new List<List<ExpansionOptions>>(), (listOfExpansionLists, next) =>
            {
                if (!selects.ExpansionIsValid(next.MemberName))
                    return listOfExpansionLists;

                Type currentParentType = sourceType.GetCurrentType();
                Type memberType = currentParentType.GetMemberInfo(next.MemberName).GetMemberType();
                Type elementType = memberType.GetCurrentType();

                ExpansionOptions expansionOption = new ExpansionOptions
                {
                    MemberType = memberType,
                    ParentType = currentParentType,
                    MemberName = next.MemberName,
                    FilterOption = GetFilter(),
                    QueryOption = GetQuery(),
                    Selects = next.Selects
                };

                List<List<ExpansionOptions>> navigationItems = next.ExpandedItems == null
                    ? new List<List<ExpansionOptions>>()
                    : next.ExpandedItems.GetExpansions
                    (
                        new HashSet<string>
                        (
                            next.Selects ?? new List<string>(), 
                            new SelectsEqualityComparer()
                        ), 
                        elementType
                    )
                    .Select
                    (
                        expansions =>
                        {
                            expansions.Insert(0, expansionOption);
                            return expansions;
                        }
                    ).ToList();

                if (navigationItems.Any())
                    listOfExpansionLists.AddRange(navigationItems);
                else
                    listOfExpansionLists.Add(new List<ExpansionOptions> { expansionOption });

                return listOfExpansionLists;

                ExpansionFilterOption GetFilter()
                    => HasFilter()
                        ? new ExpansionFilterOption 
                        { 
                            FilterLambdaOperator = next.Filter.FilterLambdaOperator
                        }
                        : null;

                ExpansionQueryOption GetQuery()
                    => HasQuery()
                        ? new ExpansionQueryOption
                        {
                            SortCollection = next.QueryFunction.SortCollection
                        }
                        : null;

                bool HasFilter()
                    => memberType.IsList() && next?.Filter?.FilterLambdaOperator != null;

                bool HasQuery()
                    => memberType.IsList() && next?.QueryFunction?.SortCollection != null;
            });
        }

        private static bool ExpansionIsValid(this HashSet<string> siblingSelects, string expansion)
        {
            if (siblingSelects == null || !siblingSelects.Any())
                return true;

            return siblingSelects.Contains(expansion);
        }

        public static ICollection<Expression<Func<TSource, object>>> BuildIncludes<TSource>(this IEnumerable<List<Expansion>> includes, List<string> selects)
            where TSource : class
        {
            return GetAllExpansions(new List<LambdaExpression>());

            List<Expression<Func<TSource, object>>> GetAllExpansions(List<LambdaExpression> valueMemberSelectors)
            {
                string parameterName = "i";
                ParameterExpression param = Expression.Parameter(typeof(TSource), parameterName);

                valueMemberSelectors.AddSelectors(selects, param, param);

                return includes
                    .Select(include => BuildSelectorExpression<TSource>(include, valueMemberSelectors, parameterName))
                    .Concat(valueMemberSelectors.Select(selector => (Expression<Func<TSource, object>>)selector))
                    .ToList();
            }
        }

        private static Expression<Func<TSource, object>> BuildSelectorExpression<TSource>(List<Expansion> fullName, List<LambdaExpression> valueMemberSelectors, string parameterName = "i")
        {
            ParameterExpression param = Expression.Parameter(typeof(TSource), parameterName);

            return (Expression<Func<TSource, object>>)Expression.Lambda
            (
                typeof(Func<,>).MakeGenericType(new[] { param.Type, typeof(object) }),
                BuildSelectorExpression(param, fullName, valueMemberSelectors, parameterName),
                param
            );
        }

        //e.g. /opstenant?$top=5&$expand=Buildings($expand=Builder($expand=City))
        private static Expression BuildSelectorExpression(Expression sourceExpression, List<Expansion> parts, List<LambdaExpression> valueMemberSelectors, string parameterName = "i")
        {
            Expression parent = sourceExpression;

            //Arguments to create a nested expression when the parent expansion is a collection
            //See AddChildSeelctors() below
            List<LambdaExpression> childValueMemberSelectors = new List<LambdaExpression>();

            for (int i = 0; i < parts.Count; i++)
            {
                if (parent.Type.IsList())
                {
                    Expression selectExpression = GetSelectExpression
                    (
                        parts.Skip(i),
                        parent,
                        childValueMemberSelectors,
                        parameterName
                    );

                    AddChildSeelctors();

                    return selectExpression;
                }
                else
                {
                    if (parent.Type != parts[i].ParentType)
                    {
                        //Handle cast in $expand
                        //e.g. /opstenant?$expand=Buildings($expand=Domain.OData.DerivedBuilding/DerivedBuildingProperty)
                        parent = Expression.Convert(parent, parts[i].ParentType);
                    }

                    parent = Expression.MakeMemberAccess(parent, parent.Type.GetMemberInfo(parts[i].MemberName));

                    if (parent.Type.IsList())
                    {
                        ParameterExpression childParam = Expression.Parameter(parent.GetUnderlyingElementType(), parameterName.ChildParameterName());
                        //selectors from an underlying list element must be added here.
                        childValueMemberSelectors.AddSelectors
                        (
                            parts[i].Selects,
                            childParam,
                            childParam
                        );
                    }
                    else
                    {
                        valueMemberSelectors.AddSelectors(parts[i].Selects, Expression.Parameter(sourceExpression.Type, parameterName), parent);
                    }
                }
            }

            AddChildSeelctors();

            return parent;

            //Adding childValueMemberSelectors created above and in a the recursive call:
            //i0 => i0.Builder.Name becomes
            //i => i.Buildings.Select(i0 => i0.Builder.Name)
            void AddChildSeelctors()
            {
                childValueMemberSelectors.ForEach(selector =>
                {
                    valueMemberSelectors.Add(Expression.Lambda
                    (
                        typeof(Func<,>).MakeGenericType(new[] { sourceExpression.Type, typeof(object) }),
                        Expression.Call
                        (
                            typeof(Enumerable),
                            "Select",
                            new Type[] { parent.GetUnderlyingElementType(), typeof(object) },
                            parent,
                            selector
                        ),
                        Expression.Parameter(sourceExpression.Type, parameterName)
                    ));
                });
            }
        }

        private static void AddSelectors(this List<LambdaExpression> valueMemberSelectors, List<string> selects, ParameterExpression param, Expression parentBody)
        {
            if (parentBody.Type.IsList() || parentBody.Type.IsLiteralType())
                return;

            valueMemberSelectors.AddRange
            (
                parentBody.Type
                    .GetSelectedMembers(selects)
                    .Select(member => Expression.MakeMemberAccess(parentBody, member))
                    .Select
                    (
                        selector => selector.Type.IsValueType
                            ? (Expression)Expression.Convert(selector, typeof(object))
                            : selector
                    )
                    .Select
                    (
                        selector => Expression.Lambda
                        (
                            typeof(Func<,>).MakeGenericType(new[] { param.Type, typeof(object) }),
                            selector,
                            param
                        )
                    )
            );
        }



        private static Expression GetSelectExpression(IEnumerable<Expansion> expansions, Expression parent, List<LambdaExpression> valueMemberSelectors, string parameterName)
        {
            ParameterExpression parameter = Expression.Parameter(parent.GetUnderlyingElementType(), parameterName.ChildParameterName());
            Expression selectorBody = BuildSelectorExpression(parameter, expansions.ToList(), valueMemberSelectors, parameter.Name);
            return Expression.Call
            (
                typeof(Enumerable),
                "Select",
                new Type[] { parameter.Type, selectorBody.Type },
                parent,
                Expression.Lambda
                (
                    typeof(Func<,>).MakeGenericType(new[] { parameter.Type, selectorBody.Type }),
                    selectorBody,
                    parameter
                )
            );
        }
    }
}
