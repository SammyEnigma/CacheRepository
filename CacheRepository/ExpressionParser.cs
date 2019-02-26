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

    class TreeNode
    {
        private ExpressionType _exp_op;
        public TreeNode(ExpressionType op)
        {
            _exp_op = op;
        }

        public int NodeHash;
        public ExpressionType Op => this._exp_op;
        public TreeNode Parent;

        private TreeNode _left;
        public TreeNode Left
        {
            set
            {
                this._left = value;
            }
            get
            {
                return this._left;
            }
        }

        private TreeNode _right;
        public TreeNode Right
        {
            set
            {
                this._right = value;
            }
            get
            {
                return this._right;
            }
        }
        public static TreeNode Current { get; }
    }

    public class ExprVisitor : ExpressionVisitor
    {
        public string MemberName { get; private set; }
        public Type MemberType { get; private set; }
        public object MemberValue { get; private set; }

        private List<Expression> _cache = new List<Expression>();
        private List<int> _hash = new List<int>();
        private TreeNode root;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (root == null)
            {
                root = new TreeNode(node.NodeType);
            }
            else
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
