using System;
using PartCover.Browser.Api;
using PartCover.Framework.Reports;
using PartCover.Framework.Walkers;
using System.IO;
using PartCover.Browser.Stuff;

namespace PartCover.Browser.Features
{
    public class CoverageReportService
        : IFeature
        , ICoverageReportService
    {
        public event EventHandler<EventArgs> ReportClosing;
        public event EventHandler<EventArgs> ReportOpened;

        string reportFileName;
        CoverageReportWrapper reportWrapper;
        public ICoverageReport Report
        {
            get { return reportWrapper; }
        }

        public string ReportFileName
        {
            get { return reportFileName; }
        }

        public void LoadFromFile(string fileName)
        {
            CoverageReport report = new CoverageReport();
            ReportReader reportReader = new ReportReader();
            using (StreamReader reader = new StreamReader(fileName))
            {
                reportReader.ReadReport(report, reader);
            }
            setReport(report);
            reportFileName = fileName;
        }

        private void setReport(CoverageReport report)
        {
            if (Report != null && ReportClosing != null)
                ReportClosing(this, EventArgs.Empty);

            CoverageReportWrapper wrapper = new CoverageReportWrapper(report);
            wrapper.Build();

            reportFileName = null;
            reportWrapper = wrapper;
            if (Report != null && ReportOpened != null)
                ReportOpened(this, EventArgs.Empty);
        }

        public void SaveReport(string fileName)
        {
            DefaultReportWriter reportWriter = new DefaultReportWriter();
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                reportWriter.WriteReport(reportWrapper.Report, writer);
                reportFileName = fileName;
            }
        }

        public void Load(CoverageReport report)
        {
            setReport(report);
        }

        public void Attach(IServiceContainer container) { }

        public void Detach(IServiceContainer container) { }

        public void Build(IServiceContainer container) { }

        public void Destroy(IServiceContainer container) { }

    }
}
