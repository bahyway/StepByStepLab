## Question01:
YES PLEASE DO THIS: Now, shall I proceed with creating the ANTLR grammar file and transpiler for the Akkadian DSL?
## Answer01:

Akkadian DSL - Complete ANTLR Grammar & Transpiler
Let me create a full-featured graph query DSL inspired by ancient Akkadian linguistics but with modern, semantic syntax.

1. ANTLR4 Grammar File
Create: Akkadian.g4

grammar Akkadian;

// ==================== PARSER RULES ====================

// Root rule - a complete Akkadian query
query
    : (discoverStatement | createStatement | updateStatement | deleteStatement) EOF
    ;

// DISCOVER: Query/Read operations (like SELECT/MATCH)
discoverStatement
    : DISCOVER entityPattern (connectionClause)? (whereClause)? (orderClause)? (limitClause)? returnClause
    ;

// CREATE: Insert operations
createStatement
    : CREATE nodeDefinition (COMMA nodeDefinition)* (linkClause)?
    ;

// UPDATE: Modify operations
updateStatement
    : UPDATE entityPattern SET setClause (whereClause)?
    ;

// DELETE: Remove operations
deleteStatement
    : DELETE entityPattern (whereClause)?
    ;

// Entity pattern - defines what we're looking for
entityPattern
    : nodeType (AS alias)?
    ;

// Node type with optional label
nodeType
    : IDENTIFIER (COLON IDENTIFIER)?
    ;

// Connection between entities (relationships/edges)
connectionClause
    : CONNECTED_TO entityPattern viaClause (WITH_DEPTH depthSpecifier)?
    ;

viaClause
    : VIA relationshipType (AS alias)?
    ;

relationshipType
    : IDENTIFIER (COLON IDENTIFIER)?
    ;

depthSpecifier
    : INTEGER                           # exactDepth
    | INTEGER TO INTEGER                # rangeDepth
    | INTEGER TO STAR                   # unboundedDepth
    ;

// WHERE: Filter conditions
whereClause
    : WHERE condition
    ;

condition
    : condition AND condition                           # andCondition
    | condition OR condition                            # orCondition
    | NOT condition                                     # notCondition
    | LPAREN condition RPAREN                           # parenCondition
    | propertyReference comparisonOp value              # comparisonCondition
    | propertyReference IN LPAREN valueList RPAREN      # inCondition
    | propertyReference IS NULL                         # isNullCondition
    | propertyReference IS NOT NULL                     # isNotNullCondition
    | functionCall comparisonOp value                   # functionCondition
    ;

// Comparison operators
comparisonOp
    : EQUALS | NOT_EQUALS | GT | LT | GTE | LTE
    | EXCEEDS | BELOW | MATCHES | CONTAINS | STARTS_WITH | ENDS_WITH
    ;

// Property reference (e.g., person.name, customer.age)
propertyReference
    : IDENTIFIER DOT IDENTIFIER
    ;

// Function calls (e.g., COUNT(person.id), AVG(order.amount))
functionCall
    : IDENTIFIER LPAREN (propertyReference | STAR) RPAREN
    ;

// ORDER BY clause
orderClause
    : ORDER BY orderItem (COMMA orderItem)*
    ;

orderItem
    : propertyReference (ASC | DESC)?
    ;

// LIMIT clause
limitClause
    : LIMIT INTEGER (OFFSET INTEGER)?
    ;

// RETURN: What to return
returnClause
    : RETURN returnItem (COMMA returnItem)*
    | YIELD returnItem (COMMA returnItem)*
    ;

returnItem
    : propertyReference (AS alias)?              # propertyReturn
    | functionCall (AS alias)?                   # functionReturn
    | IDENTIFIER (AS alias)?                     # identifierReturn
    | STAR                                       # allReturn
    ;

// CREATE specific clauses
nodeDefinition
    : LPAREN alias COLON IDENTIFIER propertiesClause? RPAREN
    ;

linkClause
    : LINK nodeReference TO nodeReference VIA relationshipType propertiesClause?
    ;

nodeReference
    : IDENTIFIER
    ;

