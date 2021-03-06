﻿using Jasper;
using Jasper.Bus.Transports.Configuration;

namespace benchmarks
{
    public class Receiver1 : JasperRegistry
    {
        public Receiver1()
        {
            Services.Scan(_ =>
            {
                _.TheCallingAssembly();
                _.WithDefaultConventions();
            });

            Transports.LightweightListenerAt(2233);
        }
    }
}
