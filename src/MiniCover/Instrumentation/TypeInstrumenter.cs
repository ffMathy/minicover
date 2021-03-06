﻿using System.Linq;
using MiniCover.Extensions;
using MiniCover.Model;
using Mono.Cecil;

namespace MiniCover.Instrumentation
{
    public class TypeInstrumenter
    {
        private readonly MethodInstrumenter _methodInstrumenter;

        public TypeInstrumenter(MethodInstrumenter methodInstrumenter)
        {
            _methodInstrumenter = methodInstrumenter;
        }

        public void InstrumentType(
            InstrumentationContext context,
            TypeDefinition typeDefinition,
            InstrumentedAssembly instrumentedAssembly)
        {
            foreach (var methodDefinition in typeDefinition.Methods)
            {
                if (!methodDefinition.HasBody || !methodDefinition.DebugInformation.HasSequencePoints)
                    continue;

                var methodDocuments = methodDefinition.GetAllDocuments();

                var isSource = methodDocuments.Any(d => context.IsSource(d.Url));
                var isTest = methodDocuments.Any(d => context.IsTest(d.Url));

                if (!isSource && !isTest)
                    continue;

                _methodInstrumenter.InstrumentMethod(
                    context,
                    isSource,
                    methodDefinition,
                    instrumentedAssembly);
            }

            foreach (var nestedType in typeDefinition.NestedTypes)
                InstrumentType(context, nestedType, instrumentedAssembly);
        }
    }
}
