using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using CxPreflight.Core;

namespace CxPreflight.Addin
{
    /// <summary>
    /// Reads what the checks need from the model. Runs on Revit's API thread.
    /// P0: element count and levels with their Building Story flag.
    /// </summary>
    internal static class ModelReader
    {
        public static ModelSummary Read(Document doc)
        {
            var clock = Stopwatch.StartNew();

            int elements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .GetElementCount();

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Select(l => new LevelInfo(
                    l.Name,
                    UnitUtils.ConvertFromInternalUnits(l.Elevation, UnitTypeId.Millimeters),
                    l.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY)?.AsInteger() == 1))
                .ToList();

            clock.Stop();
            return new ModelSummary
            {
                DocumentTitle = doc.Title,
                ElementCount = elements,
                Levels = levels,
                CheckedAt = DateTime.Now,
                DurationMs = clock.ElapsedMilliseconds,
            };
        }
    }
}
