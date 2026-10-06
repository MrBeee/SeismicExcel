using ExcelDna.Integration;
using MathNet.Numerics.IntegralTransforms;
using SharpProj;
using System;
using System.Numerics;

namespace SeismicExcel
{
    public static class FourierTransforms
    {
        [ExcelFunction(Name = "SE.fft.FFT1D", Description = "Compute forward FFT of a real or complex vector. Input is n x 1 for real data or n x 2 for complex data [Real, Imaginary].")]
        public static object[,] Fft1D(
            [ExcelArgument(Name = "input", Description = "Input vector with points vertically. Use 1 column for real data, or 2 columns for complex data as [Real, Imaginary].")] object[,] input,
            [ExcelArgument(Name = "AmpPha", Description = "Optional output mode. Default = 0. 0 = [Real, Imaginary], 1 = [Amplitude, Phase], 2 = [Amplitude].")] object AmpPha = null)
        {
            int nRows = input.GetLength(0);
            int nCols = input.GetLength(1);

            if (nCols != 1 && nCols != 2)
            {
                return new object[,] { { "#ERR: Input must have 1 column (real) or 2 columns (real, imaginary)." } };
            }

            int mode = ExcelValueConverters.ToInt32(AmpPha);
            if (mode < 0 || mode > 2)
            {
                return new object[,] { { "#ERR: AmpPha must be 0, 1 or 2." } };
            }

            Complex[] data = new Complex[nRows];

            for (int row = 0; row < nRows; row++)
            {
                double real = ExcelValueConverters.ToDouble(input[row, 0]);
                double imaginary = nCols == 2 ? ExcelValueConverters.ToDouble(input[row, 1]) : 0.0;
                data[row] = new Complex(real, imaginary);
            }

            Fourier.Forward(data, FourierOptions.Matlab);

            object[,] result = mode == 2 ? new object[nRows, 1] : new object[nRows, 2];

            for (int row = 0; row < nRows; row++)
            {
                if (mode == 1)
                {
                    result[row, 0] = data[row].Magnitude;
                    result[row, 1] = Math.Atan2(data[row].Imaginary, data[row].Real);
                }
                else if (mode == 2)
                {
                    result[row, 0] = data[row].Magnitude;
                }
                else
                {
                    result[row, 0] = data[row].Real;
                    result[row, 1] = data[row].Imaginary;
                }
            }

            return result;
        }


