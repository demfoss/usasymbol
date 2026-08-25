using USASymbol.Models;

namespace usasymbol.Services.Interface
{
    public interface IAreaCodeService
    {
        AreaCodeResult? GetByAreaCode(string areaCode);
    }
}