// SET clause for UPDATE
setClause
    : propertyReference EQUALS value (COMMA propertyReference EQUALS value)*
    ;

// Properties definition
propertiesClause
    : LBRACE propertyPair (COMMA propertyPair)* RBRACE
    ;

propertyPair
    : IDENTIFIER COLON value
    ;

// Values
value
    : STRING                    # stringValue
    | INTEGER                   # intValue
    | FLOAT                     # floatValue
    | BOOLEAN                   # boolValue
    | NULL                      # nullValue
    | LBRACKET valueList RBRACKET  # arrayValue
    ;

valueList
    : value (COMMA value)*
    ;

alias
    : IDENTIFIER
    ;

// ==================== LEXER RULES ====================

// Keywords (case-insensitive)
DISCOVER        : D I S C O V E R ;
CREATE          : C R E A T E ;
UPDATE          : U P D A T E ;
DELETE          : D E L E T E ;
CONNECTED_TO    : C O N N E C T E D '_' T O ;
VIA             : V I A ;
WHERE           : W H E R E ;
AND             : A N D ;
OR              : O R ;
NOT             : N O T ;
IN              : I N ;
IS              : I S ;
NULL            : N U L L ;
RETURN          : R E T U R N ;
YIELD           : Y I E L D ;
AS              : A S ;
ORDER           : O R D E R ;
BY              : B Y ;
ASC             : A S C ;
DESC            : D E S C ;
LIMIT           : L I M I T ;
OFFSET          : O F F S E T ;
SET             : S E T ;
LINK            : L I N K ;
TO              : T O ;
WITH_DEPTH      : W I T H '_' D E P T H ;

// Semantic operators (Akkadian-inspired alternatives)
EXCEEDS         : E X C E E D S ;        // Greater than
BELOW           : B E L O W ;            // Less than
MATCHES         : M A T C H E S ;        // Regex match
CONTAINS        : C O N T A I N S ;      // String contains
STARTS_WITH     : S T A R T S '_' W I T H ;
ENDS_WITH       : E N D S '_' W I T H ;

// Symbols
LPAREN          : '(' ;
RPAREN          : ')' ;
LBRACE          : '{' ;
RBRACE          : '}' ;
LBRACKET        : '[' ;
RBRACKET        : ']' ;
COMMA           : ',' ;
DOT             : '.' ;
COLON           : ':' ;
SEMICOLON       : ';' ;
EQUALS          : '=' ;
NOT_EQUALS      : '!=' | '<>' ;
GT              : '>' ;
LT              : '<' ;
GTE             : '>=' ;
LTE             : '<=' ;
STAR            : '*' ;

