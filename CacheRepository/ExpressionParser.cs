using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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

    class user
    {
        public string name;
        public int age;

        public predicate<user> where()
        {
            return new predicate<user>();
        }
    }

    class predicate<T>
    {
        private System.Text.StringBuilder sql = new System.Text.StringBuilder();

        public predicate<T> Single(System.Linq.Expressions.Expression<Func<T, bool>> single)
        {
            Parse(single, string.Empty);
            return this;
        }

        public predicate<T> Single(predicate<T> inner)
        {
            sql.Append($" ({inner.ToString()})");
            return this;
        }

        public predicate<T> And(System.Linq.Expressions.Expression<Func<T, bool>> and)
        {
            Parse(and, "AND");
            return this;
        }

        public predicate<T> And(predicate<T> inner)
        {
            sql.Append($" AND ({inner.ToString()})");
            return this;
        }

        public predicate<T> Or(System.Linq.Expressions.Expression<Func<T, bool>> or)
        {
            Parse(or, "OR");
            return this;
        }

        public predicate<T> Or(predicate<T> inner)
        {
            sql.Append($" OR ({inner.ToString()})");
            return this;
        }

        public predicate<T> Not(System.Linq.Expressions.Expression<Func<T, bool>> not)
        {
            Parse(not, string.Empty);
            return this;
        }

        public predicate<T> Not(predicate<T> inner)
        {
            sql.Append($" Not({inner.ToString()})");
            return this;
        }

        private void Parse(System.Linq.Expressions.Expression<Func<T, bool>> expression, string op)
        {
            var expr_unary = expression.Body as System.Linq.Expressions.UnaryExpression;
            var expr_binary = expression.Body as System.Linq.Expressions.BinaryExpression;

            if (expr_unary == null && expr_binary == null)
                throw new ArgumentException("参数类型错误");

            if (expr_unary != null)
            {
                var operand = expr_unary.Operand as System.Linq.Expressions.MemberExpression;
                if (expr_unary.NodeType == System.Linq.Expressions.ExpressionType.Not)
                {
                    sql.Append($" ([{operand.Member.Name}] <> 0)");
                }
                else
                {
                    sql.Append($" ([{operand.Member.Name}] = 1)");
                }
            }
            else if (expr_binary != null)
            {
                var left = expr_binary.Left as System.Linq.Expressions.MemberExpression;
                var right = expr_binary.Right as System.Linq.Expressions.ConstantExpression;
                sql.Append($" {op} ([{left.Member.Name}] {NodeTypeToString(expr_binary.NodeType, false)} {right.Value})");
            }
        }

        private object NodeTypeToString(System.Linq.Expressions.ExpressionType nodeType, bool rightIsNull)
        {
            switch (nodeType)
            {
                case System.Linq.Expressions.ExpressionType.Add:
                    return "+";
                case System.Linq.Expressions.ExpressionType.And:
                    return "&";
                case System.Linq.Expressions.ExpressionType.AndAlso:
                    return "AND";
                case System.Linq.Expressions.ExpressionType.Divide:
                    return "/";
                case System.Linq.Expressions.ExpressionType.Equal:
                    return rightIsNull ? "IS" : "=";
                case System.Linq.Expressions.ExpressionType.ExclusiveOr:
                    return "^";
                case System.Linq.Expressions.ExpressionType.GreaterThan:
                    return ">";
                case System.Linq.Expressions.ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case System.Linq.Expressions.ExpressionType.LessThan:
                    return "<";
                case System.Linq.Expressions.ExpressionType.LessThanOrEqual:
                    return "<=";
                case System.Linq.Expressions.ExpressionType.Modulo:
                    return "%";
                case System.Linq.Expressions.ExpressionType.Multiply:
                    return "*";
                case System.Linq.Expressions.ExpressionType.Negate:
                    return "-";
                case System.Linq.Expressions.ExpressionType.Not:
                    return "NOT";
                case System.Linq.Expressions.ExpressionType.NotEqual:
                    return "<>";
                case System.Linq.Expressions.ExpressionType.Or:
                    return "|";
                case System.Linq.Expressions.ExpressionType.OrElse:
                    return "OR";
                case System.Linq.Expressions.ExpressionType.Subtract:
                    return "-";
            }
            throw new Exception($"Unsupported node type: {nodeType}");
        }

        public override string ToString()
        {
            return sql.ToString();
        }
    }

    class MyExpressionVisitor : ExpressionVisitor
    {
        private StringBuilder sb = new StringBuilder();
        private bool invert = false;
        public void Test()
        {
            var list = new System.Collections.Generic.List<int> { 1, 2, 3 };
            Expression<Func<ExUser, object>> exp1 = p => !(!(p.Id > 1 && p.Id < 20) || p.Name == "abc");
            Visit(exp1);
            System.Console.WriteLine(this);
            sb.Clear();
            Expression<Func<ExUser, object>> exp2 = p => !(p.Id == 1 || p.Id == 2 || p.Id == 3);
            Visit(exp2);
            System.Console.WriteLine(this);
            sb.Clear();
            Expression<Func<ExUser, object>> exp3 = p => p.Name.Contains("a");
            Visit(exp3);
            System.Console.WriteLine(this);
            sb.Clear();
            Expression<Func<ExUser, object>> exp4 = p => list.Contains(p.Id);
            Visit(exp4);
            System.Console.WriteLine(this);
            sb.Clear();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            sb.Append("(");
            this.Visit(node.Left);

            switch (node.NodeType)
            {
                case ExpressionType.And:
                    sb.Append(" AND ");
                    break;

                case ExpressionType.AndAlso:
                    sb.Append(" AND ");
                    break;

                case ExpressionType.Or:
                    sb.Append(" OR ");
                    break;

                case ExpressionType.OrElse:
                    sb.Append(" OR ");
                    break;

                case ExpressionType.Equal:
                    if (IsNullConstant(node.Right))
                    {
                        sb.Append(" IS ");
                    }
                    else
                    {
                        sb.Append(" = ");
                    }
                    break;

                case ExpressionType.NotEqual:
                    if (IsNullConstant(node.Right))
                    {
                        sb.Append(" IS NOT ");
                    }
                    else
                    {
                        sb.Append(" <> ");
                    }
                    break;

                case ExpressionType.LessThan:
                    sb.Append(" < ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    sb.Append(" <= ");
                    break;

                case ExpressionType.GreaterThan:
                    sb.Append(" > ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" >= ");
                    break;

                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", node.NodeType));
            }

            this.Visit(node.Right);
            sb.Append(")");
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    sb.Append(" NOT ");
                    this.Visit(node.Operand);
                    break;
                case ExpressionType.Convert:
                    this.Visit(node.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", node.NodeType));
            }

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value == null)
            {
                sb.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(node.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        sb.Append(((bool)node.Value) ? 1 : 0);
                        break;

                    case TypeCode.String:
                        sb.Append("'");
                        sb.Append(node.Value);
                        sb.Append("'");
                        break;

                    case TypeCode.DateTime:
                        sb.Append("'");
                        sb.Append(node.Value);
                        sb.Append("'");
                        break;

                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", node.Value));

                    default:
                        sb.Append(node.Value);
                        break;
                }
            }

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // 闭包带进来的变量是生成类型的一个Field
            if (node.Expression.NodeType == ExpressionType.Constant)
            {
                var container = ((ConstantExpression)node.Expression).Value;
                var value = ((FieldInfo)node.Member).GetValue(container);
                foreach (var item in ((IEnumerable)value))
                    sb.Append(item + ",");
            }

            if (node.Expression.NodeType == ExpressionType.Parameter)
                sb.Append(node.Member.Name);

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == typeof(string).GetMethod("Contains", new[] { typeof(string) }))
            {
                sb.Append(" (LIKE '%");
                this.Visit(node.Arguments[0]);
                sb.Append("%')");
                return node;
            }
            if (node.Method == typeof(string).GetMethod("StartsWith", new[] { typeof(string) }))
            {
                sb.Append(" (LIKE '");
                this.Visit(node.Arguments[0]);
                sb.Append("%')");
                return node;
            }
            if (node.Method == typeof(string).GetMethod("EndsWith", new[] { typeof(string) }))
            {
                sb.Append(" (LIKE '%");
                this.Visit(node.Arguments[0]);
                sb.Append("')");
                return node;
            }

            // 注意区分contains方法方式，一个是在对象上list.contains，一个是在string上string.contains
            if (node.Method.Name == "Contains")
            {
                Expression collection;
                Expression property;
                if (node.Method.IsDefined(typeof(ExtensionAttribute)) && node.Arguments.Count == 2) // 支持直接调用扩展方法的形式
                {
                    collection = node.Arguments[0];
                    property = node.Arguments[1];
                }
                else if (!node.Method.IsDefined(typeof(ExtensionAttribute)) && node.Arguments.Count == 1)
                {
                    collection = node.Object;
                    property = node.Arguments[0];
                }
                else
                {
                    throw new Exception("Unsupported method call: " + node.Method.Name);
                }
                sb.Append(" (");
                this.Visit(property);
                sb.Append(" IN (");
                this.Visit(collection);
                sb.Append(" )) ");
            }

            return node;
        }

        private bool IsNullConstant(Expression exp)
        {
            return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}
