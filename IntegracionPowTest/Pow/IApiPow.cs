using IntegracionPowTest.Pow.EntidadesPow;


namespace IntegracionPowTest.Pow;

public interface IApiPow {

    public Task InformarStockYPreciosAsync(NovedadPow novedadPow);

}