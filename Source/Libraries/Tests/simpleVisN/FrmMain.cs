﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NPlot;
using openHistorian;
using openHistorian.Server.Database;
using openHistorian.Server.Database.Archive;
using openVisN;
using PlotSurface2D = NPlot.Windows.PlotSurface2D;

namespace simpleVisN
{
    public partial class FrmMain : Form
    {
        IHistorianDatabase m_archiveFile;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {

        }

        private void btnConvertHistorianFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlgOpen = new OpenFileDialog())
            {
                dlgOpen.Filter = "openHistorian 1.0 file|*.d";
                if (dlgOpen.ShowDialog() == DialogResult.OK)
                {
                    using (SaveFileDialog dlgSave = new SaveFileDialog())
                    {
                        dlgSave.Filter = "openHistorian 2.0 file|*.d2";
                        if (dlgSave.ShowDialog() == DialogResult.OK)
                        {
                            openHistorian.Utility.ConvertArchiveFile.ConvertVersion1File(dlgOpen.FileName, dlgSave.FileName, CompressionMethod.TimeSeriesEncoded);
                            MessageBox.Show("Done!");
                        }
                    }

                }
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlgOpen = new OpenFileDialog())
            {
                dlgOpen.Filter = "openHistorian 2.0 file|*.d2";
                if (dlgOpen.ShowDialog() == DialogResult.OK)
                {
                    m_archiveFile = new ArchiveDatabaseEngine(null, dlgOpen.FileName);
                }
            }
            BuildListOfAllPoints();
        }

        void BuildListOfAllPoints()
        {
            HashSet<ulong> keys = new HashSet<ulong>();
            using (var reader = m_archiveFile.OpenDataReader())
            {
                var scanner = reader.Read(0, ulong.MaxValue);
                ulong key1, key2, value1, value2;
                while (scanner.Read(out key1, out key2, out value1, out value2))
                {
                    keys.Add(key2);
                }
            }
            var AllKeys = keys.ToList();
            AllKeys.Sort();

            chkAllPoints.Items.Clear();
            AllKeys.ForEach((x) => chkAllPoints.Items.Add(x));
        }

        private void btnPlot_Click(object sender, EventArgs e)
        {
            var keys = new List<ulong>(chkAllPoints.CheckedItems.OfType<ulong>());

            plot.Clear();
            
            plot.AddInteraction(new PlotSurface2D.Interactions.HorizontalDrag());
            plot.AddInteraction(new PlotSurface2D.Interactions.VerticalDrag());
            plot.AddInteraction(new PlotSurface2D.Interactions.AxisDrag(false));

            if (keys.Count == 0)
                return;

            QueryResults query = new QueryResults(m_archiveFile, 0, ulong.MaxValue, keys);

            foreach (var point in keys)
            {
                query.GetPointList(point);

                List<double> y = new List<double>();
                List<DateTime> x = new List<DateTime>();

                foreach (var value in query.GetPointList(point))
                {
                    x.Add(new DateTime((long)value.Key));
                    y.Add(BitMath.ConvertToSingle(value.Value));
                }
                //LinePlot lines = new LinePlot(y, x);

                //plot.Add(lines);
            }

            plot.Refresh();

        }
    }
}
