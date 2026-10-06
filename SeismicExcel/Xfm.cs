using ExcelDna.Integration;
using MathNet.Numerics.IntegralTransforms;
using SharpProj;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace SeismicExcel
{
    public static class SharpProj
    {
        /// <summary>
        /// Converts WGS84 Lat/Lon (EPSG:4326) into Web Mercator X/Y meters (EPSG:3857).
        /// </summary>
        /// <param name="latitude">Latitude in decimal degrees</param>
        /// <param name="longitude">Longitude in decimal degrees</param>
        /// <returns>A 1x2 row array containing { X_Meters, Y_Meters }</returns>
        /// <example>
        /// In cell A1, enter a Latitude: 52.3676
        /// In cell B1, enter a Longitude: 4.9041
        /// In cell C1, type the formula: =SE.tst.TransformToWebMercator(A1, B1)
        /// Press Enter. Excel will automatically spill two values across cells C1 and D1:
        /// C1 (X in Meters):  ~545921.915
        /// D1 (Y in Meters): ~6866867.122
        /// </example>
        [ExcelFunction(Name = "SE.xfm.LatLongToWebMercator", Description = "Converts Lat/Lon degrees (EPSG:4326) to Web Mercator meters (EPSG:3857) using SharpProj")]
        public static object TransformLatLongToWebMercator(
            [ExcelArgument(Name = "latitude", Description = "Latitude in decimal degrees (e.g. 52.3676)")] double latitude,
            [ExcelArgument(Name = "longitude", Description = "Longitude in decimal degrees (e.g. 4.9041)")] double longitude)
        {
            try
            {
                using (var pc = new ProjContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var t = CoordinateTransform.Create(src, dst))
                    {
                        var r = t.Apply(new PPoint(longitude, latitude));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        /// <summary>
        /// Converts Web Mercator X/Y meters (EPSG:3857) into WGS84 Lat/Lon (EPSG:4326).
        /// </summary>
        /// </example>
        [ExcelFunction(Name = "SE.xfm.WebMercatorToLatLong", Description = "Converts Web Mercator meters (EPSG:3857) to Lat/Lon degrees (EPSG:4326) using SharpProj")]
        public static object TransformWebMercatorToLatLong(
            [ExcelArgument(Name = "Easting", Description = "Easting in meters (e.g. 545,922")] double easting,
            [ExcelArgument(Name = "Northing", Description = "northing in meters (e.g. 6,866,867)")] double northing)
        {
            try
            {
                using (var pc = new ProjContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(3857, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(4326, pc).WithNormalizedAxis(pc))
                    using (var t = CoordinateTransform.Create(src, dst))
                    {
                        var r = t.Apply(new PPoint(easting, northing));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (System.Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        /// <summary>
        /// Converts coordinates between two EPSG CRSs.
        /// </summary>
        [ExcelFunction(Name = "SE.xfm.FromEpsgNoToEpsgNo", Description = "Transforms X/Y coordinates from one EPSG CRS to another using SharpProj. Optionally selects a specific transform candidate.")]
        public static object FromEpsgNoToEpsgNo(
            [ExcelArgument(Name = "Source CRS", Description = "Optional EPSG number of source CRS. Default = 4326")] object fromCrs = null,
            [ExcelArgument(Name = "Destination CRS", Description = "Optional EPSG number of destination CRS. Default = 3857")] object toCrs = null,
            [ExcelArgument(Name = "X", Description = "Input X coordinate (for EPSG:4326 this is longitude)")] object x = null,
            [ExcelArgument(Name = "Y", Description = "Input Y coordinate (for EPSG:4326 this is latitude)")] object y = null,
            [ExcelArgument(Name = "Candidate Index", Description = "Optional transform candidate index. Leave blank for SharpProj default selection; use 0, 1, ... to force a specific candidate.")] object candidateIndex = null)
        {
            try
            {
                int actualFromCrs = fromCrs is ExcelMissing || fromCrs is ExcelEmpty || fromCrs == null
                    ? 4326
                    : ExcelValueConverters.ToInt32(fromCrs);

                int actualToCrs = toCrs is ExcelMissing || toCrs is ExcelEmpty || toCrs == null
                    ? 3857
                    : ExcelValueConverters.ToInt32(toCrs);

                double actualX;
                if (!ExcelValueConverters.TryGetDouble(x, out actualX))
                    return "#ERR: X must be supplied and numeric.";

                double actualY;
                if (!ExcelValueConverters.TryGetDouble(y, out actualY))
                    return "#ERR: Y must be supplied and numeric.";

                bool hasCandidateIndex = !(candidateIndex is ExcelMissing || candidateIndex is ExcelEmpty || candidateIndex == null);
                int actualCandidateIndex = hasCandidateIndex ? ExcelValueConverters.ToInt32(candidateIndex) : -1;

                if (hasCandidateIndex && actualCandidateIndex < 0)
                    return "#ERR: Candidate Index must be 0 or greater.";

                using (var pc = new ProjContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualFromCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualToCrs, pc).WithNormalizedAxis(pc))
                    using (var t = CoordinateTransform.Create(src, dst))
                    {
                        global::SharpProj.CoordinateTransform transformToApply = t;

                        if (hasCandidateIndex)
                        {
                            string selectionError;
                            transformToApply = TryGetCandidateTransform(t, actualCandidateIndex, out selectionError);
                            if (transformToApply == null)
                                return selectionError;
                        }

                        var r = transformToApply.Apply(new PPoint(actualX, actualY));
                        return new object[,] { { r.X, r.Y } };
                    }
                }
            }
            catch (Exception ex)
            {
                return $"#ERR: {ex.Message}";
            }
        }

        private static global::SharpProj.CoordinateTransform TryGetCandidateTransform(
            global::SharpProj.CoordinateTransform transform,
            int candidateIndex,
            out string errorMessage)
        {
            errorMessage = null;

            var chooser = transform as global::SharpProj.ChooseCoordinateTransform;
            if (chooser == null)
            {
                if (candidateIndex == 0)
                    return transform;

                errorMessage = "#ERR: Transform does not expose multiple candidates.";
                return null;
            }

            IEnumerable enumerable = chooser as IEnumerable;
            if (enumerable == null)
            {
                errorMessage = "#ERR: Unable to enumerate transform candidates.";
                return null;
            }

            int index = 0;
            foreach (object item in enumerable)
            {
                var candidate = item as global::SharpProj.CoordinateTransform;
                if (candidate == null)
                    continue;

                if (index == candidateIndex)
                    return candidate;

                index++;
            }

            errorMessage = "#ERR: Candidate Index out of range.";
            return null;
        }

        [ExcelFunction(Name = "SE.xfm.ListTransformInfo", Description = "Lists diagnostic information about the SharpProj transform selected between two EPSG CRSs")]
        public static object[,] ListTransformInfo(
            [ExcelArgument(Name = "Source CRS", Description = "Optional EPSG number of source CRS. Default = 4326")] object fromCrs = null,
            [ExcelArgument(Name = "Destination CRS", Description = "Optional EPSG number of destination CRS. Default = 3857")] object toCrs = null)
        {
            try
            {
                int actualFromCrs = fromCrs is ExcelMissing || fromCrs is ExcelEmpty || fromCrs == null
                    ? 4326
                    : ExcelValueConverters.ToInt32(fromCrs);

                int actualToCrs = toCrs is ExcelMissing || toCrs is ExcelEmpty || toCrs == null
                    ? 3857
                    : ExcelValueConverters.ToInt32(toCrs);

                using (var pc = new ProjContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualFromCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualToCrs, pc).WithNormalizedAxis(pc))
                    using (var t = CoordinateTransform.Create(src, dst))
                    {
                        var rows = new List<KeyValuePair<string, string>>();

                        rows.Add(new KeyValuePair<string, string>("SourceCrs", actualFromCrs.ToString()));
                        rows.Add(new KeyValuePair<string, string>("DestinationCrs", actualToCrs.ToString()));

                        AddTransformInfo(rows, "Transform", t);
                        AddCandidateInfo(rows, t);

                        return ToKeyValueResult(rows);
                    }
                }
            }
            catch (Exception ex)
            {
                return new object[,] { { "#ERR: " + ex.Message } };
            }
        }

        private static void AddCandidateInfo(List<KeyValuePair<string, string>> rows, object target)
        {
            if (!(target is global::SharpProj.ChooseCoordinateTransform))
                return;

            IEnumerable enumerable = target as IEnumerable;
            if (enumerable == null)
                return;

            int index = 0;
            foreach (object item in enumerable)
            {
                if (item == null)
                    continue;

                AddTransformInfo(rows, "Candidate[" + index + "]", item);
                index++;
            }
        }

        private static void AddTransformInfo(List<KeyValuePair<string, string>> rows, string prefix, object target)
        {
            rows.Add(new KeyValuePair<string, string>(prefix + ".RuntimeType", target.GetType().FullName));
            rows.Add(new KeyValuePair<string, string>(prefix + ".PublicProperties", string.Join(", ", GetPublicPropertyNames(target))));

            AddPropertyInfo(rows, target, prefix, "Name");
            AddPropertyInfo(rows, target, prefix, "Type");
            AddPropertyInfo(rows, target, prefix, "Identifier");
            AddPropertyInfo(rows, target, prefix, "Identifiers");
            AddPropertyInfo(rows, target, prefix, "Accuracy");
            AddPropertyInfo(rows, target, prefix, "Count");
            AddPropertyInfo(rows, target, prefix, "IsAvailable");
            AddPropertyInfo(rows, target, prefix, "HasBallParkTransformation");
            AddPropertyInfo(rows, target, prefix, "HasInverse");
            AddPropertyInfo(rows, target, prefix, "MethodName");
            AddPropertyInfo(rows, target, prefix, "Scope");
            AddPropertyInfo(rows, target, prefix, "Remarks");
            AddPropertyInfo(rows, target, prefix, "GridUsages");
            AddPropertyInfo(rows, target, prefix, "Parameters");
            AddPropertyInfo(rows, target, prefix, "SourceCRS");
            AddPropertyInfo(rows, target, prefix, "TargetCRS");
            AddPropertyInfo(rows, target, prefix, "Context");
            AddPropertyInfo(rows, target, prefix, "UsageArea");

            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "Name");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "WestLongitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "SouthLatitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "EastLongitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "NorthLatitude");
            AddNestedPropertyInfo(rows, target, prefix, "UsageArea", "Center");

            if (!(target is string))
                AddEnumerableInfo(rows, target, prefix);
        }

        private static IEnumerable<string> GetPublicPropertyNames(object value)
        {
            return value.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Select(p => p.Name)
                .OrderBy(p => p);
        }

        private static void AddPropertyInfo(List<KeyValuePair<string, string>> rows, object target, string prefix, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                return;

            object value;
            try
            {
                value = property.GetValue(target, null);
            }
            catch
            {
                return;
            }

            rows.Add(new KeyValuePair<string, string>(prefix + "." + propertyName, FormatValue(value)));
        }

        private static void AddNestedPropertyInfo(List<KeyValuePair<string, string>> rows, object target, string prefix, string propertyName, string nestedPropertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                return;

            object nestedTarget;
            try
            {
                nestedTarget = property.GetValue(target, null);
            }
            catch
            {
                return;
            }

            if (nestedTarget == null)
                return;

            PropertyInfo nestedProperty = nestedTarget.GetType().GetProperty(nestedPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (nestedProperty == null || !nestedProperty.CanRead || nestedProperty.GetIndexParameters().Length != 0)
                return;

            object nestedValue;
            try
            {
                nestedValue = nestedProperty.GetValue(nestedTarget, null);
            }
            catch
            {
                return;
            }

            rows.Add(new KeyValuePair<string, string>(prefix + "." + propertyName + "." + nestedPropertyName, FormatValue(nestedValue)));
        }

        private static void AddEnumerableInfo(List<KeyValuePair<string, string>> rows, object target, string prefix)
        {
            IEnumerable enumerable = target as IEnumerable;
            if (enumerable == null || target is string)
                return;

            var items = new List<string>();
            int count = 0;

            foreach (object item in enumerable)
            {
                count++;
                if (items.Count < 8)
                    items.Add(FormatValue(item));
            }

            rows.Add(new KeyValuePair<string, string>(prefix + ".EnumerableCount", count.ToString()));

            if (items.Count > 0)
                rows.Add(new KeyValuePair<string, string>(prefix + ".EnumerableItems", string.Join(" | ", items)));
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is string s)
                return s;

            if (value is IEnumerable enumerable)
            {
                var items = new List<string>();
                int count = 0;

                foreach (object item in enumerable)
                {
                    count++;
                    if (items.Count < 8)
                        items.Add(FormatSimpleValue(item));
                }

                if (count == 0)
                    return string.Empty;

                if (count > items.Count)
                    items.Add("...");

                return string.Join("; ", items);
            }

            return FormatSimpleValue(value);
        }

        private static string FormatSimpleValue(object value)
        {
            if (value == null)
                return string.Empty;

            string text;
            try
            {
                text = Convert.ToString(value);
            }
            catch
            {
                return value.GetType().FullName;
            }

            if (string.IsNullOrEmpty(text))
                return value.GetType().FullName;

            if (text == value.GetType().FullName || text == value.GetType().Name)
            {
                string described = DescribeObject(value);
                return string.IsNullOrEmpty(described) ? text : described;
            }

            return text;
        }

        private static string DescribeObject(object value)
        {
            var parts = new List<string>();

            foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length != 0 || !property.CanRead)
                    continue;

                object propertyValue;
                try
                {
                    propertyValue = property.GetValue(value, null);
                }
                catch
                {
                    continue;
                }

                if (propertyValue == null)
                    continue;

                if (!IsSimpleValue(propertyValue))
                    continue;

                parts.Add(property.Name + "=" + Convert.ToString(propertyValue));
                if (parts.Count >= 6)
                    break;
            }

            return parts.Count == 0 ? string.Empty : string.Join("; ", parts);
        }

        private static bool IsSimpleValue(object value)
        {
            Type type = value.GetType();

            if (type.IsEnum)
                return true;

            if (type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(ushort) ||
                type == typeof(int) ||
                type == typeof(uint) ||
                type == typeof(long) ||
                type == typeof(ulong) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal) ||
                type == typeof(DateTime))
            {
                return true;
            }

            return false;
        }

        private static object[,] ToKeyValueResult(List<KeyValuePair<string, string>> rows)
        {
            object[,] result = new object[rows.Count, 2];

            for (int i = 0; i < rows.Count; i++)
            {
                result[i, 0] = rows[i].Key;
                result[i, 1] = rows[i].Value;
            }

            return result;
        }

        [ExcelFunction(Name = "SE.xfm.ListTransformOptions", Description = "Lists selectable transform candidates between two EPSG CRSs, including heuristic pros and cons")]
        public static object[,] ListTransformOptions(
            [ExcelArgument(Name = "Source CRS", Description = "Optional EPSG number of source CRS. Default = 4326")] object fromCrs = null,
            [ExcelArgument(Name = "Destination CRS", Description = "Optional EPSG number of destination CRS. Default = 3857")] object toCrs = null)
        {
            try
            {
                int actualFromCrs = fromCrs is ExcelMissing || fromCrs is ExcelEmpty || fromCrs == null
                    ? 4326
                    : ExcelValueConverters.ToInt32(fromCrs);

                int actualToCrs = toCrs is ExcelMissing || toCrs is ExcelEmpty || toCrs == null
                    ? 3857
                    : ExcelValueConverters.ToInt32(toCrs);

                using (var pc = new ProjContext())
                {
                    using (var src = CoordinateReferenceSystem.CreateFromEpsg(actualFromCrs, pc).WithNormalizedAxis(pc))
                    using (var dst = CoordinateReferenceSystem.CreateFromEpsg(actualToCrs, pc).WithNormalizedAxis(pc))
                    using (var t = CoordinateTransform.Create(src, dst))
                    {
                        List<global::SharpProj.CoordinateTransform> candidates = GetCandidateTransforms(t);
                        bool hasMultipleCandidates = t is global::SharpProj.ChooseCoordinateTransform && candidates.Count > 1;

                        var rows = new List<object[]>();
                        rows.Add(new object[]
                        {
                            "CandidateIndex",
                            "DefaultSelection",
                            "Name",
                            "Type",
                            "Accuracy",
                            "IsAvailable",
                            "HasBallPark",
                            "UsageArea",
                            "Pros",
                            "Cons"
                        });

                        for (int i = 0; i < candidates.Count; i++)
                        {
                            global::SharpProj.CoordinateTransform candidate = candidates[i];
                            string usageAreaName = candidate.UsageArea == null ? string.Empty : candidate.UsageArea.Name;

                            rows.Add(new object[]
                            {
                                i,
                                hasMultipleCandidates ? (i == 0 ? "Yes" : "No") : "Only",
                                candidate.Name,
                                Convert.ToString(candidate.Type),
                                candidate.Accuracy,
                                candidate.IsAvailable,
                                candidate.HasBallParkTransformation,
                                usageAreaName,
                                BuildCandidatePros(candidate, i, hasMultipleCandidates),
                                BuildCandidateCons(candidate, i, hasMultipleCandidates)
                            });
                        }

                        return ToObjectTable(rows);
                    }
                }
            }
            catch (Exception ex)
            {
                return new object[,] { { "#ERR: " + ex.Message } };
            }
        }

        private static List<global::SharpProj.CoordinateTransform> GetCandidateTransforms(global::SharpProj.CoordinateTransform transform)
        {
            var result = new List<global::SharpProj.CoordinateTransform>();

            var chooser = transform as global::SharpProj.ChooseCoordinateTransform;
            if (chooser == null)
            {
                result.Add(transform);
                return result;
            }

            IEnumerable enumerable = chooser as IEnumerable;
            if (enumerable == null)
            {
                result.Add(transform);
                return result;
            }

            foreach (object item in enumerable)
            {
                var candidate = item as global::SharpProj.CoordinateTransform;
                if (candidate != null)
                    result.Add(candidate);
            }

            if (result.Count == 0)
                result.Add(transform);

            return result;
        }

        private static string BuildCandidatePros(global::SharpProj.CoordinateTransform candidate, int candidateIndex, bool hasMultipleCandidates)
        {
            var pros = new List<string>();

            if (hasMultipleCandidates && candidateIndex == 0)
                pros.Add("First candidate returned by SharpProj");

            if (candidate.IsAvailable)
                pros.Add("Available");

            if (!candidate.HasBallParkTransformation)
                pros.Add("No ballpark transform");

            if (candidate.Accuracy > 0)
                pros.Add("Reported accuracy: " + Convert.ToString(candidate.Accuracy));

            if (candidate.UsageArea != null && !string.IsNullOrWhiteSpace(candidate.UsageArea.Name))
                pros.Add("Defined usage area");

            return pros.Count == 0 ? "None obvious from exposed metadata" : string.Join("; ", pros);
        }

        private static string BuildCandidateCons(global::SharpProj.CoordinateTransform candidate, int candidateIndex, bool hasMultipleCandidates)
        {
            var cons = new List<string>();

            if (!candidate.IsAvailable)
                cons.Add("Not available");

            if (candidate.HasBallParkTransformation)
                cons.Add("Uses ballpark transform");

            if (candidate.Accuracy <= 0)
                cons.Add("No reported accuracy");

            if (hasMultipleCandidates && candidateIndex > 0)
                cons.Add("Not the first candidate returned by SharpProj");

            if (candidate.UsageArea == null || string.IsNullOrWhiteSpace(candidate.UsageArea.Name))
                cons.Add("No usage area metadata");

            return cons.Count == 0 ? "None obvious from exposed metadata" : string.Join("; ", cons);
        }

        private static object[,] ToObjectTable(List<object[]> rows)
        {
            int rowCount = rows.Count;
            int colCount = rows.Max(r => r.Length);
            object[,] result = new object[rowCount, colCount];

            for (int row = 0; row < rowCount; row++)
            {
                object[] values = rows[row];
                for (int col = 0; col < values.Length; col++)
                {
                    result[row, col] = values[col];
                }
            }

            return result;
        }
    }
}

