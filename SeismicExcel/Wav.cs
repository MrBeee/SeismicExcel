using System;
using ExcelDna.Integration;

namespace SeismicExcel
{
    public static class Wav
    {
        [ExcelFunction(Name = "SE.wav.Klauder", Description = "Compute a Klauder wavelet for the supplied time samples")]
        public static object[,] Klauder(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "Fmin", Description = "Minimum frequency")] double Fmin,
            [ExcelArgument(Name = "Fmax", Description = "Maximum frequency")] double Fmax,
            [ExcelArgument(Name = "Tsweep", Description = "Sweep length")] double Tsweep)
        {
            const double PI = 3.141592653589793238;

            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            if (Tsweep == 0.0)
            {
                result[0, 0] = "#ERR: Tsweep must be non-zero.";
                return result;
            }

            if (Fmin == Fmax)
            {
                result[0, 0] = "#ERR: Fmin and Fmax must be different.";
                return result;
            }

            double p = PI * (Fmin + Fmax);
            double a = PI * (Fmax - Fmin) / Tsweep;

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else if (t == 0.0)
                    {
                        result[i, j] = 1.0;
                    }
                    else
                    {
                        result[i, j] = Math.Sin((Tsweep - t) * a * t) * Math.Cos(p * t) / (a * t * Tsweep);
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.LinSweep", Description = "Compute a linear sweep for the supplied time samples")]
        public static object[,] LinSweep(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "Fstart", Description = "Start frequency")] double Fstart,
            [ExcelArgument(Name = "Fd", Description = "Maximum displacement frequency")] double Fd,
            [ExcelArgument(Name = "Fstop", Description = "Stop frequency")] double Fstop,
            [ExcelArgument(Name = "Tsweep", Description = "Sweep length")] double Tsweep,
            [ExcelArgument(Name = "TstartTaper", Description = "Start taper length")] double TstartTaper,
            [ExcelArgument(Name = "TstopTaper", Description = "Stop taper length")] double TstopTaper,
            [ExcelArgument(Name = "Fnyquist", Description = "Optional Nyquist frequency. Leave empty or 0 to disable alias protection.")] object Fnyquist)
        {
            const double PI = 3.141592653589793238;

            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            double fn = ExcelValueConverters.ToDouble(Fnyquist);
            double fmin = Fstart < Fstop ? Fstart : Fstop;

            if (Tsweep < 0.0 || TstartTaper < 0.0 || TstopTaper < 0.0)
            {
                result[0, 0] = "#ERR: Tsweep and taper values must be >= 0.";
                return result;
            }

            if (TstartTaper + TstopTaper > Tsweep)
            {
                result[0, 0] = "#ERR: Tsweep must be >= TstartTaper + TstopTaper.";
                return result;
            }

            double t1 = TstartTaper;
            double t2 = Tsweep - TstopTaper;
            double t3 = Tsweep;
            double fd = Fd < fmin ? fmin : Fd;

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else
                    {
                        double f = 0.0;
                        double a = 1.0;
                        double p = 0.0;
                        double y = 0.0;

                        if (t <= 0.0 || t >= Tsweep)
                        {
                            y = 0.0;
                        }
                        else
                        {
                            f = Fstart + t * (Fstop - Fstart) / t3;
                            if (f < fd)
                                a = (f * f) / (fd * fd);

                            p = 2.0 * PI * (Fstart + (0.5 * t / t3) * (Fstop - Fstart)) * t;
                            y = a * Math.Sin(p);

                            if (t > 0.0 && t < t1)
                            {
                                y *= 0.5 * (1.0 - Math.Cos(PI * t / t1));
                            }
                            else if (t > t2 && t < t3)
                            {
                                y *= 0.5 * (1.0 + Math.Cos(PI * (t - t2) / (t3 - t2)));
                            }

                            if (fn > 0.0)
                            {
                                if (f > fn)
                                {
                                    y = 0.0;
                                }
                                else if (f / fn > 0.8)
                                {
                                    y *= 1.0 - (f / fn - 0.8) / 0.2;
                                }
                            }
                        }

                        result[i, j] = y;
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.LFSweep", Description = "Compute a low-frequency sweep for the supplied time samples")]
        public static object[,] LFSweep(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "Fmin", Description = "Minimum frequency")] double Fmin,
            [ExcelArgument(Name = "Fd", Description = "Maximum displacement frequency")] double Fd,
            [ExcelArgument(Name = "Fmax", Description = "Maximum frequency")] double Fmax,
            [ExcelArgument(Name = "Tsweep", Description = "Sweep length")] double Tsweep,
            [ExcelArgument(Name = "TstartTaper", Description = "Start taper length")] double TstartTaper,
            [ExcelArgument(Name = "TstopTaper", Description = "Stop taper length")] double TstopTaper,
            [ExcelArgument(Name = "Fnyquist", Description = "Optional Nyquist frequency. Leave empty or 0 to disable alias protection.")] object Fnyquist)
        {
            const double PI = 3.141592653589793238;

            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            double fn = Fnyquist is ExcelMissing || Fnyquist is ExcelEmpty ? 0.0 : Convert.ToDouble(Fnyquist);

            if (Tsweep < 0.0 || TstartTaper < 0.0 || TstopTaper < 0.0)
            {
                result[0, 0] = "#ERR: Tsweep and taper values must be >= 0.";
                return result;
            }

            if (TstartTaper + TstopTaper > Tsweep)
            {
                result[0, 0] = "#ERR: Tsweep must be >= TstartTaper + TstopTaper.";
                return result;
            }

            double t1 = TstartTaper;
            double t2 = Tsweep - TstopTaper;
            double t3 = Tsweep;
            double fd = Fd < Fmin ? Fmin : Fd;

            double b;
            double tNon;
            double tLin;
            double phi0;
            double phiD;

            if (fd == Fmin)
            {
                b = 0.0;
                tNon = 0.0;
                tLin = Tsweep;
                phi0 = 0.0;
                phiD = 0.0;
            }
            else
            {
                b = Math.Pow(fd, -3.0) - Math.Pow(Fmin, -3.0);
                double c = -3.0 * (Fmax - fd) / (Math.Pow(fd, 4.0) * b);
                tNon = Tsweep / (1.0 + c);
                tLin = tNon * c;

                phi0 = 3.0 * PI * tNon * Math.Pow(Fmin, -2.0) / b;
                phiD = 2.0 * PI * (tNon * Math.Pow(b + Math.Pow(Fmin, -3.0), 2.0 / 3.0) / (b * 2.0 / 3.0)) - phi0;
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else
                    {
                        double f = 0.0;
                        double a = 0.0;
                        double p = 0.0;
                        double y = 0.0;

                        if (t <= 0.0 || t >= Tsweep)
                        {
                            y = 0.0;
                        }
                        else if (t < tNon)
                        {
                            f = Math.Pow(b * t / tNon + Math.Pow(Fmin, -3.0), -1.0 / 3.0);
                            a = (f * f) / (fd * fd);
                            p = 2.0 * PI * (tNon * Math.Pow(b * t / tNon + Math.Pow(Fmin, -3.0), 2.0 / 3.0) / (b * 2.0 / 3.0)) - phi0;
                            y = a * Math.Sin(p);
                        }
                        else
                        {
                            f = fd + (t - tNon) * (Fmax - fd) / tLin;
                            p = 2.0 * PI * (fd + (0.5 * (t - tNon) / tLin) * (Fmax - fd)) * (t - tNon) + phiD;
                            y = Math.Sin(p);
                        }

                        if (t > 0.0 && t < t1)
                        {
                            y *= 0.5 * (1.0 - Math.Cos(PI * t / t1));
                        }
                        else if (t > t2 && t < t3)
                        {
                            y *= 0.5 * (1.0 + Math.Cos(PI * (t - t2) / (t3 - t2)));
                        }

                        if (fn > 0.0)
                        {
                            if (f > fn)
                            {
                                y = 0.0;
                            }
                            else if (f / fn > 0.8)
                            {
                                y *= 1.0 - (f / fn - 0.8) / 0.2;
                            }
                        }

                        result[i, j] = y;
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.LFSweepIF", Description = "Compute instantaneous frequency for a low-frequency sweep")]
        public static object[,] LFSweepIF(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "Fmin", Description = "Minimum frequency")] double Fmin,
            [ExcelArgument(Name = "Fd", Description = "Maximum displacement frequency")] double Fd,
            [ExcelArgument(Name = "Fmax", Description = "Maximum frequency")] double Fmax,
            [ExcelArgument(Name = "Tsweep", Description = "Sweep length")] double Tsweep,
            [ExcelArgument(Name = "mode", Description = "Optional out-of-range mode. 0 = return 0, 1 = return #N/A, 2 = return #NUM!")] object mode = null)
        {
            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            int nMode = ExcelValueConverters.ToInt32(mode);
            if (nMode < 0 || nMode > 2)
            {
                result[0, 0] = "#ERR: mode must be 0, 1 or 2.";
                return result;
            }

            if (Tsweep <= 0.0)
            {
                result[0, 0] = "#ERR: Tsweep must be > 0.";
                return result;
            }

            double fd = Fd < Fmin ? Fmin : Fd;

            double b;
            double tNon;
            double tLin;

            if (fd == Fmin)
            {
                b = 0.0;
                tNon = 0.0;
                tLin = Tsweep;
            }
            else
            {
                b = Math.Pow(fd, -3.0) - Math.Pow(Fmin, -3.0);
                double c = -3.0 * (Fmax - fd) / (Math.Pow(fd, 4.0) * b);
                tNon = Tsweep / (1.0 + c);
                tLin = tNon * c;
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else if (t < 0.0 || t > Tsweep)
                    {
                        switch (nMode)
                        {
                            default:
                            case 0:
                                result[i, j] = 0.0;
                                break;
                            case 1:
                                result[i, j] = ExcelError.ExcelErrorNA;
                                break;
                            case 2:
                                result[i, j] = ExcelError.ExcelErrorNum;
                                break;
                        }
                    }
                    else if (t < tNon)
                    {
                        result[i, j] = Math.Pow(b * t / tNon + Math.Pow(Fmin, -3.0), -1.0 / 3.0);
                    }
                    else
                    {
                        result[i, j] = fd + ((t - tNon) / tLin) * (Fmax - fd);
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.LFSweepIT", Description = "Compute instantaneous time for a low-frequency sweep")]
        public static object[,] LFSweepIT(
            [ExcelArgument(Name = "freq", Description = "Frequency samples as an Excel range or single value")] object freq,
            [ExcelArgument(Name = "Fmin", Description = "Minimum frequency")] double Fmin,
            [ExcelArgument(Name = "Fd", Description = "Transition frequency")] double Fd,
            [ExcelArgument(Name = "Fmax", Description = "Maximum frequency")] double Fmax,
            [ExcelArgument(Name = "Tsweep", Description = "Sweep length")] double Tsweep,
            [ExcelArgument(Name = "mode", Description = "Optional out-of-range mode. 0 = return 0, 1 = return #N/A, 2 = return #NUM!")] object mode = null)
        {
            object[,] freqValues = To2DArray(freq);

            int nRows = freqValues.GetLength(0);
            int nCols = freqValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            int nMode = ExcelValueConverters.ToInt32(mode);
            if (nMode < 0 || nMode > 2)
            {
                result[0, 0] = "#ERR: mode must be 0, 1 or 2.";
                return result;
            }

            if (Tsweep <= 0.0)
            {
                result[0, 0] = "#ERR: Tsweep must be > 0.";
                return result;
            }

            double fd = Fd < Fmin ? Fmin : Fd;
            if (Fmax < fd)
            {
                result[0, 0] = "#ERR: Fmax must be >= max(Fmin, Fd).";
                return result;
            }

            double b;
            double tNon;
            double tLin;

            if (fd == Fmin)
            {
                b = 0.0;
                tNon = 0.0;
                tLin = Tsweep;
            }
            else
            {
                b = Math.Pow(fd, -3.0) - Math.Pow(Fmin, -3.0);
                double c = -3.0 * (Fmax - fd) / (Math.Pow(fd, 4.0) * b);
                tNon = Tsweep / (1.0 + c);
                tLin = tNon * c;
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double f;
                    if (!ExcelValueConverters.TryGetDouble(freqValues[i, j], out f))
                    {
                        result[i, j] = "#ERR: Non-numeric value in frequency series.";
                    }
                    else if (double.IsNaN(f) || double.IsInfinity(f))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in frequency series.";
                    }
                    else if (f < Fmin || f > Fmax)
                    {
                        switch (nMode)
                        {
                            default:
                            case 0:
                                result[i, j] = 0.0;
                                break;
                            case 1:
                                result[i, j] = ExcelError.ExcelErrorNA;
                                break;
                            case 2:
                                result[i, j] = ExcelError.ExcelErrorNum;
                                break;
                        }
                    }
                    else if (f < fd)
                    {
                        result[i, j] = tNon * (Math.Pow(f, -3.0) - Math.Pow(Fmin, -3.0)) / b;
                    }
                    else if (Fmax == fd)
                    {
                        result[i, j] = tNon;
                    }
                    else
                    {
                        result[i, j] = tNon + (f - fd) * tLin / (Fmax - fd);
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.Ormsby", Description = "Compute an Ormsby wavelet for the supplied time samples")]
        public static object[,] Ormsby(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "F1", Description = "Low cut frequency")] double F1,
            [ExcelArgument(Name = "F2", Description = "Low pass frequency")] double F2,
            [ExcelArgument(Name = "F3", Description = "High pass frequency")] double F3,
            [ExcelArgument(Name = "F4", Description = "High cut frequency")] double F4)
        {
            const double PI = 3.141592653589793238;

            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            if (F1 == F2)
            {
                result[0, 0] = "#ERR: F1 and F2 must be different.";
                return result;
            }

            if (F3 == F4)
            {
                result[0, 0] = "#ERR: F3 and F4 must be different.";
                return result;
            }

            double c1 = (PI * PI * F1 * F1) / (PI * F2 - PI * F1);
            double c2 = (PI * PI * F2 * F2) / (PI * F2 - PI * F1);
            double c3 = (PI * PI * F3 * F3) / (PI * F4 - PI * F3);
            double c4 = (PI * PI * F4 * F4) / (PI * F4 - PI * F3);

            double normalization = c4 - c3 - c2 + c1;
            if (normalization == 0.0)
            {
                result[0, 0] = "#ERR: Invalid frequency combination.";
                return result;
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else if (t == 0.0)
                    {
                        result[i, j] = 1.0;
                    }
                    else
                    {
                        double s1 = Sinc(PI * F1 * t);
                        double s2 = Sinc(PI * F2 * t);
                        double s3 = Sinc(PI * F3 * t);
                        double s4 = Sinc(PI * F4 * t);

                        result[i, j] =
                            (c4 * s4 * s4 -
                             c3 * s3 * s3 -
                             c2 * s2 * s2 +
                             c1 * s1 * s1) / normalization;
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.Ricker", Description = "Compute a Ricker wavelet for the supplied time sample(s)")]
        public static object[,] Ricker(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "Fmain", Description = "Main frequency")] double Fmain)
        {
            const double PI = 3.141592653589793238;

            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else
                    {
                        double x = PI * PI * Fmain * Fmain * t * t;
                        result[i, j] = (1.0 - 2.0 * x) * Math.Exp(-x);
                    }
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.wav.Sinc", Description = "Compute a sinc wavelet for the supplied time samples")]
        public static object[,] Sinc(
            [ExcelArgument(Name = "time", Description = "Time samples as an Excel range or single value")] object time,
            [ExcelArgument(Name = "Fmax", Description = "Maximum frequency")] double Fmax)
        {
            const double PI = 3.141592653589793238;

            object[,] timeValues = To2DArray(time);

            int nRows = timeValues.GetLength(0);
            int nCols = timeValues.GetLength(1);
            object[,] result = new object[nRows, nCols];

            if (Fmax == 0.0)
            {
                result[0, 0] = "#ERR: Fmax must be non-zero.";
                return result;
            }

            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    double t;
                    if (!ExcelValueConverters.TryGetDouble(timeValues[i, j], out t))
                    {
                        result[i, j] = "#ERR: Non-numeric value in time series.";
                    }
                    else if (double.IsNaN(t) || double.IsInfinity(t))
                    {
                        result[i, j] = "#ERR: Invalid numeric value in time series.";
                    }
                    else if (t == 0.0)
                    {
                        result[i, j] = 1.0;
                    }
                    else
                    {
                        double x = 2.0 * PI * Fmax * t;
                        result[i, j] = Math.Sin(x) / x;
                    }
                }
            }

            return result;
        }

        private static object[,] To2DArray(object value)
        {
            object[,] array = value as object[,];
            if (array != null)
                return array;

            object[,] singleValue = new object[1, 1];
            singleValue[0, 0] = value;
            return singleValue;
        }

        private static double Sinc(double x)
        {
            if (x == 0.0)
                return 1.0;

            return Math.Sin(x) / x;
        }
    }
}