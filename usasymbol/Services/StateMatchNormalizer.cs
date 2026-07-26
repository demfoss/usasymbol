using USASymbol.Models.ViewModels;

namespace USASymbol.Services
{
    public interface INormalizer
    {
        double Normalize(
            string metricKey,
            double raw,
            double min,
            double max,
            int direction,
            NormalizationFrame frame);
    }

    /// <summary>
    /// Min-max normalization shared by state and county metric components.
    /// Callers supply the min/max population for the selected frame.
    /// </summary>
    public sealed class StateMatchNormalizer : INormalizer
    {
        public double Normalize(
            string metricKey,
            double raw,
            double min,
            double max,
            int direction,
            NormalizationFrame frame)
        {
            if (max <= min)
                return 0.5;

            var normalized = Math.Clamp((raw - min) / (max - min), 0, 1);
            return direction >= 0 ? normalized : 1 - normalized;
        }
    }
}
