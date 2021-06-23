using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.Core.Extensions
{
    public static class QueryableExtension
    {
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> source, String filters)
               where T : BaseEntity
        {
            if (String.IsNullOrWhiteSpace(filters))
            {
                return source;
            }

            return source.Where(filters);
        }

        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, String orders)
            where T : BaseEntity
        {
            if (!String.IsNullOrWhiteSpace(orders))
            {
                List<String> orderClauseList = new();

                foreach (String clause in orders.Split(",", StringSplitOptions.RemoveEmptyEntries).Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                {
                    Boolean isDescending = clause.EndsWith(" desc", StringComparison.InvariantCultureIgnoreCase);
                    Int32 indexOfFirstSpace = clause.IndexOf(" ", StringComparison.Ordinal);
                    String propertyName = indexOfFirstSpace == -1 ? clause : clause.Remove(indexOfFirstSpace);

                    orderClauseList.Add(propertyName + (isDescending ? " descending" : " ascending"));
                }

                source = source.OrderBy(String.Join(", ", orderClauseList));
            }

            return source;
        }

        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> source, Int32 pageIndex, Int32 pageSize, out Int32 totalCount)
            where T : BaseEntity
        {
            totalCount = source.Count();

            return source.Skip(pageSize * pageIndex).Take(pageSize);
        }

        public static IQueryable<T> ApplyReorganize<T>(this IQueryable<T> source, String fields)
            where T : BaseEntity
        {
            if (!String.IsNullOrWhiteSpace(fields))
            {
                source = source.Select(CreateSelector<T>(CreateParameterTrees(fields)));
            }

            return source;
        }

        private static List<ParameterTree> CreateParameterTrees(String fields)
        {
            List<ParameterTree> trees = new();

            foreach (String filed in fields.Split(',').Select(x => x.Trim()))
            {
                List<String> members = filed.Split('.').ToList();

                String firstMember = members.FirstOrDefault();

                ParameterTree tree = trees.FirstOrDefault(x => x.Name == firstMember);

                if (tree == null)
                {
                    tree = new ParameterTree()
                    {
                        Name = firstMember,
                        Children = new List<ParameterTree>()
                    };

                    trees.Add(tree);
                }

                if (members.Count > 1)
                {
                    tree.Children.AddRange(CreateParameterTrees(String.Join('.', members.Skip(1))));
                }
            }

            return trees;
        }

        private static Expression<Func<T, T>> CreateSelector<T>(List<ParameterTree> parameterTrees)
        {
            List<String> customFields = new();

            ParameterExpression parameterExpression = Expression.Parameter(typeof(T));

            NewExpression newExpression = Expression.New(typeof(T));

            List<MemberAssignment> bindings = new();

            foreach (ParameterTree parameterTree in parameterTrees)
            {
                PropertyInfo property = typeof(T).GetProperties().FirstOrDefault(y => y.Name.Equals(parameterTree.Name, StringComparison.InvariantCultureIgnoreCase));

                if (property == null)
                {
                    customFields.Add(parameterTree.Name);

                    continue;
                }

                Expression expression = Expression.Property(parameterExpression, property);

                if (parameterTree.Children != null && parameterTree.Children.Count > 0)
                {
                    if (!IsSubclassOfRawGeneric(typeof(ICollection<>), property.PropertyType))
                    {
                        Expression childExpression = CreateExpression(property.PropertyType, expression, parameterTree.Children, parameterExpression);
                        expression = Expression.Condition(Expression.Equal(expression, Expression.Constant(null)), Expression.Constant(null, childExpression.Type), childExpression);
                    }
                    else
                    {
                        MethodInfo createExpressMethod = typeof(QueryableExtension).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(x => x.Name == nameof(QueryableExtension.CreateExpression) && x.IsGenericMethod).MakeGenericMethod(property.PropertyType.GenericTypeArguments[0]);

                        expression = createExpressMethod.Invoke(null, new Object[] { parameterTree.Children, expression }) as Expression;
                    }
                }

                bindings.Add(Expression.Bind(property, expression));
            }

            MemberInitExpression initExpression = Expression.MemberInit(newExpression, bindings);

            return Expression.Lambda<Func<T, T>>(initExpression, parameterExpression);
        }

        private static Expression CreateExpression(Type type, Expression memberExpression, List<ParameterTree> parameterTrees, ParameterExpression parameterExpression)
        {
            List<String> customFields = new();

            NewExpression newExpression = newExpression = Expression.New(type);

            List<MemberAssignment> bindings = new();

            foreach (ParameterTree parameterTree in parameterTrees)
            {
                PropertyInfo property = type.GetProperties().FirstOrDefault(y => y.Name.Equals(parameterTree.Name, StringComparison.InvariantCultureIgnoreCase));

                if (property == null)
                {
                    if (parameterTree.Children == null || parameterTree.Children.Count == 0)
                    {
                        customFields.Add(parameterTree.Name);
                    }

                    continue;
                }

                Expression expression = Expression.Property(memberExpression, property);

                if (parameterTree.Children != null && parameterTree.Children.Count > 0)
                {
                    Expression childExpression = CreateExpression(property.PropertyType, expression, parameterTree.Children, parameterExpression);
                    expression = Expression.Condition(Expression.Equal(expression, Expression.Constant(null)), Expression.Constant(null, childExpression.Type), childExpression);
                }

                bindings.Add(Expression.Bind(property, expression));
            }

            return Expression.MemberInit(newExpression, bindings);
        }

        private static Boolean IsSubclassOfRawGeneric(Type genericType, Type type)
        {
            while (type != null && type != typeof(Object))
            {
                Type currentType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericType == currentType)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private class ParameterTree
        {
            public String Name { get; set; }
            public List<ParameterTree> Children { get; set; }
        }
    }
}
