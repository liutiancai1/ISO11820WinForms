using System;
using System.ComponentModel.DataAnnotations;
using ISO11820WinForms.Utilities;
using TestServer.Models;

namespace ISO11820WinForms.Models
{
    /// <summary>
    /// 试验信息数据传输对象
    /// 用于替代 ViewTestInfo 视图，直接从 testmaster 和 productmaster 表查询数据
    /// </summary>
    public class TestInfoDto
    {
        // 来自 testmaster 表的字段
        public string Productid { get; set; } = string.Empty;
        public string Testid { get; set; } = string.Empty;
        public double Ambtemp { get; set; }
        public double Ambhumi { get; set; }
        public DateTime Testdate { get; set; }
        public int Totaltesttime { get; set; }
        public string According { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Apparatusid { get; set; } = string.Empty;
        public string Apparatusname { get; set; } = string.Empty;
        public DateTime Apparatuschkdate { get; set; }
        public int Constpower { get; set; }
        public string Rptno { get; set; } = string.Empty;
        public double Preweight { get; set; }
        public double Postweight { get; set; }
        public double Lostweight { get; set; }
        public double LostweightPer { get; set; }
        public string Phenocode { get; set; } = string.Empty;
        public int Flametime { get; set; }
        public int Flameduration { get; set; }
        public double Maxtf1 { get; set; }
        public double Maxtf2 { get; set; }
        public double Maxts { get; set; }
        public double Maxtc { get; set; }
        public int Maxtf1Time { get; set; }
        public int Maxtf2Time { get; set; }
        public int MaxtsTime { get; set; }
        public int MaxtcTime { get; set; }
        public int Finaltf1Time { get; set; }
        public int Finaltf2Time { get; set; }
        public int FinaltsTime { get; set; }
        public int FinaltcTime { get; set; }
        public double Finaltf1 { get; set; }
        public double Finaltf2 { get; set; }
        public double Finalts { get; set; }
        public double Finaltc { get; set; }
        public double Deltatf1 { get; set; }
        public double Deltatf2 { get; set; }
        public double Deltatf { get; set; }
        public double Deltats { get; set; }
        public double Deltatc { get; set; }
        public string? Memo { get; set; }
        
        // 来自 productmaster 表的字段
        public string Productname { get; set; } = string.Empty;
        public string Specific { get; set; } = string.Empty;
        public double Diameter { get; set; }
        public double Height { get; set; }

        /// <summary>
        /// 从 Testmaster 实体创建 DTO
        /// </summary>
        public static TestInfoDto FromTestmaster(Testmaster test)
        {
            return new TestInfoDto
            {
                Productid = test.Productid,
                Testid = test.Testid,
                Ambtemp = test.Ambtemp,
                Ambhumi = test.Ambhumi,
                Testdate = test.Testdate,
                Totaltesttime = test.Totaltesttime,
                According = test.According,
                Operator = test.Operator,
                Apparatusid = test.Apparatusid,
                Apparatusname = test.Apparatusname,
                Apparatuschkdate = test.Apparatuschkdate,
                Constpower = test.Constpower,
                Rptno = test.Rptno,
                Preweight = test.Preweight,
                Postweight = test.Postweight,
                Lostweight = test.Lostweight,
                LostweightPer = test.LostweightPer,
                Phenocode = test.Phenocode,
                Flametime = test.Flametime,
                Flameduration = test.Flameduration,
                Maxtf1 = test.Maxtf1,
                Maxtf2 = test.Maxtf2,
                Maxts = test.Maxts,
                Maxtc = test.Maxtc,
                Maxtf1Time = test.Maxtf1Time,
                Maxtf2Time = test.Maxtf2Time,
                MaxtsTime = test.MaxtsTime,
                MaxtcTime = test.MaxtcTime,
                Finaltf1Time = test.Finaltf1Time,
                Finaltf2Time = test.Finaltf2Time,
                FinaltsTime = test.FinaltsTime,
                FinaltcTime = test.FinaltcTime,
                Finaltf1 = test.Finaltf1,
                Finaltf2 = test.Finaltf2,
                Finalts = test.Finalts,
                Finaltc = test.Finaltc,
                Deltatf1 = test.Deltatf1,
                Deltatf2 = test.Deltatf2,
                Deltatf = test.Deltatf,
                Deltats = test.Deltats,
                Deltatc = test.Deltatc,
                Memo = ReportPathMemoHelper.GetDisplayMemo(test.Memo),
                // 从关联的 Product 获取产品信息
                Productname = test.Product?.Productname ?? string.Empty,
                Specific = test.Product?.Specific ?? string.Empty,
                Diameter = test.Product?.Diameter ?? 0,
                Height = test.Product?.Height ?? 0
            };
        }
    }
}
