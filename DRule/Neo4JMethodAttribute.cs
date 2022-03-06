namespace DRule;

public class Neo4JMethodAttribute : Attribute
{
    public Type Convertor { get; set; }

    public Neo4JMethodAttribute(Type convertor)
    {
        Convertor = convertor;
    }
}