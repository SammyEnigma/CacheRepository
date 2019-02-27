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
        public TreeNode(ExpressionType op, TreeNode parent)
        {
            _exp_op = op;
            Parent = parent;
        }

        public ExpressionType Op => this._exp_op;
        public TreeNode Parent;

        private TreeNode _unary;
        public TreeNode Unary
        {
            set
            {
                this._unary = value;
            }
            get
            {
                return this._unary;
            }
        }

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
    }

    public class ExprVisitor : ExpressionVisitor
    {
        public string MemberName { get; private set; }
        public Type MemberType { get; private set; }
        public object MemberValue { get; private set; }

        private List<Expression> _cache = new List<Expression>();
        private List<int> _hash = new List<int>();
        private TreeNode _root;
        private TreeNode _current;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.Left is MemberExpression && node.Right is ConstantExpression)
            {
                var member = node.Left as MemberExpression;
                var constant = node.Right as ConstantExpression;
                Console.WriteLine("expression tree: Name={0}, Type={1}, Value={2}", member.Member.Name, member.Type, constant.Value);
                _current = _current.Parent.Right;
            }
            else
            {
                if (_root == null)
                {
                    _root = new TreeNode(node.NodeType, null);
                    _current = _root;
                }
                else
                {
                    _current.Left = new TreeNode(node.Left.NodeType, _current);
                    _current.Right = new TreeNode(node.Right.NodeType, _current);
                    _current = _current.Left;
                }
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Convert)
            {
                if (_root == null)
                {
                    _root = new TreeNode(node.NodeType, null);
                    _current = _root;
                }
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
