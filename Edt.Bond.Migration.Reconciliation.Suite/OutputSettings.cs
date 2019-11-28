using Edt.Bond.Migration.Reconciliation.Framework;
using NUnit.Framework;
using System.IO;

namespace Edt.Bond.Migration.Reconciliation.Suite
{
    [SetUpFixture]
    public class OutputSettings
    {
        [OneTimeSetUp]
        public void OutputAppConfig()
        {
            Directory.CreateDirectory(".\\logs");
            using (var sw = new StreamWriter(".\\logs\\settings.csv"))
            {
                sw.WriteLine("conv tool dir:"+ Settings.ConversionToolOutputDirectory);
                sw.WriteLine("edt case id:"+Settings.EdtCaseId);
                sw.WriteLine("edt cfs:"+Settings.EdtCfsDirectory);
                sw.WriteLine("dataset:"+Settings.EdtImporterDatasetName);
                sw.WriteLine("idx path:"+Settings.IdxFilePath);
                sw.WriteLine("sample size:"+Settings.IdxSampleSize);
                sw.WriteLine("mirco src dir:"+Settings.MicroFocusSourceDirectory);
                sw.WriteLine("standard map path:"+Settings.StandardMapPath);
            }
        }
    }
}
