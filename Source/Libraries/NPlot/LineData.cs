﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NPlot
{
    public class LineData
    {
        IList<double> m_x;
        IList<double> m_y;
        public double MaxValueX { get; private set; }
        public double MaxValueY { get; private set; }
        public double MinValueX { get; private set; }
        public double MinValueY { get; private set; }
        public int Count { get; private set; }

        public LineData(IList<double> x, IList<double> y)
        {
            m_x = x;
            m_y = y;
            Count = x.Count;
            if (x.Count>0)
            {
                MaxValueX = x.Max();
                MinValueX = x.Min();
            }
            if (y.Count > 0)
            {
                MaxValueY = y.Max();
                MinValueY = y.Min(); 
            }
        }

        public Axis GetX()
        {
            if (Count > 0)
                return new DateTimeAxis(MinValueX, MaxValueX);
            DateTime d = DateTime.Now.Date;
            return new DateTimeAxis(d, d.AddDays(1));
        }

        public Axis GetY()
        {
            if (Count > 0)
                return new LinearAxis(MinValueY, MaxValueY);
            return new LinearAxis(0.0, 1.0);
        }

        public PointD Get(int x)
        {
            return new PointD(m_x[x], m_y[x]);
        }

        /// <summary>
        /// Writes data out as text. 
        /// </summary>
        /// <param name="sb">StringBuilder to write to.</param>
        /// <param name="region">Only write out data in this region if onlyInRegion is true.</param>
        /// <param name="onlyInRegion">If true, only data in region is written, else all data is written.</param>
        public void WriteData(System.Text.StringBuilder sb, RectangleD region, bool onlyInRegion)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (!(onlyInRegion &&
                       (Get(i).X >= region.X && Get(i).X <= region.X + region.Width) &&
                       (Get(i).Y >= region.Y && Get(i).Y <= region.Y + region.Height)))
                    continue;

                sb.Append(Get(i).ToString());
                sb.Append("\r\n");
            }
        }

    }
}
