using System;
using System.Linq.Expressions;

namespace CacheRepository
{
    class ExUser
    {
        public int Id;
        public string Name;
    }

    class ExprVisitor : ExpressionVisitor
    {
        public string MemberName { get; private set; }
        public Type MemberType { get; private set; }
        public object MemberValue { get; private set; }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.Left is MemberExpression && node.Right is ConstantExpression)
            {
                var member = node.Left as MemberExpression;
                var constant = node.Right as ConstantExpression;
                Console.WriteLine("expression tree: Name={0}, Type={1}, Value={2}", member.Member.Name, member.Type, constant.Value);

            }
            return base.VisitBinary(node);
        }

        public void Test()
        {
            Expression<Func<ExUser, object>> exp = p => !((p.Id > 1 && p.Id < 20) || p.Name == "abc");
            Visit(exp);
        }
    }
}
