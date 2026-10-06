using System;
using System.Numerics;
using ExcelDna.Integration;

namespace SeismicExcel
{
    internal static class ComplexMatrix4
    {
        public static Complex[,] Invert4x4(Complex[,] A)
        {
            if (A.GetLength(0) != 4 || A.GetLength(1) != 4)
                throw new ArgumentException("Matrix must be 4x4.");

            Complex[,] aug = new Complex[4, 8];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                    aug[i, j] = A[i, j];
                aug[i, 4 + i] = Complex.One;
            }

            for (int col = 0; col < 4; col++)
            {
                int pivot = col;
                double max = aug[pivot, col].Magnitude;

                for (int row = col + 1; row < 4; row++)
                {
                    double mag = aug[row, col].Magnitude;
                    if (mag > max)
                    {
                        max = mag;
                        pivot = row;
                    }
                }

                if (max == 0)
                    throw new InvalidOperationException("Matrix is singular.");

                if (pivot != col)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        Complex tmp = aug[col, j];
                        aug[col, j] = aug[pivot, j];
                        aug[pivot, j] = tmp;
                    }
                }

                Complex diag = aug[col, col];
                for (int j = 0; j < 8; j++)
                    aug[col, j] /= diag;

                for (int row = col + 1; row < 4; row++)
                {
                    Complex factor = aug[row, col];
                    for (int j = 0; j < 8; j++)
                        aug[row, j] -= factor * aug[col, j];
                }
            }

            for (int col = 3; col >= 0; col--)
            {
                for (int row = col - 1; row >= 0; row--)
                {
                    Complex factor = aug[row, col];
                    for (int j = 0; j < 8; j++)
                        aug[row, j] -= factor * aug[col, j];
                }
            }

            Complex[,] inv = new Complex[4, 4];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    inv[i, j] = aug[i, 4 + j];

            return inv;
        }
    }

    public static class Avo
    {
        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        /// <summary>
        /// Shuey 2-parameter approximation to the Aki–Richards AVA equation
        /// </summary>
        [ExcelFunction(Name = "SE.avo.Shuey2", Description = "Shuey 2-parameter approximation to the Aki–Richards AVA equation")]
        public static object Shuey2(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "theta1", Description = "Incident angle in degrees")] double theta1)
        {
            double dRho = Rho2 - Rho1;
            double dVp = Vp2 - Vp1;
            double dVs = Vs2 - Vs1;
            double Rho = (Rho1 + Rho2) / 2.0;
            double Vp = (Vp1 + Vp2) / 2.0;
            double Vs = (Vs1 + Vs2) / 2.0;

            double R0 = 0.5 * (dVp / Vp + dRho / Rho);
            double g = 0.5 * dVp / Vp - 2 * (Vs * Vs) / (Vp * Vp) * (dRho / Rho + 2 * dVs / Vs);

            double R1 = R0 + g * Math.Pow(Math.Sin(DegreesToRadians(theta1)), 2);

            if (R1 > 2) throw new System.Exception("#CritAngle");
            if (R1 < -2) throw new System.Exception("#CritAngle");

            return R1;
        }

        /// <summary>
        /// Shuey 3-parameter approximation to the Aki–Richards AVA equation
        /// </summary>
        [ExcelFunction(Name = "SE.avo.Shuey3", Description = "Shuey 2-parameter approximation to the Aki–Richards AVA equation")]
        public static object Shuey3(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "theta1", Description = "Incident angle in degrees")] double theta1)
        {
            theta1 = DegreesToRadians(theta1);

            double dRho = Rho2 - Rho1;
            double dVp = Vp2 - Vp1;
            double dVs = Vs2 - Vs1;
            double Rho = (Rho1 + Rho2) / 2.0;
            double Vp = (Vp1 + Vp2) / 2.0;
            double Vs = (Vs1 + Vs2) / 2.0;

            double R0 = 0.5 * (dVp / Vp + dRho / Rho);
            double g = 0.5 * dVp / Vp - 2 * (Vs * Vs) / (Vp * Vp) * (dRho / Rho + 2 * dVs / Vs);
            double f = 0.5 * dVp / Vp;
            double R1 = R0 + g * Math.Pow(Math.Sin(theta1), 2) + f * (Math.Pow(Math.Tan(theta1), 2) - Math.Pow(Math.Sin(theta1), 2));

            if (R1 > 2) throw new System.Exception("#CritAngle");
            if (R1 < -2) throw new System.Exception("#CritAngle");
            return R1;
        }

        /// <summary>
        /// Bortfeld 2-term approximation to the Zoeppritz equations
        /// </summary>
        [ExcelFunction(Name = "SE.avo.Bortfeld2", Description = "Bortfeld 2-term approximation to the Zoeppritz equations")]
        public static object Bortfeld2(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "theta1", Description = "Incident angle in degrees")] double theta1)
        {
            theta1 = DegreesToRadians(theta1);

            double sinTheta2 = Vp2 / Vp1 * Math.Sin(theta1);

            if (sinTheta2 > 1) throw new System.Exception("#CritAngle");

            double theta2 = Math.Asin(sinTheta2);
            double term1 = 0.5 * Math.Log((Vp2 * Rho2 * Math.Cos(theta1)) / (Vp1 * Rho1 * Math.Cos(theta2)));
            double term2 = Math.Pow(Math.Sin(theta1) / Vp1, 2) * (Vs1 * Vs1 - Vs2 * Vs2) * (2 + Math.Log(Rho2 / Rho1) / Math.Log(Vs2 / Vs1));

            return term1 + term2;
        }

        /// <summary>
        /// Bortfeld 3-term approximation to the Zoeppritz equations
        /// </summary>
        [ExcelFunction(Name = "SE.avo.Bortfeld3", Description = "Bortfeld 3-term approximation to the Zoeppritz equations")]
        public static object Bortfeld3(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "theta1", Description = "Incident angle in degrees")] double theta1)
        {
            theta1 = DegreesToRadians(theta1);

            double dRho = Rho2 - Rho1;
            double dVp = Vp2 - Vp1;
            double dVs = Vs2 - Vs1;
            double Rho = (Rho1 + Rho2) / 2.0;
            double Vp = (Vp1 + Vp2) / 2.0;
            double Vs = (Vs1 + Vs2) / 2.0;
            double k = Math.Pow(2 * Vs / Vp, 2);
            double rsh = 0.5 * (dVp / Vp - k * dRho / Rho - 2 * k * dVs / Vs);

            double term1 = 0.5 * (dVp / Vp + dRho / Rho);
            double term2 = rsh * Math.Pow(Math.Sin(theta1), 2);
            double term3 = 0.5 * dVp / Vp * Math.Pow(Math.Tan(theta1), 2) * Math.Pow(Math.Sin(theta1), 2);

            double R1 = term1 + term2 + term3;

            if (R1 > 2) throw new System.Exception("#CritAngle");
            if (R1 < -2) throw new System.Exception("#CritAngle");

            return R1;
        }

        /// <summary>
        /// Fatti 3-term approximation to the Zoeppritz equations
        /// </summary>
        [ExcelFunction(Name = "SE.avo.Fatti", Description = "Fatti 3-term approximation to the Zoeppritz equations")]
        public static object Fatti(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "theta1", Description = "Incident angle in degrees")] double theta1)
        {
            theta1 = DegreesToRadians(theta1);

            double dRho = Rho2 - Rho1;
            double dVp = Vp2 - Vp1;
            double dVs = Vs2 - Vs1;
            double Rho = (Rho1 + Rho2) / 2.0;
            double Vp = (Vp1 + Vp2) / 2.0;
            double Vs = (Vs1 + Vs2) / 2.0;
            double dip = (Vp2 * Rho2 - Vp1 * Rho1) / (Vp2 * Rho2 + Vp1 * Rho1);
            double dis = (Vs2 * Rho2 - Vs1 * Rho1) / (Vs2 * Rho2 + Vs1 * Rho1);

            double term1 = (1.0 + Math.Pow(Math.Tan(theta1), 2)) * dip;
            double term2 = -8.0 * (Vs * Vs) / (Vp * Vp) * Math.Pow(Math.Sin(theta1), 2) * dis;
            double term3 = -1.0 * (0.5 * Math.Pow(Math.Tan(theta1), 2) - 2.0 * (Vs * Vs) / (Vp * Vp) * Math.Pow(Math.Sin(theta1), 2)) * dRho / Rho;

            double R1 = term1 + term2 + term3;

            if (R1 > 2) throw new System.Exception("#CritAngle");
            if (R1 < -2) throw new System.Exception("#CritAngle");

            return R1;
        }

        /// <summary>
        /// Aki-Richards approximation to the Zoeppritz equations
        /// </summary>
        [ExcelFunction(Name = "SE.avo.AkiRichards", Description = "Aki-Richards approximation to the Zoeppritz equations")]
        public static object AkiRichards(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "theta1", Description = "Incident angle in degrees")] double theta1)
        {
            theta1 = DegreesToRadians(theta1);

            double sinTheta2 = Vp2 / Vp1 * Math.Sin(theta1);

            if (sinTheta2 > 1) throw new System.Exception("#CritAngle");

            double theta2 = Math.Asin(sinTheta2);
            double dRho = Rho2 - Rho1;
            double dVp = Vp2 - Vp1;
            double dVs = Vs2 - Vs1;
            double meantheta = (theta1 + theta2) / 2.0;
            double Rho = (Rho1 + Rho2) / 2.0;
            double Vp = (Vp1 + Vp2) / 2.0;
            double Vs = (Vs1 + Vs2) / 2.0;

            double w = 0.5 * dRho / Rho;
            double x = 2.0 * (Vs * Vs) / (Vp1 * Vp1) * dRho / Rho;
            double y = 0.5 * (dVp / Vp);
            double z = 4.0 * (Vs * Vs) / (Vp1 * Vp1) * (dVs / Vs);

            double term1 = w;
            double term2 = -1.0 * x * Math.Pow(Math.Sin(theta1), 2);
            double term3 = y / Math.Pow(Math.Cos(meantheta), 2);
            double term4 = -1 * z * Math.Pow(Math.Sin(theta1), 2);

            return term1 + term2 + term3 + term4;
        }

        /// <summary>
        /// Full Zoeppritz solution for reflection/transmission coefficients, following the matrix
        /// formulation of Zhang and Brown.
        /// </summary>
        [ExcelFunction(Name = "SE.avo.Zoeppritz", Description = "Full Zoeppritz solution for reflection/transmission coefficients")]
        public static object Zoeppritz(
            [ExcelArgument(Name = "Vp1", Description = "P-wave velocity of the first layer [m/s]")] double Vp1,
            [ExcelArgument(Name = "Vs1", Description = "S-wave velocity of the first layer [m/s]")] double Vs1,
            [ExcelArgument(Name = "Rho1", Description = "Density of the first layer [g/cm^3]")] double Rho1,
            [ExcelArgument(Name = "Vp2", Description = "P-wave velocity of the second layer [m/s]")] double Vp2,
            [ExcelArgument(Name = "Vs2", Description = "S-wave velocity of the second layer [m/s]")] double Vs2,
            [ExcelArgument(Name = "Rho2", Description = "Density of the second layer [g/cm^3]")] double Rho2,
            [ExcelArgument(Name = "rayParam", Description = "Ray parameter")] double ray,
            [ExcelArgument(Name = "PSmode", Description = "Mode selector [P/S/p/s] for Top and Bottom layers. Capitals (=P/S) travel downwards (Pp)")] string mode = "Pp",
            [ExcelArgument(Name = "outputFlag", Description = "Optional output selector. 0 = return [Real, Imaginary], 1 = return Real part, 2 = return Imaginary part.")] object outputFlag = null)
        {
            if ((Vs1 > Vp1) || (Vs2 > Vp2))
                throw new System.Exception("#S-waves can't travel faster than p-waves");

            int actualOutputFlag = outputFlag is ExcelMissing || outputFlag is ExcelEmpty || outputFlag == null
                ? 0
                : ExcelValueConverters.ToInt32(outputFlag);

            if (actualOutputFlag < 0 || actualOutputFlag > 2)
                return "#ERR: outputFlag must be 0, 1 or 2.";

            int nMode;

            if (mode[0] == 'P')
                nMode = 0;
            else if (mode[0] == 'S')
                nMode = 1;
            else if (mode[0] == 'p')
                nMode = 2;
            else if (mode[0] == 's')
                nMode = 3;
            else
                throw new System.Exception("#Mode string should start with P / S / p / s");

            if (mode[1] == 'p')
                nMode += 0;
            else if (mode[1] == 's')
                nMode += 4;
            else if (mode[1] == 'P')
                nMode += 8;
            else if (mode[1] == 'S')
                nMode += 12;
            else
                throw new System.Exception("#Mode string should end with P / S / p / s");

            if ((nMode < 0) || (nMode > 15))
                throw new System.Exception("#Incorrect nMode. Range 0-15");

            if ((nMode & 2) == 0)
            {
                if ((nMode & 1) == 0)
                {
                    if (Vp1 * ray > 1) throw new System.Exception("#Ray parameter too large. Check Vp1 and ray");
                }
                else
                {
                    if (Vs1 * ray > 1) throw new System.Exception("#Ray parameter too large. Check Vs1 and ray");
                }
            }
            else
            {
                if ((nMode & 1) == 0)
                {
                    if (Vp2 * ray > 1) throw new System.Exception("#Ray parameter too large. Check Vp2 and ray");
                }
                else
                {
                    if (Vs2 * ray > 1) throw new System.Exception("#Ray parameter too large. Check Vs2 and ray");
                }
            }

            Complex one = new Complex(1.0, 0.0);
            Complex two = new Complex(2.0, 0.0);

            Complex Sin1P1 = Vp1 * ray;
            Complex Cos1P1 = Complex.Sqrt(one - Sin1P1 * Sin1P1);

            Complex Sin1S1 = Vs1 * ray;
            Complex Cos1S1 = Complex.Sqrt(one - Sin1S1 * Sin1S1);
            Complex Sin2S1 = two * Sin1S1 * Cos1S1;
            Complex Cos2S1 = two * Cos1S1 * Cos1S1 - one;

            Complex Sin1P2 = Vp2 * ray;
            Complex Cos1P2 = Complex.Sqrt(one - Sin1P2 * Sin1P2);

            Complex Sin1S2 = Vs2 * ray;
            Complex Cos1S2 = Complex.Sqrt(one - Sin1S2 * Sin1S2);
            Complex Sin2S2 = two * Sin1S2 * Cos1S2;
            Complex Cos2S2 = two * Cos1S2 * Cos1S2 - one;

            Complex Zp1 = Vp1 * Rho1;
            Complex Zp2 = Vp2 * Rho2;
            Complex Zs1 = Vs1 * Rho1;
            Complex Zs2 = Vs2 * Rho2;

            Complex[,] P = new Complex[4, 4];
            Complex[,] R = new Complex[4, 4];

            P[0, 0] = -Sin1P1;
            P[0, 1] = -Cos1S1;
            P[0, 2] = Sin1P2;
            P[0, 3] = Cos1S2;

            P[1, 0] = Cos1P1;
            P[1, 1] = -Sin1S1;
            P[1, 2] = Cos1P2;
            P[1, 3] = -Sin1S2;

            P[2, 0] = Cos1P1 * Sin1S1 * Zs1 * two;
            P[2, 1] = Cos2S1 * Zs1;
            P[2, 2] = Cos1P2 * Sin1S2 * Zs2 * two;
            P[2, 3] = Cos2S2 * Zs2;

            P[3, 0] = -Cos2S1 * Zp1;
            P[3, 1] = Sin2S1 * Zs1;
            P[3, 2] = Cos2S2 * Zp2;
            P[3, 3] = -Sin2S2 * Zs2;

            Complex[,] O;
            try
            {
                O = ComplexMatrix4.Invert4x4(P);
            }
            catch (System.Exception)
            {
                throw new System.Exception("#Can't resolve equations; determinant = 0");
            }

            R[0, 0] = Sin1P1;
            R[0, 1] = Cos1S1;
            R[0, 2] = -Sin1P2;
            R[0, 3] = -Cos1S2;

            R[1, 0] = Cos1P1;
            R[1, 1] = -Sin1S1;
            R[1, 2] = Cos1P2;
            R[1, 3] = -Sin1S2;

            R[2, 0] = Cos1P1 * Sin1S1 * Zs1 * two;
            R[2, 1] = Cos2S1 * Zs1;
            R[2, 2] = Cos1P2 * Sin1S2 * Zs2 * two;
            R[2, 3] = Cos2S2 * Zs2;

            R[3, 0] = Cos2S1 * Zp1;
            R[3, 1] = -Sin2S1 * Zs1;
            R[3, 2] = -Cos2S2 * Zp2;
            R[3, 3] = Sin2S2 * Zs2;

            Complex[,] Q = new Complex[4, 4];
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    Complex sum = Complex.Zero;
                    for (int i = 0; i < 4; ++i)
                    {
                        sum += O[r, i] * R[i, c];
                    }
                    Q[r, c] = sum;
                }
            }

            int row = nMode >> 2;
            int col = nMode & 3;

            Complex returnVal = Q[row, col];

            if (actualOutputFlag == 1)
                return returnVal.Real;

            if (actualOutputFlag == 2)
                return returnVal.Imaginary;

            return new object[,] { { returnVal.Real, returnVal.Imaginary } };
        }

        /// <summary>
        /// Gardner's density equation.
        /// </summary>
        /// <param name="Vp">P-wave velocity</param>
        /// <param name="alpha">Optional Gardner coefficient alpha. Default = 0.31</param>
        /// <param name="beta">Optional Gardner exponent beta. Default = 0.25</param>
        /// <returns>Estimated density</returns>
        [ExcelFunction(Name = "SE.avo.GardnerDensity", Description = "Compute density from P-wave velocity using Gardner's equation")]
        public static object GardnerDensity(
            [ExcelArgument(Name = "Vp", Description = "P-wave velocity")] double Vp,
            [ExcelArgument(Name = "alpha", Description = "Optional Gardner coefficient alpha. Default = 0.31")] object alpha,
            [ExcelArgument(Name = "beta", Description = "Optional Gardner exponent beta. Default = 0.25")] object beta)
        {
            double a = alpha is ExcelMissing || alpha is ExcelEmpty ? 0.31 : Convert.ToDouble(alpha);
            double b = beta is ExcelMissing || beta is ExcelEmpty ? 0.25 : Convert.ToDouble(beta);

            return a * Math.Pow(Vp, b);
        }

    /// <summary>
    /// Inverse Gardner equation for estimating P-wave velocity from density.
    /// </summary>
    /// <param name="rho">Density</param>
    /// <param name="alpha">Optional Gardner coefficient alpha. Default = 0.31</param>
    /// <param name="beta">Optional Gardner exponent beta. Default = 0.25</param>
    /// <returns>Estimated P-wave velocity</returns>
    [ExcelFunction(Name = "SE.avo.GardnerVelocity", Description = "Compute P-wave velocity from density using the inverse Gardner equation")]
    public static object GardnerVelocity(
        [ExcelArgument(Name = "rho", Description = "Density")] double rho,
        [ExcelArgument(Name = "alpha", Description = "Optional Gardner coefficient alpha. Default = 0.31")] object alpha,
        [ExcelArgument(Name = "beta", Description = "Optional Gardner exponent beta. Default = 0.25")] object beta)
    {
        double a = alpha is ExcelMissing || alpha is ExcelEmpty ? 0.31 : Convert.ToDouble(alpha);
        double b = beta is ExcelMissing || beta is ExcelEmpty ? 0.25 : Convert.ToDouble(beta);

        return Math.Pow(rho / a, 1.0 / b);
    }

        /// <summary>
        /// Computes ray parameter from angle of incidence and P-wave velocity.
        /// </summary>
        [ExcelFunction(Name = "SE.avo.RayParameter", Description = "Compute ray parameter from angle of incidence in degrees and P-wave velocity")]
        public static object RayParameter(
            [ExcelArgument(Name = "AngleOfIncidence", Description = "Angle of incidence in degrees")] double angleOfIncidence,
            [ExcelArgument(Name = "Vp", Description = "P-wave velocity [m/s]")] double vp)
        {
            if (vp == 0.0)
                return "#ERR: Vp must be non-zero.";

            return Math.Sin(DegreesToRadians(angleOfIncidence)) / vp;
        }

    }
}