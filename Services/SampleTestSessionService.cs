using ISO11820WinForms.Core;
using ISO11820WinForms.Models;

namespace ISO11820WinForms.Services
{
    public class SampleTestSessionService
    {
        private readonly TestMaster _master;

        public SampleTestSessionService(TestMaster master)
        {
            _master = master;
        }

        public async Task<CommandResult> StartHeatingAsync()
        {
            var result = await _master.StartHeatingAsync();
            return result == 0
                ? CommandResult.Ok("已开始加热")
                : CommandResult.Fail("开始加热失败");
        }

        public CommandResult StopHeating()
        {
            var result = _master.StopHeating();
            return result == 0
                ? CommandResult.Ok("已停止加热")
                : CommandResult.Fail("停止加热失败");
        }

        public CommandResult StartRecording()
        {
            if (!_master.HasActiveTest)
            {
                return CommandResult.Fail("请先新建本次试验");
            }

            return _master.StartRecording()
                ? CommandResult.Ok("已开始记录")
                : CommandResult.Fail("开始记录失败");
        }

        public CommandResult StopRecording()
        {
            return _master.StopRecording()
                ? CommandResult.Ok("已停止记录")
                : CommandResult.Fail("停止记录失败");
        }

        public async Task<CommandResult> SubmitPostTestAsync(string phenoCode, int flameTime, int flameDuration, double postWeight)
        {
            var activeTest = _master.GetActiveTestOrNull();
            if (activeTest == null)
            {
                return CommandResult.Fail("请先新建本次试验");
            }

            if (_master.Status != MasterStatus.Complete && activeTest.Totaltesttime <= 0)
            {
                return CommandResult.Fail("试验尚未完成");
            }

            _master.SetPostTestData(phenoCode, flameTime, flameDuration, postWeight);
            await _master.PostTestProcess();
            var reportResult = _master.LastPostTestReportResult;
            _master.ResetTestData();
            if (reportResult == null)
            {
                return CommandResult.Ok("试验数据已保存。");
            }

            if (reportResult.Success)
            {
                return CommandResult.Ok(
                    "试验数据已保存，Excel报告已生成。",
                    reportGenerated: true,
                    excelReportPath: reportResult.ExcelFilePath,
                    pdfReportPath: reportResult.PdfFilePath,
                    testPackagePath: reportResult.TestPackagePath);
            }

            return CommandResult.Ok($"试验数据已保存，但Excel报告生成失败：{reportResult.ErrorMessage}");
        }
    }
}