// Literals
BOOLEAN         : 'true' | 'false' | 'TRUE' | 'FALSE' ;
INTEGER         : [0-9]+ ;
FLOAT           : [0-9]+ '.' [0-9]+ ;
STRING          : '\'' (~['\r\n] | '\\' .)* '\''
                | '"' (~["\r\n] | '\\' .)* '"' ;

// Identifiers
IDENTIFIER      : [a-zA-Z_][a-zA-Z0-9_]* ;

// Whitespace and comments
WS              : [ \t\r\n]+ -> skip ;
LINE_COMMENT    : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT   : '/*' .*? '*/' -> skip ;

// Case-insensitive fragments
fragment A : [aA] ;
fragment B : [bB] ;
fragment C : [cC] ;
fragment D : [dD] ;
fragment E : [eE] ;
fragment F : [fF] ;
fragment G : [gG] ;
fragment H : [hH] ;
fragment I : [iI] ;
fragment J : [jJ] ;
fragment K : [kK] ;
fragment L : [lL] ;
fragment M : [mM] ;
fragment N : [nN] ;
fragment O : [oO] ;
fragment P : [pP] ;
fragment Q : [qQ] ;
fragment R : [rR] ;
fragment S : [sS] ;
fragment T : [tT] ;
fragment U : [uU] ;
fragment V : [vV] ;
fragment W : [wW] ;
fragment X : [xX] ;
fragment Y : [yY] ;
fragment Z : [zZ] ;

2. AST (Abstract Syntax Tree) Classes
Create: BahyWay.SharedKernel.Akkadian/Domain/Ast/

namespace BahyWay.SharedKernel.Akkadian.Domain.Ast;

// Base node
public abstract record AstNode;

// Root query node
public record AkkadianQuery(Statement Statement) : AstNode;

// Statement types
public abstract record Statement : AstNode;
public record DiscoverStatement(
    EntityPattern Entity,
    ConnectionClause? Connection,
    WhereClause? Where,
    OrderClause? Order,
    LimitClause? Limit,
    ReturnClause Return
) : Statement;

public record CreateStatement(
    List<NodeDefinition> Nodes,
    LinkClause? Link
) : Statement;

public record UpdateStatement(
    EntityPattern Entity,
    SetClause Set,
    WhereClause? Where
) : Statement;

public record DeleteStatement(
    EntityPattern Entity,
    WhereClause? Where
) : Statement;

// Patterns
public record EntityPattern(
    string NodeType,
    string? Label,
    string? Alias
) : AstNode;

public record ConnectionClause(
    EntityPattern TargetEntity,
    RelationshipPattern Relationship,
    DepthSpecifier? Depth
) : AstNode;

public record RelationshipPattern(
    string Type,
    string? Label,
    string? Alias
) : AstNode;

// Depth specifiers
public abstract record DepthSpecifier : AstNode;
public record ExactDepth(int Depth) : DepthSpecifier;
public record RangeDepth(int Min, int Max) : DepthSpecifier;
public record UnboundedDepth(int Min) : DepthSpecifier;

// Conditions
public abstract record Condition : AstNode;
public record AndCondition(Condition Left, Condition Right) : Condition;
public record OrCondition(Condition Left, Condition Right) : Condition;
public record NotCondition(Condition Inner) : Condition;
public record ComparisonCondition(
    PropertyReference Property,
    ComparisonOperator Operator,
    Value Value
) : Condition;
public record InCondition(PropertyReference Property, List<Value> Values) : Condition;
public record IsNullCondition(PropertyReference Property, bool IsNot) : Condition;
public record FunctionCondition(
    FunctionCall Function,
    ComparisonOperator Operator,
    Value Value
) : Condition;

// Comparison operators
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Exceeds,      // Semantic alias for >
    Below,        // Semantic alias for <
    Matches,      // Regex
    Contains,     // String contains
    StartsWith,
    EndsWith
}

// Property reference
public record PropertyReference(string EntityAlias, string PropertyName) : AstNode;

// Function call
public record FunctionCall(string Name, FunctionArgument Argument) : AstNode;
public abstract record FunctionArgument : AstNode;
public record PropertyArgument(PropertyReference Property) : FunctionArgument;
public record AllArgument : FunctionArgument;

// Clauses
public record WhereClause(Condition Condition) : AstNode;
public record OrderClause(List<OrderItem> Items) : AstNode;
public record OrderItem(PropertyReference Property, SortDirection Direction) : AstNode;
public enum SortDirection { Ascending, Descending }

public record LimitClause(int Limit, int? Offset) : AstNode;

public record ReturnClause(List<ReturnItem> Items) : AstNode;
public abstract record ReturnItem : AstNode;
public record PropertyReturnItem(PropertyReference Property, string? Alias) : ReturnItem;
public record FunctionReturnItem(FunctionCall Function, string? Alias) : ReturnItem;
public record IdentifierReturnItem(string Identifier, string? Alias) : ReturnItem;
public record AllReturnItem : ReturnItem;

// CREATE specific
public record NodeDefinition(
    string Alias,
    string Label,
    Dictionary<string, Value>? Properties
) : AstNode;

public record LinkClause(
    string FromNode,
    string ToNode,
    RelationshipPattern Relationship,
    Dictionary<string, Value>? Properties
) : AstNode;

// UPDATE specific
public record SetClause(Dictionary<PropertyReference, Value> Assignments) : AstNode;

// Values
public abstract record Value : AstNode;
public record StringValue(string Val) : Value;
public record IntValue(int Val) : Value;
public record FloatValue(double Val) : Value;
public record BoolValue(bool Val) : Value;
public record NullValue : Value;
public record ArrayValue(List<Value> Values) : Value;

3. Gremlin Transpiler
Create: BahyWay.SharedKernel.Akkadian/Transpilers/GremlinTranspiler.cs

using System.Text;
using BahyWay.SharedKernel.Akkadian.Domain.Ast;

namespace BahyWay.SharedKernel.Akkadian.Transpilers;

public class GremlinTranspiler
{
    public string Transpile(AkkadianQuery query)
    {
        return query.Statement switch
        {
            DiscoverStatement discover => TranspileDiscover(discover),
            CreateStatement create => TranspileCreate(create),
            UpdateStatement update => TranspileUpdate(update),
            DeleteStatement delete => TranspileDelete(delete),
            _ => throw new NotSupportedException($"Statement type {query.Statement.GetType()} not supported")
        };
    }

    private string TranspileDiscover(DiscoverStatement stmt)
    {
        var builder = new StringBuilder("g");

        // Start with vertices
        builder.Append(".V()");

        // Filter by label
        if (stmt.Entity.Label != null)
        {
            builder.Append($".hasLabel('{stmt.Entity.Label}')");
        }

        // Store as variable if aliased
        if (stmt.Entity.Alias != null)
        {
            builder.Append($".as('{stmt.Entity.Alias}')");
        }

        // Handle connections (traversals)
        if (stmt.Connection != null)
        {
            builder.Append(TranspileConnection(stmt.Connection));
        }

        // Apply WHERE filters
        if (stmt.Where != null)
        {
            builder.Append(TranspileWhere(stmt.Where));
        }

        // Apply ORDER BY
        if (stmt.Order != null)
        {
            builder.Append(TranspileOrder(stmt.Order));
        }

        // Apply LIMIT/OFFSET
        if (stmt.Limit != null)
        {
            if (stmt.Limit.Offset.HasValue)
            {
                builder.Append($".range({stmt.Limit.Offset}, {stmt.Limit.Offset + stmt.Limit.Limit})");
            }
            else
            {
                builder.Append($".limit({stmt.Limit.Limit})");
            }
        }

        // Project/Return
        builder.Append(TranspileReturn(stmt.Return));

        return builder.ToString();
    }

    private string TranspileConnection(ConnectionClause connection)
    {
        var builder = new StringBuilder();

        // Determine depth
        var depthModifier = connection.Depth switch
        {
            ExactDepth exact => $".times({exact.Depth})",
            RangeDepth range => "", // Will use repeat().until()
            UnboundedDepth unbounded => "", // Will use repeat().until()
            _ => ""
        };

        // Edge traversal
        if (connection.Relationship.Label != null)
        {
            builder.Append($".outE('{connection.Relationship.Label}')");
        }
        else
        {
            builder.Append(".outE()");
        }

        if (connection.Relationship.Alias != null)
        {
            builder.Append($".as('{connection.Relationship.Alias}')");
        }

        // To target vertex
        builder.Append(".inV()");

        if (connection.TargetEntity.Label != null)
        {
            builder.Append($".hasLabel('{connection.TargetEntity.Label}')");
        }

        if (connection.TargetEntity.Alias != null)
        {
            builder.Append($".as('{connection.TargetEntity.Alias}')");
        }

        return builder.ToString();
    }

    private string TranspileWhere(WhereClause where)
    {
        return TranspileCondition(where.Condition);
    }

    private string TranspileCondition(Condition condition)
    {
        return condition switch
        {
            AndCondition and =>
                $".and({TranspileCondition(and.Left)}, {TranspileCondition(and.Right)})",

            OrCondition or =>
                $".or({TranspileCondition(or.Left)}, {TranspileCondition(or.Right)})",

            NotCondition not =>
                $".not({TranspileCondition(not.Inner)})",

            ComparisonCondition comp => TranspileComparison(comp),

            InCondition inCond =>
                $".has('{inCond.Property.PropertyName}', within({string.Join(", ", inCond.Values.Select(FormatValue))}))",

            IsNullCondition isNull =>
                isNull.IsNot
                    ? $".has('{isNull.Property.PropertyName}')"
                    : $".hasNot('{isNull.Property.PropertyName}')",

            _ => throw new NotSupportedException($"Condition type {condition.GetType()} not supported")
        };
    }

    private string TranspileComparison(ComparisonCondition comp)
    {
        var op = comp.Operator switch
        {
            ComparisonOperator.Equals or ComparisonOperator.Exceeds => "eq",
            ComparisonOperator.NotEquals => "neq",
            ComparisonOperator.GreaterThan => "gt",
            ComparisonOperator.LessThan or ComparisonOperator.Below => "lt",
            ComparisonOperator.GreaterThanOrEqual => "gte",
            ComparisonOperator.LessThanOrEqual => "lte",
            ComparisonOperator.Contains => "containing",
            ComparisonOperator.StartsWith => "startingWith",
            ComparisonOperator.EndsWith => "endingWith",
            _ => throw new NotSupportedException($"Operator {comp.Operator} not supported")
        };

        return $".has('{comp.Property.PropertyName}', {op}({FormatValue(comp.Value)}))";
    }

    private string TranspileOrder(OrderClause order)
    {
        var items = order.Items.Select(item =>
        {
            var dir = item.Direction == SortDirection.Descending ? "desc" : "asc";
            return $"by('{item.Property.PropertyName}', {dir})";
        });

        return $".order().{string.Join(".", items)}";
    }

    private string TranspileReturn(ReturnClause returnClause)
    {
        if (returnClause.Items.Any(i => i is AllReturnItem))
        {
            return ".valueMap(true)"; // Include all properties + id
        }

        var projections = returnClause.Items.Select(item => item switch
        {
            PropertyReturnItem prop => $"'{prop.Alias ?? prop.Property.PropertyName}'",
            IdentifierReturnItem id => $"'{id.Alias ?? id.Identifier}'",
            _ => "*"
        });

        return $".project({string.Join(", ", projections)})";
    }

    private string TranspileCreate(CreateStatement stmt)
    {
        var builder = new StringBuilder("g");

        foreach (var node in stmt.Nodes)
        {
            builder.Append($".addV('{node.Label}')");

            if (node.Properties != null)
            {
                foreach (var (key, value) in node.Properties)
                {
                    builder.Append($".property('{key}', {FormatValue(value)})");
                }
            }

            builder.Append($".as('{node.Alias}')");
        }

        if (stmt.Link != null)
        {
            builder.Append($".select('{stmt.Link.FromNode}')");
            builder.Append($".addE('{stmt.Link.Relationship.Type}')");
            builder.Append($".to(select('{stmt.Link.ToNode}'))");

            if (stmt.Link.Properties != null)
            {
                foreach (var (key, value) in stmt.Link.Properties)
                {
                    builder.Append($".property('{key}', {FormatValue(value)})");
                }
            }
        }

        return builder.ToString();
    }

    private string TranspileUpdate(UpdateStatement stmt)
    {
        var builder = new StringBuilder("g.V()");

        if (stmt.Entity.Label != null)
        {
            builder.Append($".hasLabel('{stmt.Entity.Label}')");
        }

        if (stmt.Where != null)
        {
            builder.Append(TranspileWhere(stmt.Where));
        }

        foreach (var (prop, value) in stmt.Set.Assignments)
        {
            builder.Append($".property('{prop.PropertyName}', {FormatValue(value)})");
        }

        return builder.ToString();
    }

    private string TranspileDelete(DeleteStatement stmt)
    {
        var builder = new StringBuilder("g.V()");

        if (stmt.Entity.Label != null)
        {
            builder.Append($".hasLabel('{stmt.Entity.Label}')");
        }

        if (stmt.Where != null)
        {
            builder.Append(TranspileWhere(stmt.Where));
        }

        builder.Append(".drop()");

        return builder.ToString();
    }

    private string FormatValue(Value value)
    {
        return value switch
        {
            StringValue s => $"'{s.Val.Replace("'", "\\'")}'",
            IntValue i => i.Val.ToString(),
            FloatValue f => f.Val.ToString("F2"),
            BoolValue b => b.Val.ToString().ToLower(),
            NullValue => "null",
            ArrayValue a => $"[{string.Join(", ", a.Values.Select(FormatValue))}]",
            _ => throw new NotSupportedException($"Value type {value.GetType()} not supported")
        };
    }
}

4. Apache AGE (OpenCypher) Transpiler
Create: BahyWay.SharedKernel.Akkadian/Transpilers/AgeCypherTranspiler.cs

using System.Text;
using BahyWay.SharedKernel.Akkadian.Domain.Ast;

namespace BahyWay.SharedKernel.Akkadian.Transpilers;

public class AgeCypherTranspiler
{
    public string Transpile(AkkadianQuery query)
    {
        return query.Statement switch
        {
            DiscoverStatement discover => TranspileDiscover(discover),
            CreateStatement create => TranspileCreate(create),
            UpdateStatement update => TranspileUpdate(update),
            DeleteStatement delete => TranspileDelete(delete),
            _ => throw new NotSupportedException($"Statement type {query.Statement.GetType()} not supported")
        };
    }

    private string TranspileDiscover(DiscoverStatement stmt)
    {
        var builder = new StringBuilder();

        // MATCH clause
        builder.Append("MATCH ");
        builder.Append(TranspileEntityPattern(stmt.Entity));

        if (stmt.Connection != null)
        {
            builder.Append(TranspileConnection(stmt.Connection));
        }

        // WHERE clause
        if (stmt.Where != null)
        {
            builder.Append(" WHERE ");
            builder.Append(TranspileCondition(stmt.Where.Condition));
        }

        // RETURN clause
        builder.Append(" RETURN ");
        builder.Append(TranspileReturn(stmt.Return));

        // ORDER BY
        if (stmt.Order != null)
        {
            builder.Append(" ORDER BY ");
            var items = stmt.Order.Items.Select(item =>
            {
                var dir = item.Direction == SortDirection.Descending ? "DESC" : "ASC";
                return $"{item.Property.EntityAlias}.{item.Property.PropertyName} {dir}";
            });
            builder.Append(string.Join(", ", items));
        }

        // LIMIT/OFFSET
        if (stmt.Limit != null)
        {
            if (stmt.Limit.Offset.HasValue)
            {
                builder.Append($" SKIP {stmt.Limit.Offset}");
            }
            builder.Append($" LIMIT {stmt.Limit.Limit}");
        }

        return builder.ToString();
    }

    private string TranspileEntityPattern(EntityPattern entity)
    {
        var label = entity.Label != null ? $":{entity.Label}" : "";
        var alias = entity.Alias ?? entity.NodeType.ToLower();
        return $"({alias}{label})";
    }

    private string TranspileConnection(ConnectionClause connection)
    {
        var builder = new StringBuilder();

        // Relationship
        var relLabel = connection.Relationship.Label != null ? $":{connection.Relationship.Label}" : "";
        var relAlias = connection.Relationship.Alias ?? "";

        // Depth specifier
        var depth = connection.Depth switch
        {
            ExactDepth exact => $"*{exact.Depth}",
            RangeDepth range => $"*{range.Min}..{range.Max}",
            UnboundedDepth unbounded => $"*{unbounded.Min}..",
            _ => ""
        };

        builder.Append($"-[{relAlias}{relLabel}{depth}]->");
        builder.Append(TranspileEntityPattern(connection.TargetEntity));

        return builder.ToString();
    }

    private string TranspileCondition(Condition condition)
    {
        return condition switch
        {
            AndCondition and =>
                $"({TranspileCondition(and.Left)} AND {TranspileCondition(and.Right)})",

            OrCondition or =>
                $"({TranspileCondition(or.Left)} OR {TranspileCondition(or.Right)})",

            NotCondition not =>
                $"NOT ({TranspileCondition(not.Inner)})",

            ComparisonCondition comp => TranspileComparison(comp),

            InCondition inCond =>
                $"{inCond.Property.EntityAlias}.{inCond.Property.PropertyName} IN [{string.Join(", ", inCond.Values.Select(FormatValue))}]",

            IsNullCondition isNull =>
                isNull.IsNot
                    ? $"{isNull.Property.EntityAlias}.{isNull.Property.PropertyName} IS NOT NULL"
                    : $"{isNull.Property.EntityAlias}.{isNull.Property.PropertyName} IS NULL",

            FunctionCondition func =>
                $"{TranspileFunctionCall(func.Function)} {MapOperator(func.Operator)} {FormatValue(func.Value)}",

            _ => throw new NotSupportedException($"Condition type {condition.GetType()} not supported")
        };
    }

    private string TranspileComparison(ComparisonCondition comp)
    {
        var op = MapOperator(comp.Operator);
        return $"{comp.Property.EntityAlias}.{comp.Property.PropertyName} {op} {FormatValue(comp.Value)}";
    }

    private string MapOperator(ComparisonOperator op)
    {
        return op switch
        {
            ComparisonOperator.Equals => "=",
            ComparisonOperator.NotEquals => "<>",
            ComparisonOperator.GreaterThan or ComparisonOperator.Exceeds => ">",
            ComparisonOperator.LessThan or ComparisonOperator.Below => "<",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThanOrEqual => "<=",
            ComparisonOperator.Contains => "CONTAINS",
            ComparisonOperator.StartsWith => "STARTS WITH",
            ComparisonOperator.EndsWith => "ENDS WITH",
            ComparisonOperator.Matches => "=~",
            _ => throw new NotSupportedException($"Operator {op} not supported")
        };
    }

    private string TranspileFunctionCall(FunctionCall func)
    {
        var arg = func.Argument switch
        {
            PropertyArgument prop => $"{prop.Property.EntityAlias}.{prop.Property.PropertyName}",
            AllArgument => "*",
            _ => throw new NotSupportedException()
        };

        return $"{func.Name.ToUpper()}({arg})";
    }

    private string TranspileReturn(ReturnClause returnClause)
    {
        if (returnClause.Items.Any(i => i is AllReturnItem))
        {
            return "*";
        }

        var items = returnClause.Items.Select(item => item switch
        {
            PropertyReturnItem prop =>
                prop.Alias != null
                    ? $"{prop.Property.EntityAlias}.{prop.Property.PropertyName} AS {prop.Alias}"
                    : $"{prop.Property.EntityAlias}.{prop.Property.PropertyName}",

            FunctionReturnItem func =>
                func.Alias != null
                    ? $"{TranspileFunctionCall(func.Function)} AS {func.Alias}"
                    : TranspileFunctionCall(func.Function),

            IdentifierReturnItem id =>
                id.Alias != null
                    ? $"{id.Identifier} AS {id.Alias}"
                    : id.Identifier,

            _ => "*"
        });

        return string.Join(", ", items);
    }

    private string TranspileCreate(CreateStatement stmt)
    {
        var builder = new StringBuilder("CREATE ");

        // Nodes
        for (int i = 0; i < stmt.Nodes.Count; i++)
        {
            if (i > 0) builder.Append(", ");

            var node = stmt.Nodes[i];
            builder.Append($"({node.Alias}:{node.Label}");

            if (node.Properties != null && node.Properties.Any())
            {
                builder.Append(" {");
                builder.Append(string.Join(", ", node.Properties.Select(kv => $"{kv.Key}: {FormatValue(kv.Value)}")));
                builder.Append("}");
            }

            builder.Append(")");
        }

        // Link
        if (stmt.Link != null)
        {
            builder.Append($", ({stmt.Link.FromNode})-[:{stmt.Link.Relationship.Type}");

            if (stmt.Link.Properties != null && stmt.Link.Properties.Any())
            {
                builder.Append(" {");
                builder.Append(string.Join(", ", stmt.Link.Properties.Select(kv => $"{kv.Key}: {FormatValue(kv.Value)}")));
                builder.Append("}");
            }

            builder.Append($"]->({stmt.Link.ToNode})");
        }

        return builder.ToString();
    }

    private string TranspileUpdate(UpdateStatement stmt)
    {
        var builder = new StringBuilder("MATCH ");
        builder.Append(TranspileEntityPattern(stmt.Entity));

        if (stmt.Where != null)
        {
            builder.Append(" WHERE ");
            builder.Append(TranspileCondition(stmt.Where.Condition));
        }

        builder.Append(" SET ");
        var assignments = stmt.Set.Assignments.Select(kv =>
            $"{kv.Key.EntityAlias}.{kv.Key.PropertyName} = {FormatValue(kv.Value)}"
        );
        builder.Append(string.Join(", ", assignments));

        return builder.ToString();
    }

    private string TranspileDelete(DeleteStatement stmt)
    {
        var builder = new StringBuilder("MATCH ");
        builder.Append(TranspileEntityPattern(stmt.Entity));

        if (stmt.Where != null)
        {
            builder.Append(" WHERE ");
            builder.Append(TranspileCondition(stmt.Where.Condition));
        }

        builder.Append($" DELETE {stmt.Entity.Alias ?? stmt.Entity.NodeType.ToLower()}");

        return builder.ToString();
    }

    private string FormatValue(Value value)
    {
        return value switch
        {
            StringValue s => $"'{s.Val.Replace("'", "\\'")}'",
            IntValue i => i.Val.ToString(),
            FloatValue f => f.Val.ToString("F2"),
            BoolValue b => b.Val.ToString().ToLower(),
            NullValue => "NULL",
            ArrayValue a => $"[{string.Join(", ", a.Values.Select(FormatValue))}]",
            _ => throw new NotSupportedException($"Value type {value.GetType()} not supported")
        };
    }
}

5. Example Queries
// Example 1: Simple discovery
DISCOVER Person AS p
WHERE p.age EXCEEDS 30
RETURN p.name, p.email

// Transpiles to Cypher:
MATCH (p:Person)
WHERE p.age > 30
RETURN p.name, p.email

// Example 2: Connected entities
DISCOVER Customer AS c
CONNECTED_TO Order AS o VIA PLACED
WHERE o.total EXCEEDS 1000
RETURN c.name, o.order_id, o.total
ORDER BY o.total DESC
LIMIT 10

// Transpiles to Cypher:
MATCH (c:Customer)-[:PLACED]->(o:Order)
WHERE o.total > 1000
RETURN c.name, o.order_id, o.total
ORDER BY o.total DESC
LIMIT 10

// Example 3: Multi-hop traversal
DISCOVER Person AS p
CONNECTED_TO Company AS c VIA WORKS_AT WITH_DEPTH 1 TO 3
WHERE c.industry = 'Technology'
RETURN p.name, c.name

// Transpiles to Cypher:
MATCH (p:Person)-[:WORKS_AT*1..3]->(c:Company)
WHERE c.industry = 'Technology'
RETURN p.name, c.name

// Example 4: Create nodes and relationship
CREATE (p:Person {name: 'Alice', age: 30}),
       (c:Company {name: 'TechCorp'})
LINK p TO c VIA WORKS_AT {since: 2020}

// Transpiles to Cypher:
CREATE (p:Person {name: 'Alice', age: 30}),
       (c:Company {name: 'TechCorp'}),
       (p)-[:WORKS_AT {since: 2020}]->(c)

// Example 5: Update
UPDATE Customer AS c
SET c.status = 'VIP', c.updated_at = '2025-01-01'
WHERE c.total_orders EXCEEDS 100

// Transpiles to Cypher:
MATCH (c:Customer)
WHERE c.total_orders > 100
SET c.status = 'VIP', c.updated_at = '2025-01-01'

This is the complete foundation! Would you like me to continue with:

Parser implementation (using ANTLR visitor pattern)
Unit tests for the transpilers
Integration with PostgreSQL + Apache AGE
Visual Studio Code extension for Akkadian syntax highlighting
Let me know which part you'd like next!

## Question02:
YES PLEASE I WANT THEM ALL: This is the complete foundation! Would you like me to continue with:

Parser implementation (using ANTLR visitor pattern)
Unit tests for the transpilers
Integration with PostgreSQL + Apache AGE
Visual Studio Code extension for Akkadian syntax highlighting

## Answer02:
