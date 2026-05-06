namespace ISO11820WinForms.Services
{
    public sealed class CalibrationUniformityResult
    {
        public double TAvg { get; init; }
        public double TAvgAxis1 { get; init; }
        public double TAvgAxis2 { get; init; }
        public double TAvgAxis3 { get; init; }
        public double TAvgLevelA { get; init; }
        public double TAvgLevelB { get; init; }
        public double TAvgLevelC { get; init; }
        public double TDevAxis1 { get; init; }
        public double TDevAxis2 { get; init; }
        public double TDevAxis3 { get; init; }
        public double TDevLevelA { get; init; }
        public double TDevLevelB { get; init; }
        public double TDevLevelC { get; init; }
        public double TAvgDevAxis { get; init; }
        public double TAvgDevLevel { get; init; }
    }

    public static class CalibrationCalculationService
    {
        public const double CalibrationTargetTemp = 750.0;
        public const double CalibrationTolerance = 5.0;

        public static readonly string[] SurfacePositions =
        {
            "A1", "A2", "A3",
            "B1", "B2", "B3",
            "C1", "C2", "C3"
        };

        public static readonly int[] CenterPositions =
        {
            145, 135, 125, 115, 105,
            95, 85, 75, 65, 55,
            45, 35, 25, 15, 5
        };

        public static CalibrationUniformityResult CalculateSurfaceUniformity(IReadOnlyDictionary<string, double> values)
        {
            double a1 = values["A1"];
            double a2 = values["A2"];
            double a3 = values["A3"];
            double b1 = values["B1"];
            double b2 = values["B2"];
            double b3 = values["B3"];
            double c1 = values["C1"];
            double c2 = values["C2"];
            double c3 = values["C3"];

            double tAvg = (a1 + a2 + a3 + b1 + b2 + b3 + c1 + c2 + c3) / 9.0;

            double axis1 = (a1 + b1 + c1) / 3.0;
            double axis2 = (a2 + b2 + c2) / 3.0;
            double axis3 = (a3 + b3 + c3) / 3.0;

            double levelA = (a1 + a2 + a3) / 3.0;
            double levelB = (b1 + b2 + b3) / 3.0;
            double levelC = (c1 + c2 + c3) / 3.0;

            double devAxis1 = CalculateDeviation(axis1, tAvg);
            double devAxis2 = CalculateDeviation(axis2, tAvg);
            double devAxis3 = CalculateDeviation(axis3, tAvg);
            double devLevelA = CalculateDeviation(levelA, tAvg);
            double devLevelB = CalculateDeviation(levelB, tAvg);
            double devLevelC = CalculateDeviation(levelC, tAvg);

            return new CalibrationUniformityResult
            {
                TAvg = tAvg,
                TAvgAxis1 = axis1,
                TAvgAxis2 = axis2,
                TAvgAxis3 = axis3,
                TAvgLevelA = levelA,
                TAvgLevelB = levelB,
                TAvgLevelC = levelC,
                TDevAxis1 = devAxis1,
                TDevAxis2 = devAxis2,
                TDevAxis3 = devAxis3,
                TDevLevelA = devLevelA,
                TDevLevelB = devLevelB,
                TDevLevelC = devLevelC,
                TAvgDevAxis = (devAxis1 + devAxis2 + devAxis3) / 3.0,
                TAvgDevLevel = (devLevelA + devLevelB + devLevelC) / 3.0
            };
        }

        public static bool IsTemperatureStable(double temperature)
        {
            return temperature >= CalibrationTargetTemp - CalibrationTolerance
                && temperature <= CalibrationTargetTemp + CalibrationTolerance;
        }

        private static double CalculateDeviation(double value, double average)
        {
            return 100 * Math.Abs(value - average) / average;
        }
    }
}
