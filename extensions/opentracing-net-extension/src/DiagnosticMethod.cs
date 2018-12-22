using System;
using System.Reflection;

namespace OpentracingExtension
{
    public class DiagnosticMethod
    {
        private IDiagnosticProcessor processor;
        private MethodInfo method;
        private string diagnosticName;

        public DiagnosticMethod(IDiagnosticProcessor processor, MethodInfo method, string diagnosticName)
        {
            this.processor = processor;
            this.method = method;
            this.diagnosticName = diagnosticName;
        }

        public void Invoke(string diagnosticName, object value)
        {
            if(this.diagnosticName != diagnosticName)
            {
                return;
            }

            try
            {
                this.method.Invoke(this.processor, new object[]{value});
            }
            catch{}
        }
    }
}
