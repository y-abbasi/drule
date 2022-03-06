using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ConsoleApp1;

public class Neo4JExpressionVisitor : ExpressionVisitor, IExpressionVisitor
{
    private static readonly Dictionary<ExpressionType, string> LogicalOperators;
    private static readonly Dictionary<string, string> MethodOperators;
    private static readonly Dictionary<string, string> CollectionMethodOperators;
    private static readonly Dictionary<Type, Func<object, string>> TypeConverters;

    static Neo4JExpressionVisitor()
    {
        MethodOperators = new Dictionary<string, string>
        {
            {"Equals", " = "},
            {"StartsWith", " Starts With "},
            {"EndsWith", " Ends With "},
            {"Contains", " Contains "},
        };
        CollectionMethodOperators = new Dictionary<string, string>
        {
            {"Exists", "size({0}) > 0"},
            {"Any", "size({0}) > 0"},
            {"Count", "size({0})"},
            {"First", "{0}[0..1][0]"},
            {"FirstOrDefault", "{0}[0..1][0]"},
            {"Last", "{0}[size({0})-1..size({0})][0]"},
            {"LastOrDefault", "{0}[size({0})-1..size({0})][0]"},
            {"Contains", "In {0}"}
        };
        //mappings for table, shown above
        LogicalOperators = new Dictionary<ExpressionType, string>
        {
            [ExpressionType.Not] = "not",
            [ExpressionType.GreaterThan] = ">",
            [ExpressionType.GreaterThanOrEqual] = ">=",
            [ExpressionType.LessThan] = "<",
            [ExpressionType.LessThanOrEqual] = "<=",
            [ExpressionType.Equal] = "=",
            [ExpressionType.NotEqual] = "<>",
            [ExpressionType.Not] = "not",
            [ExpressionType.AndAlso] = "and",
            [ExpressionType.OrElse] = "or"
        };

        //if type is string we will wrap it into single quotes
        //if it is a DateTime we will format it like datetime'2008-07-10T00:00:00Z'
        //bool.ToString() returns "True" or "False" with first capital letter, so .ToLower() is applied
        //if it is one of the rest "simple" types we will just call .ToString() method on it
        TypeConverters = new Dictionary<Type, Func<object, string>>
        {
            [typeof(string)] = x => $"'{x}'",
            [typeof(DateTime)] =
                x => $"'{(DateTime) x:yyyy-MM-ddTHH:mm:ss}'",
            [typeof(bool)] = x => x.ToString()!.ToLower()
        };
    }

    private StringBuilder _queryStringBuilder;
    private Stack<string> _fieldNames;

    public Neo4JExpressionVisitor()
    {
        //here we will collect our query
        _queryStringBuilder = new StringBuilder();
        //will be discussed below
        _fieldNames = new Stack<string>();
    }

    //entry point
    public string GetQuery(LambdaExpression predicate)
    {
        //Visit transfer abstract Expression to concrete method, like VisitUnary
        //it's invocation chain (at case of unary operator) approximetely looks this way:
        //inside visitor: predicate.Body.Accept(ExpressionVisitor this)
        //inside expression(visitor is this from above): visitor.VisitUnary(this) 
        //here this is Expression
        //we not pass whole predicate, just Body, because we not need predicate.Parameters: "x =>" part
        Visit(predicate.Body);
        var query = _queryStringBuilder.ToString();

        _queryStringBuilder.Clear();

        return query;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        //assume we only allow not (!) unary operator:
        if (node.NodeType != ExpressionType.Not)
            throw new NotSupportedException("Only not(\"!\") unary operator is supported!");

        _queryStringBuilder.Append($"{LogicalOperators[node.NodeType]} "); //!

        _queryStringBuilder.Append("("); //(!
        //go down from a tree
        Visit(node.Operand); //(!expression
        _queryStringBuilder.Append(")"); //(!expression)

        //we should return expression, it will allow to create new expression based on existing one,
        //but, at our case, it is not needed, so just return initial node argument
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        _queryStringBuilder.Append(node.Name);
        return base.VisitParameter(node);
    }

    //corresponds to: and, or, greater than, less than, etc.
    protected override Expression VisitBinary(BinaryExpression node)
    {
        _queryStringBuilder.Append("("); //(
        //left side of binary operator
        Visit(node.Left); //(leftExpr

        if ((node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual) &&
            node.Right is ConstantExpression {Value: null})
        {
            _queryStringBuilder.Append($" is {(node.NodeType == ExpressionType.NotEqual ? "not " : "")}null)");
            return node;
        }

        _queryStringBuilder.Append($" {LogicalOperators[node.NodeType]} "); //(leftExpr and

        //right side of binary operator
        Visit(node.Right); //(leftExpr and RighExpr
        _queryStringBuilder.Append(")"); //(leftExpr and RighExpr)

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        //corresponds to: order.Customer, .order, today variables
        //when we pass parameters to expression via closure, CLR internally creates class:

        //class NameSpace+<>c__DisplayClass12_0
        //{
        //    public Order order;
        //    public DateTime today;
        //}

        //which contains values of parameters. When we face order.Customer, it's node.Expression
        //will not have reference to value "Tom", but instead reference to parent (.order), so we
        //will go to it via Visit(node.Expression) and also save node.Member.Name into 
        //Stack(_fieldNames) fo further usage. order.Customer has type ExpressionType.MemberAccess. 
        //.order - ExpressionType.Constant, because it's node.Expression is ExpressionType.Constant
        //(VisitConstant will be called) that is why we can get it's actual value(instance of Order). 
        //Our Stack at this point: "Customer" <- "order". Firstly we will get "order" field value, 
        //when it will be reached, on NameSpace+<>c__DisplayClass12_0 class instance
        //(type.GetField(fieldName)) then value of "Customer" property
        //(type.GetProperty(fieldName).GetValue(input)) on it. We started from 
        //order.Customer Expression then go up via reference to it's parent - "order", get it's value 
        //and then go back - get value of "Customer" property on order. Forward and backward
        //directions, at this case, reason to use Stack structure

        if (node.Expression.NodeType == ExpressionType.Constant
            ||
            node.Expression.NodeType == ExpressionType.MemberAccess)
        {
            _fieldNames.Push(node.Member.Name);
            Visit(node.Expression);
        }
        else
            //corresponds to: x.Customer - just write "Customer"

            _queryStringBuilder.Append($"{(node.Expression as ParameterExpression).Name}.{node.Member.Name}");

        return node;
    }

