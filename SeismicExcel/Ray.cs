using System;
using ExcelDna.Integration;

namespace SeismicExcel
{
    public static class RayTracer
    {
        [ExcelFunction(Name = "SE.ray.segment", Description = "Return cumulative horizontal ray offsets for the supplied resampled depths.")]
        public static object[,] Segment(
            [ExcelArgument(Name = "input depth list", Description = "Input depth values as a vertical array of strictly increasing interval end depths. Depth 0.0 is implied and must not be included.")] object[,] InputZ,
            [ExcelArgument(Name = "P-wave velocities", Description = "P-wave velocities corresponding to the input depth intervals.")] object[,] InputVp,
            [ExcelArgument(Name = "Kz values", Description = "Kz values corresponding to the input depth intervals.")] object[,] InputKz,
            [ExcelArgument(Name = "Ray Parameter", Description = "Ray parameter for the ray tracing segment")] object InputRayParam,
            [ExcelArgument(Name = "resamp depth", Description = "Resampled depth values as a vertical array.")] object[,] resampZ,
            [ExcelArgument(Name = "Ray direction", Description = "Upwards = 0, downwards = 1. Default is upwards.")] object InputUpwards = null)
        {
            if (InputZ.GetLength(1) != 1)
                return new object[,] { { "#ERR: input depth must be a vertical array with exactly one column." } };

            if (InputVp.GetLength(1) != 1)
                return new object[,] { { "#ERR: P-wave velocities must be a vertical array with exactly one column." } };

            if (InputKz.GetLength(1) != 1)
                return new object[,] { { "#ERR: Kz values must be a vertical array with exactly one column." } };

            if (resampZ.GetLength(1) != 1)
                return new object[,] { { "#ERR: resamp depth must be a vertical array with exactly one column." } };

            double rayParam;
            if (!ExcelValueConverters.TryGetDouble(InputRayParam, out rayParam))
                return new object[,] { { "#ERR: Ray Parameter must be numeric." } };

            int upwardsFlag;
            if (InputUpwards == null || InputUpwards is ExcelMissing || InputUpwards is ExcelEmpty)
            {
                upwardsFlag = 0;
            }
            else if (InputUpwards is bool b)
            {
                upwardsFlag = b ? 1 : 0;
            }
            else
            {
                double directionValue;
                if (!ExcelValueConverters.TryGetDouble(InputUpwards, out directionValue) || (directionValue != 0.0 && directionValue != 1.0))
                    return new object[,] { { "#ERR: Ray direction must be 0 (upwards) or 1 (downwards)." } };

                upwardsFlag = (int)directionValue;
            }

            int inputRows = InputZ.GetLength(0);
            if (inputRows < 1)
                return new object[,] { { "#ERR: input depth must contain at least one value." } };

            if (InputVp.GetLength(0) != inputRows)
                return new object[,] { { "#ERR: input depth and P-wave velocities must have the same number of rows." } };

            if (InputKz.GetLength(0) != inputRows)
                return new object[,] { { "#ERR: input depth and Kz values must have the same number of rows." } };

            double[] inputDepths = new double[inputRows];
            double[] inputVelocities = new double[inputRows];
            double[] inputKzValues = new double[inputRows];

            for (int i = 0; i < inputRows; i++)
            {
                if (!ExcelValueConverters.TryGetDouble(InputZ[i, 0], out inputDepths[i]))
                    return new object[,] { { "#ERR: input depth contains non-numeric values." } };

                if (!ExcelValueConverters.TryGetDouble(InputVp[i, 0], out inputVelocities[i]))
                    return new object[,] { { "#ERR: P-wave velocities contain non-numeric values." } };

                if (!ExcelValueConverters.TryGetDouble(InputKz[i, 0], out inputKzValues[i]))
                    return new object[,] { { "#ERR: Kz values contain non-numeric values." } };

                if (inputDepths[i] <= 0.0)
                    return new object[,] { { "#ERR: input depth values must be > 0. Depth 0.0 is implied and must not be included." } };

                if (i > 0 && inputDepths[i] <= inputDepths[i - 1])
                    return new object[,] { { "#ERR: input depth must be strictly increasing." } };
            }

            int nRows = resampZ.GetLength(0);
            object[,] result = new object[nRows, 1];
            double[] fragmentValues = new double[nRows];
            double[] stepDiffs = new double[nRows];
            double[] increments = new double[nRows];
            int[] depthIndices = new int[nRows];
            bool[] isValid = new bool[nRows];
            bool[] rowCritical = new bool[nRows];
            bool[] incrementValid = new bool[nRows];
            bool[] incrementCritical = new bool[nRows];
            object[] rowErrors = new object[nRows];
            object[] incrementErrors = new object[nRows];

            for (int i = 0; i < nRows; i++)
            {
                double z;
                if (!ExcelValueConverters.TryGetDouble(resampZ[i, 0], out z))
                {
                    rowErrors[i] = "#ERR: resamp depth contains a non-numeric value.";
                    continue;
                }

                int depthIndex = LookupIndexByDepthRange(inputDepths, z);
                if (depthIndex < 0)
                {
                    rowErrors[i] = ExcelError.ExcelErrorNA;
                    continue;
                }

                double vpBaseValue = inputVelocities[depthIndex];
                double kzValue = inputKzValues[depthIndex];
                double vpDepthValue = vpBaseValue + kzValue * z;

                double asinArgument0 = rayParam * vpBaseValue;
                double asinArgumentZ = rayParam * vpDepthValue;

                if (asinArgument0 <= -1.0 || asinArgument0 >= 1.0 || asinArgumentZ <= -1.0 || asinArgumentZ >= 1.0)
                {
                    rowErrors[i] = ExcelError.ExcelErrorNA;
                    rowCritical[i] = true;
                    continue;
                }

                double aoi0Radians = Math.Asin(asinArgument0);
                double aoiZRadians = Math.Asin(asinArgumentZ);

                double fragmentValue;
                if (kzValue > 0.0)
                {
                    double denominator = rayParam * kzValue;
                    if (denominator == 0.0)
                    {
                        rowErrors[i] = ExcelError.ExcelErrorDiv0;
                        continue;
                    }

                    fragmentValue = (Math.Cos(aoi0Radians) - Math.Cos(aoiZRadians)) / denominator;
                }
                else
                {
                    fragmentValue = z * Math.Sin(aoi0Radians);
                }

                fragmentValues[i] = fragmentValue;
                depthIndices[i] = depthIndex;
                isValid[i] = true;
            }

            for (int i = 1; i < nRows; i++)
            {
                if (isValid[i] && isValid[i - 1])
                    stepDiffs[i] = fragmentValues[i] - fragmentValues[i - 1];
            }

            for (int i = 0; i < nRows; i++)
            {
                if (i == 0)
                {
                    increments[i] = 0.0;
                    incrementValid[i] = true;
                    continue;
                }

                if (!isValid[i])
                {
                    incrementErrors[i] = rowErrors[i];
                    incrementCritical[i] = rowCritical[i];
                    continue;
                }

                if (!isValid[i - 1])
                {
                    incrementErrors[i] = ExcelError.ExcelErrorNA;
                    incrementCritical[i] = rowCritical[i - 1];
                    continue;
                }

                if (depthIndices[i - 1] == depthIndices[i])
                {
                    increments[i] = stepDiffs[i];
                    incrementValid[i] = true;
                }
                else
                {
                    if (i + 1 < nRows && isValid[i + 1])
                    {
                        increments[i] = 0.5 * (stepDiffs[i - 1] + stepDiffs[i + 1]);
                        incrementValid[i] = true;
                    }
                    else
                    {
                        incrementErrors[i] = ExcelError.ExcelErrorNA;
                        incrementCritical[i] = i + 1 < nRows && rowCritical[i + 1];
                    }
                }
            }

            if (upwardsFlag == 0)
            {
                double runningOffset = 0.0;
                bool criticalEncountered = false;

                for (int i = nRows - 1; i >= 0; i--)
                {
                    if (i == nRows - 1)
                    {
                        result[i, 0] = 0.0;
                        continue;
                    }

                    if (criticalEncountered)
                    {
                        result[i, 0] = ExcelError.ExcelErrorNA;
                        continue;
                    }

                    if (!incrementValid[i])
                    {
                        result[i, 0] = incrementErrors[i];

                        if (incrementCritical[i])
                            criticalEncountered = true;

                        continue;
                    }

                    runningOffset += increments[i];
                    result[i, 0] = runningOffset;
                }
            }
            else
            {
                double runningOffset = 0.0;
                bool criticalEncountered = false;

                for (int i = 0; i < nRows; i++)
                {
                    if (i == 0)
                    {
                        result[i, 0] = 0.0;
                        continue;
                    }

                    if (criticalEncountered)
                    {
                        result[i, 0] = ExcelError.ExcelErrorNA;
                        continue;
                    }

                    if (!incrementValid[i])
                    {
                        result[i, 0] = incrementErrors[i];

                        if (incrementCritical[i])
                            criticalEncountered = true;

                        continue;
                    }

                    runningOffset += increments[i];
                    result[i, 0] = runningOffset;
                }
            }

            return result;
        }

        [ExcelFunction(Name = "SE.ray.parameter", Description = "Return ray parameter (slowness) in [s/m] from P-wave velocity and angle in degrees.")]
        public static object RayParameter(
            [ExcelArgument(Name = "P-wave velocity", Description = "P-wave velocity [m/s].")] double vp,
            [ExcelArgument(Name = "angle", Description = "Angle in degrees.")] double angle)
        {
            if (vp == 0.0)
                return "#ERR: P-wave velocity must be non-zero.";

            return Math.Sin(angle * Math.PI / 180.0) / vp;
        }

        private static int LookupIndexByDepthRange(double[] inputDepths, double depth)
        {
            if (depth < 0.0)
                return -1;

            int lastIndex = inputDepths.Length - 1;

            if (depth > inputDepths[lastIndex])
                return -1;

            if (depth == inputDepths[lastIndex])
                return lastIndex;

            int low = 0;
            int high = lastIndex;

            while (low < high)
            {
                int mid = low + (high - low) / 2;

                if (depth < inputDepths[mid])
                    high = mid;
                else
                    low = mid + 1;
            }

            return low;
        }
    }
}