        [ExcelFunction(Name = "SE.fft.IFFT1D", Description = "Compute inverse FFT of a real or complex vector. Input is n x 1 for real data or n x 2 for complex data [Real, Imaginary].")]
        public static object[,] IFft1D(
            [ExcelArgument(Name = "input", Description = "Input vector with points vertically. Use 1 column for real data, or 2 columns for complex data as [Real, Imaginary].")] object[,] input,
            [ExcelArgument(Name = "AmpPha", Description = "Optional output mode. Default = 0. 0 = [Real, Imaginary], 1 = [Amplitude, Phase], 2 = [Amplitude].")] object AmpPha = null)
        {
            int nRows = input.GetLength(0);
            int nCols = input.GetLength(1);

            if (nCols != 1 && nCols != 2)
            {
                return new object[,] { { "#ERR: Input must have 1 column (real) or 2 columns (real, imaginary)." } };
            }

            int mode = ExcelValueConverters.ToInt32(AmpPha);
            if (mode < 0 || mode > 2)
            {
                return new object[,] { { "#ERR: AmpPha must be 0, 1 or 2." } };
            }

            Complex[] data = new Complex[nRows];

            for (int row = 0; row < nRows; row++)
            {
                double real;
                if (!ExcelValueConverters.TryGetDouble(input[row, 0], out real))
                {
                    return new object[,] { { "#ERR: Non-numeric value in input." } };
                }

                double imaginary = 0.0;
                if (nCols == 2 && !ExcelValueConverters.TryGetDouble(input[row, 1], out imaginary))
                {
                    return new object[,] { { "#ERR: Non-numeric value in input." } };
                }

                data[row] = new Complex(real, imaginary);
            }

            Fourier.Inverse(data, FourierOptions.Matlab);

            object[,] result = mode == 2 ? new object[nRows, 1] : new object[nRows, 2];

            for (int row = 0; row < nRows; row++)
            {
                if (mode == 1)
                {
                    result[row, 0] = data[row].Magnitude;
                    result[row, 1] = (data[row].Real == 0.0 && data[row].Imaginary == 0.0)
                        ? 0.0
                        : Math.Atan2(data[row].Imaginary, data[row].Real);
                }
                else if (mode == 2)
                {
                    result[row, 0] = data[row].Magnitude;
                }
                else
                {
                    result[row, 0] = data[row].Real;
                    result[row, 1] = data[row].Imaginary;
                }
            }

            return result;
        }

[ExcelFunction(Name = "SE.fft.Correlate", Description = "Circular correlation of two real or complex sequences. Mode string uses T, t or F for reference, inputdata and outputdata respectively.")]
        public static object Correlate(
            [ExcelArgument(Name = "reference", Description = "Reference sequence or spectrum as n x 1 real or n x 2 [Real, Imaginary].")] object[,] reference,
            [ExcelArgument(Name = "inputdata", Description = "Input sequence or spectrum as n x 1 real or n x 2 [Real, Imaginary].")] object[,] inputdata,
            [ExcelArgument(Name = "mode", Description = "Optional mode string. Default = TTT. Use T, t or F for reference, inputdata and outputdata.")] object mode = null,
            [ExcelArgument(Name = "nFFT", Description = "Optional FFT size. Use 0 or leave empty to use the largest input length.")] object nFFT = null)
        {
            return ExecuteBinaryFrequencyOperation(reference, inputdata, mode, nFFT, true);
        }

        [ExcelFunction(Name = "SE.fft.Convolve", Description = "Circular convolution of two real or complex sequences. Mode string uses T, t or F for reference, inputdata and outputdata respectively.")]
        public static object Convolve(
            [ExcelArgument(Name = "reference", Description = "Reference sequence or spectrum as n x 1 real or n x 2 [Real, Imaginary].")] object[,] reference,
            [ExcelArgument(Name = "inputdata", Description = "Input sequence or spectrum as n x 1 real or n x 2 [Real, Imaginary].")] object[,] inputdata,
            [ExcelArgument(Name = "mode", Description = "Optional mode string. Default = TTT. Use T, t or F for reference, inputdata and outputdata.")] object mode = null,
            [ExcelArgument(Name = "nFFT", Description = "Optional FFT size. Use 0 or leave empty to use the largest input length.")] object nFFT = null)
        {
            return ExecuteBinaryFrequencyOperation(reference, inputdata, mode, nFFT, false);
        }

