using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NodeMenuAttribute : Attribute
{
    public string Path { get; }

    public NodeMenuAttribute(string path)
    {
        Path = path;
    }
}