    //corresponds to: 1, "Tom", instance of NameSpace+<>c__DisplayClass12_0, instance of Order, i.e.
    //any expression with value
    protected override Expression VisitConstant(ConstantExpression node)
    {
        //just write value
        _queryStringBuilder.Append(GetValue(node.Value));
        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        dynamic args = node.Arguments.OfType<ConstantExpression>().ToList();
        if (node.Type == typeof(DateTime))
        {
            VisitConstant(Expression.Constant(new DateTime(args[0].Value, args[1].Value, args[2].Value)));
            return node;
        }

        return base.VisitNew(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var special = node.Method.GetCustomAttribute<Neo4JMethodAttribute>();
        if (special is not null)
        {
            (Activator.CreateInstance(special.Convertor) as IMethodVisitor)!.Visit(this, node.Arguments.ToArray());
            return node;
        }

        if ((node.Arguments[0].Type != typeof(string) &&
             node.Arguments[0].Type.GetInterface(nameof(IEnumerable)) != null) ||
            node.Object?.Type.GetInterface(nameof(IEnumerable)) != null)
        {
            var tmp = _queryStringBuilder;
            _queryStringBuilder = new StringBuilder();
            var leftExp = node.Object ?? node.Arguments[0];
            Visit(leftExp);
            var left = _queryStringBuilder.ToString();
            _queryStringBuilder = new StringBuilder();
            var rightExp = Visit((node.Arguments.Count > 1) ? node.Arguments[1] : node.Arguments[0]);
            var right = _queryStringBuilder.ToString();
            _queryStringBuilder = tmp;

            if (new[] {"Any", "Exists"}.Contains(node.Method.Name))
            {
                _queryStringBuilder.Append(
                    $"size([{(rightExp as LambdaExpression)!.Parameters[0].Name} in {left} where {right}]) > 0");
            }
            else
                _queryStringBuilder.Append(string.Format(
                    $"{right} {CollectionMethodOperators[node.Method.Name]}",
                    left));
        }
        else
        {
            Visit(node.Object ?? node.Arguments[0]);
            _queryStringBuilder.Append(MethodOperators[node.Method.Name]);
            if (node.Object is not null && node.Arguments.Any())
                Visit(node.Arguments[0]);
            if (node.Arguments.Count > 1) Visit(node.Arguments[1]);
        }

        return node;
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        _queryStringBuilder.Append("[");
        if (node.Initializers.Any())
        {
            base.Visit(node.Initializers[0].Arguments[0]);
            foreach (var init in node.Initializers.Skip(1))
            {
                _queryStringBuilder.Append(",");
                base.Visit(init.Arguments[0]);
            }
        }

        _queryStringBuilder.Append("] ");
        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        _queryStringBuilder.Append("[");
        if (node.Expressions.Any())
        {
            base.Visit(node.Expressions[0]);
            foreach (var init in node.Expressions.Skip(1))
            {
                _queryStringBuilder.Append(",");
                base.Visit(init);
            }
        }

        _queryStringBuilder.Append("] ");
        return node;
    }
    
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Visit(node.Body);
        return node;
    }
    
    private string GetValue(object? input)
    {
        if (input == null) return "null";
        var type = input.GetType();
        //if it is not simple value
        if (type.IsClass && type != typeof(string))
        {
            //proper order of selected names provided by means of Stack structure
            var fieldName = _fieldNames.Pop();
            var fieldInfo = type.GetField(fieldName);
            object value;
            value = fieldInfo != null ? fieldInfo.GetValue(input) : type.GetProperty(fieldName).GetValue(input);
            return GetValue(value);
        }
        else
        {
            //our predefined _typeConverters
            if (TypeConverters.ContainsKey(type))
                return TypeConverters[type](input);
            if (type.IsEnum)
                return $"'{input}'";
            //rest types
            return input.ToString();
        }
    }

    void IExpressionVisitor.AppendToResult(string text)
    {
        _queryStringBuilder.Append(text);
    }
}

public interface IExpressionVisitor
{
    Expression Visit(Expression node);
    internal void AppendToResult(string text);
}