﻿//HintName: Mapper.g.cs
// <auto-generated />
#nullable enable
public partial class Mapper
{
    [global::System.CodeDom.Compiler.GeneratedCode("Riok.Mapperly", "0.0.1.0")]
    partial void Map(global::A src, global::B target, int value)
    {
        target.StringValue = src.StringValue;
        target.Value = value.ToString();
    }
}