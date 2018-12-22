using System;

namespace OpentracingExtension
{
    public class DiagnosticNameAttribute : Attribute
    {
        public string Name { get; }

        public DiagnosticNameAttribute(string name)
        {
            Name = name;
        }
    }
}
