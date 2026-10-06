using System;
using ExcelDna.Integration;

namespace SeismicExcel
{
    public static class Taper
    {
        [ExcelFunction(Name = "SE.tpr.Hamming1D", Description = "Generate a full Hamming taper as a vertical spilled array of length N.")]
        public static object[,] Hamming1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            return CreateWindow(n, HammingValue, ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.Hanning1D", Description = "Generate a full Hanning taper as a vertical spilled array of length N.")]
        public static object[,] Hanning1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            return CreateWindow(n, HanningValue, ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.Blackman1D", Description = "Generate a full Blackman taper as a vertical spilled array of length N.")]
        public static object[,] Blackman1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            return CreateWindow(n, BlackmanValue, ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.Cosine1D", Description = "Generate a full cosine taper as a vertical spilled array of length N.")]
        public static object[,] Cosine1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            return CreateWindow(n, CosineValue, ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.BlackmanHarris1D", Description = "Generate a full Blackman-Harris taper as a vertical spilled array of length N.")]
        public static object[,] BlackmanHarris1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            return CreateWindow(n, BlackmanHarrisValue, ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.Bartlett1D", Description = "Generate a full Bartlett taper as a vertical spilled array of length N.")]
        public static object[,] Bartlett1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            return CreateWindow(n, BartlettValue, ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.TukeyTails1D", Description = "Generate a Tukey-style center lobe as a vertical spilled array of length N. Alpha is optional and defaults to 0.5.")]
        public static object[,] TukeyTails1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "alpha", Description = "Optional active-width parameter. Default = 0.5")] object alpha = null,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            double a = alpha is ExcelMissing || alpha is ExcelEmpty || alpha == null
                ? 0.5
                : Convert.ToDouble(alpha);

            if (a < 0.0 || a > 1.0)
                return ErrorResult("alpha must be in the range [0, 1].");

            return CreateWindow(n, (i, length) => TukeyTailsValue(i, length, a), ExcelValueConverters.ToBoolean(symmetric));
        }

        [ExcelFunction(Name = "SE.tpr.TukeyCenter1D", Description = "Generate a standard Tukey taper as a vertical spilled array of length N. Alpha is optional and defaults to 0.5.")]
        public static object[,] TukeyCenter1D(
            [ExcelArgument(Name = "N", Description = "Total number of samples in the taper")] double N,
            [ExcelArgument(Name = "alpha", Description = "Optional Tukey shape parameter. Default = 0.5")] object alpha = null,
            [ExcelArgument(Name = "symmetric", Description = "Optional flag. Default = FALSE. TRUE computes a symmetric window; FALSE computes a periodic window using N+1 internally.")] object symmetric = null)
        {
            int n = ToLength(N);
            if (n <= 1)
                return ErrorResult("N must be greater than 1.");

            double a = alpha is ExcelMissing || alpha is ExcelEmpty || alpha == null
                ? 0.5
                : Convert.ToDouble(alpha);

            if (a < 0.0 || a > 1.0)
                return ErrorResult("alpha must be in the range [0, 1].");

            return CreateWindow(n, (i, length) => TukeyCenterValue(i, length, a), ExcelValueConverters.ToBoolean(symmetric));
        }

        private static object[,] CreateWindow(int n, Func<int, int, double> valueFunction, bool symmetric)
        {
            object[,] result = new object[n, 1];
            int valueLength = symmetric ? n : n + 1;

            for (int i = 0; i < n; i++)
            {
                result[i, 0] = valueFunction(i, valueLength);
            }

            return result;
        }

        private static object[,] ErrorResult(string message)
        {
            return new object[,] { { "#ERR: " + message } };
        }

        private static int ToLength(double n)
        {
            return (int)Math.Round(n);
        }

        private static double HammingValue(int i, int n)
        {
            double x = 2.0 * Math.PI * i / (n - 1.0);
            return 25.0 / 46.0 - 21.0 / 46.0 * Math.Cos(x);
        }

        private static double HanningValue(int i, int n)
        {
            double x = 2.0 * Math.PI * i / (n - 1.0);
            return 0.5 - 0.5 * Math.Cos(x);
        }

        private static double BlackmanValue(int i, int n)
        {
            double x = 2.0 * Math.PI * i / (n - 1.0);
            return 0.42 - 0.5 * Math.Cos(x) + 0.08 * Math.Cos(2.0 * x);
        }

        private static double CosineValue(int i, int n)
        {
            return Math.Sin(Math.PI * i / (n - 1.0));
        }

        private static double BlackmanHarrisValue(int i, int n)
        {
            double x = 2.0 * Math.PI * i / (n - 1.0);
            return 0.35875
                 - 0.48829 * Math.Cos(x)
                 + 0.14128 * Math.Cos(2.0 * x)
                 - 0.01168 * Math.Cos(3.0 * x);
        }

        private static double BartlettValue(int i, int n)
        {
            double half = (n - 1.0) / 2.0;
            return 1.0 - Math.Abs((i - half) / half);
        }

        private static double TukeyTailsValue(int i, int n, double alpha)
        {
            if (alpha <= 0.0)
                return 0.0;

            if (alpha > 1.0)
                alpha = 1.0;

            double total = n - 1.0;
            double activeWidth = alpha * total;
            double left = 0.5 * (total - activeWidth);
            double right = left + activeWidth;

            if (i < left || i > right)
                return 0.0;

            if (activeWidth < 1e-12)
                return 0.0;

            return 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * (i - left) / activeWidth);
        }

        private static double TukeyCenterValue(int i, int n, double alpha)
        {
            double half = 0.5 * alpha * (n - 1.0);

            if (half < 1e-12)
                return 1.0;

            if (i <= half)
                return 0.5 * (1.0 + Math.Cos(Math.PI * (i / half - 1.0)));

            if (i >= (n - 1.0) - half)
                return 0.5 * (1.0 + Math.Cos(Math.PI * (((n - 1.0) - i) / half - 1.0)));

            return 1.0;
        }
    }
}