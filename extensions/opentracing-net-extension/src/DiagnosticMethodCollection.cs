using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace OpentracingExtension
{
    public class DiagnosticMethodCollection : IEnumerable<DiagnosticMethod>
    {
        private List<DiagnosticMethod> methods = new List<DiagnosticMethod>();

        public DiagnosticMethodCollection(IDiagnosticProcessor processor)
        {
            if(processor != null)
            {
                foreach(var method in processor.GetType().GetMethods())
                {
                    var attribute = method.GetCustomAttribute<DiagnosticNameAttribute>();
                    if(attribute == null)
                    {
                        continue;
                    }

                    this.methods.Add(new DiagnosticMethod(processor, method, attribute.Name));
                }
            }
        }

        public IEnumerator<DiagnosticMethod> GetEnumerator()
        {
            return this.methods.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.methods.GetEnumerator();
        }
    }
}
