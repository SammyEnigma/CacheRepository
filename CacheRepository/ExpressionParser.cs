using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CacheRepository
{
    public class ExUser
    {
        public int Id;
        public string Name;
    }

    public class ExprVisitor : ExpressionVisitor
    {
        public string MemberName { get; private set; }
        public Type MemberType { get; private set; }
        public object MemberValue { get; private set; }

        private List<Expression> _cache = new List<Expression>();
        private List<int> _hash = new List<int>();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (_hash.Contains(node.GetHashCode()))
            {

            }
            if (node.Left is MemberExpression && node.Right is ConstantExpression)
            {
                var member = node.Left as MemberExpression;
                var constant = node.Right as ConstantExpression;
                Console.WriteLine("expression tree: Name={0}, Type={1}, Value={2}", member.Member.Name, member.Type, constant.Value);

            }
            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Convert)
            {
                _cache.Add(node.Operand);
                _hash.Add(node.Operand.GetHashCode());
            }
            return base.VisitUnary(node);
        }

        public void Test()
        {
            Expression<Func<ExUser, object>> exp1 = p => !(!(p.Id > 1 && p.Id < 20) || p.Name == "abc");
            Expression<Func<ExUser, object>> exp2 = p => !(p.Id == 1 || p.Id == 2 || p.Id == 3);
            Visit(exp2);
        }
    }
}
