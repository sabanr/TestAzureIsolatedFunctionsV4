using IntegracionPowTest.Pow.EntidadesPow;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;

namespace IntegracionPowTest.Pow;

public interface IApiPow {

    public Task InformarStockYPreciosAsync(NovedadPow novedadPow);

}