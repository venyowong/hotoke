using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace OpentracingExtension
{
    public class DiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        private ConcurrentBag<IDiagnosticProcessor> processors = 
            new ConcurrentBag<IDiagnosticProcessor>();

        public void AddProcessor(IDiagnosticProcessor processor)
        {
            if(processor != null)
            {
                this.processors.Add(processor);
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            foreach(var processor in this.processors)
            {
                if(value.Name == processor.ListenerName)
                {
                    value.Subscribe(new DiagnosticObserver(processor));
                }
            }
        }
    }
}
