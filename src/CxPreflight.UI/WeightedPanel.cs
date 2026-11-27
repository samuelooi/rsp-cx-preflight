using System;
using System.Windows;
using System.Windows.Controls;

namespace CxPreflight.UI
{
    /// <summary>
    /// Lays children out left to right, each taking a share of the width
    /// proportional to its Weight, with a fixed gap. Used for the segmented
    /// readiness bar (10 / 10 / 50 / 20 / 10).
    /// </summary>
    public sealed class WeightedPanel : Panel
    {
        public static readonly DependencyProperty WeightProperty = DependencyProperty.RegisterAttached(
            "Weight", typeof(double), typeof(WeightedPanel),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public static double GetWeight(DependencyObject d) => (double)d.GetValue(WeightProperty);
        public static void SetWeight(DependencyObject d, double v) => d.SetValue(WeightProperty, v);

        public double Gap { get; set; } = 3;

        protected override Size MeasureOverride(Size available)
        {
            double h = 0;
            foreach (UIElement c in InternalChildren)
            {
                c.Measure(new Size(double.PositiveInfinity, available.Height));
                h = Math.Max(h, c.DesiredSize.Height);
            }
            return new Size(double.IsInfinity(available.Width) ? 0 : available.Width, h);
        }

        protected override Size ArrangeOverride(Size final)
        {
            int n = InternalChildren.Count;
            if (n == 0) return final;
            double total = 0;
            foreach (UIElement c in InternalChildren) total += Weight(c);
            double usable = Math.Max(0, final.Width - Gap * (n - 1));
            double x = 0;
            foreach (UIElement c in InternalChildren)
            {
                double w = total > 0 ? usable * Weight(c) / total : 0;
                c.Arrange(new Rect(x, 0, w, final.Height));
                x += w + Gap;
            }
            return final;
        }

        private static double Weight(UIElement c)
        {
            // Items in an ItemsControl are wrapped in a ContentPresenter; read the weight from its content.
            if (c is ContentPresenter cp && cp.Content is ISegment s) return s.Weight;
            return GetWeight(c);
        }
    }

    public interface ISegment
    {
        double Weight { get; }
    }
}
