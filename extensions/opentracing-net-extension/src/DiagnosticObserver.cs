using System;
using System.Collections.Generic;

namespace OpentracingExtension
{
    public class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        private DiagnosticMethodCollection methodCollection;

        public DiagnosticObserver(IDiagnosticProcessor processor)
        {
            this.methodCollection = new DiagnosticMethodCollection(processor);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            foreach(var method in this.methodCollection)
            {
                method.Invoke(value.Key, value.Value);
            }
        }
    }
}