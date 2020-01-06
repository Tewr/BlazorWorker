using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace BlazorWorker.Demo.Shared
{
    public class MathsService
    {
        public event EventHandler<int> Pi;

        private IEnumerable<int> AlternatingSequence()
        {
            var i = 1;
            yield return i;
            var flip = false;
            while (true) yield return ((flip = !flip) ? -1 : 1) * (i += 2);
        }

        public double EstimatePI(int sumLength)
        {
            var lastReport = 0;
            return (4 * AlternatingSequence().Take(sumLength)
                .Select((x, i) => {

                    // Keep reporting events down a bit, serialization is expensive!
                    var progressDelta = (Math.Abs(i - lastReport) / (double)sumLength) * 100;
                    if (progressDelta > 3 || i >= sumLength - 1)
                    {
                        lastReport = i;
                        Pi?.Invoke(this, i);
                    }
                    return x; })
                .Sum(x => 1.0 / x));
        }
    }
}