        private static object ExecuteBinaryFrequencyOperation(
            object[,] reference,
            object[,] inputdata,
            object mode,
            object nFFT,
            bool useConjugateReference)
        {
            string modeText = mode == null || mode is ExcelMissing || mode is ExcelEmpty
                ? "TTT"
                : mode.ToString();

            modeText = modeText.Trim('\'', '"');
            if (modeText.Length == 0)
                modeText = "TTT";

            if (modeText.Length != 3)
                return "#Mode string should contain 3 characters";

            int nRefMode = ParseMode(modeText[0]);
            int nDatMode = ParseMode(modeText[1]);
            int nOutMode = ParseMode(modeText[2]);

            if (nRefMode < 0 || nDatMode < 0 || nOutMode < 0)
                return "#Mode string should be 3 chars, using 'T', 't' or 'F'";

            int nRows0 = reference.GetLength(0);
            int nCols0 = reference.GetLength(1);
            int nRows1 = inputdata.GetLength(0);
            int nCols1 = inputdata.GetLength(1);

            if (nRows0 <= nCols0)
                return "#Reference must have vertical orientation";

            if (nRows1 <= nCols1)
                return "#Inputdata must have vertical orientation";

            if (nCols0 > 2)
                return "#Reference can have max two (re, im) columns";

            if (nCols1 > 2)
                return "#Inputdata can have max two (re, im) columns";

            int fftSize = ExcelValueConverters.ToInt32(nFFT);
            if (fftSize > 0)
            {
                if (fftSize < nRows0)
                    return "#nFFT too small given size of reference";

                if (fftSize < nRows1)
                    return "#nFFT too small given size of inputdata";
            }
            else
            {
                fftSize = nRows0 > nRows1 ? nRows0 : nRows1;
            }

            Complex[] a = new Complex[fftSize];
            Complex[] b = new Complex[fftSize];

            string errorMessage;
            if (!TryLoadComplexVector(reference, nRefMode, a, "Reference", out errorMessage))
                return errorMessage;

            if (!TryLoadComplexVector(inputdata, nDatMode, b, "Inputdata", out errorMessage))
                return errorMessage;

            if (nRefMode < 2)
                Fourier.Forward(a, FourierOptions.Matlab);

            if (nDatMode < 2)
                Fourier.Forward(b, FourierOptions.Matlab);

            for (int i = 0; i < fftSize; i++)
            {
                b[i] = useConjugateReference
                    ? b[i] * Complex.Conjugate(a[i])
                    : b[i] * a[i];
            }

            if (nOutMode < 2)
                Fourier.Inverse(b, FourierOptions.Matlab);

            int outputRows = fftSize;
            int outputCols = nOutMode < 2 ? 1 : 2;

            object[,] result = new object[outputRows, outputCols];

            for (int row = 0; row < outputRows; row++)
            {
                int sourceIndex = nOutMode == 1 ? GetSwappedSourceIndex(row, outputRows) : row;

                result[row, 0] = b[sourceIndex].Real;

                if (outputCols == 2)
                    result[row, 1] = b[sourceIndex].Imaginary;
            }

            return result;
        }

        private static int ParseMode(char c)
        {
            if (c == 'T')
                return 0;

            if (c == 't')
                return 1;

            if (c == 'F')
                return 2;

            return -1;
        }

        private static void GetCallerShape(int fftSize, out int callerRows, out int callerCols, out bool singleCellCaller)
        {
            callerRows = fftSize;
            callerCols = 2;
            singleCellCaller = true;

            ExcelReference caller = XlCall.Excel(XlCall.xlfCaller) as ExcelReference;
            if (caller == null)
                return;

            callerRows = caller.RowLast - caller.RowFirst + 1;
            callerCols = caller.ColumnLast - caller.ColumnFirst + 1;
            singleCellCaller = callerRows == 1 && callerCols == 1;
        }

        private static bool TryLoadComplexVector(object[,] source, int mode, Complex[] target, string name, out string errorMessage)
        {
            errorMessage = null;

            int nRows = source.GetLength(0);
            int nCols = source.GetLength(1);

            if (nCols != 1 && nCols != 2)
            {
                errorMessage = "#" + name + " can have max two (re, im) columns";
                return false;
            }

            for (int row = 0; row < nRows; row++)
            {
                int sourceIndex = mode == 1 ? GetSwappedSourceIndex(row, nRows) : row;

                double real;
                if (!ExcelValueConverters.TryGetDouble(source[sourceIndex, 0], out real))
                {
                    errorMessage = "#" + name + " contains non-numeric values";
                    return false;
                }

                double imaginary = 0.0;
                if (nCols == 2 && !ExcelValueConverters.TryGetDouble(source[sourceIndex, 1], out imaginary))
                {
                    errorMessage = "#" + name + " contains non-numeric values";
                    return false;
                }

                target[row] = new Complex(real, imaginary);
            }

            return true;
        }

        private static int GetSwappedSourceIndex(int index, int length)
        {
            int shift = (length + 1) / 2;
            return (index + shift) % length;
        }
    }
}

// Copilot on FFT implementation: "D:\Source\VS22\SeismicExcel\SeismicExcel\docs\FFTimplementation.md"

